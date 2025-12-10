namespace PhotoOrganizer;

public class PhotoOrganizerProcessor
{
    private readonly string _sourcePath;
    private readonly string _destPath;
    private readonly HashSet<string> _processedHashes = new();
    private readonly GeocodingService _geocodingService;
    private readonly Dictionary<string, int> _folderCounters = new();
    private readonly object _hashLock = new();
    private readonly object _counterLock = new();
    private readonly SemaphoreSlim _semaphore;
    private int _processed;
    private int _duplicates;
    private int _errors;

    public PhotoOrganizerProcessor(string sourcePath, string destPath)
    {
        _sourcePath = sourcePath;
        _destPath = destPath;
        _geocodingService = new GeocodingService();
        var maxDegreeOfParallelism = Environment.ProcessorCount;
        _semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
    }

    public async Task ProcessPhotosAsync()
    {
        if (!System.IO.Directory.Exists(_sourcePath))
        {
            throw new DirectoryNotFoundException($"Source folder not found: {_sourcePath}");
        }

        System.IO.Directory.CreateDirectory(_destPath);

        var files = System.IO.Directory.GetFiles(_sourcePath, "*.*", SearchOption.AllDirectories)
            .Where(MetadataReader.IsSupportedImageFile)
            .ToList();

        Console.WriteLine($"Found {files.Count} images to process.");
        Console.WriteLine($"Using {Environment.ProcessorCount} threads for parallel processing.");
        Console.WriteLine();

        _processed = 0;
        _duplicates = 0;
        _errors = 0;

        var tasks = files.Select(async file =>
        {
            await _semaphore.WaitAsync();
            try
            {
                await ProcessFileAsync(file, files.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("???????????????????????????????????????");
        Console.WriteLine("PROCESSING SUMMARY");
        Console.WriteLine("???????????????????????????????????????");
        Console.WriteLine($"Total files: {files.Count}");
        Console.WriteLine($"Successfully processed: {_processed - _duplicates - _errors}");
        Console.WriteLine($"Duplicates ignored: {_duplicates}");
        Console.WriteLine($"Errors: {_errors}");
        Console.WriteLine("???????????????????????????????????????");
    }

    private async Task ProcessFileAsync(string file, int totalFiles)
    {
        int currentProcessed;
        
        lock (_counterLock)
        {
            _processed++;
            currentProcessed = _processed;
        }

        Console.Write($"\rProcessing {currentProcessed}/{totalFiles}... ");

        try
        {
            var metadata = MetadataReader.ExtractMetadata(file);
            
            if (metadata == null)
            {
                lock (_counterLock)
                {
                    _errors++;
                }
                return;
            }

            bool isDuplicate;
            lock (_hashLock)
            {
                isDuplicate = _processedHashes.Contains(metadata.FileHash);
                if (!isDuplicate)
                {
                    _processedHashes.Add(metadata.FileHash);
                }
            }

            if (isDuplicate)
            {
                lock (_counterLock)
                {
                    _duplicates++;
                }
                Console.WriteLine();
                Console.WriteLine($"  ? Duplicate found: {Path.GetFileName(file)}");
                return;
            }

            await ProcessPhotoAsync(file, metadata);
        }
        catch (Exception ex)
        {
            lock (_counterLock)
            {
                _errors++;
            }
            Console.WriteLine();
            Console.WriteLine($"  ? Error processing {Path.GetFileName(file)}: {ex.Message}");
        }
    }

    private async Task ProcessPhotoAsync(string sourceFile, PhotoMetadata metadata)
    {
        var year = metadata.DateTaken.Year.ToString();
        string folderName;

        if (metadata.Latitude.HasValue && metadata.Longitude.HasValue)
        {
            var cityName = await _geocodingService.GetCityNameAsync(
                metadata.Latitude.Value, 
                metadata.Longitude.Value);
            
            if (cityName == "Unknown Location")
            {
                folderName = year;
            }
            else
            {
                folderName = $"{year} - {cityName}";
            }
        }
        else
        {
            folderName = year;
        }

        var destinationFolder = Path.Combine(_destPath, folderName);
        
        lock (_counterLock)
        {
            System.IO.Directory.CreateDirectory(destinationFolder);
        }

        int counter;
        lock (_counterLock)
        {
            if (!_folderCounters.ContainsKey(destinationFolder))
            {
                _folderCounters[destinationFolder] = 0;
            }

            _folderCounters[destinationFolder]++;
            counter = _folderCounters[destinationFolder];
        }

        var datePrefix = metadata.DateTaken.ToString("yyyy-MM-dd");
        var newFileName = $"{datePrefix}_IMG{counter:D4}{metadata.FileExtension}";
        var destinationPath = Path.Combine(destinationFolder, newFileName);

        lock (_counterLock)
        {
            while (File.Exists(destinationPath))
            {
                _folderCounters[destinationFolder]++;
                counter = _folderCounters[destinationFolder];
                newFileName = $"{datePrefix}_IMG{counter:D4}{metadata.FileExtension}";
                destinationPath = Path.Combine(destinationFolder, newFileName);
            }
        }

        File.Copy(sourceFile, destinationPath, false);
    }
}

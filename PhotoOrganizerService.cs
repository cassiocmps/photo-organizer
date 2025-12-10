namespace PhotoOrganizer;

public class PhotoOrganizerProcessor
{
    private readonly string _sourcePath;
    private readonly string _destPath;
    private readonly HashSet<string> _processedHashes = new();
    private readonly GeocodingService _geocodingService;
    private readonly Dictionary<string, int> _folderCounters = new();

    public PhotoOrganizerProcessor(string sourcePath, string destPath)
    {
        _sourcePath = sourcePath;
        _destPath = destPath;
        _geocodingService = new GeocodingService();
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
        Console.WriteLine();

        int processed = 0;
        int duplicates = 0;
        int errors = 0;

        foreach (var file in files)
        {
            processed++;
            Console.Write($"\rProcessing {processed}/{files.Count}... ");

            try
            {
                var metadata = MetadataReader.ExtractMetadata(file);
                
                if (metadata == null)
                {
                    errors++;
                    continue;
                }

                if (_processedHashes.Contains(metadata.FileHash))
                {
                    duplicates++;
                    Console.WriteLine();
                    Console.WriteLine($"  ? Duplicate found: {Path.GetFileName(file)}");
                    continue;
                }

                _processedHashes.Add(metadata.FileHash);

                await ProcessPhotoAsync(file, metadata);
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine();
                Console.WriteLine($"  ? Error processing {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("???????????????????????????????????????");
        Console.WriteLine("PROCESSING SUMMARY");
        Console.WriteLine("???????????????????????????????????????");
        Console.WriteLine($"Total files: {files.Count}");
        Console.WriteLine($"Successfully processed: {processed - duplicates - errors}");
        Console.WriteLine($"Duplicates ignored: {duplicates}");
        Console.WriteLine($"Errors: {errors}");
        Console.WriteLine("???????????????????????????????????????");
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
        System.IO.Directory.CreateDirectory(destinationFolder);

        if (!_folderCounters.ContainsKey(destinationFolder))
        {
            _folderCounters[destinationFolder] = 0;
        }

        _folderCounters[destinationFolder]++;
        var counter = _folderCounters[destinationFolder];

        var datePrefix = metadata.DateTaken.ToString("yyyy-MM-dd");
        var newFileName = $"{datePrefix}_IMG{counter:D4}{metadata.FileExtension}";
        var destinationPath = Path.Combine(destinationFolder, newFileName);

        while (File.Exists(destinationPath))
        {
            _folderCounters[destinationFolder]++;
            counter = _folderCounters[destinationFolder];
            newFileName = $"{datePrefix}_IMG{counter:D4}{metadata.FileExtension}";
            destinationPath = Path.Combine(destinationFolder, newFileName);
        }

        File.Copy(sourceFile, destinationPath, false);
    }
}

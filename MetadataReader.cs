using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoOrganizer;

public static class MetadataReader
{
    private static readonly string[] SupportedExtensions = 
        [".jpg", ".jpeg", ".png", ".heic", ".heif", ".tiff", ".tif", ".bmp", ".gif"];

    public static bool IsSupportedImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public static PhotoMetadata? ExtractMetadata(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var hash = FileHasher.CalculateSha256(filePath);
            
            DateTime dateTaken = fileInfo.CreationTime;
            double? latitude = null;
            double? longitude = null;

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);

                var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifSubIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var exifDate) == true)
                {
                    dateTaken = exifDate;
                }

                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                if (gpsDirectory != null)
                {
                    var location = gpsDirectory.GetGeoLocation();
                    if (location != null && !location.IsZero)
                    {
                        latitude = location.Latitude;
                        longitude = location.Longitude;
                    }
                }
            }
            catch
            {
                // If EXIF read fails, use file creation date
            }

            return new PhotoMetadata(
                dateTaken,
                latitude,
                longitude,
                Path.GetFileNameWithoutExtension(filePath),
                Path.GetExtension(filePath),
                hash
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting metadata from {filePath}: {ex.Message}");
            return null;
        }
    }
}

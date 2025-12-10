namespace PhotoOrganizer;

public record PhotoMetadata(
    DateTime DateTaken,
    double? Latitude,
    double? Longitude,
    string OriginalFileName,
    string FileExtension,
    string FileHash
);

# Photo Organizer - Smart Photo Backup Organizer

## Description

Console application in C# .NET 10 for intelligently organizing photo backups, with hash-based deduplication, EXIF metadata extraction, economical geocoding, and automatic organization by date and location.

## Features

### Smart Deduplication (SHA-256)
- Calculates SHA-256 hash for each file
- Automatically ignores duplicates (even with different names)
- Logs found duplicates
- Thread-safe hash tracking

### Metadata Extraction
- Reads photo date via EXIF (`DateTimeOriginal`)
- Fallback to file creation date if EXIF not available
- Extracts GPS coordinates (Latitude/Longitude)
- Supports multiple formats: JPG, JPEG, PNG, HEIC, HEIF, TIFF, BMP, GIF

### Economical Geocoding (Spatial Caching)
- **Smart Cache**: Before calling API, checks in-memory cache
- **Haversine Formula**: Calculates distance between coordinates
- **Reuse**: If distance < 10km, reuses city from cache
- **OpenStreetMap API (Nominatim)**: Free and respectful geocoding
- **Exponential Backoff**: Automatic retry with 2s, 4s, 8s delays
- **Rate Limit Handling**: Detects HTTP 429 errors and retries up to 3 times
- **Custom User-Agent**: Required by Nominatim
- **Thread-safe caching**: Safe for concurrent access

### Multithreaded Processing
- **Parallel Processing**: Uses all available CPU cores
- **Thread-Safe**: All shared resources protected with locks
- **SemaphoreSlim**: Controls maximum concurrent operations
- **Performance**: Significantly faster for large photo collections
- **Auto-scaling**: Adapts to available processor count

### Automatic Organization

**Folder Structure:**
```
{destPath}/
  2023/
    2023-03-15_IMG0001.jpg
    2023-08-22_IMG0002.jpg
  2023 - Los Angeles/
    2023-08-10_IMG0001.jpg
    2023-08-11_IMG0002.jpg
  2024/
    2024-12-01_IMG0001.jpg
    2024-12-25_IMG0002.jpg
  2024 - New York/
    2024-07-10_IMG0001.jpg
    2024-07-11_IMG0002.jpg
  2024 - San Francisco/
    2024-05-20_IMG0001.jpg
    2024-05-20_IMG0002.jpg
    2024-06-15_IMG0003.jpg
```

**File Naming:**
- Format: `yyyy-MM-dd_IMG####.ext`
- Example: `2024-05-20_IMG0001.jpg`
- Sequential counter per folder
- Ensures chronological order
- Photos without GPS are saved in year-only folders (e.g., `2024/`)
- Photos with GPS are saved in year-city folders (e.g., `2024 - New York/`)
- All folders are at the same level (no nesting)

### Safety
- Uses `File.Copy` (not Move) to preserve originals
- Try/Catch for corrupted files
- Robust error handling
- Detailed final report

## Installation

### Prerequisites
- .NET 10 SDK

### NuGet Packages
```xml
<PackageReference Include="MetadataExtractor" Version="2.8.1" />
```

### Build
```bash
dotnet build
```

## Usage

### Syntax
```bash
PhotoOrganizer <sourcePath> [destPath]
```

### Parameters
- **sourcePath**: Path to folder with mixed photos (recursive search) - **Required**
- **destPath**: Root path where organized structure will be created - **Optional**
  - If not provided, creates a folder named `{SourceFolderName}_Organized` next to the source folder

### Examples

**Windows (with destination):**
```bash
PhotoOrganizer.exe "C:\Users\User\Pictures\Backup" "D:\Photos\Organized"
```

**Windows (auto destination):**
```bash
PhotoOrganizer.exe "C:\Users\User\Pictures\Backup"
# Creates: C:\Users\User\Pictures\Backup_Organized
```

**Linux/macOS (with destination):**
```bash
dotnet PhotoOrganizer.dll "/home/user/photos/backup" "/media/external/organized"
```

**Linux/macOS (auto destination):**
```bash
dotnet PhotoOrganizer.dll "/home/user/photos/backup"
# Creates: /home/user/photos/backup_Organized
```

### Run
```bash
# With custom destination
dotnet run -- "C:\Photos\Backup" "C:\Photos\Organized"

# Auto destination (creates Backup_Organized folder)
dotnet run -- "C:\Photos\Backup"
```

**Resulting Structure:**
- Photos with GPS: `{Year} - {City}/`
- Photos without GPS: `{Year}/`
- All folders at the same level (no nesting)
- Processed in parallel using multiple threads
- Automatic retry on rate limit errors

### Modern C# Features
- Records (`PhotoMetadata`, `LocationCacheItem`)
- Async/Await for API calls
- Pattern Matching
- Nullable Reference Types
- Collection Expressions `[...]`
- String Interpolation
- LINQ

## Technical Details

### Multithreading Architecture
- **SemaphoreSlim**: Limits concurrent operations to processor count
- **Lock Objects**: Three separate locks for different resources:
  - `_hashLock`: Protects duplicate detection hash set
  - `_counterLock`: Protects folder counters and file system operations
  - Thread-safe increment of statistics counters
- **Task Parallel Library**: Uses `Task.WhenAll` for efficient async processing
- **Performance**: Can process hundreds of photos per minute on modern hardware

### Haversine Formula
Calculates geodesic distance between two points on Earth's surface:
```
d = 2R * arcsin(sqrt(sin²(delta_phi/2) + cos(phi1) * cos(phi2) * sin²(delta_lambda/2)))
```
- R = 6371 km (Earth's radius)
- phi = latitude in radians
- lambda = longitude in radians

### Geocoding Cache
1. For each photo with GPS, checks existing cache
2. Calculates Haversine distance to each cache item
3. If distance < 10km: reuses city
4. If not: calls Nominatim API and adds to cache
5. **Result**: 95%+ reduction in API calls for grouped photos

### API Rate Limit Protection
- **Retry Logic**: Automatically retries failed geocoding requests up to 3 times
- **Exponential Backoff**: Progressive delays between retries (2s -> 4s -> 8s)
- **HTTP 429 Detection**: Specifically handles rate limit responses
- **Graceful Degradation**: Returns "Unknown Location" after all retries exhausted
- **Console Logging**: Informs user when retries are occurring
- **Respectful API Usage**: Complies with Nominatim usage policies

### Error Handling
- Files without EXIF: uses creation date
- Corrupted files: log and continue
- Geocoding failure: "Unknown Location"
- Rate limit (429): automatic retry with exponential backoff
- Source folder not found: fatal exception
- Thread-safe error counting and logging

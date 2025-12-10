namespace PhotoOrganizer;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("   PHOTO ORGANIZER - Smart Backup");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();

        if (args.Length < 1)
        {
            Console.WriteLine("USAGE: PhotoOrganizer <sourcePath> [destPath]");
            Console.WriteLine();
            Console.WriteLine("  sourcePath: Path to folder with mixed photos");
            Console.WriteLine("  destPath:   (Optional) Root path for organized structure");
            Console.WriteLine("              If not provided, creates 'Organized' folder next to source");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  PhotoOrganizer C:\\Photos\\Backup");
            Console.WriteLine("  PhotoOrganizer C:\\Photos\\Backup C:\\Photos\\Organized");
            return;
        }

        var sourcePath = args[0];
        string destPath;

        if (args.Length >= 2)
        {
            destPath = args[1];
        }
        else
        {
            var sourceDirectory = new DirectoryInfo(sourcePath);
            var parentDirectory = sourceDirectory.Parent?.FullName ?? Path.GetDirectoryName(sourcePath);
            var sourceFolderName = sourceDirectory.Name;
            destPath = Path.Combine(parentDirectory!, $"{sourceFolderName}_Organized");
        }

        Console.WriteLine($"📁 Source: {sourcePath}");
        Console.WriteLine($"📁 Destination: {destPath}");
        Console.WriteLine();

        try
        {
            var organizer = new PhotoOrganizerProcessor(sourcePath, destPath);
            await organizer.ProcessPhotosAsync();
            
            Console.WriteLine();
            Console.WriteLine("✓ Processing completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"✗ Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

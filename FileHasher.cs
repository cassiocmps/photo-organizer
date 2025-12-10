using System.Security.Cryptography;

namespace PhotoOrganizer;

public static class FileHasher
{
    public static string CalculateSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }
}

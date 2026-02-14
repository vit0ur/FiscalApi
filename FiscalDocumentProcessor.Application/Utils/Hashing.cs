using System.Security.Cryptography;

namespace FiscalDocumentProcessor.Application.Utils;

public static class Hashing
{
    public static string Sha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash);
    }
}

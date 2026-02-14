using System.IO.Compression;
using System.Security.Cryptography;
using FiscalDocumentProcessor.Application.Abstractions;

namespace FiscalDocumentProcessor.Infrastructure.Services;

public sealed class HashService : IHashService
{
    public string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}

public sealed class CompressionService : ICompressionService
{
    public byte[]? Compress(byte[]? data)
    {
        if (data is null || data.Length == 0) return null;
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    public byte[]? Decompress(byte[]? data)
    {
        if (data is null || data.Length == 0) return null;
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

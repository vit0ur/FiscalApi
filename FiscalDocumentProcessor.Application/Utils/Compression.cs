using System.IO.Compression;

namespace FiscalDocumentProcessor.Application.Utils;

public static class Compression
{
    public static byte[] Gzip(byte[] input)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(input, 0, input.Length);
        }
        return ms.ToArray();
    }
}

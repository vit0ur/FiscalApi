namespace FiscalDocumentProcessor.Application.Abstractions;

public interface ICompressionService
{
    byte[]? Compress(byte[]? data);
    byte[]? Decompress(byte[]? data);
}

namespace FiscalDocumentProcessor.Application.Abstractions;

public interface IHashService
{
    string ComputeSha256(byte[] data);
}

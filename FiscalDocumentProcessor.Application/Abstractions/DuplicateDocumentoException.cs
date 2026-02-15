namespace FiscalDocumentProcessor.Application.Abstractions;

public sealed class DuplicateDocumentoException : Exception
{
    public DuplicateDocumentoException(string message, Exception? inner = null) : base(message, inner) { }
}

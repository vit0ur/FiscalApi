using System.Threading;
namespace FiscalDocumentProcessor.Application.Abstractions;

public interface IXmlValidator
{
    /// <summary>
    /// Validates XML bytes against configured XSD(s). If XSDs are not configured, this is a no-op.
    /// Throws an exception when validation fails.
    /// </summary>
    Task ValidateAsync(byte[] xmlBytes, CancellationToken ct = default);
}

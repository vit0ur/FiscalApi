namespace FiscalDocumentProcessor.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishDocumentoProcessadoAsync(Guid documentoId, string tipoDocumento, string chaveAcesso, DateTime dataProcessamento, CancellationToken ct);
}

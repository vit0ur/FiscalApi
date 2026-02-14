using FiscalDocumentProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FiscalDocumentProcessor.Infrastructure.Messaging;

public sealed class NullEventPublisher : IEventPublisher
{
    private readonly ILogger<NullEventPublisher> _logger;
    public NullEventPublisher(ILogger<NullEventPublisher> logger) => _logger = logger;

    public Task PublishDocumentoProcessadoAsync(Guid documentoId, string tipoDocumento, string chaveAcesso, DateTime dataProcessamento, CancellationToken ct)
    {
        _logger.LogWarning("NullEventPublisher: would publish Documento {DocumentoId} Tipo={Tipo} Chave={Chave}", documentoId, tipoDocumento, string.IsNullOrWhiteSpace(chaveAcesso) ? "(empty)" : "(masked)");
        return Task.CompletedTask;
    }
}

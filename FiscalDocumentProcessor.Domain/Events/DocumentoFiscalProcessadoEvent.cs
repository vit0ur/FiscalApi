using FiscalDocumentProcessor.Domain.Enums;

namespace FiscalDocumentProcessor.Domain.Events;

public record DocumentoFiscalProcessadoEvent(
    Guid DocumentoId,
    TipoDocumento TipoDocumento,
    string ChaveAcesso,
    DateTime DataProcessamento);

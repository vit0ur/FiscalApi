using FiscalDocumentProcessor.Domain.Entities;
using FiscalDocumentProcessor.Domain.Enums;

namespace FiscalDocumentProcessor.Application.Parsing;

public interface IXmlFiscalParser
{
    bool CanParse(ReadOnlySpan<char> xmlSnippet);
    (TipoDocumento tipo, DocumentoFiscal dados, string? chaveAcesso) Parse(Stream xmlStream);
}

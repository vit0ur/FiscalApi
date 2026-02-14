using FiscalDocumentProcessor.Domain.Enums;
using TipoDocumento = FiscalDocumentProcessor.Domain.Enums.TipoDocumento;
namespace FiscalDocumentProcessor.Domain.Entities;

public class DocumentoFiscal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TipoDocumento TipoDocumento { get; set; }
    public string ChaveAcesso { get; set; } = string.Empty; // unique
    public string? CNPJEmitente { get; set; }
    public string? CNPJDestinatario { get; set; }
    public string? UF { get; set; }
    public DateTime? DataEmissao { get; set; }
    public decimal? ValorTotal { get; set; }
    public byte[]? XmlOriginalGzip { get; set; }
    public string HashXml { get; set; } = string.Empty; // SHA256 unique
    public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;
    public StatusDocumento Status { get; set; } = StatusDocumento.Recebido;
}

public enum StatusDocumento
{
    Recebido = 0,
    Processado = 1,
    Erro = 2
}

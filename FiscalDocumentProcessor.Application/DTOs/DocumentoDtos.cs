using System.ComponentModel.DataAnnotations;
using FiscalDocumentProcessor.Domain.Enums;

namespace FiscalDocumentProcessor.Application.DTOs;

public record DocumentoFiscalResponse(
    Guid Id,
    TipoDocumento TipoDocumento,
    string ChaveAcesso,
    string CNPJEmitente,
    string CNPJDestinatario,
    string UF,
    DateTime DataEmissao,
    decimal ValorTotal,
    DateTime DataProcessamento,
    string Status);

public class DocumentoFiscalUpdateRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class DocumentoFiscalListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? CNPJ { get; set; }
    public string? UF { get; set; }
    public TipoDocumento? TipoDocumento { get; set; }
}

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}

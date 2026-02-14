using FiscalDocumentProcessor.Domain.Entities;

namespace FiscalDocumentProcessor.Application.Abstractions;

public interface IDocumentRepository
{
    Task<DocumentoFiscal?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<DocumentoFiscal?> GetByHashAsync(string hash, CancellationToken ct);
    Task<DocumentoFiscal> AddAsync(DocumentoFiscal documento, CancellationToken ct);
    Task UpdateAsync(DocumentoFiscal documento, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<DocumentoFiscal> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        DateTime? dataInicio,
        DateTime? dataFim,
        string? cnpj,
        string? uf,
        string? tipoDocumento,
        CancellationToken ct);
}

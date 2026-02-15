using System.Linq.Expressions;
using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Domain.Entities;
using FiscalDocumentProcessor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FiscalDocumentProcessor.Infrastructure.Persistence;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;
    public DocumentRepository(AppDbContext db) => _db = db;

    public async Task<DocumentoFiscal?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Documentos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<DocumentoFiscal?> GetByHashAsync(string hash, CancellationToken ct)
        => await _db.Documentos.FirstOrDefaultAsync(x => x.HashXml == hash, ct);

    public async Task<DocumentoFiscal?> GetByChaveAsync(string chave, CancellationToken ct)
        => await _db.Documentos.FirstOrDefaultAsync(x => x.ChaveAcesso == chave, ct);

    public async Task<DocumentoFiscal> AddAsync(DocumentoFiscal documento, CancellationToken ct)
    {
        _db.Documentos.Add(documento);
        await _db.SaveChangesAsync(ct);
        return documento;
    }

    public async Task UpdateAsync(DocumentoFiscal documento, CancellationToken ct)
    {
        _db.Documentos.Update(documento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var d = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (d != null)
        {
            _db.Documentos.Remove(d);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<(IReadOnlyList<DocumentoFiscal> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, DateTime? dataInicio, DateTime? dataFim, string? cnpj, string? uf, string? tipoDocumento, CancellationToken ct)
    {
        var q = _db.Documentos.AsNoTracking().AsQueryable();
        if (dataInicio.HasValue) q = q.Where(x => x.DataEmissao >= dataInicio);
        if (dataFim.HasValue) q = q.Where(x => x.DataEmissao <= dataFim);
        if (!string.IsNullOrWhiteSpace(cnpj)) q = q.Where(x => (x.CNPJEmitente == cnpj) || (x.CNPJDestinatario == cnpj));
        if (!string.IsNullOrWhiteSpace(uf)) q = q.Where(x => x.UF == uf);
        if (!string.IsNullOrWhiteSpace(tipoDocumento) && Enum.TryParse<TipoDocumento>(tipoDocumento, true, out var td)) q = q.Where(x => x.TipoDocumento == td);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.DataProcessamento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}

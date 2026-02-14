using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FiscalDocumentProcessor.Infrastructure.Persistence;

namespace FiscalDocumentProcessor.Infrastructure.Repositories;

public class DocumentoFiscalRepository : IDocumentoFiscalRepository
{
    private readonly AppDbContext _ctx;
    public DocumentoFiscalRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(DocumentoFiscal doc, CancellationToken ct = default) => await _ctx.Documentos.AddAsync(doc, ct);

    public async Task<DocumentoFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _ctx.Documentos.FindAsync(new object[] { id }, ct);

    public async Task<DocumentoFiscal?> GetByHashAsync(string hash, CancellationToken ct = default) => await _ctx.Documentos.FirstOrDefaultAsync(d => d.HashXml == hash, ct);

    public IQueryable<DocumentoFiscal> Query() => _ctx.Documentos.AsNoTracking();

    public void Update(DocumentoFiscal doc) => _ctx.Documentos.Update(doc);

    public void Remove(DocumentoFiscal doc) => _ctx.Documentos.Remove(doc);
}

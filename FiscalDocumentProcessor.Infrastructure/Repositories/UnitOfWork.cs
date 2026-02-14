using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Infrastructure.Persistence;

namespace FiscalDocumentProcessor.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;
    public IDocumentoFiscalRepository Documentos { get; }

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx = ctx;
        Documentos = new DocumentoFiscalRepository(ctx);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);
}

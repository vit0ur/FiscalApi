using FiscalDocumentProcessor.Domain.Entities;

namespace FiscalDocumentProcessor.Application.Abstractions;

public interface IUnitOfWork
{
    IDocumentoFiscalRepository Documentos { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IDocumentoFiscalRepository
{
    Task AddAsync(DocumentoFiscal doc, CancellationToken ct = default);
    Task<DocumentoFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DocumentoFiscal?> GetByHashAsync(string hash, CancellationToken ct = default);
    IQueryable<DocumentoFiscal> Query();
    void Update(DocumentoFiscal doc);
    void Remove(DocumentoFiscal doc);
}

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default);
}

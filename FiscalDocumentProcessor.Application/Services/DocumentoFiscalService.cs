using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.DTOs;
using FiscalDocumentProcessor.Application.Parsing;
using FiscalDocumentProcessor.Application.Utils;
using FiscalDocumentProcessor.Domain.Events;
using Microsoft.Extensions.Logging;

namespace FiscalDocumentProcessor.Application.Services;

public class DocumentoFiscalService
{
    private readonly IUnitOfWork _uow;
    private readonly IXmlFiscalParser _parser;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<DocumentoFiscalService> _logger;

    public DocumentoFiscalService(IUnitOfWork uow, IXmlFiscalParser parser, IRabbitMqPublisher publisher, ILogger<DocumentoFiscalService>? logger = null)
    {
        _uow = uow;
        _parser = parser;
        _publisher = publisher;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentoFiscalService>.Instance;
    }

    public async Task<DocumentoFiscalResponse> UploadAsync(Stream xmlStream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await xmlStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var hash = Hashing.Sha256(bytes);

        var existing = await _uow.Documentos.GetByHashAsync(hash, ct);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        ms.Position = 0;
        var (tipo, docParsed, chave) = _parser.Parse(ms);

        docParsed.HashXml = hash;
        docParsed.XmlOriginalGzip = Compression.Gzip(bytes);
        docParsed.DataProcessamento = DateTime.UtcNow;

        await _uow.Documentos.AddAsync(docParsed, ct);
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            var dup = await _uow.Documentos.GetByHashAsync(hash, ct);
            if (dup is not null) return ToResponse(dup);
            throw;
        }

        try
        {
            await _publisher.PublishAsync(
                "fiscal.documents.exchange",
                "fiscal.document.processed",
                new DocumentoFiscalProcessadoEvent(
                    docParsed.Id, docParsed.TipoDocumento, docParsed.ChaveAcesso ?? string.Empty, docParsed.DataProcessamento),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao publicar no RabbitMQ. Documento {DocumentoId} persistido mesmo assim.",
                docParsed.Id);
        }

        return ToResponse(docParsed);
    }

    public PagedResult<DocumentoFiscalResponse> List(DocumentoFiscalListRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page = Math.Max(1, request.Page);

        var query = _uow.Documentos.Query();

        if (request.TipoDocumento.HasValue)
            query = query.Where(d => d.TipoDocumento == request.TipoDocumento.Value);
        if (!string.IsNullOrWhiteSpace(request.UF))
            query = query.Where(d => d.UF == request.UF);
        if (!string.IsNullOrWhiteSpace(request.CNPJ))
            query = query.Where(d => d.CNPJEmitente == request.CNPJ || d.CNPJDestinatario == request.CNPJ);
        if (request.DataInicio.HasValue)
            query = query.Where(d => d.DataEmissao >= request.DataInicio.Value);
        if (request.DataFim.HasValue)
            query = query.Where(d => d.DataEmissao <= request.DataFim.Value);

        var total = query.Count();
        var items = query.OrderByDescending(d => d.DataEmissao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();

        return new PagedResult<DocumentoFiscalResponse> { Page = page, PageSize = pageSize, Total = total, Items = items };
    }

    public async Task<DocumentoFiscalResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var doc = await _uow.Documentos.GetByIdAsync(id, ct);
        return doc is null ? null : ToResponse(doc);
    }

    public async Task<bool> UpdateAsync(Guid id, DocumentoFiscalUpdateRequest request, CancellationToken ct)
    {
        var doc = await _uow.Documentos.GetByIdAsync(id, ct);
        if (doc is null) return false;
        if (!Enum.TryParse<FiscalDocumentProcessor.Domain.Entities.StatusDocumento>(request.Status, true, out var st)) return false;
        doc.Status = st;
        _uow.Documentos.Update(doc);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var doc = await _uow.Documentos.GetByIdAsync(id, ct);
        if (doc is null) return false;
        _uow.Documentos.Remove(doc);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    private static DocumentoFiscalResponse ToResponse(FiscalDocumentProcessor.Domain.Entities.DocumentoFiscal d) => new(
        d.Id,
        d.TipoDocumento,
        d.ChaveAcesso ?? string.Empty,
        CnpjMask.Mask(d.CNPJEmitente),
        CnpjMask.Mask(d.CNPJDestinatario),
        d.UF ?? string.Empty,
        d.DataEmissao ?? DateTime.MinValue,
        d.ValorTotal ?? 0m,
        d.DataProcessamento,
        d.Status.ToString());
}

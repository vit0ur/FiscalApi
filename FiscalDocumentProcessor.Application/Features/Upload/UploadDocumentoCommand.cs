using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.Parsing;
using FiscalDocumentProcessor.Domain.Entities;
using MediatR;

namespace FiscalDocumentProcessor.Application.Features.Upload;

public sealed record UploadDocumentoCommand(string FileName, byte[] XmlBytes) : IRequest<UploadDocumentoResult>;
public sealed record UploadDocumentoResult(Guid DocumentoId, bool JaExistia);

public sealed class UploadDocumentoCommandHandler : IRequestHandler<UploadDocumentoCommand, UploadDocumentoResult>
{
    private readonly IDocumentRepository _repo;
    private readonly IHashService _hash;
    private readonly ICompressionService _zip;
    private readonly IEventPublisher _publisher;
    private readonly IXmlValidator? _validator;

    public UploadDocumentoCommandHandler(IDocumentRepository repo, IHashService hash, ICompressionService zip, IEventPublisher publisher, IXmlValidator? validator = null)
    {
        _repo = repo; _hash = hash; _zip = zip; _publisher = publisher; _validator = validator;
    }

    public async Task<UploadDocumentoResult> Handle(UploadDocumentoCommand request, CancellationToken ct)
    {
        var hash = _hash.ComputeSha256(request.XmlBytes);
        var existing = await _repo.GetByHashAsync(hash, ct);
        if (existing is not null)
        {
            return new UploadDocumentoResult(existing.Id, true);
        }

        if (_validator is not null)
        {
            await _validator.ValidateAsync(request.XmlBytes, ct);
        }

        using var ms = new MemoryStream(request.XmlBytes);
        var parsed = await XmlDocumentParser.ParseAsync(ms, ct);

        var doc = new DocumentoFiscal
        {
            TipoDocumento = parsed.Tipo,
            ChaveAcesso = parsed.ChaveAcesso ?? string.Empty,
            CNPJEmitente = parsed.CNPJEmitente,
            CNPJDestinatario = parsed.CNPJDestinatario,
            UF = parsed.UF,
            DataEmissao = parsed.DataEmissao,
            ValorTotal = parsed.ValorTotal,
            XmlOriginalGzip = _zip.Compress(request.XmlBytes),
            HashXml = hash,
            DataProcessamento = DateTime.UtcNow,
            Status = StatusDocumento.Recebido
        };

        doc = await _repo.AddAsync(doc, ct);

        await _publisher.PublishDocumentoProcessadoAsync(doc.Id, doc.TipoDocumento.ToString(), doc.ChaveAcesso, doc.DataProcessamento, ct);

        return new UploadDocumentoResult(doc.Id, false);
    }
}

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

        var chaveAcesso = string.IsNullOrWhiteSpace(parsed.ChaveAcesso) ? null : parsed.ChaveAcesso;
        if (!string.IsNullOrWhiteSpace(chaveAcesso))
        {
            var existingByChave = await _repo.GetByChaveAsync(chaveAcesso, ct);
            if (existingByChave is not null)
            {
                return new UploadDocumentoResult(existingByChave.Id, true);
            }
        }

        var doc = new DocumentoFiscal
        {
            TipoDocumento = parsed.Tipo,
            ChaveAcesso = chaveAcesso,
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

        try
        {
            doc = await _repo.AddAsync(doc, ct);
        }
        catch
        {
            var dup = await _repo.GetByHashAsync(hash, ct) ??
                      (!string.IsNullOrWhiteSpace(chaveAcesso) ? await _repo.GetByChaveAsync(chaveAcesso, ct) : null);
            if (dup is not null)
            {
                return new UploadDocumentoResult(dup.Id, true);
            }
            throw;
        }

        await _publisher.PublishDocumentoProcessadoAsync(doc.Id, doc.TipoDocumento.ToString(), doc.ChaveAcesso ?? string.Empty, doc.DataProcessamento, ct);

        return new UploadDocumentoResult(doc.Id, false);
    }
}

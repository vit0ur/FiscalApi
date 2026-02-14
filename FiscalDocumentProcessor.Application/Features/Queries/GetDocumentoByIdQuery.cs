using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.DTOs;
using MediatR;

namespace FiscalDocumentProcessor.Application.Features.Queries;

public sealed record GetDocumentoByIdQuery(Guid Id) : IRequest<DocumentoFiscalDto?>;

public sealed class GetDocumentoByIdQueryHandler : IRequestHandler<GetDocumentoByIdQuery, DocumentoFiscalDto?>
{
    private readonly IDocumentRepository _repo;
    public GetDocumentoByIdQueryHandler(IDocumentRepository repo) => _repo = repo;

    public async Task<DocumentoFiscalDto?> Handle(GetDocumentoByIdQuery request, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(request.Id, ct);
        return d?.ToDto();
    }
}

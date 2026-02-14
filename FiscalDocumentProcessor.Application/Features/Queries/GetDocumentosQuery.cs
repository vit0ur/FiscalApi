using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.DTOs;
using MediatR;

namespace FiscalDocumentProcessor.Application.Features.Queries;

public sealed record GetDocumentosQuery(int Page, int PageSize, DateTime? DataInicio, DateTime? DataFim, string? Cnpj, string? Uf, string? TipoDocumento) : IRequest<GetDocumentosResult>;
public sealed record GetDocumentosResult(IReadOnlyList<DocumentoFiscalDto> Items, int TotalCount, int Page, int PageSize);

public sealed class GetDocumentosQueryHandler : IRequestHandler<GetDocumentosQuery, GetDocumentosResult>
{
    private readonly IDocumentRepository _repo;
    public GetDocumentosQueryHandler(IDocumentRepository repo) => _repo = repo;

    public async Task<GetDocumentosResult> Handle(GetDocumentosQuery request, CancellationToken ct)
    {
        var (items, total) = await _repo.GetPagedAsync(request.Page, request.PageSize, request.DataInicio, request.DataFim, request.Cnpj, request.Uf, request.TipoDocumento, ct);
        return new GetDocumentosResult(items.Select(i => i.ToDto()).ToList(), total, request.Page, request.PageSize);
    }
}

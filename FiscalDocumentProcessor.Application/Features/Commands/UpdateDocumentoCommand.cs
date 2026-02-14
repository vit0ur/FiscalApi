using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Domain.Entities;
using MediatR;

namespace FiscalDocumentProcessor.Application.Features.Commands;

public sealed record UpdateDocumentoCommand(Guid Id, string? UF, DateTime? DataEmissao, decimal? ValorTotal, StatusDocumento? Status) : IRequest<bool>;

public sealed class UpdateDocumentoCommandHandler : IRequestHandler<UpdateDocumentoCommand, bool>
{
    private readonly IDocumentRepository _repo;
    public UpdateDocumentoCommandHandler(IDocumentRepository repo) => _repo = repo;

    public async Task<bool> Handle(UpdateDocumentoCommand request, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(request.Id, ct);
        if (d is null) return false;
        if (request.UF is not null) d.UF = request.UF;
        if (request.DataEmissao is not null) d.DataEmissao = request.DataEmissao;
        if (request.ValorTotal is not null) d.ValorTotal = request.ValorTotal;
        if (request.Status is not null) d.Status = request.Status.Value;
        await _repo.UpdateAsync(d, ct);
        return true;
    }
}

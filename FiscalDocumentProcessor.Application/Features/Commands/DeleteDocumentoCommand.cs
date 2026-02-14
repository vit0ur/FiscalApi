using FiscalDocumentProcessor.Application.Abstractions;
using MediatR;

namespace FiscalDocumentProcessor.Application.Features.Commands;

public sealed record DeleteDocumentoCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteDocumentoCommandHandler : IRequestHandler<DeleteDocumentoCommand, bool>
{
    private readonly IDocumentRepository _repo;
    public DeleteDocumentoCommandHandler(IDocumentRepository repo) => _repo = repo;

    public async Task<bool> Handle(DeleteDocumentoCommand request, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(request.Id, ct);
        if (d is null) return false;
        await _repo.DeleteAsync(request.Id, ct);
        return true;
    }
}

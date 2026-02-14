using System.ComponentModel.DataAnnotations;
using FiscalDocumentProcessor.Application.DTOs;
using FiscalDocumentProcessor.Application.Features.Commands;
using FiscalDocumentProcessor.Application.Features.Upload;
using FiscalDocumentProcessor.Application.Features.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FiscalDocumentProcessor.Api.Controllers;

[ApiController]
[Route("documentos")]
public class DocumentosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentosController> _logger;
    public DocumentosController(IMediator mediator, ILogger<DocumentosController> logger)
    { _mediator = mediator; _logger = logger; }

    [HttpGet]
    public async Task<ActionResult<GetDocumentosResult>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? dataInicio = null, [FromQuery] DateTime? dataFim = null,
        [FromQuery] string? cnpj = null, [FromQuery] string? uf = null, [FromQuery] string? tipoDocumento = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 500);
        var result = await _mediator.Send(new GetDocumentosQuery(page, pageSize, dataInicio, dataFim, cnpj, uf, tipoDocumento), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentoFiscalDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDocumentoByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    public sealed record UpdateRequest(string? UF, DateTime? DataEmissao, decimal? ValorTotal, string? Status);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRequest body, CancellationToken ct)
    {
        Domain.Entities.StatusDocumento? status = null;
        if (!string.IsNullOrWhiteSpace(body.Status) && Enum.TryParse<Domain.Entities.StatusDocumento>(body.Status, true, out var st))
            status = st;
        var ok = await _mediator.Send(new UpdateDocumentoCommand(id, body.UF, body.DataEmissao, body.ValorTotal, status), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var ok = await _mediator.Send(new DeleteDocumentoCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult> Upload([Required] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("Arquivo vazio");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var result = await _mediator.Send(new UploadDocumentoCommand(file.FileName, bytes), ct);
        return result.JaExistia ? Ok(new { result.DocumentoId, message = "Documento já existente (idempotente)" }) : Accepted(new { result.DocumentoId });
    }
}

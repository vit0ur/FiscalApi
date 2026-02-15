using FiscalDocumentProcessor.Domain.Entities;

namespace FiscalDocumentProcessor.Application.DTOs;

public record DocumentoFiscalDto(
    Guid Id,
    string TipoDocumento,
    string ChaveAcesso,
    string? CNPJEmitente,
    string? CNPJDestinatario,
    string? UF,
    DateTime? DataEmissao,
    decimal? ValorTotal,
    DateTime DataProcessamento,
    string Status
);

public static class DocumentoFiscalMapper
{
    public static string MaskCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return cnpj ?? string.Empty;
            var digits = new string(cnpj.Where(char.IsDigit).ToArray()).PadLeft(14,'0');

            return digits.Substring(0,2) + "." + digits.Substring(2,3) + ".***." + "**" + "/" + digits.Substring(8,3) + "-" + digits.Substring(11,2);
    }

    public static DocumentoFiscalDto ToDto(this DocumentoFiscal d)
        => new(
            d.Id,
            d.TipoDocumento.ToString(),
            d.ChaveAcesso ?? string.Empty,
            MaskCnpj(d.CNPJEmitente),
            MaskCnpj(d.CNPJDestinatario),
            d.UF,
            d.DataEmissao,
            d.ValorTotal,
            d.DataProcessamento,
            d.Status.ToString());
}

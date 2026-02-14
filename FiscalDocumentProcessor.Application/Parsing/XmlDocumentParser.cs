using System.Xml;
using FiscalDocumentProcessor.Domain.Entities;
using FiscalDocumentProcessor.Domain.Enums;

namespace FiscalDocumentProcessor.Application.Parsing;

public static class XmlDocumentParser
{
    public sealed record Parsed(
        TipoDocumento Tipo,
        string? ChaveAcesso,
        string? CNPJEmitente,
        string? CNPJDestinatario,
        string? UF,
        DateTime? DataEmissao,
        decimal? ValorTotal);

    public static XmlReaderSettings SecureSettings => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        Async = true
    };

    public static async Task<Parsed> ParseAsync(Stream xmlStream, CancellationToken ct)
    {
        using var reader = XmlReader.Create(xmlStream, SecureSettings);

        var doc = new XmlDocument { XmlResolver = null };
        await Task.Run(() => doc.Load(reader), ct);

        var root = doc.DocumentElement?.Name?.ToLowerInvariant() ?? string.Empty;
        TipoDocumento tipo = root.Contains("cte") ? TipoDocumento.CTe
            : (root.Contains("nfse") || root.Contains("compnfse") || root.Contains("nfs-e")) ? TipoDocumento.NFSe
            : TipoDocumento.NFe;

        string? chave = TryFirst(doc, new[]{"//infNFe/@Id", "//protNFe/infProt/chNFe", "//infCte/@Id", "//ChaveNFe", "//Chave"});
        if (!string.IsNullOrEmpty(chave)) chave = new string(chave.Where(char.IsLetterOrDigit).ToArray());

        string? cnpjEmit = TryFirst(doc, new[]{"//emit/CNPJ", "//prestador/identificacaoprestador/Cnpj", "//PrestadorServico/IdentificacaoPrestador/Cnpj", "//emit/CPF"});
        string? cnpjDest = TryFirst(doc, new[]{"//dest/CNPJ", "//tomador/identificacaotomador/CpfCnpj/Cnpj", "//TomadorServico/IdentificacaoTomador/CpfCnpj/Cnpj", "//rem/CNPJ", "//ent/CNPJ", "//dest/CPF"});

        string? uf = TryFirst(doc, new[]{"//emit/enderEmit/UF", "//dest/enderDest/UF", "//ide/UF"});

        DateTime? emissao = TryParseDate(TryFirst(doc, new[]{"//ide/dhEmi", "//ide/dEmi", "//infNFe/ide/dhEmi"}));
        decimal? valorTotal = TryParseDecimal(TryFirst(doc, new[]{"//total/ICMSTot/vNF", "//ValorNfse", "//vNF"}));

        return new Parsed(tipo, chave, cnpjEmit, cnpjDest, uf, emissao, valorTotal);
    }

    private static string? TryFirst(XmlDocument doc, IEnumerable<string> xpaths)
    {
        foreach (var xp in xpaths)
        {
            var node = doc.SelectSingleNode(xp);
            if (node != null)
            {
                return node is XmlAttribute attr ? attr.Value : node.InnerText;
            }
        }
        return null;
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dto))
            return dto.UtcDateTime;

        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d))
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);

        return null;
    }

    private static decimal? TryParseDecimal(string? s)
        => decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}

using System.Xml;
using FiscalDocumentProcessor.Domain.Entities;
using FiscalDocumentProcessor.Domain.Enums;

namespace FiscalDocumentProcessor.Application.Parsing;

public class XmlFiscalParser : IXmlFiscalParser
{
    public bool CanParse(ReadOnlySpan<char> xmlSnippet)
    {
        var s = xmlSnippet.ToString();
        return s.Contains("<NFe") || s.Contains("<CTe") || s.Contains("<NFSe");
    }

    public (TipoDocumento tipo, DocumentoFiscal dados, string? chaveAcesso) Parse(Stream xmlStream)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        using var reader = XmlReader.Create(xmlStream, settings);

        string cnpjEmit = string.Empty;
        string cnpjDest = string.Empty;
        string uf = string.Empty;
        DateTime dataEmissao = DateTime.UtcNow;
        decimal valorTotal = 0m;
        string? chaveAcesso = null;
        TipoDocumento tipo = TipoDocumento.NFe;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var name = reader.Name;
                if (name.Contains("NFe", StringComparison.OrdinalIgnoreCase)) tipo = TipoDocumento.NFe;
                if (name.Contains("CTe", StringComparison.OrdinalIgnoreCase)) tipo = TipoDocumento.CTe;
                if (name.Contains("NFSe", StringComparison.OrdinalIgnoreCase)) tipo = TipoDocumento.NFSe;

                if (name.Equals("chNFe", StringComparison.OrdinalIgnoreCase) || name.Equals("chCTe", StringComparison.OrdinalIgnoreCase))
                    chaveAcesso = reader.ReadElementContentAsString();
                else if (name.EndsWith("CNPJ", StringComparison.OrdinalIgnoreCase))
                {
                    var value = reader.ReadElementContentAsString();
                    if (string.IsNullOrEmpty(cnpjEmit)) cnpjEmit = value; else if (string.IsNullOrEmpty(cnpjDest)) cnpjDest = value;
                }
                else if (name.EndsWith("UF", StringComparison.OrdinalIgnoreCase))
                    uf = reader.ReadElementContentAsString();
                else if (name.Equals("dhEmi", StringComparison.OrdinalIgnoreCase) || name.Equals("dEmi", StringComparison.OrdinalIgnoreCase))
                {
                    var s = reader.ReadElementContentAsString();
                    if (DateTime.TryParse(s, out var d)) dataEmissao = DateTime.SpecifyKind(d, DateTimeKind.Utc);
                }
                else if (name.Equals("vNF", StringComparison.OrdinalIgnoreCase) || name.Equals("vTotTrib", StringComparison.OrdinalIgnoreCase) || name.Equals("vPrest", StringComparison.OrdinalIgnoreCase))
                {
                    var s = reader.ReadElementContentAsString();
                    if (decimal.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                        valorTotal = v;
                }
            }
        }

        var doc = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            TipoDocumento = tipo,
            ChaveAcesso = chaveAcesso ?? string.Empty,
            CNPJEmitente = cnpjEmit,
            CNPJDestinatario = cnpjDest,
            UF = uf,
            DataEmissao = dataEmissao,
            ValorTotal = valorTotal,
            DataProcessamento = DateTime.UtcNow,
            Status = FiscalDocumentProcessor.Domain.Entities.StatusDocumento.Processado
        };

        return (tipo, doc, chaveAcesso);
    }
}

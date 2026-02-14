using System.Text;
using FiscalDocumentProcessor.Application.Parsing;
using FluentAssertions;
using NUnit.Framework;

namespace FiscalDocumentProcessor.Tests;

public class XmlParserTests
{
    [Test]
    public async Task Parse_NFe_Should_Extract_Basics()
    {
        var xml = """
        <NFe><infNFe Id="NFe35191030290856000128550010001084231001084230"><ide><dEmi>2019-10-10</dEmi></ide><emit><CNPJ>30290856000128</CNPJ><enderEmit><UF>SP</UF></enderEmit></emit><dest><CNPJ>11222333000144</CNPJ></dest><total><ICMSTot><vNF>123.45</vNF></ICMSTot></total></infNFe></NFe>
        """;
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var parsed = await XmlDocumentParser.ParseAsync(ms, default);
        parsed.Tipo.Should().Be(Domain.Enums.TipoDocumento.NFe);
        parsed.CNPJEmitente.Should().Contain("30290856000128");
        parsed.UF.Should().Be("SP");
        parsed.ValorTotal.Should().Be(123.45m);
    }
}

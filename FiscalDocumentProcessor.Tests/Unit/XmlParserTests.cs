using System.Text;
using FiscalDocumentProcessor.Application.Parsing;
using NUnit.Framework;
using FluentAssertions;

namespace FiscalDocumentProcessor.Tests.Unit;

public class XmlParserTests
{
    [Test]
    public void Should_Parse_Basic_NFe_Xml()
    {
        var parser = new XmlFiscalParser();
        var xml = @"<NFe><infNFe><ide><dhEmi>2024-01-01T00:00:00Z</dhEmi></ide>
        <emit><CNPJ>12345678000199</CNPJ><enderEmit><UF>SP</UF></enderEmit></emit>
        <dest><CNPJ>99887766000111</CNPJ></dest>
        <total><ICMSTot><vNF>100.50</vNF></ICMSTot></total>
        <chNFe>35190100000000000000550010000000011000000010</chNFe>
        </infNFe></NFe>";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var (tipo, doc, chave) = parser.Parse(ms);
        tipo.ToString().Should().Be("NFe");
        doc.UF.Should().Be("SP");
        doc.ValorTotal.Should().Be(100.50m);
        chave.Should().NotBeNull();
    }
}

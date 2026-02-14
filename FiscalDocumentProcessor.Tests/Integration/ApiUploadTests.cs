using System.Net;
using System.Text;
using FiscalDocumentProcessor.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using FluentAssertions;

namespace FiscalDocumentProcessor.Tests.Integration;

public class ApiUploadTests
{
    [Test]
    public async Task Should_Upload_And_Return_200()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();
        using var content = new MultipartFormDataContent();
        var xml = "<NFe><infNFe><ide><dhEmi>2024-01-01T00:00:00Z</dhEmi></ide><emit><CNPJ>1</CNPJ></emit><dest><CNPJ>2</CNPJ></dest></infNFe></NFe>";
        content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "arquivo", "doc.xml");
        var resp = await client.PostAsync("/documentos/upload", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net.Http;
using System.Text;
using NBomber.Contracts;
using NBomber.CSharp;
using NUnit.Framework;

namespace FiscalDocumentProcessor.Tests.Integration;

public class LoadTest
{
    [Test]
    public void Simple_Load_Test()
    {
        var httpFactory = HttpClientFactory.Create();
        var scenario = ScenarioBuilder.CreateScenario("upload_xml", async ctx =>
        {
            var client = httpFactory.CreateClient();
            using var content = new MultipartFormDataContent();
            var xml = "<NFe><infNFe><ide><dhEmi>2024-01-01T00:00:00Z</dhEmi></ide><emit><CNPJ>1</CNPJ></emit><dest><CNPJ>2</CNPJ></dest></infNFe></NFe>";
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "arquivo", "doc.xml");
            var res = await client.PostAsync("http://localhost:8080/documentos/upload", content);
            return Response.Ok(statusCode: (int)res.StatusCode);
        })
        .WithLoadSimulations(Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(10)));

        NBomberRunner.RegisterScenarios(scenario).Run();
    }
}

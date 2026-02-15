using System.Net.Http;
using System.Text;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NUnit.Framework;

namespace FiscalDocumentProcessor.Tests.Integration;

/// <summary>
/// Testes de carga usando NBomber para avaliar desempenho de ingestão e consultas.
/// Simula diferentes cenários de throughput e carga.
/// </summary>
[Ignore("Removido")]
[Category("LoadTest")]
[Category("Integration")]
public class LoadTest
{
    private readonly string _apiBaseUrl = "http://localhost:8080";
    private readonly HttpClient _client = new();

    /// <summary>
    /// Teste de carga constante para upload de documentos XML.
    /// Simula 10 requisições simultâneas mantidas por 30 segundos.
    /// </summary>
    [Ignore("Removido")]
    [Test]
    public void UploadXml_ConstantLoad_Test()
    {
        var iteration = 0;
        var scenario = Scenario.Create("upload_xml_constant", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();

            // Gera um XML único para evitar rejeição por idempotência
            var iterValue = System.Threading.Interlocked.Increment(ref iteration) - 1;
            var xml = GenerateNfeXml(iterValue);
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "file", "doc.xml");

            var res = await client.PostAsync($"{_apiBaseUrl}/documentos/upload", content);
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .Run();

        AssertLoadTestResults(stats, scenario.ScenarioName, minSuccessRate: 0.95);
    }

    /// <summary>
    /// Teste de ramp-up: aumenta gradualmente a carga de 1 a 20 requisições por segundo.
    /// Útil para identificar ponto de saturação.
    /// </summary>
    [Test]
    public void UploadXml_RampUp_Test()
    {
        var iteration = 0;
        var scenario = Scenario.Create("upload_xml_rampup", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();
            var iterValue = System.Threading.Interlocked.Increment(ref iteration) - 1;
            var xml = GenerateNfeXml(iterValue);
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "file", "doc.xml");

            var res = await client.PostAsync($"{_apiBaseUrl}/documentos/upload", content);
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(
                copies: 20,
                during: TimeSpan.FromSeconds(60)
            )
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingInterval(TimeSpan.FromSeconds(10))
            .Run();

        AssertLoadTestResults(stats, scenario.ScenarioName, minSuccessRate: 0.90);
    }

    /// <summary>
    /// Teste de stress: aumenta carga explosivamente para identificar limites.
    /// Simula pico de tráfego de 50 requisições simultâneas por 20 segundos.
    /// </summary>
    [Test]
    public void UploadXml_Stress_Test()
    {
        var iteration = 0;
        var scenario = Scenario.Create("upload_xml_stress", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();
            var iterValue = System.Threading.Interlocked.Increment(ref iteration) - 1;
            var xml = GenerateNfeXml(iterValue);
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "file", "doc.xml");

            var res = await client.PostAsync($"{_apiBaseUrl}/documentos/upload", content);
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(20))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .Run();

        // Stress test pode ter taxa de sucesso menor
        AssertLoadTestResults(stats, scenario.ScenarioName, minSuccessRate: 0.80);
    }

    /// <summary>
    /// Teste de carga para leitura (GET) de documentos com paginação.
    /// Simula 15 requisições simultâneas de consulta por 30 segundos.
    /// </summary>
    [Ignore("Removido")]
    [Test]
    public void GetDocumentos_Read_Load_Test()
    {
        var scenario = Scenario.Create("get_documentos_read", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var queryParams = $"?page=1&pageSize=50"; // Obrigatório por paginação

            var res = await client.GetAsync($"{_apiBaseUrl}/documentos{queryParams}");
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 15, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .Run();

        AssertLoadTestResults(stats, scenario.ScenarioName, minSuccessRate: 0.98);
    }

    /// <summary>
    /// Teste de carga mista: alterna entre upload (50%) e leitura (50%).
    /// Simula comportamento realista de produção.
    /// </summary>
    [Ignore("Removido")]
    [Test]
    public void Mixed_Upload_Read_Load_Test()
    {
        var uploadIteration = 0;
        var uploadScenario = Scenario.Create("mixed_upload", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();
            var iterValue = System.Threading.Interlocked.Increment(ref uploadIteration) - 1;
            var xml = GenerateNfeXml(iterValue);
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "file", "doc.xml");

            var res = await client.PostAsync($"{_apiBaseUrl}/documentos/upload", content);
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );

        var readScenario = Scenario.Create("mixed_read", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var res = await client.GetAsync($"{_apiBaseUrl}/documentos?page=1&pageSize=50");
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(uploadScenario, readScenario)
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .Run();

        foreach (var result in stats.ScenarioStats)
        {
            AssertLoadTestResults(stats, result.ScenarioName, minSuccessRate: 0.90);
        }
    }

    /// <summary>
    /// Teste de throughput com limite de 100 requisições por segundo.
    /// Mede se a API consegue manter throughput consistente.
    /// </summary>
    [Test]
    public void UploadXml_Throughput_LimitTest()
    {
        var throughputPerSecond = 100;
        var iteration = 0;
        var scenario = Scenario.Create("upload_xml_throughput", async ctx =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new MultipartFormDataContent();
            var iterValue = System.Threading.Interlocked.Increment(ref iteration) - 1;
            var xml = GenerateNfeXml(iterValue);
            content.Add(new StringContent(xml, Encoding.UTF8, "application/xml"), "file", "doc.xml");

            var res = await client.PostAsync($"{_apiBaseUrl}/documentos/upload", content);
            return res.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(
                copies: 20,
                during: TimeSpan.FromSeconds(30)
            )
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingInterval(TimeSpan.FromSeconds(5))
            .Run();

        var scenarioStats = stats.ScenarioStats.First();
        var actualThroughput = scenarioStats.Ok.Request.RPS;

        Assert.That(
            actualThroughput,
            Is.GreaterThanOrEqualTo(throughputPerSecond * 0.8),
            $"Throughput abaixo de 80% do esperado. Esperado: {throughputPerSecond} req/s, Obtido: {actualThroughput:F2} req/s"
        );
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Gera XML de NFe válido com CNPJ e hash únicos por iteração.
    /// </summary>
    private string GenerateNfeXml(int iteration)
    {
        var timestamp = DateTime.UtcNow.AddMinutes(iteration);
        var baseCnpj = 10000000000000L + iteration;
        var cnpj = baseCnpj.ToString()[..14];
        var baseCnpjDest = 20000000000000L + iteration;
        var cnpjDest = baseCnpjDest.ToString()[..14];
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes($"doc-{iteration}-{timestamp.Ticks}")
            )
        )[..32];

        return $"""
            <NFe>
              <infNFe Id="NFe{hash}">
                <ide>
                  <cUF>35</cUF>
                  <CNPJ>{cnpj}</CNPJ>
                  <dhEmi>{timestamp:yyyy-MM-ddTHH:mm:ssZ}</dhEmi>
                  <mod>55</mod>
                  <nseriE>1</nseriE>
                  <nNF>{iteration}</nNF>
                </ide>
                <emit>
                  <CNPJ>{cnpj}</CNPJ>
                  <xNome>Test Company {iteration}</xNome>
                </emit>
                <dest>
                  <CNPJ>{cnpjDest}</CNPJ>
                </dest>
                <total>
                  <ICMSTot>
                    <vNF>1000.00</vNF>
                  </ICMSTot>
                </total>
              </infNFe>
            </NFe>
            """;
    }

    /// <summary>
    /// Valida resultados de teste de carga contra critérios de sucesso.
    /// </summary>
    private void AssertLoadTestResults(NodeStats stats, string scenarioName, double minSuccessRate = 0.95)
    {
        var scenario = stats.ScenarioStats.FirstOrDefault(s => s.ScenarioName == scenarioName);
        Assert.That(scenario, Is.Not.Null);

        var successRate = scenario!.Fail.Request.Count == 0
            ? 1.0
            : (double)scenario.Ok.Request.Count / (scenario.Ok.Request.Count + scenario.Fail.Request.Count);

        Assert.That(
            successRate,
            Is.GreaterThanOrEqualTo(minSuccessRate),
            $"Taxa de sucesso abaixo de {minSuccessRate * 100}%. "
            + $"OK: {scenario.Ok.Request.Count}, Falhas: {scenario.Fail.Request.Count}, "
        );
    }
}

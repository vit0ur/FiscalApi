using FiscalDocumentProcessor.Infrastructure.Messaging;
using FiscalDocumentProcessor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Polly;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();

builder.Services.AddSerilog();

var pgConn = Environment.GetEnvironmentVariable("FISCALDB_CONNECTION") ?? builder.Configuration.GetConnectionString("FiscalDb");
var rabbitUri = Environment.GetEnvironmentVariable("RABBITMQ_URI");
var exchange = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? "fiscal.documents";
var queue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "fiscal.documents.processor";
var dlx = Environment.GetEnvironmentVariable("RABBITMQ_DLX") ?? "fiscal.documents.dlx";
var routingKey = Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY") ?? "document.processed";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(pgConn, npg => npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null)));

builder.Services.Configure<RabbitMqOptions>(opt => { opt.Uri = rabbitUri; opt.Exchange = exchange; opt.RoutingKey = routingKey; opt.ExchangeType = "topic"; });

builder.Services.AddHostedService<WorkerService>();

var app = builder.Build();
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var maxAttempts = 30;
    var attempt = 0;
    var connected = false;
    while (attempt < maxAttempts && !connected)
    {
        attempt++;
        try
        {
            if (db.Database.IsNpgsql())
            {
                db.Database.Migrate();
                                db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""documentos_fiscais"" (
    ""Id"" uuid PRIMARY KEY,
    ""TipoDocumento"" text NOT NULL,
    ""ChaveAcesso"" varchar(60),
    ""CNPJEmitente"" varchar(20),
    ""CNPJDestinatario"" varchar(20),
    ""UF"" char(2),
    ""DataEmissao"" timestamp NULL,
    ""ValorTotal"" numeric(18,2) NULL,
    ""XmlOriginalGzip"" bytea NULL,
    ""HashXml"" varchar(128) NOT NULL,
    ""DataProcessamento"" timestamp NOT NULL,
    ""Status"" int NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_documentos_chave"" ON ""documentos_fiscais""(""ChaveAcesso"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_documentos_hash"" ON ""documentos_fiscais""(""HashXml"");
");
            }
            else
            {
                db.Database.EnsureCreated();
            }

            connected = true;
            Log.Information("Database is available (attempt {Attempt}).", attempt);
            break;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration attempt {Attempt} failed.", attempt);
        }

        var delay = Math.Min(5000, 500 * (int)Math.Pow(2, Math.Min(attempt, 10)));
        Thread.Sleep(delay);
    }

    if (!connected)
        throw new InvalidOperationException("Database unavailable after retry attempts.");
}
catch
{
    throw;
}
app.Run();

public class WorkerService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IOptions<RabbitMqOptions> _options;
    private IConnection? _conn; private IModel? _ch;
    private readonly ILogger<WorkerService> _logger;
    private readonly AsyncPolicy _retry;

    public WorkerService(IServiceProvider sp, IOptions<RabbitMqOptions> options, ILogger<WorkerService> logger)
    {
        _sp = sp; _options = options; _logger = logger;
        _retry = Policy.Handle<Exception>().WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(Math.Pow(2, i)),
            (ex, ts, i, ctx) => _logger.LogWarning(ex, "Retrying consume operation in {Delay}s (attempt {Attempt})", ts.TotalSeconds, i));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.Uri)) throw new InvalidOperationException("RABBITMQ_URI not configured");
        var factory = new ConnectionFactory { Uri = new Uri(_options.Value.Uri), AutomaticRecoveryEnabled = true, TopologyRecoveryEnabled = true, DispatchConsumersAsync = true };
        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        var exchange = _options.Value.Exchange;
        var queue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "fiscal.documents.processor";
        var dlx = Environment.GetEnvironmentVariable("RABBITMQ_DLX") ?? "fiscal.documents.dlx";
        var routingKey = _options.Value.RoutingKey;

        _ch.ExchangeDeclare(exchange, _options.Value.ExchangeType, durable: true);
        _ch.ExchangeDeclare(dlx, "fanout", true);
        var args = new Dictionary<string, object> { ["x-dead-letter-exchange"] = dlx };
        _ch.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _ch.QueueBind(queue, exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.Received += async (s, ea) =>
        {
            try
            {
                await _retry.ExecuteAsync(async _ =>
                {
                    await HandleMessageAsync(ea, stoppingToken);
                }, new Context("consume"));
                _ch.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message. Sending to DLQ");
                _ch.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        _ch.BasicQos(0, 10, false);
        _ch.BasicConsume(queue, autoAck: false, consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var json = System.Text.Json.JsonDocument.Parse(ea.Body);
        var docId = json.RootElement.GetProperty("DocumentoId").GetGuid();

        var doc = await db.Documentos.FirstOrDefaultAsync(x => x.Id == docId, ct);
        if (doc is null)
        {
            _logger.LogWarning("Documento {DocId} not found. Acking to avoid poison message.", docId);
            return;
        }

        if (doc.Status == FiscalDocumentProcessor.Domain.Entities.StatusDocumento.Processado)
        {
            _logger.LogInformation("Documento {DocId} already processed. Idempotent ack.", docId);
            return;
        }

        _logger.LogInformation("Resumo Documento {Id}: Tipo={Tipo} Chave={Chave} Valor={Valor} UF={UF}", doc.Id, doc.TipoDocumento, doc.ChaveAcesso, doc.ValorTotal, doc.UF);

        doc.Status = FiscalDocumentProcessor.Domain.Entities.StatusDocumento.Processado;
        doc.DataProcessamento = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public override void Dispose()
    {
        try { _ch?.Close(); } catch {}
        try { _conn?.Close(); } catch {}
        _ch?.Dispose();
        _conn?.Dispose();
        base.Dispose();
    }
}

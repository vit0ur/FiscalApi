using System.Text;
using System.Text.Json;
using FiscalDocumentProcessor.Application.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace FiscalDocumentProcessor.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string? Uri { get; set; }
    public string Exchange { get; set; } = "fiscal.documents";
    public string RoutingKey { get; set; } = "document.processed";
    public string ExchangeType { get; set; } = "topic";
}

public sealed class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly AsyncRetryPolicy _retry;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value; _logger = logger;
        _retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, ts, i, ctx) => _logger.LogWarning(ex, "Retrying RabbitMQ publish in {Delay}s (attempt {Attempt})", ts.TotalSeconds, i));
        EnsureConnected();
    }

    private void EnsureConnected()
    {
        if (string.IsNullOrWhiteSpace(_options.Uri))
            throw new InvalidOperationException("RabbitMQ URI must be provided via environment variable RABBITMQ_URI");

        var factory = new ConnectionFactory { Uri = new Uri(_options.Uri), AutomaticRecoveryEnabled = true, TopologyRecoveryEnabled = true, DispatchConsumersAsync = true, RequestedConnectionTimeout = TimeSpan.FromSeconds(10) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, _options.ExchangeType, durable: true, autoDelete: false);
        _channel.ConfirmSelect();
    }

    public async Task PublishDocumentoProcessadoAsync(Guid documentoId, string tipoDocumento, string chaveAcesso, DateTime dataProcessamento, CancellationToken ct)
    {
        var payload = new { DocumentoId = documentoId, TipoDocumento = tipoDocumento, ChaveAcesso = chaveAcesso, DataProcessamento = dataProcessamento };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        await _retry.ExecuteAsync(async _ =>
        {
            var props = _channel!.CreateBasicProperties();
            props.DeliveryMode = 2; // persistent
            props.ContentType = "application/json";
            props.Headers = new Dictionary<string, object> { ["x-origin"] = "FiscalDocumentProcessor.Api" };

            _channel.BasicPublish(exchange: _options.Exchange, routingKey: _options.RoutingKey, basicProperties: props, body: body);
            if (!_channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
            {
                throw new Exception("Publisher confirm not received");
            }
            await Task.CompletedTask;
        }, new Context("publish"));

        _logger.LogInformation("Published DocumentoFiscalProcessadoEvent for {DocumentoId}", documentoId);
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch {}
        try { _connection?.Close(); } catch {}
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

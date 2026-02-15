using System.Reflection;
using System.Text.Json.Serialization;
using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.Features.Commands;
using FiscalDocumentProcessor.Application.Features.Queries;
using FiscalDocumentProcessor.Infrastructure.Messaging;
using FiscalDocumentProcessor.Infrastructure.Persistence;
using FiscalDocumentProcessor.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using FiscalDocumentProcessor.Application.Features.Upload;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

var pgConn = Environment.GetEnvironmentVariable("FISCALDB_CONNECTION") ?? builder.Configuration.GetConnectionString("Postgres");
var rabbitUri = Environment.GetEnvironmentVariable("RABBITMQ_URI");

if (!string.IsNullOrWhiteSpace(pgConn))
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(pgConn, npg => { npg.MigrationsAssembly("FiscalDocumentProcessor.Infrastructure"); npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null); })
           .EnableSensitiveDataLogging(false));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite("Data Source=fiscal.db").EnableSensitiveDataLogging(false));
}

builder.Services.AddMediatR(Assembly.GetExecutingAssembly(), typeof(UploadDocumentoCommand).Assembly);

builder.Services.Configure<RabbitMqOptions>(opt =>
{
    opt.Uri = rabbitUri;
    opt.Exchange = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE") ?? "fiscal.documents";
    opt.RoutingKey = Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY") ?? "document.processed";
});

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
if (!string.IsNullOrWhiteSpace(rabbitUri))
{
    builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();
    Log.Warning("RABBITMQ_URI not configured. Using NullEventPublisher. Set RABBITMQ_URI in production.");
}

builder.Services.AddSingleton<IHashService, HashService>();
builder.Services.AddSingleton<ICompressionService, CompressionService>();
builder.Services.AddSingleton<IXmlValidator, XsdXmlValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FiscalDocumentProcessor API", Version = "v1" });
    c.EnableAnnotations();
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddHealthChecks();

var app = builder.Build();


app.UseSerilogRequestLogging();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/problem+json";
        var exFeature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var err = exFeature?.Error;
        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "An unexpected error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? err?.ToString() : "Internal server error"
        };
        ctx.Response.StatusCode = pd.Status.Value;
        await System.Text.Json.JsonSerializer.SerializeAsync(ctx.Response.Body, pd, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    });
});

app.Use(async (ctx, next) =>
{
    if (!ctx.Request.IsHttps)
    {
        if (!app.Environment.IsDevelopment())
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsync("HTTPS required");
            return;
        }
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FiscalDocumentProcessor.Infrastructure.Persistence.AppDbContext>();

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
                await db.Database.MigrateAsync();
                                await db.Database.ExecuteSqlRawAsync(@"
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
        await Task.Delay(delay);
    }

    if (!connected)
    {
        Log.Error("Could not migrate/connect to the database after {MaxAttempts} attempts.", maxAttempts);
        throw new InvalidOperationException("Database unavailable after retry attempts.");
    }
}

app.Run();

public partial class Program { }

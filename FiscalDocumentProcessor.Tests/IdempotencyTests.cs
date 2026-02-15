using System.Text;
using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.Features.Upload;
using FiscalDocumentProcessor.Infrastructure.Persistence;
using FiscalDocumentProcessor.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace FiscalDocumentProcessor.Tests;

public class IdempotencyTests
{
    private DbContextOptions<AppDbContext> _opts = null!;

    [SetUp]
    public void Setup()
    {
        _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
    }

    [Test]
    public async Task Upload_Same_Xml_Twice_Should_Be_Idempotent()
    {
        await using var db = new AppDbContext(_opts);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE documentos_fiscais (Id TEXT PRIMARY KEY, TipoDocumento TEXT NOT NULL, ChaveAcesso TEXT, CNPJEmitente TEXT, CNPJDestinatario TEXT, UF TEXT, DataEmissao TEXT, ValorTotal REAL, XmlOriginalGzip BLOB, HashXml TEXT NOT NULL, DataProcessamento TEXT NOT NULL, Status INTEGER NOT NULL);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE UNIQUE INDEX IX_documentos_Chave ON documentos_fiscais(ChaveAcesso);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE UNIQUE INDEX IX_documentos_Hash ON documentos_fiscais(HashXml);");

        var repo = new DocumentRepository(db);
        var hash = new HashService(); var zip = new CompressionService();
        var publisher = new Mock<IEventPublisher>();
        var handler = new UploadDocumentoCommandHandler(repo, hash, zip, publisher.Object);

        var xml = "<NFe><infNFe Id=\"x\"><ide><dEmi>2020-01-01</dEmi></ide></infNFe></NFe>";
        var cmd = new UploadDocumentoCommand("nfe.xml", Encoding.UTF8.GetBytes(xml));

        var r1 = await handler.Handle(cmd, default);
        var r2 = await handler.Handle(cmd, default);

        r1.JaExistia.Should().BeFalse();
        r2.JaExistia.Should().BeTrue();
        r2.DocumentoId.Should().Be(r1.DocumentoId);
    }

    [Test]
    public async Task Upload_Different_Xml_Same_Chave_Should_Be_Idempotent()
    {
        await using var db = new AppDbContext(_opts);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE documentos_fiscais (Id TEXT PRIMARY KEY, TipoDocumento TEXT NOT NULL, ChaveAcesso TEXT, CNPJEmitente TEXT, CNPJDestinatario TEXT, UF TEXT, DataEmissao TEXT, ValorTotal REAL, XmlOriginalGzip BLOB, HashXml TEXT NOT NULL, DataProcessamento TEXT NOT NULL, Status INTEGER NOT NULL);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE UNIQUE INDEX IX_documentos_Chave ON documentos_fiscais(ChaveAcesso);");
        await db.Database.ExecuteSqlRawAsync(@"CREATE UNIQUE INDEX IX_documentos_Hash ON documentos_fiscais(HashXml);");

        var repo = new DocumentRepository(db);
        var hash = new HashService(); var zip = new CompressionService();
        var publisher = new Mock<IEventPublisher>();
        var handler = new UploadDocumentoCommandHandler(repo, hash, zip, publisher.Object);

        var xml1 = "<NFe><infNFe Id=\"NFe123\"><ide><dEmi>2020-01-01</dEmi></ide><total><ICMSTot><vNF>1</vNF></ICMSTot></total></infNFe></NFe>";
        var xml2 = "<NFe><infNFe Id=\"NFe123\"><ide><dEmi>2020-01-02</dEmi></ide><total><ICMSTot><vNF>2</vNF></ICMSTot></total></infNFe></NFe>";

        var r1 = await handler.Handle(new UploadDocumentoCommand("nfe1.xml", Encoding.UTF8.GetBytes(xml1)), default);
        var r2 = await handler.Handle(new UploadDocumentoCommand("nfe2.xml", Encoding.UTF8.GetBytes(xml2)), default);

        r1.JaExistia.Should().BeFalse();
        r2.JaExistia.Should().BeTrue();
        r2.DocumentoId.Should().Be(r1.DocumentoId);
    }
}

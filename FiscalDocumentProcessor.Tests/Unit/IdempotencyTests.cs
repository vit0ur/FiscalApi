using System.Text;
using FiscalDocumentProcessor.Application.Abstractions;
using FiscalDocumentProcessor.Application.Parsing;
using FiscalDocumentProcessor.Application.Services;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using FiscalDocumentProcessor.Domain.Entities;

namespace FiscalDocumentProcessor.Tests.Unit;

public class IdempotencyTests
{
    [Test]
    public async Task Upload_Should_Return_Existing_When_Duplicate()
    {
        var repo = new Mock<IDocumentoFiscalRepository>();
        var uow = new Mock<IUnitOfWork>();
        var pub = new Mock<IRabbitMqPublisher>();
        var parser = new XmlFiscalParser();

        var existing = new DocumentoFiscal { Id = Guid.NewGuid(), HashXml = "ABC", UF = "SP" };
        uow.SetupGet(x => x.Documentos).Returns(repo.Object);
        repo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var service = new DocumentoFiscalService(uow.Object, parser, pub.Object);
        var xml = "<NFe></NFe>";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var resp = await service.UploadAsync(ms, CancellationToken.None);
        resp.Id.Should().Be(existing.Id);
        pub.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

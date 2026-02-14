using System.Text;
using FiscalDocumentProcessor.Application.Abstractions;
using Moq;
using NUnit.Framework;
using FluentAssertions;

namespace FiscalDocumentProcessor.Tests;

public class PublisherTests
{
    [Test]
    public async Task Should_Call_Publisher()
    {
        var publisher = new Mock<IEventPublisher>();
        await publisher.Object.PublishDocumentoProcessadoAsync(Guid.NewGuid(), "NFe", "chave", DateTime.UtcNow, default);
        publisher.Invocations.Count.Should().Be(1);
    }
}

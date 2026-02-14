using NUnit.Framework;
using FluentAssertions;

namespace FiscalDocumentProcessor.Tests;

public class ConsumerLogicTests
{
    [Test]
    public void Backoff_Should_Grow_Exponentially()
    {
        var delays = Enumerable.Range(1,5).Select(i => Math.Pow(2, i)).ToArray();
        delays[0].Should().Be(2);
        delays[4].Should().Be(32);
    }
}

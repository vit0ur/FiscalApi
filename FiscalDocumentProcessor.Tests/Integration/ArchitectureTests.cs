using NetArchTest.Rules;
using NUnit.Framework;

namespace FiscalDocumentProcessor.Tests.Integration;

public class ArchitectureTests
{
    [Test]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(FiscalDocumentProcessor.Domain.Entities.DocumentoFiscal).Assembly)
            .Should().NotHaveDependencyOn("FiscalDocumentProcessor.Infrastructure")
            .GetResult();
        Assert.IsTrue(result.IsSuccessful);
    }
}

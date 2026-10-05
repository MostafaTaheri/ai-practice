using AIPractice.Domain.Abstractions;
using AIPractice.Domain.Exceptions;
using NetArchTest.Rules;

namespace AIPractice.ArchTests;

public class DomainLayerTests
{
    [Fact]
    public void BusinessRules_ShouldInherit_BaseBusinessRule()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .That()
            .ImplementInterface(typeof(IBusinessRule))
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseBusinessRule))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"All concrete business rules must inherit from BaseBusinessRule. Violating types:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ValueObjects_ShouldInherit_ValueObject()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .That()
            .ResideInNamespaceContaining(".ValueObjects")
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(ValueObject))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"All value objects must inherit from ValueObject. Violating types:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DomainExceptions_ShouldInherit_DomainBaseException()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .That()
            .ResideInNamespace("AIPractice.Domain.Exceptions")
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(BaseException))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"All concrete domain exceptions must inherit from BaseException. Violating types:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Entities_ShouldReside_InDomainLayer()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .That()
            .Inherit(typeof(Entity<>))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespace("AIPractice.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"All entities must reside in the Domain layer. Violating types:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}

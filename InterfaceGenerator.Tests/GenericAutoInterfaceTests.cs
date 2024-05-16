using System;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace InterfaceGenerator.Tests;

public class GenericAutoInterfaceTests
{
    [Fact]
    public void GenericParametersGeneratedCorrectly()
    {
        var t = typeof(IGenericAutoInterfaceTestsService<,>);
        var genericArgs = t.GetGenericArguments();

        genericArgs.Should().HaveCount(2);
        genericArgs[0].Name.Should().Be("TX");
        genericArgs[1].Name.Should().Be("TY");

        genericArgs[0].IsClass.Should().BeTrue();
        genericArgs[0]
            .GenericParameterAttributes
            .Should()
            .HaveFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        var iEquatableOfTx = typeof(IEquatable<>).MakeGenericType(genericArgs[0]);
        genericArgs[0].GetGenericParameterConstraints().Should().HaveCount(1).And.Contain(iEquatableOfTx);

        genericArgs[1].IsValueType.Should().BeTrue();

        // base
        var interfaces = t.GetInterfaces();
        interfaces.Should().HaveCount(2);
        interfaces[0].Name.Should().Be(typeof(IAutoInterface<>).Name);
        interfaces[1].Name.Should().Be(typeof(IAutoInterface).Name);
        var interfaceGenericArgs = interfaces[0].GetGenericArguments();
        interfaceGenericArgs.Should().HaveCount(1);
        interfaceGenericArgs[0].Name.Should().Be(typeof(GenericAutoInterfaceTestsService<,>).Name);
    }
}

[GenerateGenericAutoInterface]
// ReSharper disable once UnusedType.Global
public class GenericAutoInterfaceTestsService<TX, TY> : IGenericAutoInterfaceTestsService<TX, TY>
    where TX : class, IEquatable<TX>, new ()
    where TY : struct
{
    public string A { get; set; }
}
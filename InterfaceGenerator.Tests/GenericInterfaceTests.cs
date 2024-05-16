﻿using System;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace InterfaceGenerator.Tests;

public class GenericInterfaceTests
{
    [Fact]
    public void GenericParametersGeneratedCorrectly()
    {
        var t = typeof(IGenericInterfaceTestsService<,>);
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

        // does not implement IAutoInterface because not public
        t.GetInterfaces().Should().HaveCount(0);
    }

    [Fact]
    public void ImplementsIAutoInterface()
    {
        var t = typeof(IGenericInterfaceTestsService2<,>);

        var interfaces = t.GetInterfaces();
        interfaces.Should().HaveCount(1);
        interfaces[0].Should().Be(typeof(IAutoInterface));
    }
}

[GenerateAutoInterface]
// ReSharper disable once UnusedType.Global
internal class GenericInterfaceTestsService<TX, TY> : IGenericInterfaceTestsService<TX, TY>
    where TX : class, IEquatable<TX>, new()
    where TY : struct
{
}

[GenerateAutoInterface]
// ReSharper disable once UnusedType.Global
public class GenericInterfaceTestsService2<TX, TY> : IGenericInterfaceTestsService2<TX, TY>
    where TX : class, IEquatable<TX>, new()
    where TY : struct
{
}
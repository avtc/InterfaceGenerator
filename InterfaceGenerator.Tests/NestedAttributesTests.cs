using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentAssertions.Common;
using Xunit;

namespace InterfaceGenerator.Tests;

public class NestedAttributesTests
{
    private readonly INestedAttributesService _sut;

    public NestedAttributesTests()
    {
        _sut = new NestedAttributesService();
    }

    [Fact]
    public void GetSetIndexer_IsImplemented()
    {
        var indexer = typeof(INestedAttributesService).GetIndexerByParameterTypes(new[] { typeof(string) });

        indexer.Should().NotBeNull();

        indexer.GetMethod.Should().NotBeNull();
        indexer.SetMethod.Should().NotBeNull();

        var _ = _sut[string.Empty];
        _sut[string.Empty] = 0;
    }

    [Fact]
    public void PublicProperty_IsImplemented()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.PublicProperty))!;

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().NotBeNull();

        var _ = _sut.PublicProperty;
        _sut.PublicProperty = string.Empty;
    }

    [Fact]
    public void InitProperty_IsImplemented()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.InitOnlyProperty))!;

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().NotBeNull();

        prop.SetMethod!.ReturnParameter!.GetRequiredCustomModifiers().Should().Contain(typeof(IsExternalInit));

        var _ = _sut.InitOnlyProperty;
    }

    [Fact]
    public void PrivateSetter_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.PropertyWithPrivateSetter))!;

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().BeNull();

        var _ = _sut.PropertyWithPrivateSetter;
    }

    [Fact]
    public void PrivateGetter_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.PropertyWithPrivateGetter))!;

        prop.Should().NotBeNull();

        prop.SetMethod.Should().NotBeNull();
        prop.GetMethod.Should().BeNull();

        _sut.PropertyWithPrivateGetter = string.Empty;
    }

    [Fact]
    public void ProtectedSetter_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.PropertyWithProtectedSetter))!;

        prop.Should().NotBeNull();

        prop.GetMethod.Should().NotBeNull();
        prop.SetMethod.Should().BeNull();

        var _ = _sut.PropertyWithProtectedSetter;
    }

    [Fact]
    public void ProtectedGetter_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(INestedAttributesService.PropertyWithProtectedGetter))!;

        prop.Should().NotBeNull();

        prop.SetMethod.Should().NotBeNull();
        prop.GetMethod.Should().BeNull();

        _sut.PropertyWithProtectedGetter = string.Empty;
    }

    [Fact]
    public void IgnoredProperty_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(NestedAttributesService.IgnoredProperty));

        prop.Should().BeNull();
    }

    [Fact]
    public void StaticProperty_IsOmitted()
    {
        var prop = typeof(INestedAttributesService)
            .GetProperty(nameof(NestedAttributesService.StaticProperty));

        prop.Should().BeNull();
    }

    [Fact]
    public void ImplementsIAutoInterface()
    {
        var interfaces = typeof(INestedAttributesService).GetInterfaces();
        interfaces.Should().HaveCount(1);
        interfaces[0].Name.Should().Be(typeof(IAutoInterface).Name);
    }
}

public class NestedGenerateAttribute : GenerateAutoInterfaceAttribute
{
}

public class NestedIgnoreAttribute : AutoInterfaceIgnoreAttribute
{
}

// ReSharper disable UnusedMember.Local, ValueParameterNotUsed
[NestedGenerate]
public class NestedAttributesService : INestedAttributesService
{
    public int this[string x]
    {
        get => 0;
        set
        {
        }
    }

    public string PublicProperty { get; set; }

    public string InitOnlyProperty { get; init; }

    public string PropertyWithPrivateSetter { get; private set; }

    public string PropertyWithPrivateGetter { private get; set; }

    public string PropertyWithProtectedSetter { get; protected set; }

    public string PropertyWithProtectedGetter { protected get; set; }

    [NestedIgnore] public string IgnoredProperty { get; set; }

    public static string StaticProperty { get; set; }
}
// ReSharper enable UnusedMember.Local, ValueParameterNotUsed
namespace InterfaceGenerator.Tests;

[GenerateAutoInterface(Name = "ICustomNameInterface")]
internal class CustomName1 : ICustomNameInterface
{

}

[GenerateAutoInterface(NameTemplate = "IPrefix{Name}Suffix")]
internal class CustomName2 : IPrefixCustomName2Suffix
{

}

[NestedCustomName3(NameTemplate = "INested{Name}")]
internal class CustomName3 : INestedCustomName3
{

}

public class NestedCustomName3Attribute : GenerateAutoInterfaceAttribute
{
}

[NestedCustomName4]
public class CustomName4 : INestedCustomName4
{

}

[AutoInterfaceNameTemplate("INested{Name}")]
public class NestedCustomName4Attribute : GenerateGenericAutoInterfaceAttribute
{
}
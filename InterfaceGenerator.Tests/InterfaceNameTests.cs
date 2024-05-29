namespace InterfaceGenerator.Tests;

[GenerateAutoInterface(Name = "ICustomNameInterface")]
internal class CustomName1 : ICustomNameInterface
{

}

[GenerateAutoInterface(NameTemplate = "IPrefix{Name}Suffix")]
internal class CustomName2 : IPrefixCustomName2Suffix
{

}

[NestedCustomName(NameTemplate = "INested{Name}")]
internal class CustomName3 : INestedCustomName3
{

}

public class NestedCustomNameAttribute : GenerateAutoInterfaceAttribute
{
}
using System;

namespace InterfaceGenerator
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = true)]
    public class AutoInterfaceIgnoreAttribute : Attribute
    {
    }
}
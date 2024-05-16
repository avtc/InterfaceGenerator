using System;

namespace InterfaceGenerator
{
    /// <summary>
    /// Mark the class/struct with the attribute to generate auto interface.
    /// If an implementation is public or Visibility modifier is public then auto interface will derive from IAutoInterface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class GenerateAutoInterfaceAttribute : Attribute
    {
        public string? VisibilityModifier { get; set; }
        public string? Name { get; set; }

        public GenerateAutoInterfaceAttribute()
        {
        }
    }
}
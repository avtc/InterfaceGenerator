using System;

namespace InterfaceGenerator
{
    /// <summary>
    /// Mark the class/struct with the attribute to generate auto interface.
    /// If an implementation is public or Visibility modifier is public then auto interface will derive from IAutoInterface<TImplementer>.
    /// TImplementer must be public.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class GenerateGenericAutoInterfaceAttribute : Attribute
    {
        public string? VisibilityModifier { get; set; }
        public string? Name { get; set; }
        /// <summary>
        /// Default is "I{Name}"
        /// </summary>
        public string NameTemplate { get; set; } = "I{Name}";

        public GenerateGenericAutoInterfaceAttribute()
        {
        }
    }
}
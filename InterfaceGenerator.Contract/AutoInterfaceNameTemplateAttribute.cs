using System;

namespace InterfaceGenerator
{
    /// <summary>
    /// Mark the attribute derived from GenerateAutoInterfaceAttribute or from GenerateGenericAutoInterfaceAttribute 
    /// with the attribute to override interface name template.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AutoInterfaceNameTemplateAttribute : Attribute
    {
        /// <param name="NameTemplate">Default is "I{Name}"</param>
        public AutoInterfaceNameTemplateAttribute(string NameTemplate)
        {
            this.NameTemplate = NameTemplate;
        }

        /// <summary>
        /// Default is "I{Name}"
        /// </summary>
        public string NameTemplate { get; set; } = "I{Name}";
    }
}
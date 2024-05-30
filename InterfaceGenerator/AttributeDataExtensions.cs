using Microsoft.CodeAnalysis;

namespace InterfaceGenerator
{
    internal static class AttributeDataExtensions
    {
        public static TValue? GetParamValue<TValue>(this AttributeData attributeData, string paramName)
        {
            // Check constructor arguments
            var constructor = attributeData.AttributeConstructor;
            if (constructor != null)
            {
                var parameters = constructor.Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name == paramName)
                    {
                        var argument = attributeData.ConstructorArguments[i];
                        return (TValue?)argument.Value;
                    }
                }
            }

            // Check named arguments
            foreach (var arg in attributeData.NamedArguments)
            {
                if (arg.Key == paramName)
                {
                    return (TValue?)arg.Value.Value;
                }
            }

            return default;
        }
    }
}
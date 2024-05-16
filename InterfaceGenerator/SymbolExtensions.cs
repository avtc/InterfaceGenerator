using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace InterfaceGenerator
{
    internal static class SymbolExtensions
    {
        public static bool TryGetAttribute(
            this ISymbol symbol,
            INamedTypeSymbol attributeType,
            out IEnumerable<AttributeData> attributes)
        {
            attributes = symbol
                .GetAttributes()
                .Where(a => a.AttributeClass!.GetBaseTypesAndThis().Any(i => SymbolEqualityComparer.Default.Equals(i, attributeType)));
            return attributes.Any();
        }

        public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
        {
            return symbol
                .GetAttributes()
                .Any(a => a.AttributeClass!.GetBaseTypesAndThis().Any(i => SymbolEqualityComparer.Default.Equals(i, attributeType)));
        }

        public static bool Is(this ITypeSymbol symbol, INamedTypeSymbol baseType)
        {
            return symbol.GetBaseTypesAndThis().Any(i => SymbolEqualityComparer.Default.Equals(i, baseType));
        }

        //Ref: https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
        public static string GetFullMetadataName(this ISymbol symbol, bool useNameWhenNotFound = false)
        {
            if (IsRootNamespace(symbol))
            {
                return useNameWhenNotFound ? symbol.Name : string.Empty;
            }

            var stringBuilder = new StringBuilder(symbol.MetadataName);
            var last = symbol;

            symbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(symbol))
            {
                if (symbol is ITypeSymbol && last is ITypeSymbol)
                {
                    stringBuilder.Insert(0, '+');
                }
                else
                {
                    stringBuilder.Insert(0, '.');
                }

                stringBuilder.Insert(0, symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                symbol = symbol.ContainingSymbol;
            }

            var retVal = stringBuilder.ToString();
            if (string.IsNullOrWhiteSpace(retVal) && useNameWhenNotFound)
            {
                return symbol.Name;
            }

            return retVal;
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            return symbol is INamespaceSymbol { IsGlobalNamespace: true };
        }

        // Ref: https://github.com/dotnet/roslyn/blob/0c8ac4c91d0c61869a523433792691adab34242e/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ITypeSymbolExtensions.cs#L114
        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol? type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            return type.GetBaseTypesAndThis().SelectMany(x => x.GetMembers());
        }
    }
}
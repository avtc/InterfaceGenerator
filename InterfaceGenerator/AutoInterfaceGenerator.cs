using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace InterfaceGenerator
{
    [Generator]
    public class AutoInterfaceGenerator : ISourceGenerator
    {
        private INamedTypeSymbol _generateAutoInterfaceAttribute = null!;
        private INamedTypeSymbol _generateGenericAutoInterfaceAttribute = null!;
        private INamedTypeSymbol _ignoreAttribute = null!;
        private INamedTypeSymbol _nameTemplateAttribute = null!;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

            #if DEBUG
            if (!Debugger.IsAttached)
            {
                // sadly this is Windows only so as of now :(
                Debugger.Launch();
            }
            #endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                ExecuteCore(context);
            }
            catch (Exception exception)
            {
                RaiseExceptionDiagnostic(context, exception);
            }
        }

        private static void RaiseExceptionDiagnostic(GeneratorExecutionContext context, Exception exception)
        {
            var descriptor = new DiagnosticDescriptor(
                "IG0001",
                $"Exception thrown in InterfaceGenerator",
                $"{exception.GetType().FullName} {exception.Message} {exception.StackTrace.Trim()}",
                "InterfaceGenerator",
                DiagnosticSeverity.Error,
                true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            var diagnostic = Diagnostic.Create(descriptor, null);
            
            context.ReportDiagnostic(diagnostic);
        }
        
        private void ExecuteCore(GeneratorExecutionContext context)
        {
            // setting the culture to invariant prevents errors such as emitting a decimal comma (0,1) instead of
            // a decimal point (0.1) in certain cultures
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            GenerateInterfaces(context);

            Thread.CurrentThread.CurrentCulture = prevCulture;
        }

        private void GenerateInterfaces(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var compilation = GetCompilation(context);
            InitAttributes(compilation);

            var classSymbols = GetImplTypeSymbols(compilation, receiver);

            List<string> classSymbolNames = new List<string>();

            foreach (var implTypeSymbol in classSymbols)
            {
                if (!implTypeSymbol.TryGetAttribute(_generateAutoInterfaceAttribute, out var attributes)
                    && !implTypeSymbol.TryGetAttribute(_generateGenericAutoInterfaceAttribute, out attributes))
                {
                    continue;
                }

                if(classSymbolNames.Contains(implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true)))
                {
                    continue; // partial class, already added
                }

                classSymbolNames.Add(implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true));

                var attribute = attributes.Last(); 
                var source = SourceText.From(GenerateInterfaceCode(implTypeSymbol, attribute), Encoding.UTF8);

                context.AddSource($"{implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true)}_AutoInterface.g.cs", source);
            }
        }

        private static string InferVisibilityModifier(ISymbol implTypeSymbol, AttributeData attributeData)
        {
            string? result = attributeData.GetParamValue<string>(nameof(GenerateAutoInterfaceAttribute.VisibilityModifier));
            if (!string.IsNullOrEmpty(result))
            {
                return result!;
            }

            return implTypeSymbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                _                    => "internal",
            };
        }

        private string InferInterfaceName(ISymbol implTypeSymbol, AttributeData attributeData)
        {
            return attributeData.GetParamValue<string>(nameof(GenerateAutoInterfaceAttribute.Name))
                ?? attributeData.GetParamValue<string>(nameof(GenerateAutoInterfaceAttribute.NameTemplate))?.Replace("{Name}", implTypeSymbol.Name)
                ?? (attributeData.AttributeClass?.TryGetAttribute(_nameTemplateAttribute, out var nameTemplateAttributes) == true
                    ? nameTemplateAttributes.First().GetParamValue<string>(
                        nameof(AutoInterfaceNameTemplateAttribute.NameTemplate))!.Replace("{Name}", implTypeSymbol.Name)
                    : null)
                ?? $"I{implTypeSymbol.Name}";
        }

        private string GenerateInterfaceCode(INamedTypeSymbol implTypeSymbol, AttributeData attributeData)
        {
            using var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            var codeWriter = new IndentedTextWriter(streamWriter, "    ");

            var namespaceName = implTypeSymbol.ContainingNamespace.ToDisplayString();
            var interfaceName = InferInterfaceName(implTypeSymbol, attributeData);
            var visibilityModifier = InferVisibilityModifier(implTypeSymbol, attributeData);

            codeWriter.WriteLine("// <auto-generated/>");
            codeWriter.WriteLine("#nullable enable");
            codeWriter.WriteLine("namespace {0}", namespaceName);
            codeWriter.WriteLine("{");

            ++codeWriter.Indent;
            WriteSymbolDocsIfPresent(codeWriter, implTypeSymbol);
            codeWriter.Write("{0} partial interface {1}", visibilityModifier, interfaceName);
            WriteTypeGenericsIfNeeded(codeWriter, implTypeSymbol);
            WriteBaseInterface(codeWriter, attributeData, implTypeSymbol);
            WriteTypeParameterConstraintsIfNeeded(codeWriter, implTypeSymbol);
            codeWriter.WriteLine();
            codeWriter.WriteLine("{");

            ++codeWriter.Indent;
            GenerateInterfaceMemberDefinitions(codeWriter, implTypeSymbol);
            --codeWriter.Indent;

            codeWriter.WriteLine("}");
            --codeWriter.Indent;

            codeWriter.WriteLine("}");
            codeWriter.WriteLine("#nullable restore");

            codeWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private void WriteBaseInterface(IndentedTextWriter codeWriter, AttributeData attributeData, INamedTypeSymbol implTypeSymbol)
        {
            if (implTypeSymbol.DeclaredAccessibility != Accessibility.Public)
                return;
            if (attributeData.AttributeClass!.Is(_generateAutoInterfaceAttribute))
            {
                codeWriter.Write($" : {typeof(IAutoInterface).Namespace}.{nameof(IAutoInterface)}");
            }
            else if (attributeData.AttributeClass!.Is(_generateGenericAutoInterfaceAttribute))
            {
                codeWriter.Write($" : {typeof(IAutoInterface).Namespace}.{nameof(IAutoInterface)}<{implTypeSymbol.Name}");
                WriteTypeGenericsIfNeeded(codeWriter, implTypeSymbol);
                codeWriter.Write(">");
            }
        }

        private static void WriteTypeGenericsIfNeeded(TextWriter writer, INamedTypeSymbol implTypeSymbol)
        {
            if (!implTypeSymbol.IsGenericType)
            {
                return;
            }

            writer.Write("<");
            writer.WriteJoin(", ", implTypeSymbol.TypeParameters.Select(x => x.Name));
            writer.Write(">");
        }

        private static void WriteTypeParameterConstraintsIfNeeded(TextWriter writer, INamedTypeSymbol implTypeSymbol)
        {
            if (!implTypeSymbol.IsGenericType)
            {
                return;
            }

            WriteTypeParameterConstraints(writer, implTypeSymbol.TypeParameters);
        }

        private void GenerateInterfaceMemberDefinitions(TextWriter writer, INamedTypeSymbol implTypeSymbol)
        {
            foreach (var member in implTypeSymbol.GetAllMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public 
                    || member.HasAttribute(_ignoreAttribute)
                    || member.ContainingType.Name == nameof(Object))
                {
                    continue;
                }
                
                GenerateInterfaceMemberDefinition(writer, member);
            }
        }

        private static void GenerateInterfaceMemberDefinition(TextWriter writer, ISymbol member)
        {
            switch (member)
            {
                case IPropertySymbol propertySymbol:
                    GeneratePropertyDefinition(writer, propertySymbol);
                    break;
                case IMethodSymbol methodSymbol:
                    GenerateMethodDefinition(writer, methodSymbol);
                    break;
            }
        }

        private static void WriteSymbolDocsIfPresent(TextWriter writer, ISymbol symbol)
        {
            var xml = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml))
            {
                return;
            }

            // omit the fist and last lines to skip the <member> tag
            
            var reader = new StringReader(xml);
            var lines = new List<string>();
            
            while (true)
            {
                var line = reader.ReadLine();
                if (line is null)
                {
                    break;
                }
                
                lines.Add(line);
            }

            for (int i = 1; i < lines.Count - 1; i++)
            {
                var line = lines[i].TrimStart(); // for some reason, 4 spaces are inserted to the beginning of the line
                writer.WriteLine("/// {0}", line);
            }
        }

        private static bool IsPublicOrInternal(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
        }

        private static void GeneratePropertyDefinition(TextWriter writer, IPropertySymbol propertySymbol)
        {
            if (propertySymbol.IsStatic)
            {
                return;
            }
            
            bool hasPublicGetter = propertySymbol.GetMethod is not null &&
                                   IsPublicOrInternal(propertySymbol.GetMethod);
            
            bool hasPublicSetter = propertySymbol.SetMethod is not null &&
                                   IsPublicOrInternal(propertySymbol.SetMethod);

            if (!hasPublicGetter && !hasPublicSetter)
            {
                return;
            }

            WriteSymbolDocsIfPresent(writer, propertySymbol);
            
            if (propertySymbol.IsIndexer)
            {
                writer.Write("{0} this[", propertySymbol.Type);
                writer.WriteJoin(", ", propertySymbol.Parameters, WriteMethodParam);
                writer.Write("] ");
            }
            else
            {
                writer.Write("{0} {1} ", propertySymbol.Type, propertySymbol.Name); // ex. int Foo
            }

            writer.Write("{ ");

            if (hasPublicGetter)
            {
                writer.Write("get; ");
            }

            if (hasPublicSetter)
            {
                if (propertySymbol.SetMethod!.IsInitOnly)
                {
                    writer.Write("init; ");
                }
                else
                {
                    writer.Write("set; ");
                }
            }

            writer.WriteLine("}");
        }

        private static void GenerateMethodDefinition(TextWriter writer, IMethodSymbol methodSymbol)
        {
            if (methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.IsStatic)
            {
                return;
            }

            if (methodSymbol.IsImplicitlyDeclared && methodSymbol.Name != "Deconstruct") 
            {
                // omit methods that are auto generated by the compiler (eg. record's methods),
                // except for the record Deconstruct method
                return;
            }
            
            WriteSymbolDocsIfPresent(writer, methodSymbol);

            writer.Write("{0} {1}", methodSymbol.ReturnType, methodSymbol.Name); // ex. int Foo

            if (methodSymbol.IsGenericMethod)
            {
                writer.Write("<");
                writer.WriteJoin(", ", methodSymbol.TypeParameters.Select(x => x.Name));
                writer.Write(">");
            }

            writer.Write("(");
            writer.WriteJoin(", ", methodSymbol.Parameters, WriteMethodParam);

            writer.Write(")");

            if (methodSymbol.IsGenericMethod)
            {
                WriteTypeParameterConstraints(writer, methodSymbol.TypeParameters);
            }

            writer.WriteLine(";");
        }

        private static void WriteMethodParam(TextWriter writer, IParameterSymbol param)
        {
            if (param.IsParams)
            {
                writer.Write("params ");
            }

            switch (param.RefKind)
            {
                case RefKind.Ref:
                    writer.Write("ref ");
                    break;
                case RefKind.Out:
                    writer.Write("out ");
                    break;
                case RefKind.In:
                    writer.Write("in ");
                    break;
            }
            
            writer.Write(param.Type);
            writer.Write(" ");

            if (StringExtensions.IsCSharpKeyword(param.Name))
            {
                writer.Write("@");
            }
            
            writer.Write(param.Name);

            if (param.HasExplicitDefaultValue)
            {
                WriteParamExplicitDefaultValue(writer, param);
            }
        }

        private static void WriteParamExplicitDefaultValue(TextWriter writer, IParameterSymbol param)
        {
            if (param.ExplicitDefaultValue is null)
            {
                writer.Write(" = default");
            }
            else
            {
                switch (param.Type.Name)
                {
                    case nameof(String):
                        writer.Write(" = \"{0}\"", param.ExplicitDefaultValue);
                        break;
                    case nameof(Single):
                        writer.Write(" = {0}f", param.ExplicitDefaultValue);
                        break;
                    case nameof(Double):
                        writer.Write(" = {0}d", param.ExplicitDefaultValue);
                        break;
                    case nameof(Decimal):
                        writer.Write(" = {0}m", param.ExplicitDefaultValue);
                        break;
                    case nameof(Boolean):
                        writer.Write(" = {0}", param.ExplicitDefaultValue.ToString().ToLower());
                        break;
                    case nameof(Nullable<bool>):
                        writer.Write(" = {0}", param.ExplicitDefaultValue.ToString().ToLower());
                        break;
                    default:
                        writer.Write(" = {0}", param.ExplicitDefaultValue);
                        break;
                }
            }
        }

        private static void WriteTypeParameterConstraints(
            TextWriter writer,
            IEnumerable<ITypeParameterSymbol> typeParameters)
        {
            foreach (var typeParameter in typeParameters)
            {
                var constraints = typeParameter.EnumGenericConstraints().ToList();
                if (constraints.Count == 0)
                {
                    break;
                }

                writer.Write(" where {0} : ", typeParameter.Name);
                writer.WriteJoin(", ", constraints);
            }
        }

        private void InitAttributes(Compilation compilation)
        {
            _generateAutoInterfaceAttribute = compilation.GetTypeByMetadataName(
                $"{typeof(GenerateAutoInterfaceAttribute).Namespace}.{nameof(GenerateAutoInterfaceAttribute)}")!;

            _generateGenericAutoInterfaceAttribute = compilation.GetTypeByMetadataName(
                $"{typeof(GenerateGenericAutoInterfaceAttribute).Namespace}.{nameof(GenerateGenericAutoInterfaceAttribute)}")!;

            _ignoreAttribute = compilation.GetTypeByMetadataName(
                $"{typeof(AutoInterfaceIgnoreAttribute).Namespace}.{nameof(AutoInterfaceIgnoreAttribute)}")!;

            _nameTemplateAttribute = compilation.GetTypeByMetadataName(
                $"{typeof(AutoInterfaceNameTemplateAttribute).Namespace}.{nameof(AutoInterfaceNameTemplateAttribute)}")!;
        }

        private static IEnumerable<INamedTypeSymbol> GetImplTypeSymbols(Compilation compilation, SyntaxReceiver receiver)
        {
            return receiver.CandidateTypes.Select(candidate => GetTypeSymbol(compilation, candidate));
        }

        private static INamedTypeSymbol GetTypeSymbol(Compilation compilation, SyntaxNode type)
        {
            var model = compilation.GetSemanticModel(type.SyntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(type)!;
            return (INamedTypeSymbol)typeSymbol;
        }

        private static Compilation GetCompilation(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            return compilation;
        }
    }
}
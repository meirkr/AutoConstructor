﻿using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator
{
    [Generator]
    public class AutoConstructorGenerator : ISourceGenerator
    {
        internal const string AttributeName = "AutoConstructor";

        internal const string AttributeFullName = $"{AttributeName}Attribute";

        internal const string AttributeText = $@"// <auto-generated />
using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class {AttributeFullName} : Attribute
{{
}}
";

        internal const string IgnoreAttributeFullName = "AutoConstructorIgnoreAttribute";

        internal const string IgnoreAttributeText = $@"// <auto-generated />
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class {IgnoreAttributeFullName} : Attribute
{{
}}
";

        internal const string InjectAttributeFullName = "AutoConstructorInjectAttribute";

        internal const string InjectAttributeText = $@"// <auto-generated />
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class {InjectAttributeFullName} : Attribute
{{
    public {InjectAttributeFullName}(string initializer, string parameterName, Type injectedType)
    {{
        Initializer = initializer;
        ParameterName = parameterName;
        InjectedType = injectedType;
    }}

    public string Initializer {{ get; }}

    public string ParameterName {{ get; }}

    public Type InjectedType {{ get; }}
}}
";

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            context.AddSource(AttributeFullName, SourceText.From(AttributeText, Encoding.UTF8));
            context.AddSource(IgnoreAttributeFullName, SourceText.From(IgnoreAttributeText, Encoding.UTF8));
            context.AddSource(InjectAttributeFullName, SourceText.From(InjectAttributeText, Encoding.UTF8));
            CSharpParseOptions? options = (context.Compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributeText, Encoding.UTF8), options))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(IgnoreAttributeText, Encoding.UTF8), options))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(InjectAttributeText, Encoding.UTF8), options));

            foreach (ClassDeclarationSyntax candidateClass in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(candidateClass.SyntaxTree);
                INamedTypeSymbol? symbol = model.GetDeclaredSymbol(candidateClass);

                if (symbol is not null)
                {
                    string namespaceName = symbol.ContainingNamespace.ToDisplayString();
                    string source = GenerateAutoConstructor(symbol, compilation);
                    context.AddSource($"{namespaceName}.{symbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass.
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private static string GenerateAutoConstructor(INamedTypeSymbol symbol, Compilation compilation)
        {
            INamedTypeSymbol? ignoreAttributeSymbol = compilation.GetTypeByMetadataName(IgnoreAttributeFullName);
            INamedTypeSymbol? injectAttributeSymbol = compilation.GetTypeByMetadataName(InjectAttributeFullName);

            var fields = symbol.GetMembers().OfType<IFieldSymbol>()
                .Where(x => x.CanBeReferencedByName && !x.IsStatic && x.IsReadOnly && !IsInitialized(x) && !HasIgnoreAttribute(x))
                .Select(GetFieldInfo)
                .ToList();

            var constructorParameters = fields.GroupBy(x => x.ParameterName).Select(x => x.First()).ToList();

            var source = new StringBuilder($@"// <auto-generated />
namespace {symbol.ContainingNamespace.ToDisplayString()}
{{
    partial class {symbol.Name}
    {{
        public {symbol.Name}({string.Join(", ", constructorParameters.Select(it => $"{it.Type} {it.ParameterName}"))})
        {{");

            foreach ((string type, string parameterName, string fieldName, string initializer) in fields)
            {
                source.Append($@"
            this.{fieldName} = {initializer};");
            }
            source.Append(@"
        }
    }
}
");
            return source.ToString();

            static bool IsInitialized(IFieldSymbol symbol)
            {
                return (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax)?.Initializer != null;
            }

            bool HasIgnoreAttribute(IFieldSymbol symbol)
            {
                AttributeData? attributeData = symbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(ignoreAttributeSymbol, SymbolEqualityComparer.Default) == true);
                return attributeData is not null;
            }

            (string Type, string ParameterName, string FieldName, string Initializer) GetFieldInfo(IFieldSymbol fieldSymbol)
            {
                ITypeSymbol type = fieldSymbol!.Type;
                string typeDisplay = type.ToDisplayString();
                string parameterName = fieldSymbol.Name.TrimStart('_');
                string initializer = parameterName;

                AttributeData? attributeData = fieldSymbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(injectAttributeSymbol, SymbolEqualityComparer.Default) == true);
                if (attributeData is not null)
                {
                    initializer = attributeData.ConstructorArguments[0].Value?.ToString() ?? "";
                    parameterName = attributeData.ConstructorArguments[1].Value?.ToString() ?? "";
                    typeDisplay = attributeData.ConstructorArguments[2].Value?.ToString() ?? "";
                }

                if (type.TypeKind == TypeKind.Class || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    initializer = $"{initializer} ?? throw new System.ArgumentNullException(nameof({parameterName}))";
                }

                return new(typeDisplay, parameterName, fieldSymbol!.Name, initializer);
            }
        }
    }
}

﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IgnoreOrInjectAttributeOnClassWithoutAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ACONS05";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        $"Remove {Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName}",
        $"{Source.InjectAttributeFullName} and {Source.IgnoreAttributeFullName} have no effect if the class is not annotated with {Source.AttributeFullName}",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/k94ll13nn3/AutoConstructor#{DiagnosticId}",
        WellKnownDiagnosticTags.Unnecessary);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        var fields = symbol.GetMembers().OfType<IFieldSymbol>()
            .Where(x => x.HasAttribute(Source.IgnoreAttributeFullName, context.Compilation) || x.HasAttribute(Source.InjectAttributeFullName, context.Compilation))
            .ToList();

        if (symbol.GetAttribute(Source.AttributeFullName, context.Compilation) is null
            && fields.Count > 0)
        {
            foreach (IFieldSymbol field in fields)
            {
                foreach (string attributeName in new[] { Source.IgnoreAttributeFullName, Source.InjectAttributeFullName })
                {
                    if (field.GetAttribute(attributeName, context.Compilation) is AttributeData attr)
                    {
                        SyntaxReference? propertyTypeIdentifier = attr.ApplicationSyntaxReference;
                        if (propertyTypeIdentifier is not null)
                        {
                            var location = Location.Create(propertyTypeIdentifier.SyntaxTree, propertyTypeIdentifier.Span);
                            var diagnostic = Diagnostic.Create(Rule, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}

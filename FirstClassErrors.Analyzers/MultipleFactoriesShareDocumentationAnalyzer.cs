using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE010 — reports factories in the same type whose <c>[DocumentedBy]</c> point at the same documentation method.
///     One documentation method describes one error, so sharing it means at least one error is mis-documented. Every
///     sharing factory is flagged.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultipleFactoriesShareDocumentationAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.MultipleFactoriesShareDocumentation);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.DocumentedByAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class) { return; }

        Dictionary<string, List<AttributeData>> referencesByTarget = new(StringComparer.Ordinal);

        foreach (ISymbol member in type.GetMembers()) {
            if (member is not IMethodSymbol method) { continue; }
            if (!SymbolFacts.TryGetDocumentedBy(method, symbols.DocumentedByAttribute!, out AttributeData? attribute, out string? targetName)) { continue; }
            if (string.IsNullOrEmpty(targetName)) { continue; }

            if (!referencesByTarget.TryGetValue(targetName!, out List<AttributeData>? references)) {
                references                     = new List<AttributeData>();
                referencesByTarget[targetName!] = references;
            }

            references.Add(attribute!);
        }

        foreach (KeyValuePair<string, List<AttributeData>> entry in referencesByTarget) {
            if (entry.Value.Count < 2) { continue; }

            foreach (AttributeData attribute in entry.Value) {
                Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                                    ?? type.Locations.FirstOrDefault()
                                    ?? Location.None;

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.MultipleFactoriesShareDocumentation, location, entry.Key));
            }
        }
    }

}

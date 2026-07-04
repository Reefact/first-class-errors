using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE006 — reports a <c>[DocumentedBy("X")]</c> whose target method name does not exist on the containing type.
///     The documentation reference is resolved by name at extraction time, so a typo fails silently.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentedByTargetNotFoundAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DocumentedByTargetNotFound);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.DocumentedByAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        IMethodSymbol method = (IMethodSymbol)context.Symbol;

        if (!SymbolFacts.TryGetDocumentedBy(method, symbols.DocumentedByAttribute!, out AttributeData? attribute, out string? targetName)) { return; }
        if (string.IsNullOrEmpty(targetName)) { return; }

        bool exists = method.ContainingType
                            .GetMembers(targetName!)
                            .OfType<IMethodSymbol>()
                            .Any();
        if (exists) { return; }

        Location location = attribute!.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                            ?? method.Locations.FirstOrDefault()
                            ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentedByTargetNotFound, location, targetName));
    }

}

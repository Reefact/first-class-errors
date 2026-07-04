using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE009 — reports a non-private static factory in a <c>[ProvidesErrorsFor]</c> type that returns an
///     <c>Error</c> but carries no <c>[DocumentedBy]</c>. Such an error is never added to the generated catalog.
///     Private methods are treated as helpers and left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ErrorFactoryNotDocumentedAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ErrorFactoryNotDocumented);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Error is null || symbols.ProvidesErrorsForAttribute is null || symbols.DocumentedByAttribute is null) { return; }

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, symbols), SymbolKind.Method);
    }

    private static void Analyze(SymbolAnalysisContext context, KnownSymbols symbols) {
        IMethodSymbol method = (IMethodSymbol)context.Symbol;

        if (!method.IsStatic || method.MethodKind != MethodKind.Ordinary) { return; }
        if (method.DeclaredAccessibility == Accessibility.Private) { return; }
        if (!SymbolFacts.IsOrInheritsFrom(method.ReturnType, symbols.Error!)) { return; }
        if (!SymbolFacts.HasAttribute(method.ContainingType, symbols.ProvidesErrorsForAttribute!)) { return; }
        if (SymbolFacts.HasAttribute(method, symbols.DocumentedByAttribute!)) { return; }

        Location location = method.Locations.FirstOrDefault() ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ErrorFactoryNotDocumented, location, method.Name));
    }

}

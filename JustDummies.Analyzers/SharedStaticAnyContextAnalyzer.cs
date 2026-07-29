using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers;

/// <summary>
///     JD020 — reports an <c>AnyContext</c> held in a static field. It looks maximally deterministic — a literal seed,
///     right there in the source — and is not: the type's own documentation states that sharing one context across
///     threads "costs the replay rather than the values", because interleaved draws make neither the sequence nor the
///     multiset stable. A suite that runs its classes in parallel therefore gets a different value per test per run,
///     from a context that reads as pinned.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharedStaticAnyContextAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.SharedStaticAnyContext);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.AnyContext is null) { return; }

        INamedTypeSymbol anyContext = symbols.AnyContext;

        context.RegisterSymbolAction(symbolContext => Analyze(symbolContext, anyContext), SymbolKind.Field, SymbolKind.Property);
    }

    private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol anyContext) {
        ITypeSymbol? type = context.Symbol switch {
            IFieldSymbol { IsStatic: true } field       => field.Type,
            IPropertySymbol { IsStatic: true } property => property.Type,
            _                                          => null,
        };

        if (type is null || !SymbolEqualityComparer.Default.Equals(type, anyContext)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.SharedStaticAnyContext, context.Symbol.Locations[0], context.Symbol.Name));
    }

}

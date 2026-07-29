using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD008 — reports a value drawn inside a theory's data provider. xUnit evaluates providers at <b>discovery</b>,
///     before any test case runs: the draw happens once for the whole run, outside every seed scope, and every case of
///     the theory shares the one value. The theory reads as if it enumerated arbitrary cases and enumerates a constant.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArbitraryValueInTheoryDataAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ArbitraryValueInTheoryData);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        // Nothing to reason about without xUnit: a data provider is an xUnit concept.
        if (symbols.MemberDataAttribute is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!GeneratorFacts.IsGenerateCall(invocation, symbols.IAny!)) { return; }
        if (!GeneratorFacts.RootsAtAmbientAny(invocation, symbols.Any!)) { return; }
        if (!XunitFacts.IsTheoryDataProvider(context.ContainingSymbol, symbols)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ArbitraryValueInTheoryData, invocation.Syntax.GetLocation()));
    }

}

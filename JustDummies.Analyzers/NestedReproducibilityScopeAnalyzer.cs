using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD018 — reports a reproducibility scope opened inside another one. Both mechanisms report a replay
///     instruction, and nesting makes the outer instruction <b>false</b>: <c>Any.Reproducibly</c> takes its seed from
///     <c>Guid.NewGuid().GetHashCode()</c>, not from the ambient source, so the inner scope draws a brand-new seed on
///     every run whatever the outer one pinned. The failure names a seed that reproduces nothing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedReproducibilityScopeAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NestedReproducibilityScope);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!IsRunner(invocation, symbols)) { return; }

        // The seeded overload pins a chosen seed deliberately; only the seedless form silently overrides the outer
        // instruction with one nobody recorded.
        if (HasSeedArgument(invocation)) { return; }

        if (IsInsideAnotherRunner(invocation, symbols)) {
            Report(context, invocation, "another Any.Reproducibly scope");

            return;
        }

        if (symbols.ReproducibleAttribute is null || symbols.FactAttribute is null) { return; }
        if (context.ContainingSymbol is not IMethodSymbol method) { return; }
        if (!XunitFacts.IsTestMethod(method, symbols.FactAttribute)) { return; }
        if (!XunitFacts.IsCoveredByReproducible(method, symbols.ReproducibleAttribute)) { return; }

        Report(context, invocation, "a [Reproducible] test");
    }

    private static void Report(OperationAnalysisContext context, IInvocationOperation invocation, string outer) {
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.NestedReproducibilityScope, invocation.Syntax.GetLocation(), outer));
    }

    private static bool IsRunner(IInvocationOperation invocation, KnownSymbols symbols) {
        return invocation.TargetMethod.Name is "Reproducibly" or "ReproduciblyAsync"
            && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, symbols.Any);
    }

    private static bool HasSeedArgument(IInvocationOperation invocation) {
        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Name == "seed" && argument.ArgumentKind != ArgumentKind.DefaultValue) { return true; }
        }

        return false;
    }

    private static bool IsInsideAnotherRunner(IInvocationOperation invocation, KnownSymbols symbols) {
        for (IOperation? current = invocation.Parent; current is not null; current = current.Parent) {
            if (current is IInvocationOperation outer && !ReferenceEquals(outer, invocation) && IsRunner(outer, symbols)) { return true; }
        }

        return false;
    }

}

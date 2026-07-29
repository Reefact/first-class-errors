using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD004 — reports a seeding call whose result is thrown away. <c>Any.UseSeed(...)</c> returns the handle that
///     closes the scope: dropping it is the leak the library's own documentation warns about, and it leaves the seed
///     pinned for whatever runs next in the same execution context. <c>Any.WithSeed(...)</c> returns an isolated
///     context and pins nothing at all, so a discarded call is dead code that reads as if it had seeded the run.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedSeedingResultAnalyzer : DiagnosticAnalyzer {

    private const string UseSeedMethodName  = "UseSeed";
    private const string WithSeedMethodName = "WithSeed";

    private const string UseSeedConsequence  = "the scope is never closed, so the seed stays pinned for whatever runs next in the same execution context; hold the handle in a using declaration";
    private const string WithSeedConsequence = "Any.WithSeed returns an isolated context and pins nothing — the ambient Any.* entry points keep drawing unseeded; capture the context and draw from it, or use Any.UseSeed to pin the ambient source";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DiscardedSeedingResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols.Any), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol anyType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, anyType)) { return; }

        string consequence = method.Name switch {
            UseSeedMethodName  => UseSeedConsequence,
            WithSeedMethodName => WithSeedConsequence,
            _                  => string.Empty,
        };

        if (consequence.Length == 0) { return; }
        if (!IsResultDiscarded(invocation)) { return; }

        // A test asserting that the seeding call rejects its argument never opens a scope, so there is nothing to
        // leak; the call being the whole body of a lambda argument is that shape.
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DiscardedSeedingResult, invocation.Syntax.GetLocation(), method.Name, consequence));
    }

    // The same two shapes JD002 reports for ReproduciblyAsync: the call stands alone as a statement, or its result is
    // explicitly discarded.
    private static bool IsResultDiscarded(IInvocationOperation invocation) {
        return invocation.Parent switch {
            IExpressionStatementOperation                            => true,
            ISimpleAssignmentOperation { Target: IDiscardOperation } => true,
            _                                                        => false,
        };
    }

}

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD025 — reports a constant listed twice in the same pool. <c>Any.OneOf(a, b, a)</c> is deduplicated when the
///     generator is built, so the pool is one value smaller than it reads, and nothing anywhere says so.
/// </summary>
/// <remarks>
///     Weighting is the reading this rule exists to refuse: listing a value twice looks like "draw this one more
///     often", and the library declines to weight a pool on purpose. The consequence surfaces somewhere else
///     entirely — a distinct collection over the pool gates against the real distinct count and names a number the
///     author cannot find in their source.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicatePoolValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DuplicatePoolValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name is not ("OneOf" or "ElementOf")) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out _, out IInvocationOperation? factory) || factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        HashSet<object?> seen = [];

        foreach (IOperation element in PoolElements(invocation)) {
            // The element's own constant, not the unwrapped operand's: the conversion to the pool's element type is
            // what decides whether two literals written differently are the same pooled value.
            Optional<object?> constant = element.ConstantValue;

            // One unfoldable element and the pool stops being knowable: a later duplicate of THAT value would go
            // unseen, and reporting the ones this walk can see would claim a completeness the walk does not have.
            if (!constant.HasValue) { return; }
            if (seen.Add(constant.Value)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.DuplicatePoolValue, element.Syntax.GetLocation()));

            return;
        }
    }

    private static IEnumerable<IOperation> PoolElements(IInvocationOperation invocation) {
        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.ParamArray) {
                if (argument.Value is IArrayCreationOperation { Initializer: { } initializer }) {
                    foreach (IOperation element in initializer.ElementValues) { yield return element; }
                }

                continue;
            }

            // ElementOf takes a materialized collection; only an inline collection expression or array creation is
            // knowable here — anything held in a variable is not this rule's business.
            if (GeneratorFacts.Unwrap(argument.Value) is IArrayCreationOperation { Initializer: { } inline }) {
                foreach (IOperation element in inline.ElementValues) { yield return element; }
            }
        }
    }

}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD006 — reports a generator-returning call whose result is thrown away. A generator is an <b>immutable recipe</b>:
///     every constraint returns a new generator rather than mutating the receiver, so <c>numbers.NonEmpty();</c> looks
///     like it constrains <c>numbers</c> and silently constrains nothing. The declared invariant is lost, the test keeps
///     drawing from the wider domain, and it fails only on the run that happens to draw outside it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedGeneratorResultAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DiscardedGeneratorResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        INamedTypeSymbol iAny = symbols.IAny;

        context.RegisterOperationAction(operationContext => Analyze(operationContext, iAny), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!GeneratorFacts.IsGenerator(invocation.TargetMethod.ReturnType, iAny)) { return; }
        if (!IsResultDiscarded(invocation)) { return; }

        // A test asserting that the constraint throws writes the illegal call as the whole body of a lambda argument;
        // reporting there would fight the suite that documents the conflict.
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DiscardedGeneratorResult, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name));
    }

    // Only the bare statement, deliberately — not `_ = generator.NonEmpty();`. What makes this rule worth an entry is
    // that the mistake is *silent*: the call reads as if it mutated the receiver. An explicit discard cannot be
    // misread that way, and it is how a test that only wants the construction to throw spells its intent (see
    // JustDummies.PropertyTests/PatternRoundTripProperties.cs). JD002 and JD004 report `_ =` because discarding is
    // never right there; here it is a legitimate, self-documenting choice.
    private static bool IsResultDiscarded(IInvocationOperation invocation) {
        return invocation.Parent is IExpressionStatementOperation;
    }

}

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD026 — reports a relative-URI chain that describes the empty reference:
///     <c>Any.Uri().Relative().WithPathSegments(0)</c> with no query, no fragment and no root.
/// </summary>
/// <remarks>
///     The whole point is <i>when</i> the library reports it. Every other unsatisfiable chain throws at the arrange
///     line; this one cannot, because emptiness is only settled once the components have been drawn — so it throws
///     inside <c>Generate()</c>, at act time, with a stack pointing at the code under test rather than at the
///     declaration that is wrong. Moving it to build time is the entire value of the rule.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyRelativeUriAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyRelativeUri);

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

        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "Uri") { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        // Only the relative family can render empty. Web, WebSocket and FTP carry an authority, so a path of zero
        // segments still renders as "/" and the reference stays valid.
        bool                 relative = false;
        IInvocationOperation? zeroSegments = null;

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                case "Relative": relative = true; break;

                // Any one of these three saves the reference from being empty.
                case "WithQuery" or "WithFragment" or "Rooted": return;

                case "WithPathSegments" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int count):
                    if (count != 0) { return; }

                    zeroSegments = constraint;

                    break;
            }
        }

        if (!relative || zeroSegments is null) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyRelativeUri, zeroSegments.Syntax.GetLocation()));
    }

}

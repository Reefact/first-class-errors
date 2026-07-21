using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE022 — reports an <c>Outcome.Try&lt;..., TException&gt;</c> whose caught type is, or derives from,
///     <see cref="System.OperationCanceledException" />. <c>Try</c> guards its catch with
///     <c>when (exception is not OperationCanceledException)</c>, so binding <c>TException</c> to a cancellation type
///     makes the catch unreachable: the filter is a contradiction, the mapper never runs, and no <c>Outcome</c> is
///     produced. It is always a mistake and, unlike an unreachable ordinary catch, the compiler does not flag it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryCatchesCancellationAnalyzer : DiagnosticAnalyzer {

    private const string OperationCanceledExceptionMetadataName = "System.OperationCanceledException";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TryCatchesCancellation);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? outcomeType = context.Compilation.GetTypeByMetadataName(OutcomeTryFacts.OutcomeMetadataName);
        if (outcomeType is null) { return; }

        INamedTypeSymbol? cancellationType = context.Compilation.GetTypeByMetadataName(OperationCanceledExceptionMetadataName);
        if (cancellationType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, outcomeType, cancellationType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol outcomeType, INamedTypeSymbol cancellationType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!OutcomeTryFacts.TryGetCaughtExceptionType(invocation, outcomeType, out ITypeSymbol? caughtType)) { return; }
        if (!SymbolFacts.IsOrInheritsFrom(caughtType!, cancellationType)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TryCatchesCancellation,
            invocation.Syntax.GetLocation(),
            caughtType!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

}

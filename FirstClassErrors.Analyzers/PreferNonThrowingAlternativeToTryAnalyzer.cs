using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE021 — reports a synchronous, value-producing <c>Outcome.Try</c> whose operation is a lambda that only calls a
///     single throwing member which already has a non-throwing counterpart (a <c>bool TryParse(..., out T)</c> or
///     <c>TryCreate</c> of matching shape) available for the target framework being compiled. There is nothing to catch:
///     the caller should map the counterpart's <c>false</c> result to an error instead. On by default as a warning
///     (advisory: suppress where the counterpart is not a true inverse of the wrapped call).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferNonThrowingAlternativeToTryAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PreferNonThrowingAlternativeToTry);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? outcomeType = context.Compilation.GetTypeByMetadataName(OutcomeTryFacts.OutcomeMetadataName);
        if (outcomeType is null) { return; }

        INamedTypeSymbol? funcOfTType = context.Compilation.GetTypeByMetadataName(TryAlternativeFacts.FuncOfTMetadataName);
        if (funcOfTType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, outcomeType, funcOfTType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol outcomeType, INamedTypeSymbol funcOfTType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!OutcomeTryFacts.TryGetCaughtExceptionType(invocation, outcomeType, out _)) { return; }
        if (!TryAlternativeFacts.TryGetPreferredAlternative(invocation, funcOfTType, out string throwingDisplay, out string alternativeDisplay)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.PreferNonThrowingAlternativeToTry,
            invocation.Syntax.GetLocation(),
            throwingDisplay,
            alternativeDisplay));
    }

}

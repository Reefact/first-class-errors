using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE019 — reports an <c>Outcome.Try&lt;..., TException&gt;</c> whose caught type is <see cref="System.Exception" />.
///     <c>Try</c> is meant to catch the single exception that denotes an anticipated failure; catching the near-root type
///     also swallows unexpected bugs and turns them into anticipated errors, defeating the purpose of <c>Outcome</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TryCatchesTooBroadlyAnalyzer : DiagnosticAnalyzer {

    private const string ExceptionMetadataName = "System.Exception";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TryCatchesTooBroadly);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? outcomeType = context.Compilation.GetTypeByMetadataName(OutcomeTryFacts.OutcomeMetadataName);
        if (outcomeType is null) { return; }

        INamedTypeSymbol? exceptionType = context.Compilation.GetTypeByMetadataName(ExceptionMetadataName);
        if (exceptionType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, outcomeType, exceptionType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol outcomeType, INamedTypeSymbol exceptionType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!OutcomeTryFacts.TryGetCaughtExceptionType(invocation, outcomeType, out ITypeSymbol? caughtType)) { return; }
        if (!SymbolEqualityComparer.Default.Equals(caughtType, exceptionType)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TryCatchesTooBroadly,
            invocation.Syntax.GetLocation(),
            caughtType!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

}

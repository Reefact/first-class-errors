using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE005 — reports a literal error code drawn from a small denylist of catch-all words (ERROR, INVALID, FAILED…)
///     that carry no diagnostic value. Opt-in: disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TooGenericErrorCodeAnalyzer : DiagnosticAnalyzer {

    private static readonly ImmutableHashSet<string> GenericCodes = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "ERROR", "INVALID", "FAILED", "FAILURE", "UNKNOWN", "EXCEPTION", "BAD", "WRONG", "GENERIC", "PROBLEM");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TooGenericErrorCode);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? errorCodeType = context.Compilation.GetTypeByMetadataName(ErrorCodeFacts.ErrorCodeMetadataName);
        if (errorCodeType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, errorCodeType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol errorCodeType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        IOperation? argument = ErrorCodeFacts.GetCreateArgument(invocation, errorCodeType);
        if (argument is null) { return; }

        if (!ErrorCodeFacts.TryGetNonEmptyLiteralCode(argument, out string code)) { return; }
        if (!GenericCodes.Contains(code.Trim())) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.TooGenericErrorCode, argument.Syntax.GetLocation(), code));
    }

}

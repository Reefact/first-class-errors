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

    private const string ErrorCodeMetadataName = "FirstClassErrors.ErrorCode";
    private const string CreateMethodName      = "Create";

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
        INamedTypeSymbol? errorCodeType = context.Compilation.GetTypeByMetadataName(ErrorCodeMetadataName);
        if (errorCodeType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, errorCodeType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol errorCodeType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (!method.IsStatic || method.Name != CreateMethodName) { return; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, errorCodeType)) { return; }
        if (invocation.Arguments.Length != 1) { return; }

        IOperation argument = invocation.Arguments[0].Value;
        if (argument.ConstantValue.Value is not string code) { return; }
        if (!GenericCodes.Contains(code.Trim())) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.TooGenericErrorCode, argument.Syntax.GetLocation(), code));
    }

}

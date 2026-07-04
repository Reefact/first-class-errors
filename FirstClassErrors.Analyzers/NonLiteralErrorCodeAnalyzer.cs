using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE003 — reports <c>ErrorCode.Create(x)</c> where the argument is not a compile-time constant. Such a code is a
///     blind spot for FCE001 (duplicate detection). Opt-in: disabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonLiteralErrorCodeAnalyzer : DiagnosticAnalyzer {

    private const string ErrorCodeMetadataName = "FirstClassErrors.ErrorCode";
    private const string CreateMethodName      = "Create";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NonLiteralErrorCode);

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
        if (argument.ConstantValue.HasValue) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.NonLiteralErrorCode, argument.Syntax.GetLocation()));
    }

}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE016 — reports a call to <c>Error.ToException()</c> whose result is discarded (the call stands alone as a
///     statement). <c>ToException()</c> only builds the exception; without a <c>throw</c> (or capturing the result)
///     nothing happens.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedToExceptionResultAnalyzer : DiagnosticAnalyzer {

    private const string ErrorMetadataName     = "FirstClassErrors.Error";
    private const string ToExceptionMethodName = "ToException";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UnusedToExceptionResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? errorType = context.Compilation.GetTypeByMetadataName(ErrorMetadataName);
        if (errorType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, errorType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol errorType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != ToExceptionMethodName || method.Parameters.Length != 0) { return; }
        if (!SymbolFacts.IsOrInheritsFrom(method.ContainingType, errorType)) { return; }

        // The result is used unless the invocation stands alone as an expression statement.
        if (invocation.Parent is not IExpressionStatementOperation) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.UnusedToExceptionResult, invocation.Syntax.GetLocation()));
    }

}

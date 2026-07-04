using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     FCE002 — reports <c>ErrorCode.Create("")</c> (or a whitespace / <c>null</c> literal), which throws an
///     <see cref="System.ArgumentException" /> at runtime. Only literal arguments are inspected; a non-literal code is
///     out of scope here (see FCE003).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyErrorCodeAnalyzer : DiagnosticAnalyzer {

    private const string ErrorCodeMetadataName = "FirstClassErrors.ErrorCode";
    private const string CreateMethodName      = "Create";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyErrorCode);

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

        IOperation       argument = invocation.Arguments[0].Value;
        Optional<object?> constant = argument.ConstantValue;

        // A non-constant argument cannot be judged statically; FCE003 flags that separately.
        if (!constant.HasValue) { return; }
        if (constant.Value is string value && !string.IsNullOrWhiteSpace(value)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyErrorCode, argument.Syntax.GetLocation()));
    }

}

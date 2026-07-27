using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD002 — reports a call to <c>Any.ReproduciblyAsync(...)</c> whose returned <see cref="System.Threading.Tasks.Task" />
///     is discarded (the call stands alone as a statement, or is assigned to <c>_</c>). The task faults with the body's
///     exception; discarding it lets a failing test pass green. Await it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedReproduciblyAsyncResultAnalyzer : DiagnosticAnalyzer {

    private const string AnyMetadataName             = "JustDummies.Any";
    private const string ReproduciblyAsyncMethodName = "ReproduciblyAsync";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DiscardedReproduciblyAsyncResult);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        INamedTypeSymbol? anyType = context.Compilation.GetTypeByMetadataName(AnyMetadataName);
        if (anyType is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, anyType), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol anyType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != ReproduciblyAsyncMethodName || !SymbolEqualityComparer.Default.Equals(method.ContainingType, anyType)) { return; }
        if (!IsResultDiscarded(invocation)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DiscardedReproduciblyAsyncResult, invocation.Syntax.GetLocation()));
    }

    // The result is thrown away either when the call stands alone as a statement or when it is explicitly discarded
    // (`_ = Any.ReproduciblyAsync(...);`). Either way the body's failures are lost.
    private static bool IsResultDiscarded(IInvocationOperation invocation) {
        return invocation.Parent switch {
            IExpressionStatementOperation                            => true,
            ISimpleAssignmentOperation { Target: IDiscardOperation } => true,
            _                                                        => false,
        };
    }

}

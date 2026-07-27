using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD001 — reports an <c>async</c> lambda passed to the synchronous <c>Any.Reproducibly(Action)</c>. Bound to an
///     <see cref="System.Action" /> it becomes <c>async void</c>, so the body's failures after the first <c>await</c>
///     escape the reproducible scope entirely and never fail the test. Use <c>Any.ReproduciblyAsync(Func&lt;Task&gt;)</c>
///     and await it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncBodyPassedToReproduciblyAnalyzer : DiagnosticAnalyzer {

    private const string AnyMetadataName        = "JustDummies.Any";
    private const string ReproduciblyMethodName = "Reproducibly";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AsyncBodyPassedToReproducibly);

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

        if (method.Name != ReproduciblyMethodName || !SymbolEqualityComparer.Default.Equals(method.ContainingType, anyType)) { return; }

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (TryGetAsyncLambda(argument.Value, out IAnonymousFunctionOperation? lambda)) {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.AsyncBodyPassedToReproducibly, lambda!.Syntax.GetLocation()));
            }
        }
    }

    // An async lambda bound to the Action parameter is wrapped in a delegate creation; unwrap it and read the
    // anonymous function's own IsAsync (its return is void — that is precisely the async-void hazard).
    private static bool TryGetAsyncLambda(IOperation value, out IAnonymousFunctionOperation? lambda) {
        IOperation inner = value is IDelegateCreationOperation delegateCreation ? delegateCreation.Target : value;
        if (inner is IAnonymousFunctionOperation { Symbol.IsAsync: true } anonymous) {
            lambda = anonymous;

            return true;
        }

        lambda = null;

        return false;
    }

}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD003 — reports the two asynchronous bodies <c>Any.Reproducibly(Action)</c> accepts that JD001 does not see: a
///     <b>synchronous</b> lambda whose body is an awaitable the call then drops, and an <c>async void</c> method passed
///     as a method group. Both reproduce JD001's damage — the reproducible scope returns before the body's assertions
///     run, and their failures never reach the test — while compiling without a single diagnostic, <c>CS4014</c>
///     included, because the enclosing lambda is not itself <c>async</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AwaitableBodyPassedToReproduciblyAnalyzer : DiagnosticAnalyzer {

    private const string ReproduciblyMethodName = "Reproducibly";
    private const string GetAwaiterMethodName   = "GetAwaiter";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AwaitableBodyPassedToReproducibly);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols.Any), OperationKind.Invocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions",
                                                     Justification =
                                                         "The rule asks for Select(argument => argument.Value). The loop unwraps a delegate creation before testing what it found and " +
                                                         "reports on the lambda body, so the projection would rename the loop variable away from what it is without removing a single " +
                                                         "step: `argument` is an IArgumentOperation, and the unwrapping still has to happen inside.")]
    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol anyType) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != ReproduciblyMethodName || !SymbolEqualityComparer.Default.Equals(method.ContainingType, anyType)) { return; }

        foreach (IArgumentOperation argument in invocation.Arguments) {
            IOperation value = argument.Value is IDelegateCreationOperation delegateCreation ? delegateCreation.Target : argument.Value;

            if (value is IAnonymousFunctionOperation { Symbol.IsAsync: false } lambda) {
                ReportDroppedAwaitables(context, lambda.Body);
            } else if (IsAsyncVoidMethodReference(value)) {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.AwaitableBodyPassedToReproducibly, value.Syntax.GetLocation()));
            }
        }
    }

    // A synchronous lambda bound to the Action parameter whose body produces an awaitable nobody awaits. JD001 reads
    // the lambda's own IsAsync and so passes this by; here the lambda is synchronous and it is the *body's* task that
    // is dropped — the same silent green, through the door JD001 leaves open.
    private static void ReportDroppedAwaitables(OperationAnalysisContext context, IOperation node) {
        // A nested lambda or local function has its own binding and its own author intent; a fire-and-forget there is
        // not this call's business.
        if (node is IAnonymousFunctionOperation or ILocalFunctionOperation) { return; }

        if (node is IExpressionStatementOperation statement && IsAwaitable(statement.Operation.Type)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.AwaitableBodyPassedToReproducibly, statement.Operation.Syntax.GetLocation()));

            return;
        }

        foreach (IOperation child in node.ChildOperations) { ReportDroppedAwaitables(context, child); }
    }

    // `async void` reaches Reproducibly as a method group: the delegate creation binds it to Action with no warning,
    // and the body's post-await exception escapes the scope's try/catch exactly as JD001's async lambda would.
    private static bool IsAsyncVoidMethodReference(IOperation value) {
        return value is IMethodReferenceOperation { Method: { IsAsync: true, ReturnsVoid: true } };
    }

    // Awaitability is a shape, not a type: anything exposing GetAwaiter() qualifies, which covers Task, Task<T>,
    // ValueTask, ValueTask<T> and a consumer's own awaitable without naming any of them.
    private static bool IsAwaitable(ITypeSymbol? type) {
        if (type is null || type.SpecialType == SpecialType.System_Void) { return false; }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType) {
            if (current.GetMembers(GetAwaiterMethodName)
                       .Any(member => member is IMethodSymbol { Parameters.IsEmpty: true, DeclaredAccessibility: Accessibility.Public })) { return true; }
        }

        return false;
    }

}

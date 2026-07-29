using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD007 — reports a value drawn during a <c>[Reproducible]</c> test class's <b>construction</b>, which xUnit runs
///     before the adapter opens the seed scope. The draw comes from the unseeded ambient source, so the seed the
///     failure reports replays the body and not the arrangement: the reader pins it, the run still differs, and the
///     test looks unreplayable while advertising that it is not.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DrawOutsideThePinnedScopeAnalyzer : DiagnosticAnalyzer {

    private const string InitializeAsyncMethodName = "InitializeAsync";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DrawOutsideThePinnedScope);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null || symbols.ReproducibleAttribute is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!GeneratorFacts.IsGenerateCall(invocation, symbols.IAny!)) { return; }
        if (!GeneratorFacts.RootsAtAmbientAny(invocation, symbols.Any!)) { return; }

        ISymbol containing = context.ContainingSymbol;
        if (!RunsBeforeTheScopeOpens(containing, out string? phase)) { return; }
        if (!XunitFacts.IsCoveredByReproducible(containing, symbols.ReproducibleAttribute!)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DrawOutsideThePinnedScope, invocation.Syntax.GetLocation(), phase));
    }

    // xUnit constructs the test-class instance, and awaits IAsyncLifetime.InitializeAsync, before it runs the
    // BeforeAfterTestAttribute hooks the adapter pins the seed from. Everything drawn there is outside the scope.
    private static bool RunsBeforeTheScopeOpens(ISymbol symbol, out string? phase) {
        phase = null;

        if (symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false }) {
            phase = "a field initializer";

            return true;
        }

        if (symbol is not IMethodSymbol method || method.IsStatic) { return false; }

        if (method.MethodKind == MethodKind.Constructor) {
            phase = "the test class constructor";

            return true;
        }

        if (method.Name == InitializeAsyncMethodName) {
            phase = "InitializeAsync";

            return true;
        }

        return false;
    }

}

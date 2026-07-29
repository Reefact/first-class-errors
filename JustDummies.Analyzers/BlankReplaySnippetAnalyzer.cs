using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD021 — reports a blank replay snippet handed to <c>Any.UseSeed(int, string)</c>. The guard rejects it at run
///     time, and because that scope is normally opened from a test-framework adapter's hook, the throw surfaces as an
///     infrastructure failure on <b>every test in the suite</b> rather than as one failing assertion — a
///     disproportionately expensive way to learn about a typo the compiler can already see.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlankReplaySnippetAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankReplaySnippet);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null) { return; }

        INamedTypeSymbol any = symbols.Any;

        context.RegisterOperationAction(operationContext => Analyze(operationContext, any), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol any) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (invocation.TargetMethod.Name != "UseSeed") { return; }
        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, any)) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Name != "replaySnippet") { continue; }
            if (!ConstantFacts.TryGetString(argument.Value, out string snippet)) { continue; }
            if (snippet.Trim().Length != 0) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankReplaySnippet, argument.Value.Syntax.GetLocation()));

            return;
        }
    }

}

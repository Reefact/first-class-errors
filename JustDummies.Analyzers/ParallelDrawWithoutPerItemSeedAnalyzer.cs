using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD022 — reports an ambient draw inside a <c>Parallel</c> work item that opens no seed scope of its own. The
///     ambient scope flows into every worker, so one shared scope reaches them all and the draws interleave: the
///     sequence is stable for nobody and the run replays nothing. The library's own documentation names this shape —
///     a scope opened <i>inside</i> the loop body gives each unit of work its own sequence, and the whole run replays.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParallelDrawWithoutPerItemSeedAnalyzer : DiagnosticAnalyzer {

    private const string ParallelMetadataName = "System.Threading.Tasks.Parallel";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ParallelDrawWithoutPerItemSeed);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        INamedTypeSymbol? parallel = context.Compilation.GetTypeByMetadataName(ParallelMetadataName);
        if (parallel is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols, parallel), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols, INamedTypeSymbol parallel) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!GeneratorFacts.IsGenerateCall(invocation, symbols.IAny!)) { return; }
        if (!GeneratorFacts.RootsAtAmbientAny(invocation, symbols.Any!)) { return; }

        IAnonymousFunctionOperation? body = EnclosingParallelBody(invocation, parallel);
        if (body is null) { return; }
        if (OpensASeedScope(body, symbols.Any!)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ParallelDrawWithoutPerItemSeed, invocation.Syntax.GetLocation()));
    }

    // The innermost lambda that is an argument to a Parallel.* call — the work item.
    private static IAnonymousFunctionOperation? EnclosingParallelBody(IOperation operation, INamedTypeSymbol parallel) {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent) {
            if (current is not IAnonymousFunctionOperation lambda) { continue; }

            for (IOperation? outer = lambda.Parent; outer is not null; outer = outer.Parent) {
                if (outer is IInvocationOperation call) {
                    return SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, parallel) ? lambda : null;
                }

                if (outer is IAnonymousFunctionOperation) { break; }
            }
        }

        return null;
    }

    private static bool OpensASeedScope(IOperation node, INamedTypeSymbol any) {
        if (node is IInvocationOperation call
         && call.TargetMethod.Name == "UseSeed"
         && SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, any)) { return true; }

        foreach (IOperation child in node.ChildOperations) {
            if (OpensASeedScope(child, any)) { return true; }
        }

        return false;
    }

}

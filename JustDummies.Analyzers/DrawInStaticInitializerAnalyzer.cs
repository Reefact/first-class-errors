using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD009 — reports a value drawn in a static field initializer or a static constructor. The type initializer runs
///     once, lazily, on whichever test first touches the type: one value is shared by every test in the class, drawn
///     under whatever seed that first test happened to pin, and replayable from none of them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DrawInStaticInitializerAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DrawInStaticInitializer);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!GeneratorFacts.IsGenerateCall(invocation, symbols.IAny!)) { return; }
        if (!GeneratorFacts.RootsAtAmbientAny(invocation, symbols.Any!)) { return; }
        if (!IsStaticInitialization(context.ContainingSymbol)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.DrawInStaticInitializer, invocation.Syntax.GetLocation()));
    }

    private static bool IsStaticInitialization(ISymbol symbol) {
        return symbol switch {
            IFieldSymbol { IsStatic: true }                                       => true,
            IPropertySymbol { IsStatic: true }                                    => true,
            IMethodSymbol { IsStatic: true, MethodKind: MethodKind.StaticConstructor } => true,
            _                                                                     => false,
        };
    }

}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD012 — reports a choice pool built from generators rather than values. <c>Any.OneOf(Any.Int32(), Any.Int32())</c>
///     compiles and infers <c>T = AnyInt32</c>, so the pool holds recipes and drawing from it yields a recipe rather
///     than a number. The surface is inconsistent about it, which is what makes it a trap: pooled generators of
///     different types fail inference and are caught by the compiler, while two of the same type sail through.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratorPooledAsValueAnalyzer : DiagnosticAnalyzer {

    private const string OneOfMethodName     = "OneOf";
    private const string ElementOfMethodName = "ElementOf";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GeneratorPooledAsValue);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null || symbols.AnyContext is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name is not (OneOfMethodName or ElementOfMethodName)) { return; }
        if (!IsChoiceFactory(method, symbols)) { return; }
        if (method.TypeArguments.Length != 1) { return; }

        if (!GeneratorFacts.IsGenerator(method.TypeArguments[0], symbols.IAny!)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorPooledAsValue, invocation.Syntax.GetLocation(), method.Name));
    }

    // Both entry points are mirrored on Any and AnyContext (SurfaceParityTests enforces the mirror), so the rule must
    // recognise either receiver or it would fire on half the surface.
    private static bool IsChoiceFactory(IMethodSymbol method, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Any)
            || SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.AnyContext);
    }

}

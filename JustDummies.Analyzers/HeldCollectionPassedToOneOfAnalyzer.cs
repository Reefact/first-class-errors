using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD013 — reports a held collection handed to <c>Any.OneOf</c>. The parameter is <c>params T[]</c>, so a single
///     <c>List&lt;Order&gt;</c> argument binds <c>T = List&lt;Order&gt;</c> and yields a pool of exactly <b>one</b>:
///     every draw returns the same list, and the "arbitrary order" the test claims to exercise never varies.
///     <c>Any.ElementOf</c> is the entry point that takes a collection and draws from its elements.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HeldCollectionPassedToOneOfAnalyzer : DiagnosticAnalyzer {

    private const string OneOfMethodName = "OneOf";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.HeldCollectionPassedToOneOf);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.AnyContext is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != OneOfMethodName) { return; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Any)
         && !SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.AnyContext)) { return; }

        // An explicit type argument states the intent — Any.OneOf<List<Order>>(orders) really does want a pool of one.
        if (!IsTypeArgumentInferred(invocation)) { return; }
        if (method.TypeArguments.Length != 1) { return; }

        if (!TryGetSingleExpandedArgument(invocation, out IOperation? single)) { return; }
        if (!IsHeldCollection(single!.Type, method.TypeArguments[0])) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.HeldCollectionPassedToOneOf, invocation.Syntax.GetLocation(), method.TypeArguments[0].ToDisplayString()));
    }

    private static bool IsTypeArgumentInferred(IInvocationOperation invocation) {
        return invocation.Syntax is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax { Expression: Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax { Name: not Microsoft.CodeAnalysis.CSharp.Syntax.GenericNameSyntax } };
    }

    // The params array was built by the compiler from exactly one argument — the shape that silently makes a pool of one.
    private static bool TryGetSingleExpandedArgument(IInvocationOperation invocation, out IOperation? single) {
        single = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { return false; }
            if (initializer.ElementValues.Length != 1) { return false; }

            single = GeneratorFacts.Unwrap(initializer.ElementValues[0]);

            return true;
        }

        return false;
    }

    // A string is IEnumerable<char>, so a single-string pool would otherwise be reported — and it is perfectly normal.
    private static bool IsHeldCollection(ITypeSymbol? argumentType, ITypeSymbol inferred) {
        if (argumentType is null) { return false; }
        if (argumentType.SpecialType == SpecialType.System_String) { return false; }
        if (inferred.TypeKind == TypeKind.TypeParameter) { return false; }

        foreach (INamedTypeSymbol implemented in argumentType.AllInterfaces) {
            if (implemented.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) { return true; }
        }

        return false;
    }

}

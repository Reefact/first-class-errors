using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD028 — reports distinctness declared over an element type that has no value equality. The default comparer
///     falls back to reference equality, every generated element is a new instance, and the requirement is therefore
///     satisfied by construction: the collection can hold the same value several times, which is exactly what the
///     declaration asks it not to.
/// </summary>
/// <remarks>
///     The library cannot report this, and that is the point: from its side the requirement is met, the draws are
///     pairwise unequal, and there is nothing to complain about. Only the element type's equality tells the two apart,
///     and it is visible here. Measured on the built library: six "distinct" elements over a two-value domain came back
///     as <c>[1, 1, 1, 2, 1, 2]</c>, green. Give the type value equality — a record, an <c>IEquatable</c>
///     implementation, an <c>Equals</c> override — or pass an explicit comparer.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InertDistinctnessAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.InertDistinctness);

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

        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        string factoryName = factory.TargetMethod.Name;
        if (factoryName is not ("ListOf" or "ArrayOf" or "SequenceOf" or "SetOf" or "DictionaryOf")) { return; }

        IInvocationOperation? declaration = DistinctnessDeclaration(factory, factoryName, constraints);
        if (declaration is null) { return; }
        if (factory.TargetMethod.TypeArguments.Length == 0 || factory.Arguments.Length == 0) { return; }

        // A dictionary is distinct on its KEYS, which is its first type argument — the same position the collection
        // generators use for their element.
        ITypeSymbol element = factory.TargetMethod.TypeArguments[0];
        if (!EqualityFacts.UsesReferenceEquality(element)) { return; }
        if (!ProducesFreshInstances(factory.Arguments[0].Value)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.InertDistinctness, declaration.Syntax.GetLocation(), element.Name));
    }

    /// <summary>
    ///     Where distinctness was declared: the factory itself for the set-like generators, otherwise the first
    ///     <c>Distinct()</c> in the chain. <c>null</c> when none was declared, and also when one was but a comparer
    ///     came with it.
    /// </summary>
    /// <remarks>
    ///     The two <c>null</c> answers are deliberately the same answer, because the rule does the same thing with
    ///     them: stand down. A comparer supplied to the factory or to <c>Distinct()</c> answers the equality question
    ///     itself, whatever the element type does — so there is nothing inert to report, exactly as when distinctness
    ///     was never asked for.
    /// </remarks>
    private static IInvocationOperation? DistinctnessDeclaration(IInvocationOperation factory, string factoryName, IReadOnlyList<IInvocationOperation> constraints) {
        bool                  impliedByFactory = factoryName is "SetOf" or "DictionaryOf";
        IInvocationOperation? declaration      = impliedByFactory ? factory : null;

        if (impliedByFactory && CarriesComparer(factory)) { return null; }

        foreach (IInvocationOperation constraint in constraints) {
            if (constraint.TargetMethod.Name != "Distinct") { continue; }
            if (CarriesComparer(constraint)) { return null; }

            declaration ??= constraint;
        }

        return declaration;
    }

    /// <summary>
    ///     Whether the element generator provably hands back a <b>new</b> instance on every draw, which is what makes
    ///     reference equality unable to bind.
    /// </summary>
    /// <remarks>
    ///     The narrowing dogfooding forced. A pool generator returns the very references it was handed, so
    ///     <c>Any.SetOf(Any.OneOf(first, second))</c> is a legal and meaningful declaration: drawing <c>first</c> twice
    ///     yields the same reference and the set rejects it exactly as asked. The rule's premise — every element is a
    ///     new instance — holds only where the chain builds the value here, so that is the only shape it claims.
    /// </remarks>
    private static bool ProducesFreshInstances(IOperation element) {
        if (GeneratorFacts.Unwrap(element) is not IInvocationOperation invocation) { return false; }
        if (invocation.TargetMethod.Name is not ("As" or "Combine")) { return false; }
        if (invocation.Arguments.Length == 0) { return false; }

        IArgumentOperation last = invocation.Arguments[invocation.Arguments.Length - 1];
        if (GeneratorFacts.Unwrap(last.Value) is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation builder }) { return false; }

        foreach (IOperation statement in builder.Body.Operations) {
            if (statement is IReturnOperation { ReturnedValue: { } value } && GeneratorFacts.Unwrap(value) is IObjectCreationOperation) { return true; }
        }

        return false;
    }

    private static bool CarriesComparer(IInvocationOperation invocation) {
        return invocation.TargetMethod.Parameters.Any(parameter => parameter.Type is INamedTypeSymbol { Name: "IEqualityComparer" });
    }

}

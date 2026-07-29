using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD016 — reports a collection chain whose constant count constraints cannot all hold, or which asks for more
///     distinct elements than its element generator can produce. Both throw at declaration time, so this moves an
///     arrange-time red to a build-time red — worth it because the chain usually sits in a helper several call frames
///     away from the test that dies on it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionConstraintsAdmitNoValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CollectionConstraintsAdmitNoValue);

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
        if (factory is null || !IsCollectionFactory(factory.TargetMethod.Name)) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        int?  exact    = null;
        int?  minimum  = null;
        int?  maximum  = null;
        bool  distinct = factory.TargetMethod.Name is "SetOf" or "DictionaryOf";
        int   contained = 0;
        IOperation? at  = null;

        foreach (IInvocationOperation constraint in constraints) {
            at = constraint;

            switch (constraint.TargetMethod.Name) {
                case "Empty":       exact   = 0;                                                          break;
                case "NonEmpty":    minimum = System.Math.Max(minimum ?? 0, 1);                           break;
                case "Distinct":    distinct = true;                                                      break;
                case "Containing" or "ContainingKey" or "ContainingEntry": contained++;                   break;

                case "WithCount" when TryConstant(constraint, out int count):      exact   = count;       break;
                case "WithMinCount" when TryConstant(constraint, out int min):     minimum = System.Math.Max(minimum ?? 0, min); break;
                case "WithMaxCount" when TryConstant(constraint, out int max):     maximum = System.Math.Min(maximum ?? int.MaxValue, max); break;

                case "WithCountBetween" when constraint.Arguments.Length == 2
                                          && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int low)
                                          && ConstantFacts.TryGetInt32(constraint.Arguments[1].Value, out int high):
                    minimum = System.Math.Max(minimum ?? 0, low);
                    maximum = System.Math.Min(maximum ?? int.MaxValue, high);

                    break;
            }
        }

        if (at is null) { return; }

        int effectiveMin = System.Math.Max(minimum ?? 0, exact ?? 0);
        int effectiveMax = System.Math.Min(maximum ?? int.MaxValue, exact ?? int.MaxValue);

        if (effectiveMin > effectiveMax) {
            Report(context, at, $"the declared counts require at least {effectiveMin} element(s) and at most {effectiveMax}");

            return;
        }

        if (contained > effectiveMax) {
            Report(context, at, $"{contained} element(s) are required to be contained, which cannot fit in a collection of at most {effectiveMax}");

            return;
        }

        // The cardinality gate (ADR-0013): a distinct collection cannot hold more elements than its element
        // generator has distinct values to give.
        if (!distinct) { return; }
        if (!TryGetProvableCardinality(factory, symbols, out int cardinality)) { return; }
        if (effectiveMin <= cardinality) { return; }

        Report(context, at, $"{effectiveMin} distinct element(s) are required, but the element generator can produce only {cardinality}");
    }

    private static void Report(OperationAnalysisContext context, IOperation at, string reason) {
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CollectionConstraintsAdmitNoValue, at.Syntax.GetLocation(), reason));
    }

    private static bool TryConstant(IInvocationOperation constraint, out int value) {
        value = 0;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out value);
    }

    private static bool IsCollectionFactory(string name) {
        return name is "ListOf" or "ArrayOf" or "SequenceOf" or "SetOf" or "DictionaryOf";
    }

    /// <summary>
    ///     An upper bound on the element generator's distinct domain, for the shapes the compiler can settle. Anything
    ///     else answers <c>false</c>: an unprovable domain must never be treated as a small one.
    /// </summary>
    private static bool TryGetProvableCardinality(IInvocationOperation factory, KnownSymbols symbols, out int cardinality) {
        cardinality = 0;

        if (factory.Arguments.Length == 0) { return false; }
        if (GeneratorFacts.Unwrap(factory.Arguments[0].Value) is not IInvocationOperation element) { return false; }

        // Walk the element chain back to its own factory, watching what the constraints along the way do to the
        // domain. AllowingCombinations() WIDENS an enum's universe to the OR-closure of its declared members — eight
        // values for four flags — so counting declared members there would under-report the domain and condemn a legal
        // chain. An unprovable domain must never be treated as a small one, so the rule stands down instead of
        // computing the closure: a deliberate false negative, not an oversight.
        IInvocationOperation root = element;
        while (root.Instance is not null && GeneratorFacts.Unwrap(root.Instance) is IInvocationOperation inner) {
            if (root.TargetMethod.Name == "AllowingCombinations") { return false; }

            root = inner;
        }

        switch (root.TargetMethod.Name) {
            case "Boolean":
                cardinality = 2;

                return true;

            case "Enum" when root.TargetMethod.TypeArguments.Length == 1 && root.TargetMethod.TypeArguments[0] is INamedTypeSymbol enumType:
                cardinality = enumType.GetMembers().OfType<IFieldSymbol>().Count(field => field.HasConstantValue);

                return cardinality > 0;

            case "OneOf" or "ElementOf":
                return TryCountDistinctConstants(root, out cardinality);

            default:
                return false;
        }
    }

    private static bool TryCountDistinctConstants(IInvocationOperation pool, out int cardinality) {
        cardinality = 0;

        foreach (IArgumentOperation argument in pool.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { return false; }

            HashSet<object?> distinct = new();
            foreach (IOperation element in initializer.ElementValues) {
                Optional<object?> constant = GeneratorFacts.Unwrap(element).ConstantValue;
                if (!constant.HasValue) { return false; }

                distinct.Add(constant.Value);
            }

            cardinality = distinct.Count;

            return cardinality > 0;
        }

        return false;
    }

}

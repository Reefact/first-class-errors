using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD017 — reports an enum constraint that steps outside the generator's universe. <c>Any.Enum&lt;T&gt;()</c> draws
///     only <b>declared</b> members, which is deliberate and surprising: on a <c>[Flags]</c> enum, writing a
///     combination in <c>OneOf</c> is the natural thing to do and the generator refuses it unless
///     <c>AllowingCombinations()</c> is declared.
/// </summary>
/// <remarks>
///     Kept apart from the interval rules because the domain is metadata — the declared members — rather than
///     arithmetic, and because the mistake has its own teachable model: the generator yields declared members, so a
///     value that is not one is not a narrowing but a category error.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumUniverseViolationAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EnumUniverseViolation);

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
        if (factory is null || factory.TargetMethod.Name != "Enum") { return; }
        if (factory.TargetMethod.TypeArguments.Length != 1 || factory.TargetMethod.TypeArguments[0] is not INamedTypeSymbol enumType) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        HashSet<object?> declared = new(enumType.GetMembers()
                                                .OfType<IFieldSymbol>()
                                                .Where(field => field.HasConstantValue)
                                                .Select(field => field.ConstantValue));

        if (declared.Count == 0) { return; }

        bool combinationsAllowed = constraints.Any(constraint => constraint.TargetMethod.Name == "AllowingCombinations");

        HashSet<object?> excluded = new();

        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (name is not ("OneOf" or "Except" or "DifferentFrom")) { continue; }

            foreach (IOperation value in ConstantArguments(constraint)) {
                Optional<object?> constant = value.ConstantValue;
                if (!constant.HasValue) { continue; }

                if (name is "Except" or "DifferentFrom") { excluded.Add(constant.Value); }

                // AllowingCombinations widens the universe to the OR-closure of the declared members, which no longer
                // matches a declared value one for one — so the rule stands down rather than approximate it.
                if (combinationsAllowed || declared.Contains(constant.Value)) { continue; }

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.EnumUniverseViolation, value.Syntax.GetLocation(),
                    $"{constant.Value} is not a declared member of {enumType.Name}"
                  + (enumType.GetAttributes().Any(IsFlagsAttribute) ? "; declare AllowingCombinations() to draw flag combinations" : string.Empty)));

                return;
            }
        }

        if (excluded.Count == 0 || !declared.All(excluded.Contains)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EnumUniverseViolation, invocation.Syntax.GetLocation(),
            $"no declared {enumType.Name} member remains once every exclusion is applied"));
    }

    private static IEnumerable<IOperation> ConstantArguments(IInvocationOperation constraint) {
        foreach (IArgumentOperation argument in constraint.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.ParamArray) {
                if (argument.Value is IArrayCreationOperation { Initializer: { } initializer }) {
                    foreach (IOperation element in initializer.ElementValues) { yield return GeneratorFacts.Unwrap(element); }
                }

                continue;
            }

            yield return GeneratorFacts.Unwrap(argument.Value);
        }
    }

    private static bool IsFlagsAttribute(AttributeData attribute) {
        return attribute.AttributeClass?.Name == "FlagsAttribute";
    }

}

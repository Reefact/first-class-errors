using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD023 — reports a scalar chain whose constant constraints leave no value at all, and JD024 — a constraint that
///     narrows nothing. The two share one walk because they read the same state from opposite sides: one asks whether
///     anything remains, the other whether anything changed.
/// </summary>
/// <remarks>
///     JD024 is the only member of the constraint family the run time never reports. Every other contradiction throws
///     eventually and loudly; an inert constraint leaves the test green while it exercises a domain the author did not
///     write. That is why it is worth an Info rule rather than nothing.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScalarChainAdmitsNoValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ScalarChainAdmitsNoValue, Descriptors.ConstraintWithNoEffect);

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
        if (factory is null || !IsIntegerFactory(factory.TargetMethod.Name)) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        AnalyzeConstraints(context, constraints);
    }

    /// <summary>
    ///     Walks the constraints in the order they were written, carrying the interval they describe, and reports the
    ///     first one that empties it, that narrows nothing, or that excludes nothing.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints) {
        ScalarConstraintState state = ScalarConstraintState.Unconstrained();

        foreach (IInvocationOperation constraint in constraints) {
            if (!TryReadArguments(constraint, out IReadOnlyList<long> arguments)) { return; }

            string name = constraint.TargetMethod.Name;

            // The exclusion that removes nothing: silent at run time, and the reason JD024 exists.
            if (name is "Except" or "DifferentFrom" && state.ExclusionIsInert(arguments)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.ConstraintWithNoEffect, constraint.Syntax.GetLocation(),
                    $"{name} removes no value the generator could produce"));

                return;
            }

            ScalarConstraintState? next = state.Apply(name, arguments);
            if (next is null) { return; }

            if (next.IsEmpty()) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.ScalarChainAdmitsNoValue, constraint.Syntax.GetLocation(), name));

                return;
            }

            if (IsNarrowingConstraint(name) && state.NarrowsNothing(next)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.ConstraintWithNoEffect, constraint.Syntax.GetLocation(),
                    $"{name} is already implied by the constraints declared before it"));

                return;
            }

            state = next;
        }
    }

    // A bound whose job is to narrow. Applying one that changes nothing is what JD024 reports; Positive() after
    // GreaterThan(5) is the shape, and it reads as a tightening that is not one.
    private static bool IsNarrowingConstraint(string name) {
        return name is "GreaterThan" or "GreaterThanOrEqualTo" or "LessThan" or "LessThanOrEqualTo" or "Between" or "Positive" or "Negative";
    }

    private static bool TryReadArguments(IInvocationOperation constraint, out IReadOnlyList<long> arguments) {
        List<long> values = [];
        arguments = values;

        foreach (IArgumentOperation argument in constraint.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.ParamArray) {
                if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { return false; }

                foreach (IOperation element in initializer.ElementValues) {
                    if (!TryReadInteger(element, out long value)) { return false; }

                    values.Add(value);
                }

                continue;
            }

            if (!TryReadInteger(argument.Value, out long single)) { return false; }

            values.Add(single);
        }

        return true;
    }

    private static bool TryReadInteger(IOperation operation, out long value) {
        value = 0;

        Optional<object?> constant = GeneratorFacts.Unwrap(operation).ConstantValue;
        if (!constant.HasValue) { return false; }

        switch (constant.Value) {
            case int i:   value = i;   return true;
            case long l:  value = l;   return true;
            case short s: value = s;   return true;
            case byte b:  value = b;   return true;
            case sbyte sb: value = sb; return true;
            default:      return false;
        }
    }

    // Only the integer generators: the model is integer arithmetic, and a floating-point or decimal domain does not
    // behave like one.
    private static bool IsIntegerFactory(string name) {
        return name is "Int32" or "Int16" or "Int64" or "Byte" or "SByte" or "UInt16" or "UInt32" or "UInt64";
    }

}

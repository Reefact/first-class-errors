using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD014 — reports a constraint argument that is a compile-time constant the generator's own guard rejects, so the
///     call throws every time it runs. The mistake is fully determined by a literal at the call site, yet it survives
///     the build and only fires when that arrange line executes — often deep inside a helper shared by many tests,
///     where the failure reads as a library problem rather than as the transposition typo it usually is.
/// </summary>
/// <remarks>
///     One rule over one table rather than a rule per method: the library validates these in one place, with one
///     message shape, and a reader who learns "a constant the guard rejects" has learned all of them. The table
///     mirrors <c>SizeGuard</c> and the per-generator guards exactly; where it cannot be certain it stays silent.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions",
                                                 Justification =
                                                     "Every TryCheck* member walks the argument list and reports the first offender through its out parameters. The rule asks for " +
                                                     "Select(argument => argument.Value), a projection that buys nothing and renames the loop variable away from what it is: `argument` " +
                                                     "is an IArgumentOperation, and `argument.Value` reads as the operation behind it. TryCheckSize cannot honour it at all — its filter " +
                                                     "produces the `out int value` its body then reports on, so a projection would force a second TryGetInt32 call. The family reads the " +
                                                     "same way on purpose, so it is declared once here rather than four times below.")]
public sealed class RejectedConstantArgumentAnalyzer : DiagnosticAnalyzer {

    // SizeGuard.MaxProducibleSize — a size the generator must actually produce is capped here.
    private const int MaxProducibleSize = 1_000_000;
    private const int MaxDecimalScale   = 28;
    private const int MinPort           = 1;
    private const int MaxPort           = 65535;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RejectedConstantArgument);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!IsJustDummiesMember(invocation.TargetMethod, symbols)) { return; }

        // A test asserting that the guard rejects the argument writes the illegal call as the whole body of a lambda
        // argument. This repository alone holds hundreds of them.
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        if (TryFindViolation(invocation, context.Compilation, out IOperation? offending, out string? reason)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.RejectedConstantArgument, offending!.Syntax.GetLocation(), invocation.TargetMethod.Name, reason));
        }
    }

    private static bool TryFindViolation(IInvocationOperation invocation, Compilation compilation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        string name = invocation.TargetMethod.Name;

        switch (name) {
            case "WithLength" or "WithMinLength" or "WithCount" or "WithMinCount":
                return TryCheckSize(invocation, capped: true, out offending, out reason);

            case "WithMaxLength" or "WithMaxCount" or "WithPathSegments":
                return TryCheckSize(invocation, capped: false, out offending, out reason);

            case "WithLengthBetween" or "WithCountBetween":
                return TryCheckSizeRange(invocation, out offending, out reason);

            case "Between":
                return TryCheckOrderedPair(invocation, out offending, out reason);

            case "MultipleOf":
                return TryCheckStrictlyPositive(invocation, out offending, out reason);

            case "WithGranularity":
                return TryCheckGranularity(invocation, compilation, out offending, out reason);

            case "WithScale":
                return TryCheckRange(invocation, 0, MaxDecimalScale, "the scale must be in the inclusive range [0, 28]", out offending, out reason);

            case "WithPort":
                return TryCheckRange(invocation, MinPort, MaxPort, "the port must be between 1 and 65535", out offending, out reason);

            case "StartingWith" or "EndingWith" or "Containing" or "WithChars" or "WithHost":
                return TryCheckNonEmptyText(invocation, out offending, out reason);

            case "OneOf" or "Except":
                return TryCheckNonEmptyPool(invocation, out offending, out reason);

            default:
                return false;
        }
    }

    private static bool TryCheckSize(IInvocationOperation invocation, bool capped, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in NumericArguments(invocation)) {
            if (!ConstantFacts.TryGetInt32(argument.Value, out int value)) { continue; }

            if (value < 0) {
                offending = argument.Value;
                reason    = "it must not be negative";

                return true;
            }

            if (capped && value > MaxProducibleSize) {
                offending = argument.Value;
                reason    = $"it must not exceed {MaxProducibleSize:N0}, the largest size the generator will produce";

                return true;
            }
        }

        return false;
    }

    private static bool TryCheckSizeRange(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        IArgumentOperation[] arguments = NumericArguments(invocation).ToArray();
        if (arguments.Length != 2) { return false; }

        if (ConstantFacts.TryGetInt32(arguments[0].Value, out int minimum) && minimum < 0) {
            offending = arguments[0].Value;
            reason    = "it must not be negative";

            return true;
        }

        if (ConstantFacts.TryGetInt32(arguments[0].Value, out int min) && min > MaxProducibleSize) {
            offending = arguments[0].Value;
            reason    = $"it must not exceed {MaxProducibleSize:N0}, the largest size the generator will produce";

            return true;
        }

        if (ConstantFacts.TryGetInt32(arguments[1].Value, out int maximum) && maximum < 0) {
            offending = arguments[1].Value;
            reason    = "it must not be negative";

            return true;
        }

        return TryCheckOrderedPair(invocation, out offending, out reason);
    }

    private static bool TryCheckOrderedPair(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        IArgumentOperation[] arguments = NumericArguments(invocation).ToArray();
        if (arguments.Length != 2) { return false; }

        if (!ConstantFacts.TryGetInt32(arguments[0].Value, out int minimum)) { return false; }
        if (!ConstantFacts.TryGetInt32(arguments[1].Value, out int maximum)) { return false; }
        if (minimum <= maximum) { return false; }

        offending = arguments[0].Value;
        reason    = $"the minimum ({minimum}) must be less than or equal to the maximum ({maximum}) — the two arguments look transposed";

        return true;
    }

    private static bool TryCheckStrictlyPositive(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in NumericArguments(invocation)) {
            if (!ConstantFacts.TryGetInt32(argument.Value, out int value) || value > 0) { continue; }

            offending = argument.Value;
            reason    = "it must be strictly positive";

            return true;
        }

        return false;
    }

    private static bool TryCheckGranularity(IInvocationOperation invocation, Compilation compilation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (!ConstantFacts.IsNonPositiveTimeSpan(argument.Value, compilation)) { continue; }

            offending = argument.Value;
            reason    = "the granularity must be strictly positive";

            return true;
        }

        return false;
    }

    private static bool TryCheckRange(IInvocationOperation invocation, int minimum, int maximum, string requirement, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in NumericArguments(invocation)) {
            if (!ConstantFacts.TryGetInt32(argument.Value, out int value)) { continue; }
            if (value >= minimum && value <= maximum) { continue; }

            offending = argument.Value;
            reason    = requirement;

            return true;
        }

        return false;
    }

    private static bool TryCheckNonEmptyText(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Type.SpecialType != SpecialType.System_String) { continue; }
            if (!ConstantFacts.TryGetString(argument.Value, out string text)) { continue; }

            if (text.Length == 0) {
                offending = argument.Value;
                reason    = "it must not be empty";

                return true;
            }

            if (invocation.TargetMethod.Name == "WithChars" && text.Any(char.IsSurrogate)) {
                offending = argument.Value;
                reason    = "a character pool must not contain a surrogate: an astral code point spans two UTF-16 units, which the draw would split. Use OneOf(...) to draw such values as whole strings";

                return true;
            }
        }

        return false;
    }

    private static bool TryCheckNonEmptyPool(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { continue; }
            if (!initializer.ElementValues.IsEmpty) { continue; }

            offending = invocation;
            reason    = "at least one value is required";

            return true;
        }

        return false;
    }

    // Only the arguments a size or bound guard inspects: an int parameter. This keeps Containing(TItem) on a
    // collection, or Between(DateTime, DateTime), out of the integer checks rather than misreading them.
    private static System.Collections.Generic.IEnumerable<IArgumentOperation> NumericArguments(IInvocationOperation invocation) {
        return invocation.Arguments.Where(argument => argument.Parameter?.Type.SpecialType == SpecialType.System_Int32);
    }

    private static bool IsJustDummiesMember(IMethodSymbol method, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(method.ContainingType?.ContainingAssembly, symbols.IAny!.ContainingAssembly);
    }

}

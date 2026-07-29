using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Reads the statically-known value of an argument. Deliberately wider than <see cref="Optional{T}" /> on
///     <c>IOperation.ConstantValue</c>: a <see cref="System.TimeSpan" /> is never a C# constant, yet
///     <c>TimeSpan.Zero</c> and <c>TimeSpan.FromSeconds(1)</c> are as statically known as any literal, and the
///     constraints that take one are exactly the ones worth checking.
/// </summary>
/// <remarks>
///     Every reader answers "no" rather than guessing. A rule built on this can therefore only under-report, which is
///     the right failure direction for a diagnostic that claims a call is certainly wrong.
/// </remarks>
internal static class ConstantFacts {

    private const string TimeSpanMetadataName = "System.TimeSpan";
    private const string ZeroFieldName        = "Zero";

    /// <summary>Reads a constant <see cref="int" />, folding named constants as the compiler does.</summary>
    public static bool TryGetInt32(IOperation operation, out int value) {
        value = 0;

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        if (unwrapped.ConstantValue is not { HasValue: true, Value: int constant }) { return false; }

        value = constant;

        return true;
    }

    /// <summary>Reads a constant <see cref="string" />. A <c>null</c> literal answers <c>false</c>: a null argument is
    /// the null-guard's business, not a constraint rule's.</summary>
    public static bool TryGetString(IOperation operation, out string value) {
        value = string.Empty;

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        if (unwrapped.ConstantValue is not { HasValue: true, Value: string constant }) { return false; }

        value = constant;

        return true;
    }

    /// <summary>
    ///     Whether the operation is a statically-known <b>non-positive</b> <see cref="System.TimeSpan" /> — the shape
    ///     every granularity guard rejects. Recognises <c>TimeSpan.Zero</c> and the <c>TimeSpan.FromXxx(constant)</c>
    ///     factories; anything else answers <c>false</c>.
    /// </summary>
    public static bool IsNonPositiveTimeSpan(IOperation operation, Compilation compilation) {
        INamedTypeSymbol? timeSpan = compilation.GetTypeByMetadataName(TimeSpanMetadataName);
        if (timeSpan is null) { return false; }

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);

        if (unwrapped is IFieldReferenceOperation field) {
            return field.Field.Name == ZeroFieldName && SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, timeSpan);
        }

        // TimeSpan.FromSeconds(0), FromMinutes(-1) ... — a single constant numeric argument settles the sign.
        if (unwrapped is IInvocationOperation { Arguments.Length: 1 } invocation
         && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, timeSpan)
         && invocation.TargetMethod.Name.StartsWith("From", System.StringComparison.Ordinal)) {

            IOperation argument = GeneratorFacts.Unwrap(invocation.Arguments[0].Value);
            if (argument.ConstantValue is { HasValue: true, Value: { } raw }) {
                return raw switch {
                    double d  => d <= 0,
                    int i     => i <= 0,
                    long l    => l <= 0,
                    _         => false,
                };
            }
        }

        return false;
    }

}

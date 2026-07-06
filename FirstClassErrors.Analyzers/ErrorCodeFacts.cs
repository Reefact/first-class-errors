using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Recognizes <c>ErrorCode.Create("...")</c> invocations and extracts the literal code, centralizing the
///     <see cref="Optional{T}" /> handling shared by the error-code analyzers (FCE001, FCE002, FCE004, FCE005).
/// </summary>
internal static class ErrorCodeFacts {

    /// <summary>
    ///     Metadata name of the <c>ErrorCode</c> type the analyzers key off of.
    /// </summary>
    public const string ErrorCodeMetadataName = "FirstClassErrors.ErrorCode";

    private const string CreateMethodName = "Create";

    /// <summary>
    ///     Returns the single argument operation of a static <c>ErrorCode.Create(x)</c> call, or <c>null</c> when the
    ///     invocation is not such a call.
    /// </summary>
    public static IOperation? GetCreateArgument(IInvocationOperation invocation, INamedTypeSymbol errorCodeType) {
        IMethodSymbol method = invocation.TargetMethod;

        if (!method.IsStatic || method.Name != CreateMethodName) { return null; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, errorCodeType)) { return null; }
        if (invocation.Arguments.Length != 1) { return null; }

        return invocation.Arguments[0].Value;
    }

    /// <summary>
    ///     Extracts the code when <paramref name="argument" /> is a non-empty literal string constant. A non-literal
    ///     argument (<see cref="Optional{T}.HasValue" /> is <c>false</c>) is FCE003's concern; a <c>null</c>, empty, or
    ///     whitespace literal is FCE002's concern. Both yield <c>false</c> here.
    /// </summary>
    public static bool TryGetNonEmptyLiteralCode(IOperation argument, out string code) {
        Optional<object?> constant = argument.ConstantValue;

        if (constant.HasValue && constant.Value is string value && !string.IsNullOrWhiteSpace(value)) {
            code = value;
            return true;
        }

        code = string.Empty;
        return false;
    }

    /// <summary>
    ///     True when <paramref name="argument" /> is a literal constant whose string value is missing (<c>null</c>) or
    ///     blank. A non-literal argument (<see cref="Optional{T}.HasValue" /> is <c>false</c>) yields <c>false</c>; that
    ///     case is FCE003's concern.
    /// </summary>
    public static bool IsBlankLiteralCode(IOperation argument) {
        Optional<object?> constant = argument.ConstantValue;

        if (!constant.HasValue) { return false; }

        return constant.Value is not string value || string.IsNullOrWhiteSpace(value);
    }

}

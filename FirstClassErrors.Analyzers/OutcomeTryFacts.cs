using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Symbol-inspection helpers shared by the <c>Outcome.Try</c> usage analyzers (FCE019, FCE020, FCE022). Every
///     <c>Try</c> overload declares the caught exception through a type parameter named <c>TException</c>; these
///     helpers recognize a call to <c>FirstClassErrors.Outcome.Try&lt;...&gt;</c> and surface the concrete type bound
///     to it.
/// </summary>
internal static class OutcomeTryFacts {

    public const string OutcomeMetadataName = "FirstClassErrors.Outcome";

    private const string TryMethodName              = "Try";
    private const string ExceptionTypeParameterName = "TException";

    /// <summary>
    ///     Determines whether <paramref name="invocation" /> is a call to <c>FirstClassErrors.Outcome.Try</c> and, if so,
    ///     yields the concrete type bound to its <c>TException</c> type parameter.
    /// </summary>
    public static bool TryGetCaughtExceptionType(
        IInvocationOperation invocation,
        INamedTypeSymbol     outcomeType,
        out ITypeSymbol?     caughtExceptionType) {

        caughtExceptionType = null;

        IMethodSymbol method = invocation.TargetMethod;
        if (method.Name != TryMethodName) { return false; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, outcomeType)) { return false; }

        for (int i = 0; i < method.TypeParameters.Length; i++) {
            if (method.TypeParameters[i].Name == ExceptionTypeParameterName) {
                caughtExceptionType = method.TypeArguments[i];

                return caughtExceptionType is not null;
            }
        }

        return false;
    }

}

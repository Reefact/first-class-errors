using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Detection logic for FCE021. Recognizes a synchronous, value-producing <c>Outcome.Try</c> whose operation is a
///     lambda that does nothing but call a single throwing member, and decides whether that member has a non-throwing
///     counterpart (<c>Try&lt;Name&gt;</c> for a method, <c>TryParse</c> or <c>TryCreate</c> for a constructor) that
///     actually resolves — with a compatible signature — in the compilation being analyzed.
/// </summary>
/// <remarks>
///     <para>
///         The rule fires wherever a matching non-throwing counterpart resolves, regardless of where the wrapped type is
///         declared. Structural matching cannot prove behavioural equivalence — a look-alike <c>TryXxx</c> may normalize
///         its input, apply a different culture, have side effects, or be unrelated — so the diagnostic is advisory: it
///         surfaces the candidate and leaves the judgement (and any <c>SuppressMessage</c>) to the developer. The
///         signature check (same parameters, matching ref kinds, a trailing <c>out</c> of the result type, returning
///         <c>bool</c>) at least guarantees the suggested call would compile.
///     </para>
///     <para>
///         The signature check <c>(same parameters, out result) : bool</c> is what makes the rule both framework-aware
///         and false-positive-resistant: the counterpart is looked up on the wrapped member's own type (and its base
///         types) through the semantic model, so it is only found when the consumer's target framework exposes it, and
///         only accepted when its shape is an exact drop-in for the throwing call.
///     </para>
/// </remarks>
internal static class TryAlternativeFacts {

    public const string FuncOfTMetadataName = "System.Func`1";

    private const string OperationParameterName = "operation";
    private const string TryPrefix              = "Try";

    private static readonly string[] ConstructorCounterpartNames = { "TryParse", "TryCreate" };

    /// <summary>
    ///     Determines whether <paramref name="tryInvocation" /> is the synchronous value-producing <c>Outcome.Try</c>
    ///     overload wrapping a single throwing framework member that has a resolvable non-throwing counterpart, and yields
    ///     display names for the message.
    /// </summary>
    public static bool TryGetPreferredAlternative(
        IInvocationOperation tryInvocation,
        INamedTypeSymbol     funcOfTType,
        out string           throwingDisplay,
        out string           alternativeDisplay) {

        throwingDisplay    = string.Empty;
        alternativeDisplay = string.Empty;

        if (!TryGetSingleThrowingCall(tryInvocation, funcOfTType, out IMethodSymbol? throwingMember, out ITypeSymbol? resultType, out bool isConstructor)) {
            return false;
        }

        INamedTypeSymbol containingType = throwingMember!.ContainingType;

        string[] counterpartNames = isConstructor ? ConstructorCounterpartNames : new[] { TryPrefix + throwingMember.Name };

        foreach (string counterpartName in counterpartNames) {
            if (!HasCompatibleCounterpart(containingType, counterpartName, throwingMember, resultType!)) { continue; }

            string typeName = containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            throwingDisplay    = isConstructor ? $"new {typeName}" : $"{typeName}.{throwingMember.Name}";
            alternativeDisplay = $"{typeName}.{counterpartName}";

            return true;
        }

        return false;
    }

    private static bool TryGetSingleThrowingCall(
        IInvocationOperation tryInvocation,
        INamedTypeSymbol     funcOfTType,
        out IMethodSymbol?   throwingMember,
        out ITypeSymbol?     resultType,
        out bool             isConstructor) {

        throwingMember = null;
        resultType     = null;
        isConstructor  = false;

        IArgumentOperation? operationArgument = FindArgument(tryInvocation, OperationParameterName);
        if (operationArgument is null) { return false; }

        // Only the synchronous value overload, whose operation is a Func<T>. The async and void overloads
        // (Func<CancellationToken, Task<T>>, Action, Func<CancellationToken, Task>) have no plain result to hand to
        // an out parameter, so they are out of scope.
        if (operationArgument.Parameter?.Type is not INamedTypeSymbol parameterType ||
            !SymbolEqualityComparer.Default.Equals(parameterType.OriginalDefinition, funcOfTType)) {
            return false;
        }

        if (operationArgument.Value is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda }) { return false; }
        if (!TryGetSingleReturnedValue(lambda.Body, out IOperation? returnedValue)) { return false; }

        switch (returnedValue) {
            // Static only: an instance call's receiver cannot be carried into a static TryXxx, so the rewrite would
            // not be one-for-one.
            case IInvocationOperation { TargetMethod: { ReturnsVoid: false, IsStatic: true } method }:
                throwingMember = method;
                resultType     = method.ReturnType;
                isConstructor  = false;

                return true;

            // An object/collection initializer cannot be carried into a TryXxx, so reject any construction that has one.
            case IObjectCreationOperation { Initializer: null, Constructor: { } constructor, Type: INamedTypeSymbol createdType }:
                throwingMember = constructor;
                resultType     = createdType;
                isConstructor  = true;

                return true;

            default:
                return false;
        }
    }

    // A lambda body counts only when it is exactly one returned expression: `() => X.Parse(s)` or
    // `() => { return X.Parse(s); }`. Anything else (extra statements, a cast around the call, no return) is left
    // alone, so the suggested rewrite stays an exact one-for-one replacement.
    private static bool TryGetSingleReturnedValue(IBlockOperation body, out IOperation? returnedValue) {
        returnedValue = null;

        if (body.Operations.Length != 1) { return false; }
        if (body.Operations[0] is not IReturnOperation { ReturnedValue: { } value }) { return false; }

        returnedValue = value;

        return true;
    }

    private static bool HasCompatibleCounterpart(
        INamedTypeSymbol containingType,
        string           counterpartName,
        IMethodSymbol    throwingMember,
        ITypeSymbol      resultType) {

        for (INamedTypeSymbol? type = containingType; type is not null; type = type.BaseType) {
            foreach (ISymbol member in type.GetMembers(counterpartName)) {
                if (member is not IMethodSymbol candidate) { continue; }
                if (!candidate.IsStatic || candidate.DeclaredAccessibility != Accessibility.Public) { continue; }
                if (candidate.ReturnType.SpecialType != SpecialType.System_Boolean) { continue; }

                IMethodSymbol constructed;
                if (throwingMember.TypeArguments.Length > 0) {
                    if (candidate.TypeParameters.Length != throwingMember.TypeArguments.Length) { continue; }
                    constructed = candidate.Construct(throwingMember.TypeArguments.ToArray());
                } else if (candidate.TypeParameters.Length != 0) {
                    continue;
                } else {
                    constructed = candidate;
                }

                if (SignatureIsDropInReplacement(throwingMember.Parameters, constructed.Parameters, resultType)) {
                    return true;
                }
            }
        }

        return false;
    }

    // The counterpart must take the throwing member's parameters unchanged — same types AND same ref kinds — then a
    // single trailing `out result`, and nothing else.
    private static bool SignatureIsDropInReplacement(
        ImmutableArray<IParameterSymbol> throwingParameters,
        ImmutableArray<IParameterSymbol> candidateParameters,
        ITypeSymbol                      resultType) {

        if (candidateParameters.Length != throwingParameters.Length + 1) { return false; }

        for (int i = 0; i < throwingParameters.Length; i++) {
            if (candidateParameters[i].RefKind == RefKind.Out) { return false; }
            if (candidateParameters[i].RefKind != throwingParameters[i].RefKind) { return false; }
            if (!SymbolEqualityComparer.Default.Equals(candidateParameters[i].Type, throwingParameters[i].Type)) { return false; }
        }

        IParameterSymbol trailing = candidateParameters[candidateParameters.Length - 1];

        return trailing.RefKind == RefKind.Out && SymbolEqualityComparer.Default.Equals(trailing.Type, resultType);
    }

    private static IArgumentOperation? FindArgument(IInvocationOperation invocation, string parameterName) {
        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Name == parameterName) { return argument; }
        }

        return null;
    }

}

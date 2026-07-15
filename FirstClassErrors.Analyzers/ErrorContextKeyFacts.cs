using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Recognizes <c>ErrorContextKey.Create&lt;T&gt;("name", ...)</c> invocations and exposes the two facts the
///     error-context analyzers key off of: the literal name argument and the declared value type <c>T</c>. Both
///     public overloads (<c>Create&lt;T&gt;(string, string?)</c> and <c>Create&lt;T&gt;(string, Func&lt;string?&gt;)</c>)
///     name their first parameter <c>name</c>, so a key declaration is matched once, where the key is registered.
/// </summary>
internal static class ErrorContextKeyFacts {

    /// <summary>
    ///     Metadata name of the (non-generic) <c>ErrorContextKey</c> type that declares the static <c>Create</c> factory.
    /// </summary>
    public const string ErrorContextKeyMetadataName = "FirstClassErrors.ErrorContextKey";

    private const string CreateMethodName = "Create";
    private const string NameParameterName = "name";

    /// <summary>
    ///     When <paramref name="invocation" /> is a static <c>ErrorContextKey.Create&lt;T&gt;("name", ...)</c> call,
    ///     yields the operation of its <c>name</c> argument and the value type <c>T</c>; otherwise returns <c>false</c>.
    /// </summary>
    public static bool TryGetCreatedKey(
        IInvocationOperation invocation,
        INamedTypeSymbol     errorContextKeyType,
        out IOperation?      nameArgument,
        out ITypeSymbol?     valueType) {

        nameArgument = null;
        valueType    = null;

        IMethodSymbol method = invocation.TargetMethod;

        if (!method.IsStatic || method.Name != CreateMethodName) { return false; }
        if (method.TypeArguments.Length != 1) { return false; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, errorContextKeyType)) { return false; }

        IArgumentOperation? name = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Name == NameParameterName);
        if (name is null) { return false; }

        nameArgument = name.Value;
        valueType    = method.TypeArguments[0];

        return true;
    }

    /// <summary>
    ///     Extracts the key name when <paramref name="nameArgument" /> is a non-empty literal string constant. A
    ///     non-literal name (built at runtime) cannot be inspected statically and yields <c>false</c>.
    /// </summary>
    public static bool TryGetLiteralName(IOperation nameArgument, out string name) {
        Optional<object?> constant = nameArgument.ConstantValue;

        if (constant.HasValue && constant.Value is string value && !string.IsNullOrWhiteSpace(value)) {
            name = value;

            return true;
        }

        name = string.Empty;

        return false;
    }

}

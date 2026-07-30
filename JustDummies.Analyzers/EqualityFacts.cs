using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     Whether a type carries value equality, decided the way <c>EqualityComparer&lt;T&gt;.Default</c> decides it: an
///     <c>IEquatable&lt;T&gt;</c> implementation, or an <c>Equals(object)</c> override somewhere below
///     <c>object</c>. A type with neither falls back to reference equality.
/// </summary>
/// <remarks>
///     The answer is only ever used to claim that equality <b>cannot</b> distinguish two values, which is a claim that
///     has to be certain — so every uncertainty resolves to "it has value equality" and the caller stands down. The
///     sealed requirement is the largest of those: an open class says nothing about the instance a generator actually
///     produces, since a derived type is free to add the equality the base lacks.
/// </remarks>
internal static class EqualityFacts {

    /// <summary>
    ///     Whether <paramref name="type" /> provably compares by reference under the default comparer.
    /// </summary>
    public static bool UsesReferenceEquality(ITypeSymbol? type) {
        // A value type never compares by reference; a type parameter, an interface, an array or a delegate is either
        // substitutable or already carries its own equality. Only a sealed class settles the question here.
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsSealed: true, IsRecord: false } named) { return false; }
        if (named.SpecialType == SpecialType.System_Object) { return false; }
        if (ImplementsIEquatable(named)) { return false; }

        for (INamedTypeSymbol? current = named; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType) {
            if (OverridesEquals(current)) { return false; }
        }

        return true;
    }

    private static bool ImplementsIEquatable(INamedTypeSymbol type) {
        return type.AllInterfaces.Any(implemented => implemented is { IsGenericType: true, Name: "IEquatable" }
                                                 && implemented.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true });
    }

    private static bool OverridesEquals(INamedTypeSymbol type) {
        return type.GetMembers("Equals")
                   .Any(member => member is IMethodSymbol { IsOverride: true, Parameters.Length: 1, ReturnType.SpecialType: SpecialType.System_Boolean });
    }

}

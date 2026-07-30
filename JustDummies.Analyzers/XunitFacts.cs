using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     Facts about an xUnit test's shape and lifecycle, needed by the rules that reason about <b>when</b> a value is
///     drawn relative to the seed scope <c>[Reproducible]</c> opens.
/// </summary>
/// <remarks>
///     Every lookup flows through <see cref="KnownSymbols" />, so a compilation that references neither xUnit nor the
///     JustDummies adapter simply never reaches these rules — JustDummies stays standalone and error-agnostic.
/// </remarks>
internal static class XunitFacts {

    /// <summary>
    ///     Whether <paramref name="symbol" /> is covered by <c>[Reproducible]</c> — declared on the member itself, on
    ///     its containing type or a base type, or on the assembly. These are the three levels the adapter honours.
    /// </summary>
    public static bool IsCoveredByReproducible(ISymbol symbol, INamedTypeSymbol reproducibleAttribute) {
        if (HasAttribute(symbol, reproducibleAttribute)) { return true; }

        for (INamedTypeSymbol? type = symbol.ContainingType; type is not null; type = type.BaseType) {
            if (HasAttribute(type, reproducibleAttribute)) { return true; }
        }

        return HasAttribute(symbol.ContainingAssembly, reproducibleAttribute);
    }

    /// <summary>
    ///     Whether <paramref name="method" /> carries an attribute xUnit treats as a test — anything implementing
    ///     <c>IFactAttribute</c>, which covers <c>[Fact]</c>, <c>[Theory]</c> and third-party derivatives alike.
    /// </summary>
    public static bool IsTestMethod(IMethodSymbol method, INamedTypeSymbol factAttribute) {
        foreach (AttributeData attribute in method.GetAttributes()) {
            if (attribute.AttributeClass is not null && Implements(attribute.AttributeClass, factAttribute)) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     Whether <paramref name="symbol" /> produces a theory's cases — a member xUnit evaluates at <b>discovery</b>,
    ///     before any test runs and outside every seed scope.
    /// </summary>
    /// <remarks>
    ///     Four shapes are recognised, because a provider is written in four ways: a member named by a
    ///     <c>[MemberData]</c> in the same type; a member returning <c>TheoryData</c>; a member returning a sequence of
    ///     object arrays; and a type implementing that sequence, which is the <c>[ClassData]</c> shape.
    /// </remarks>
    public static bool IsTheoryDataProvider(ISymbol symbol, KnownSymbols symbols) {
        // A draw inside a property's body reports the accessor (get_Cases), not the property, so normalize first —
        // otherwise a [MemberData(nameof(Cases))] never matches the member it names.
        ISymbol member = symbol is IMethodSymbol { AssociatedSymbol: not null } accessor ? accessor.AssociatedSymbol : symbol;

        if (IsNamedByMemberData(member, symbols)) { return true; }

        ITypeSymbol? returnType = member switch {
            IMethodSymbol method     => method.ReturnType,
            IPropertySymbol property => property.Type,
            _                        => null,
        };

        if (returnType is not null && (IsTheoryData(returnType) || IsObjectArraySequence(returnType))) { return true; }

        // The [ClassData] shape: the containing type is itself the sequence of cases.
        return member.ContainingType is not null && IsObjectArraySequence(member.ContainingType);
    }

    private static bool IsNamedByMemberData(ISymbol symbol, KnownSymbols symbols) {
        if (symbols.MemberDataAttribute is null || symbol.ContainingType is null) { return false; }

        IEnumerable<AttributeData> attributes = symbol.ContainingType.GetMembers()
                                                      .OfType<IMethodSymbol>()
                                                      .SelectMany(member => member.GetAttributes());

        foreach (AttributeData attribute in attributes) {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.MemberDataAttribute)) { continue; }
            if (attribute.ConstructorArguments.Length == 0) { continue; }
            if (attribute.ConstructorArguments[0].Value as string == symbol.Name) { return true; }
        }

        return false;
    }

    // Matched by name rather than by symbol: xUnit v3 roots TheoryData<...> in TheoryDataBase<,>, and which generic
    // base carries which arity has moved between releases. The namespace plus the TheoryData prefix is the stable part.
    private static bool IsTheoryData(ITypeSymbol type) {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType) {
            bool inXunit = current.ContainingNamespace is { IsGlobalNamespace: false } ns && ns.ToDisplayString() == "Xunit";
            if (inXunit && current.Name.StartsWith("TheoryData", System.StringComparison.Ordinal)) { return true; }
        }

        return false;
    }

    // IEnumerable<object[]> — the raw shape xUnit ultimately consumes, and what a [ClassData] type implements.
    private static bool IsObjectArraySequence(ITypeSymbol type) {
        IEnumerable<INamedTypeSymbol> candidates = type is INamedTypeSymbol named
            ? named.AllInterfaces.Concat(new[] { named })
            : type.AllInterfaces;

        foreach (INamedTypeSymbol candidate in candidates) {
            if (candidate.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T) { continue; }
            if (candidate.TypeArguments.Length == 1 && candidate.TypeArguments[0] is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object }) { return true; }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol? symbol, INamedTypeSymbol attributeType) {
        if (symbol is null) { return false; }

        foreach (AttributeData attribute in symbol.GetAttributes()) {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)) { return true; }
        }

        return false;
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol @interface) {
        if (SymbolEqualityComparer.Default.Equals(type, @interface)) { return true; }

        foreach (INamedTypeSymbol implemented in type.AllInterfaces) {
            if (SymbolEqualityComparer.Default.Equals(implemented, @interface)) { return true; }
        }

        return false;
    }

}

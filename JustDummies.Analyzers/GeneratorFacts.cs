using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Facts about the generator surface: recognising a value that is an <c>IAny&lt;T&gt;</c> recipe rather than the
///     value that recipe would draw. Every rule in the <c>JustDummies.Usage</c> category rests on this distinction.
/// </summary>
internal static class GeneratorFacts {

    /// <summary>
    ///     Whether <paramref name="type" /> is a JustDummies generator — the <c>IAny&lt;T&gt;</c> interface itself, or
    ///     any type implementing it. Matching the interface rather than a list of concrete builders keeps the rules
    ///     correct for <c>As(...)</c> and <c>Combine(...)</c> derivations, and for a consumer's own generator.
    /// </summary>
    public static bool IsGenerator(ITypeSymbol? type, INamedTypeSymbol iAnyType) {
        if (type is null) { return false; }

        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, iAnyType)) { return true; }

        foreach (INamedTypeSymbol implemented in type.AllInterfaces) {
            if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, iAnyType)) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     Strips the implicit conversions Roslyn inserts around a generator when it flows into an <c>object</c> or
    ///     <c>string</c> position, so the rule sees the recipe rather than the conversion wrapping it.
    /// </summary>
    public static IOperation Unwrap(IOperation operation) {
        IOperation current = operation;
        while (current is IConversionOperation { IsImplicit: true } conversion) {
            current = conversion.Operand;
        }

        return current;
    }

}

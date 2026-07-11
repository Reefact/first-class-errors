using Microsoft.CodeAnalysis;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Small symbol-inspection helpers shared by the documentation-wiring analyzers.
/// </summary>
internal static class SymbolFacts {

    /// <summary>
    ///     Finds the <c>[DocumentedBy]</c> attribute on <paramref name="method" /> and the documentation method name it
    ///     references (the single string constructor argument).
    /// </summary>
    public static bool TryGetDocumentedBy(
        IMethodSymbol      method,
        INamedTypeSymbol   documentedByAttributeType,
        out AttributeData? attribute,
        out string?        targetMethodName) {

        AttributeData? candidate = method.GetAttributes()
                                         .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, documentedByAttributeType));
        if (candidate is not null) {
            attribute        = candidate;
            targetMethodName = candidate.ConstructorArguments.Length == 1
                ? candidate.ConstructorArguments[0].Value as string
                : null;

            return true;
        }

        attribute        = null;
        targetMethodName = null;

        return false;
    }

    public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType) {
        return symbol.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
    }

    public static bool IsOrInheritsFrom(ITypeSymbol type, INamedTypeSymbol target) {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType) {
            if (SymbolEqualityComparer.Default.Equals(current, target)) {
                return true;
            }
        }

        return false;
    }

}

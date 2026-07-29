using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     Resolves the JustDummies types an analyzer matches against, by metadata name. Analyzers never reference the
///     JustDummies assembly directly — they are loaded into the consumer's compiler — so a null field simply means the
///     library is not part of the analyzed compilation, in which case the analyzer stays silent.
/// </summary>
internal sealed class KnownSymbols {

    public const string AnyMetadataName  = "JustDummies.Any";
    public const string IAnyMetadataName = "JustDummies.IAny`1";

    private KnownSymbols(Compilation compilation) {
        Any  = compilation.GetTypeByMetadataName(AnyMetadataName);
        IAny = compilation.GetTypeByMetadataName(IAnyMetadataName);
    }

    /// <summary>The <c>JustDummies.Any</c> façade, or <c>null</c> when the compilation does not reference JustDummies.</summary>
    public INamedTypeSymbol? Any { get; }

    /// <summary>The unbound <c>JustDummies.IAny&lt;T&gt;</c> generator interface, or <c>null</c> as above.</summary>
    public INamedTypeSymbol? IAny { get; }

    public static KnownSymbols From(Compilation compilation) {
        return new KnownSymbols(compilation);
    }

}

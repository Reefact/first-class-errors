using Microsoft.CodeAnalysis;

namespace FirstClassErrors.Analyzers;

/// <summary>
///     Resolves the FirstClassErrors types an analyzer matches against, by metadata name. Analyzers never reference the
///     core assembly directly; a null field simply means the core is not part of the analyzed compilation, in which
///     case the analyzer stays silent.
/// </summary>
internal sealed class KnownSymbols {

    public const string ErrorMetadataName                      = "FirstClassErrors.Error";
    public const string DocumentedByAttributeMetadataName      = "FirstClassErrors.DocumentedByAttribute";
    public const string ProvidesErrorsForAttributeMetadataName = "FirstClassErrors.ProvidesErrorsForAttribute";
    public const string ErrorDocumentationMetadataName         = "FirstClassErrors.ErrorDocumentation";

    private KnownSymbols(Compilation compilation) {
        Error                      = compilation.GetTypeByMetadataName(ErrorMetadataName);
        DocumentedByAttribute      = compilation.GetTypeByMetadataName(DocumentedByAttributeMetadataName);
        ProvidesErrorsForAttribute = compilation.GetTypeByMetadataName(ProvidesErrorsForAttributeMetadataName);
        ErrorDocumentation         = compilation.GetTypeByMetadataName(ErrorDocumentationMetadataName);
    }

    public INamedTypeSymbol? Error                      { get; }
    public INamedTypeSymbol? DocumentedByAttribute      { get; }
    public INamedTypeSymbol? ProvidesErrorsForAttribute { get; }
    public INamedTypeSymbol? ErrorDocumentation         { get; }

    public static KnownSymbols From(Compilation compilation) {
        return new KnownSymbols(compilation);
    }

}

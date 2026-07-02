#region Usings declarations

using System.Globalization;
using System.Resources;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     The localized boilerplate the Markdown renderer emits (headings, labels, table headers). Values are read from
///     the <c>MarkdownRendererStrings</c> resources for a given culture, falling back to the neutral (English)
///     resources for any culture without a translation.
/// </summary>
internal sealed class MarkdownRendererStrings {

    #region Statics members declarations

    private static readonly ResourceManager Resources =
        new("FirstClassErrors.GenDoc.Rendering.MarkdownRendererStrings", typeof(MarkdownErrorDocumentationRenderer).Assembly);

    #endregion

    #region Fields declarations

    private readonly CultureInfo _culture;

    #endregion

    #region Constructors declarations

    /// <summary>Initializes the strings for the given <paramref name="culture" />.</summary>
    public MarkdownRendererStrings(CultureInfo culture) {
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    #endregion

    /// <summary>The document title (e.g. <c>Error Catalog</c>).</summary>
    public string ErrorCatalog => Get(nameof(ErrorCatalog));

    /// <summary>The table-of-contents heading.</summary>
    public string TableOfContents => Get(nameof(TableOfContents));

    /// <summary>The placeholder shown when the catalog is empty (rendered inside italics).</summary>
    public string NoDocumentedErrors => Get(nameof(NoDocumentedErrors));

    /// <summary>The bold label preceding an error code.</summary>
    public string CodeLabel => Get(nameof(CodeLabel));

    /// <summary>The bold label preceding a source name.</summary>
    public string SourceLabel => Get(nameof(SourceLabel));

    /// <summary>The bold label preceding a business rule.</summary>
    public string BusinessRuleLabel => Get(nameof(BusinessRuleLabel));

    /// <summary>The diagnostics section heading.</summary>
    public string DiagnosticsHeading => Get(nameof(DiagnosticsHeading));

    /// <summary>The inline label preceding a diagnostic's origin.</summary>
    public string OriginLabel => Get(nameof(OriginLabel));

    /// <summary>The examples section heading.</summary>
    public string ExamplesHeading => Get(nameof(ExamplesHeading));

    /// <summary>The context section heading.</summary>
    public string ContextHeading => Get(nameof(ContextHeading));

    /// <summary>The context table's "Key" column header.</summary>
    public string ContextKeyHeader => Get(nameof(ContextKeyHeader));

    /// <summary>The context table's "Type" column header.</summary>
    public string ContextTypeHeader => Get(nameof(ContextTypeHeader));

    /// <summary>The context table's "Description" column header.</summary>
    public string ContextDescriptionHeader => Get(nameof(ContextDescriptionHeader));

    /// <summary>The context table's "Example values" column header.</summary>
    public string ContextExampleValuesHeader => Get(nameof(ContextExampleValuesHeader));

    /// <summary>The label of a source group (e.g. <c>Temperature errors</c>), from a <c>{0}</c>-placeholder format.</summary>
    public string GroupLabel(string source) {
        return string.Format(_culture, Get("GroupLabelFormat"), source);
    }

    private string Get(string key) {
        // Fall back to the key itself only if a resource is entirely missing; the neutral resources define every key.
        return Resources.GetString(key, _culture) ?? key;
    }

}

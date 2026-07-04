#region Usings declarations

using System.Globalization;
using System.Resources;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     The localized labels the HTML renderer emits (headings, column titles, buttons, notes). Values are read from
///     the <c>HtmlRendererStrings</c> resources for a given culture, falling back to the neutral (English) resources
///     for any culture without a translation.
/// </summary>
internal sealed class HtmlRendererStrings {

    #region Statics members declarations

    private static readonly ResourceManager Resources =
        new("FirstClassErrors.GenDoc.Rendering.HtmlRendererStrings", typeof(HtmlErrorDocumentationRenderer).Assembly);

    #endregion

    #region Fields declarations

    private readonly CultureInfo _culture;

    #endregion

    #region Constructors declarations

    /// <summary>Initializes the strings for the given <paramref name="culture" />.</summary>
    public HtmlRendererStrings(CultureInfo culture) {
        _culture = culture ?? CultureInfo.InvariantCulture;
    }

    #endregion

    /// <summary>The document / catalog title.</summary>
    public string ErrorCatalog => Get(nameof(ErrorCatalog));

    /// <summary>The placeholder shown when the catalog is empty.</summary>
    public string NoDocumentedErrors => Get(nameof(NoDocumentedErrors));

    /// <summary>The accessible label of the search field.</summary>
    public string Search => Get(nameof(Search));

    /// <summary>The search field placeholder.</summary>
    public string SearchPlaceholder => Get(nameof(SearchPlaceholder));

    /// <summary>The message shown when a search yields no error.</summary>
    public string NoResults => Get(nameof(NoResults));

    /// <summary>The documentation (explanation) section heading.</summary>
    public string DocumentationHeading => Get(nameof(DocumentationHeading));

    /// <summary>The bold label preceding a business rule.</summary>
    public string BusinessRuleLabel => Get(nameof(BusinessRuleLabel));

    /// <summary>The diagnostics section heading.</summary>
    public string DiagnosticsHeading => Get(nameof(DiagnosticsHeading));

    /// <summary>The diagnostics table's "possible cause" column header.</summary>
    public string DiagnosticCauseHeader => Get(nameof(DiagnosticCauseHeader));

    /// <summary>The diagnostics table's "origin" column header.</summary>
    public string DiagnosticOriginHeader => Get(nameof(DiagnosticOriginHeader));

    /// <summary>The diagnostics table's "analysis hint" column header.</summary>
    public string DiagnosticHintHeader => Get(nameof(DiagnosticHintHeader));

    /// <summary>The examples section heading.</summary>
    public string ExamplesHeading => Get(nameof(ExamplesHeading));

    /// <summary>The label preceding an example's public RFC 9457 response block.</summary>
    public string PublicResponseLabel => Get(nameof(PublicResponseLabel));

    /// <summary>The label preceding an example's internal diagnostic (log) block.</summary>
    public string DiagnosticExampleLabel => Get(nameof(DiagnosticExampleLabel));

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

    /// <summary>The "back to the catalog" link label (split layout).</summary>
    public string BackToCatalog => Get(nameof(BackToCatalog));

    /// <summary>The accessible label of an error's copy-link anchor.</summary>
    public string CopyLinkLabel => Get(nameof(CopyLinkLabel));

    /// <summary>The accessible label of the theme-toggle button.</summary>
    public string ThemeToggleLabel => Get(nameof(ThemeToggleLabel));

    /// <summary>The "skip to content" accessibility link.</summary>
    public string SkipToContent => Get(nameof(SkipToContent));

    /// <summary>The documented-error count line (a <c>{0}</c>-placeholder format).</summary>
    public string ErrorsCount(int count) {
        return string.Format(_culture, Get("ErrorsCountFormat"), count);
    }

    private string Get(string key) {
        // Fall back to the key itself only if a resource is entirely missing; the neutral resources define every key.
        return Resources.GetString(key, _culture) ?? key;
    }

}

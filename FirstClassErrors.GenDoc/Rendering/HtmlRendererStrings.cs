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

    /// <summary>The message shown when a search/filter yields no error.</summary>
    public string NoResults => Get(nameof(NoResults));

    /// <summary>The filters section label.</summary>
    public string FiltersLabel => Get(nameof(FiltersLabel));

    /// <summary>The "all sources" option of the source filter.</summary>
    public string AllSources => Get(nameof(AllSources));

    /// <summary>The "with public detail" filter label.</summary>
    public string WithDetailFilter => Get(nameof(WithDetailFilter));

    /// <summary>The catalog table's "Code" column header.</summary>
    public string CodeColumn => Get(nameof(CodeColumn));

    /// <summary>The catalog table's public-summary column header.</summary>
    public string SummaryColumn => Get(nameof(SummaryColumn));

    /// <summary>The catalog table's "Source" column header.</summary>
    public string SourceColumn => Get(nameof(SourceColumn));

    /// <summary>The catalog table's "Detail" (has public detail) column header.</summary>
    public string DetailColumn => Get(nameof(DetailColumn));

    /// <summary>The catalog table's "Examples" (count) column header.</summary>
    public string ExamplesColumn => Get(nameof(ExamplesColumn));

    /// <summary>The affirmative label used as the accessible text of a "present" marker.</summary>
    public string YesLabel => Get(nameof(YesLabel));

    /// <summary>The public-summary heading on the detail view.</summary>
    public string PublicSummaryHeading => Get(nameof(PublicSummaryHeading));

    /// <summary>The public-detail heading on the detail view.</summary>
    public string PublicDetailHeading => Get(nameof(PublicDetailHeading));

    /// <summary>The internal-diagnostic-message heading on the detail view.</summary>
    public string DiagnosticHeading => Get(nameof(DiagnosticHeading));

    /// <summary>The note clarifying that the diagnostic message is internal.</summary>
    public string DiagnosticNote => Get(nameof(DiagnosticNote));

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

    /// <summary>The inline label preceding a diagnostic's origin.</summary>
    public string OriginLabel => Get(nameof(OriginLabel));

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

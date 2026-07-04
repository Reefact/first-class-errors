#region Usings declarations

using System.Globalization;
using System.Net;
using System.Text;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders the catalog as a self-contained static HTML site: a home page (searchable, filterable catalog table)
///     plus, in the split layout, one page per error. Everything is emitted as UTF-8 text with no external dependency
///     (no CDN, no binary asset): the CSS and JS are inlined into <c>assets/app.css</c> / <c>assets/app.js</c>, icons
///     are inline SVG, and the font stack is the system default. The output is deterministic (no timestamps, errors
///     ordered by code) so it diffs cleanly in source control.
/// </summary>
/// <remarks>
///     The renderer is a pure projection of the existing <see cref="ErrorDocumentation" /> catalog: it adds no
///     extraction and reads nothing beyond the catalog and the <see cref="RenderRequest" />. The public messages are
///     localized for <see cref="RenderRequest.Culture" />; the diagnostic message is shown verbatim (author language)
///     and clearly flagged as internal — never presented as an exposable public message.
/// </remarks>
public sealed class HtmlErrorDocumentationRenderer : IErrorDocumentationRenderer {

    /// <inheritdoc />
    public string Format => "html";

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedLayouts { get; } = [RenderLayouts.Single, RenderLayouts.Split];

    /// <inheritdoc />
    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request) {
        if (catalog is null) { throw new ArgumentNullException(nameof(catalog)); }
        if (request is null) { throw new ArgumentNullException(nameof(request)); }

        if (SupportedLayouts.Contains(request.Layout, StringComparer.OrdinalIgnoreCase) is false) {
            throw new LayoutNotSupportedException(Format, request.Layout, SupportedLayouts);
        }

        IReadOnlyList<Entry> entries = BuildEntries(catalog);
        HtmlRendererStrings  strings = new(request.Culture);
        string               htmlLang = HtmlLang(request.Culture);

        bool split = string.Equals(request.Layout, RenderLayouts.Split, StringComparison.OrdinalIgnoreCase);

        List<RenderedDocument> documents = split
                                               ? RenderSplit(entries, strings, htmlLang)
                                               : RenderSingle(entries, strings, htmlLang);

        // Assets are identical in both layouts; app.css/app.js are static, the search index is derived from the catalog.
        documents.Add(new RenderedDocument("assets/app.css", HtmlRendererAssets.Css));
        documents.Add(new RenderedDocument("assets/app.js", HtmlRendererAssets.Js));
        documents.Add(new RenderedDocument("assets/search-index.json", BuildSearchIndex(entries)));

        return documents;
    }

    #region Layouts

    private static List<RenderedDocument> RenderSingle(IReadOnlyList<Entry> entries, HtmlRendererStrings strings, string htmlLang) {
        StringBuilder body = new();
        AppendHeader(body, strings);
        body.Append("<main id=\"main\" class=\"container\">\n");

        if (entries.Count == 0) {
            body.Append($"<p class=\"empty\">{Text(strings.NoDocumentedErrors)}</p>\n");
        } else {
            AppendControls(body, entries, strings);
            AppendCatalogTable(body, entries, strings, entry => $"#err-{Attr(entry.Anchor)}");
            body.Append($"<p id=\"no-results\" class=\"no-results\" hidden>{Text(strings.NoResults)}</p>\n");

            // The detail of every error, inline, each addressable by its #err-<code> anchor.
            foreach (Entry entry in entries) {
                AppendErrorDetail(body, entry, strings, headingLevel: 2);
            }
        }

        body.Append("</main>\n");

        string page = BuildPage(strings.ErrorCatalog, body.ToString(), strings, htmlLang, assetsPrefix: "assets/");

        return [new RenderedDocument("index.html", page)];
    }

    private static List<RenderedDocument> RenderSplit(IReadOnlyList<Entry> entries, HtmlRendererStrings strings, string htmlLang) {
        List<RenderedDocument> documents = [];

        // Home page: the searchable, filterable catalog table linking to one page per error.
        StringBuilder home = new();
        AppendHeader(home, strings);
        home.Append("<main id=\"main\" class=\"container\">\n");

        if (entries.Count == 0) {
            home.Append($"<p class=\"empty\">{Text(strings.NoDocumentedErrors)}</p>\n");
        } else {
            AppendControls(home, entries, strings);
            AppendCatalogTable(home, entries, strings, entry => $"errors/{Attr(entry.FileName)}");
            home.Append($"<p id=\"no-results\" class=\"no-results\" hidden>{Text(strings.NoResults)}</p>\n");
        }

        home.Append("</main>\n");
        documents.Add(new RenderedDocument("index.html", BuildPage(strings.ErrorCatalog, home.ToString(), strings, htmlLang, assetsPrefix: "assets/")));

        // One page per error.
        foreach (Entry entry in entries) {
            StringBuilder body = new();
            AppendHeader(body, strings);
            body.Append("<main id=\"main\" class=\"container\">\n");
            body.Append($"<p class=\"back\"><a href=\"../index.html\">&#8592; {Text(strings.BackToCatalog)}</a></p>\n");
            AppendErrorDetail(body, entry, strings, headingLevel: 2);
            body.Append("</main>\n");

            string title = $"{entry.Title} — {strings.ErrorCatalog}";
            documents.Add(new RenderedDocument($"errors/{entry.FileName}", BuildPage(title, body.ToString(), strings, htmlLang, assetsPrefix: "../assets/")));
        }

        return documents;
    }

    #endregion

    #region Building blocks

    private static string BuildPage(string title, string body, HtmlRendererStrings strings, string htmlLang, string assetsPrefix) {
        StringBuilder page = new();
        page.Append("<!doctype html>\n");
        page.Append($"<html lang=\"{Attr(htmlLang)}\">\n");
        page.Append("<head>\n");
        page.Append("<meta charset=\"utf-8\">\n");
        page.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        page.Append($"<title>{Text(title)}</title>\n");
        page.Append($"<link rel=\"stylesheet\" href=\"{Attr(assetsPrefix)}app.css\">\n");
        // Apply the stored theme before first paint to avoid a flash of the wrong theme.
        page.Append($"<script>{HtmlRendererAssets.ThemeInit}</script>\n");
        page.Append("</head>\n");
        page.Append("<body>\n");
        page.Append($"<a class=\"skip-link\" href=\"#main\">{Text(strings.SkipToContent)}</a>\n");
        page.Append(body);
        page.Append($"<script src=\"{Attr(assetsPrefix)}app.js\"></script>\n");
        page.Append("</body>\n</html>\n");

        return page.ToString();
    }

    private static void AppendHeader(StringBuilder html, HtmlRendererStrings strings) {
        html.Append("<header class=\"site-header\">\n");
        html.Append("<div class=\"container header-row\">\n");
        html.Append($"<h1 class=\"site-title\"><a href=\"#main\">{Text(strings.ErrorCatalog)}</a></h1>\n");
        html.Append($"<button id=\"theme-toggle\" type=\"button\" class=\"theme-toggle\" aria-label=\"{Attr(strings.ThemeToggleLabel)}\" title=\"{Attr(strings.ThemeToggleLabel)}\">{HtmlRendererAssets.ThemeIcon}</button>\n");
        html.Append("</div>\n");
        html.Append("</header>\n");
    }

    private static void AppendControls(StringBuilder html, IReadOnlyList<Entry> entries, HtmlRendererStrings strings) {
        html.Append($"<p class=\"count\">{Text(strings.ErrorsCount(entries.Count))}</p>\n");
        html.Append("<div class=\"controls\">\n");
        html.Append($"<label class=\"search\"><span class=\"visually-hidden\">{Text(strings.Search)}</span>");
        html.Append($"<input id=\"search\" type=\"search\" autocomplete=\"off\" placeholder=\"{Attr(strings.SearchPlaceholder)}\"></label>\n");

        html.Append($"<div class=\"filters\" role=\"group\" aria-label=\"{Attr(strings.FiltersLabel)}\">\n");

        // Source filter (the documented target of each error).
        IReadOnlyList<string> sources = entries.Select(e => e.Source)
                                               .Where(s => string.IsNullOrWhiteSpace(s) is false)
                                               .Select(s => s!.Trim())
                                               .Distinct(StringComparer.Ordinal)
                                               .OrderBy(s => s, StringComparer.Ordinal)
                                               .ToArray();
        if (sources.Count > 0) {
            html.Append($"<label class=\"filter\"><span>{Text(strings.SourceColumn)}</span> <select id=\"filter-source\">");
            html.Append($"<option value=\"\">{Text(strings.AllSources)}</option>");
            foreach (string source in sources) {
                html.Append($"<option value=\"{Attr(source)}\">{Text(source)}</option>");
            }

            html.Append("</select></label>\n");
        }

        html.Append($"<label class=\"filter checkbox\"><input id=\"filter-detail\" type=\"checkbox\"> <span>{Text(strings.WithDetailFilter)}</span></label>\n");
        html.Append("</div>\n</div>\n");
    }

    private static void AppendCatalogTable(StringBuilder html, IReadOnlyList<Entry> entries, HtmlRendererStrings strings, Func<Entry, string> hrefOf) {
        html.Append("<table id=\"catalog\" class=\"catalog\">\n<thead>\n<tr>");
        html.Append($"<th scope=\"col\">{Text(strings.CodeColumn)}</th>");
        html.Append($"<th scope=\"col\">{Text(strings.SummaryColumn)}</th>");
        html.Append($"<th scope=\"col\">{Text(strings.SourceColumn)}</th>");
        html.Append($"<th scope=\"col\" class=\"num\">{Text(strings.DetailColumn)}</th>");
        html.Append($"<th scope=\"col\" class=\"num\">{Text(strings.ExamplesColumn)}</th>");
        html.Append("</tr>\n</thead>\n<tbody>\n");

        foreach (Entry entry in entries) {
            string searchText = Attr(entry.SearchText);
            string hasDetail  = entry.HasDetail ? "true" : "false";
            html.Append($"<tr data-search=\"{searchText}\" data-source=\"{Attr(entry.Source ?? string.Empty)}\" data-detail=\"{hasDetail}\">");
            html.Append($"<td class=\"code\"><a href=\"{Attr(hrefOf(entry))}\"><code>{Text(entry.Code)}</code></a></td>");
            html.Append($"<td>{Text(entry.Summary)}</td>");
            html.Append($"<td>{(string.IsNullOrWhiteSpace(entry.Source) ? string.Empty : $"<span class=\"badge badge-source\">{Text(entry.Source!)}</span>")}</td>");
            html.Append($"<td class=\"num\">{PresenceMark(entry.HasDetail, strings)}</td>");
            html.Append($"<td class=\"num\">{entry.ExampleCount.ToString(CultureInfo.InvariantCulture)}</td>");
            html.Append("</tr>\n");
        }

        html.Append("</tbody>\n</table>\n");
    }

    private static void AppendErrorDetail(StringBuilder html, Entry entry, HtmlRendererStrings strings, int headingLevel) {
        ErrorDocumentation error = entry.Error;
        string             h      = "h" + headingLevel;
        string             hSub   = "h" + (headingLevel + 1);

        html.Append($"<article class=\"error-detail\" id=\"err-{Attr(entry.Anchor)}\">\n");

        // Head: title with a copyable anchor, then the code and source badges.
        html.Append("<div class=\"error-head\">\n");
        html.Append($"<{h} class=\"error-title\">{Text(entry.Title)} <a class=\"anchor\" href=\"#err-{Attr(entry.Anchor)}\" aria-label=\"{Attr(strings.CopyLinkLabel)}\" data-anchor>#</a></{h}>\n");
        html.Append("<div class=\"badges\">");
        html.Append($"<span class=\"badge badge-code\">{Text(entry.Code)}</span>");
        if (string.IsNullOrWhiteSpace(error.Source) is false) { html.Append($"<span class=\"badge badge-source\">{Text(error.Source!.Trim())}</span>"); }

        html.Append("</div>\n</div>\n");

        if (string.IsNullOrWhiteSpace(error.Explanation) is false) {
            html.Append($"<section class=\"doc\"><{hSub}>{Text(strings.DocumentationHeading)}</{hSub}><p>{Text(error.Explanation!.Trim())}</p></section>\n");
        }

        if (string.IsNullOrWhiteSpace(error.BusinessRule) is false) {
            html.Append($"<blockquote class=\"rule\"><strong>{Text(strings.BusinessRuleLabel)}</strong> {Text(error.BusinessRule!.Trim())}</blockquote>\n");
        }

        // Canonical three messages (from the first example), each explicitly labelled — public vs internal.
        ErrorDescription? headline = error.Examples.Count > 0 ? error.Examples[0] : null;
        if (headline is not null) {
            html.Append("<section class=\"messages\">\n");
            html.Append($"<div class=\"msg-card public\"><{hSub}>{Text(strings.PublicSummaryHeading)}</{hSub}><p>{Text(headline.ShortMessage)}</p></div>\n");
            if (string.IsNullOrWhiteSpace(headline.DetailedMessage) is false) {
                html.Append($"<div class=\"msg-card public\"><{hSub}>{Text(strings.PublicDetailHeading)}</{hSub}><p>{Text(headline.DetailedMessage!)}</p></div>\n");
            }

            html.Append($"<div class=\"msg-card internal\"><{hSub}>{Text(strings.DiagnosticHeading)}</{hSub}>");
            html.Append($"<p class=\"note\">{Text(strings.DiagnosticNote)}</p>");
            html.Append($"<pre class=\"log\"><code>{Text(headline.DiagnosticMessage)}</code></pre></div>\n");
            html.Append("</section>\n");
        }

        if (error.Diagnostics.Count > 0) {
            html.Append($"<section class=\"diagnostics\"><{hSub}>{Text(strings.DiagnosticsHeading)}</{hSub}>\n<ul>\n");
            foreach (ErrorDiagnostic diagnostic in error.Diagnostics) {
                html.Append($"<li><strong>{Text(diagnostic.PossibleCause)}</strong> &#8212; <em>{Text(strings.OriginLabel)}</em> {Text(diagnostic.Origin.ToString())} &#8212; {Text(diagnostic.AnalysisHint)}</li>\n");
            }

            html.Append("</ul>\n</section>\n");
        }

        if (error.Examples.Count > 0) {
            html.Append($"<section class=\"examples\"><{hSub}>{Text(strings.ExamplesHeading)}</{hSub}>\n");
            foreach (ErrorDescription example in error.Examples) {
                html.Append("<div class=\"example\">\n");
                html.Append($"<p class=\"example-label\">{Text(strings.PublicResponseLabel)}</p>\n");
                html.Append($"<pre class=\"json\"><code>{Text(ProblemDetailsJson(example, error.Code))}</code></pre>\n");
                html.Append($"<p class=\"example-label internal\">{Text(strings.DiagnosticExampleLabel)}</p>\n");
                html.Append($"<pre class=\"log\"><code>{Text(DiagnosticLogLine(example, error.Code, error.Source))}</code></pre>\n");
                html.Append("</div>\n");
            }

            html.Append("</section>\n");
        }

        if (error.Context.Count > 0) {
            html.Append($"<section class=\"context\"><{hSub}>{Text(strings.ContextHeading)}</{hSub}>\n");
            html.Append("<div class=\"table-wrap\"><table class=\"context-table\">\n<thead>\n<tr>");
            html.Append($"<th scope=\"col\">{Text(strings.ContextKeyHeader)}</th>");
            html.Append($"<th scope=\"col\">{Text(strings.ContextTypeHeader)}</th>");
            html.Append($"<th scope=\"col\">{Text(strings.ContextDescriptionHeader)}</th>");
            html.Append($"<th scope=\"col\">{Text(strings.ContextExampleValuesHeader)}</th>");
            html.Append("</tr>\n</thead>\n<tbody>\n");
            foreach (ErrorContextEntryDocumentation contextEntry in error.Context) {
                html.Append("<tr>");
                html.Append($"<td>{CodeCell(contextEntry.Key)}</td>");
                html.Append($"<td>{CodeCell(contextEntry.ValueType)}</td>");
                html.Append($"<td>{Text(contextEntry.Description ?? string.Empty)}</td>");
                html.Append($"<td>{ExampleValuesCell(contextEntry.ExampleValues)}</td>");
                html.Append("</tr>\n");
            }

            html.Append("</tbody>\n</table></div>\n</section>\n");
        }

        html.Append("</article>\n");
    }

    private static string PresenceMark(bool present, HtmlRendererStrings strings) {
        return present
                   ? $"<span class=\"present\" aria-label=\"{Attr(strings.YesLabel)}\" title=\"{Attr(strings.YesLabel)}\">&#10003;</span>"
                   : "<span class=\"absent\" aria-hidden=\"true\">&#8212;</span>";
    }

    #endregion

    #region Problem detail / log line (public vs internal example rendering)

    /// <summary>
    ///     The public, exposable RFC 9457 (<c>problem+json</c>) representation of an example — public messages plus the
    ///     code, never the diagnostic message. No <c>type</c>/<c>status</c> (the application's concern).
    /// </summary>
    private static string ProblemDetailsJson(ErrorDescription example, string? code) {
        StringBuilder json = new();
        json.Append("{\n");
        json.Append($"  \"title\": \"{JsonString(example.ShortMessage)}\"");
        if (string.IsNullOrWhiteSpace(example.DetailedMessage) is false) {
            json.Append($",\n  \"detail\": \"{JsonString(example.DetailedMessage!)}\"");
        }

        if (string.IsNullOrWhiteSpace(code) is false) {
            json.Append($",\n  \"code\": \"{JsonString(code!.Trim())}\"");
        }

        json.Append("\n}");

        return json.ToString();
    }

    /// <summary>
    ///     A structured log-line rendering of the internal diagnostic message. Illustrative and in the author (invariant)
    ///     language; the timestamp is a fixed sample so the output stays deterministic.
    /// </summary>
    private static string DiagnosticLogLine(ErrorDescription example, string? code, string? source) {
        StringBuilder line = new();
        line.Append("2026-07-04T13:42:18.734Z ERROR");
        if (string.IsNullOrWhiteSpace(source) is false) { line.Append($" [{source!.Trim()}]"); }

        line.Append($" {Inline(example.DiagnosticMessage)}");
        if (string.IsNullOrWhiteSpace(code) is false) { line.Append($" error.code={code!.Trim()}"); }

        return line.ToString();
    }

    private static string JsonString(string value) {
        return Inline(value).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<Entry> BuildEntries(IEnumerable<ErrorDocumentation> catalog) {
        List<Entry>     entries   = [];
        HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);

        // Deterministic order: by code (ordinal), uncoded errors last, keeping first-seen order among equals.
        List<ErrorDocumentation> ordered = catalog
                                          .Select((error, index) => (error, index))
                                          .OrderBy(x => x.error.Code is null)
                                          .ThenBy(x => x.error.Code, StringComparer.Ordinal)
                                          .ThenBy(x => x.index)
                                          .Select(x => x.error)
                                          .ToList();

        int position = 0;
        foreach (ErrorDocumentation error in ordered) {
            position++;

            string code  = FirstNonEmpty(error.Code) ?? $"ERROR_{position}";
            string title = FirstNonEmpty(error.Title, error.Code) ?? $"Error {position}";

            string baseName = SafeFileStem(code);
            if (baseName.Length == 0) { baseName = $"error-{position}"; }

            string name   = baseName;
            int    suffix = 2;
            while (usedNames.Add(name) is false) {
                name = $"{baseName}-{suffix}";
                suffix++;
            }

            entries.Add(new Entry(error, code, title, name));
        }

        return entries;
    }

    private static string BuildSearchIndex(IReadOnlyList<Entry> entries) {
        // A curated, deterministic index for external tooling. In-page search uses the embedded row data instead,
        // so it keeps working when the site is opened from file:// (where fetch of a local JSON is often blocked).
        StringBuilder json = new();
        json.Append("[\n");
        for (int index = 0; index < entries.Count; index++) {
            Entry entry = entries[index];
            json.Append("  {\n");
            json.Append($"    \"code\": \"{JsonString(entry.Code)}\",\n");
            json.Append($"    \"title\": \"{JsonString(entry.Title)}\",\n");
            json.Append($"    \"summary\": \"{JsonString(entry.Summary)}\",\n");
            json.Append($"    \"source\": \"{JsonString(entry.Source ?? string.Empty)}\",\n");
            json.Append($"    \"href\": \"errors/{JsonString(entry.FileName)}\",\n");
            json.Append($"    \"text\": \"{JsonString(entry.SearchText)}\"\n");
            json.Append(index == entries.Count - 1 ? "  }\n" : "  },\n");
        }

        json.Append("]\n");

        return json.ToString();
    }

    private static string HtmlLang(CultureInfo culture) {
        return string.IsNullOrEmpty(culture.Name) ? "en" : culture.TwoLetterISOLanguageName;
    }

    private static string? FirstNonEmpty(params string?[] values) {
        foreach (string? value in values) {
            if (string.IsNullOrWhiteSpace(value) is false) { return value.Trim(); }
        }

        return null;
    }

    /// <summary>Turns an error code into a safe file-name stem, keeping it recognizable (letters, digits, <c>._-</c>).</summary>
    private static string SafeFileStem(string code) {
        StringBuilder builder  = new(code.Length);
        bool          lastDash = false;
        foreach (char character in code.Trim()) {
            if (char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.') {
                builder.Append(character);
                lastDash = false;
            } else if (lastDash is false && builder.Length > 0) {
                builder.Append('-');
                lastDash = true;
            }
        }

        while (builder.Length > 0 && builder[^1] is '-' or '.') { builder.Length--; }

        return builder.ToString();
    }

    /// <summary>HTML-encodes text content (also used for attribute values via <see cref="WebUtility.HtmlEncode" />).</summary>
    private static string Text(string? value) {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string Attr(string? value) {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    /// <summary>Trims a value and folds any line break into a space so it stays on a single line.</summary>
    private static string Inline(string? value) {
        if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }

        return value.Trim().Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string CodeCell(string? value) {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"<code>{Text(value!.Trim())}</code>";
    }

    private static string ExampleValuesCell(IReadOnlyList<string?> values) {
        IEnumerable<string> rendered = values.Where(value => string.IsNullOrWhiteSpace(value) is false)
                                             .Select(value => $"<code>{Text(value!.Trim())}</code>");

        return string.Join(", ", rendered);
    }

    #endregion

    #region Nested type: Entry

    /// <summary>An error paired with its stable code, display title, and file-name stem, plus derived catalog data.</summary>
    private sealed class Entry {

        public Entry(ErrorDocumentation error, string code, string title, string fileStem) {
            Error    = error;
            Code     = code;
            Title    = title;
            Anchor   = fileStem;
            FileName = fileStem + ".html";

            ErrorDescription? first = error.Examples.Count > 0 ? error.Examples[0] : null;
            Summary      = first?.ShortMessage ?? title;
            HasDetail    = error.Examples.Any(example => string.IsNullOrWhiteSpace(example.DetailedMessage) is false);
            ExampleCount = error.Examples.Count;
            Source       = string.IsNullOrWhiteSpace(error.Source) ? null : error.Source!.Trim();
            SearchText   = BuildSearchText(error, code, title);
        }

        public ErrorDocumentation Error { get; }
        public string Code { get; }
        public string Title { get; }

        /// <summary>The <c>#err-…</c> anchor id (single layout) and file-name stem (split layout), code-derived and unique.</summary>
        public string Anchor { get; }

        public string FileName { get; }
        public string Summary { get; }
        public bool HasDetail { get; }
        public int ExampleCount { get; }
        public string? Source { get; }

        /// <summary>Lower-cased, space-joined searchable text (code, title, messages, documentation, context).</summary>
        public string SearchText { get; }

        private static string BuildSearchText(ErrorDocumentation error, string code, string title) {
            List<string?> parts = [code, title, error.Explanation, error.BusinessRule, error.Source];
            foreach (ErrorDescription example in error.Examples) {
                parts.Add(example.ShortMessage);
                parts.Add(example.DetailedMessage);
                parts.Add(example.DiagnosticMessage);
            }

            foreach (ErrorDiagnostic diagnostic in error.Diagnostics) {
                parts.Add(diagnostic.PossibleCause);
                parts.Add(diagnostic.AnalysisHint);
            }

            foreach (ErrorContextEntryDocumentation contextEntry in error.Context) {
                parts.Add(contextEntry.Key);
                parts.Add(contextEntry.Description);
            }

            string joined = string.Join(" ", parts.Where(p => string.IsNullOrWhiteSpace(p) is false)
                                                   .Select(p => p!.Trim().Replace("\r", " ").Replace("\n", " ")));

            return joined.ToLowerInvariant();
        }

    }

    #endregion

}

#region Usings declarations

using System.Globalization;
using System.Net;
using System.Text;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders the catalog as a self-contained static HTML site: a home page with a two-level table of contents
///     (grouped by source, then by error code) plus, in the split layout, one page per error. Everything is emitted as
///     UTF-8 text with no external dependency (no CDN, no binary asset): the CSS and JS are inlined into every page (so
///     each file is self-contained and stays styled even opened on its own), icons are inline SVG, and the font stack is
///     the system default. The only external file is <c>assets/search-index.json</c>, for external tooling. The output
///     is deterministic (no timestamps, errors ordered by code) so it diffs cleanly in source control.
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

        IReadOnlyList<Entry> entries  = BuildEntries(catalog);
        HtmlRendererStrings  strings  = new(request.Culture);
        string               htmlLang = HtmlLang(request.Culture);

        bool split = string.Equals(request.Layout, RenderLayouts.Split, StringComparison.OrdinalIgnoreCase);

        List<RenderedDocument> documents = split
                                               ? RenderSplit(entries, strings, htmlLang)
                                               : RenderSingle(entries, strings, htmlLang);

        // The CSS and JS are inlined into every page (see BuildPage) so each file is self-contained and stays styled
        // even when opened on its own. Only the search index remains an external asset, for external tooling.
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
            IReadOnlyList<Group> groups = GroupBySource(entries);

            AppendSearch(body, entries, strings);
            AppendToc(body, groups, strings, entry => $"#err-{Attr(entry.Anchor)}", group => $"#{Attr(group.Anchor)}");
            body.Append($"<p id=\"no-results\" class=\"no-results\" hidden>{Text(strings.NoResults)}</p>\n");

            // Body grouped by source: a section per source (its errors inlined below).
            foreach (Group group in groups) {
                body.Append($"<section class=\"error-group\" id=\"{Attr(group.Anchor)}\">\n");
                body.Append($"<h2 class=\"group-title\">{Text(group.Label)}</h2>\n");
                foreach (Entry entry in group.Entries) {
                    AppendErrorDetail(body, entry, strings, headingLevel: 3);
                }

                body.Append("</section>\n");
            }
        }

        body.Append("</main>\n");

        return [new RenderedDocument("index.html", BuildPage(strings.ErrorCatalog, body.ToString(), strings, htmlLang))];
    }

    private static List<RenderedDocument> RenderSplit(IReadOnlyList<Entry> entries, HtmlRendererStrings strings, string htmlLang) {
        List<RenderedDocument> documents = [];

        // Home page: the two-level table of contents (source, then error code) linking to one page per error.
        StringBuilder home = new();
        AppendHeader(home, strings);
        home.Append("<main id=\"main\" class=\"container\">\n");

        if (entries.Count == 0) {
            home.Append($"<p class=\"empty\">{Text(strings.NoDocumentedErrors)}</p>\n");
        } else {
            IReadOnlyList<Group> groups = GroupBySource(entries);

            AppendSearch(home, entries, strings);
            AppendToc(home, groups, strings, entry => $"errors/{Attr(entry.FileName)}", _ => null);
            home.Append($"<p id=\"no-results\" class=\"no-results\" hidden>{Text(strings.NoResults)}</p>\n");
        }

        home.Append("</main>\n");
        documents.Add(new RenderedDocument("index.html", BuildPage(strings.ErrorCatalog, home.ToString(), strings, htmlLang)));

        // One page per error.
        foreach (Entry entry in entries) {
            StringBuilder page = new();
            AppendHeader(page, strings);
            page.Append("<main id=\"main\" class=\"container\">\n");
            page.Append($"<p class=\"back\"><a href=\"../index.html\">&#8592; {Text(strings.BackToCatalog)}</a></p>\n");
            AppendErrorDetail(page, entry, strings, headingLevel: 2);
            page.Append("</main>\n");

            string title = $"{entry.Code} — {strings.ErrorCatalog}";
            documents.Add(new RenderedDocument($"errors/{entry.FileName}", BuildPage(title, page.ToString(), strings, htmlLang)));
        }

        return documents;
    }

    #endregion

    #region Building blocks

    private static string BuildPage(string title, string body, HtmlRendererStrings strings, string htmlLang) {
        StringBuilder page = new();
        page.Append("<!doctype html>\n");
        page.Append($"<html lang=\"{Attr(htmlLang)}\">\n");
        page.Append("<head>\n");
        page.Append("<meta charset=\"utf-8\">\n");
        page.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        page.Append($"<title>{Text(title)}</title>\n");
        // Inlined so each page is self-contained (styled and interactive) no matter how it is opened — no external file.
        page.Append($"<style>\n{HtmlRendererAssets.Css}\n</style>\n");
        // Apply the stored theme before first paint to avoid a flash of the wrong theme.
        page.Append($"<script>{HtmlRendererAssets.ThemeInit}</script>\n");
        page.Append("</head>\n");
        page.Append("<body>\n");
        page.Append($"<a class=\"skip-link\" href=\"#main\">{Text(strings.SkipToContent)}</a>\n");
        page.Append(body);
        page.Append($"<script>\n{HtmlRendererAssets.Js}\n</script>\n");
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

    private static void AppendSearch(StringBuilder html, IReadOnlyList<Entry> entries, HtmlRendererStrings strings) {
        html.Append($"<p class=\"count\">{Text(strings.ErrorsCount(entries.Count))}</p>\n");
        html.Append("<div class=\"controls\">\n");
        html.Append($"<label class=\"search\"><span class=\"visually-hidden\">{Text(strings.Search)}</span>");
        html.Append($"<input id=\"search\" type=\"search\" autocomplete=\"off\" placeholder=\"{Attr(strings.SearchPlaceholder)}\"></label>\n");
        html.Append("</div>\n");
    }

    private static void AppendToc(StringBuilder html, IReadOnlyList<Group> groups, HtmlRendererStrings strings, Func<Entry, string> errorHref, Func<Group, string?> groupHref) {
        html.Append($"<nav class=\"toc\" id=\"toc\" aria-label=\"{Attr(strings.ErrorCatalog)}\">\n<ul>\n");
        foreach (Group group in groups) {
            html.Append("<li class=\"toc-group\">");
            string? href = groupHref(group);
            html.Append(href is null
                            ? $"<span class=\"toc-source\">{Text(group.Label)}</span>"
                            : $"<a class=\"toc-source\" href=\"{Attr(href)}\">{Text(group.Label)}</a>");
            html.Append("\n<ul>\n");
            foreach (Entry entry in group.Entries) {
                html.Append($"<li class=\"toc-item\" data-search=\"{Attr(entry.SearchText)}\"><a href=\"{Attr(errorHref(entry))}\"><code>{Text(entry.Code)}</code></a></li>\n");
            }

            html.Append("</ul>\n</li>\n");
        }

        html.Append("</ul>\n</nav>\n");
    }

    private static void AppendErrorDetail(StringBuilder html, Entry entry, HtmlRendererStrings strings, int headingLevel) {
        ErrorDocumentation error = entry.Error;
        string             h      = "h" + headingLevel;
        string             hSub   = "h" + (headingLevel + 1);

        html.Append($"<article class=\"error-detail\" id=\"err-{Attr(entry.Anchor)}\">\n");

        // Head: the error code as the heading (with a copyable anchor), then the human title as a subtitle.
        html.Append($"<{h} class=\"error-title\"><code>{Text(entry.Code)}</code> <a class=\"anchor\" href=\"#err-{Attr(entry.Anchor)}\" aria-label=\"{Attr(strings.CopyLinkLabel)}\" data-anchor>#</a></{h}>\n");
        html.Append($"<p class=\"error-subtitle\">{Text(entry.Title)}</p>\n");

        if (string.IsNullOrWhiteSpace(error.Explanation) is false) {
            html.Append($"<section class=\"doc\"><{hSub}>{Text(strings.DocumentationHeading)}</{hSub}><p>{Text(error.Explanation!.Trim())}</p></section>\n");
        }

        if (string.IsNullOrWhiteSpace(error.BusinessRule) is false) {
            html.Append($"<blockquote class=\"rule\"><strong>{Text(strings.BusinessRuleLabel)}</strong> {Text(error.BusinessRule!.Trim())}</blockquote>\n");
        }

        if (error.Diagnostics.Count > 0) {
            html.Append($"<section class=\"diagnostics\"><{hSub}>{Text(strings.DiagnosticsHeading)}</{hSub}>\n");
            html.Append("<div class=\"table-wrap\"><table class=\"data-table\">\n<thead>\n<tr>");
            html.Append($"<th scope=\"col\">{Text(strings.DiagnosticCauseHeader)}</th>");
            html.Append($"<th scope=\"col\">{Text(strings.DiagnosticOriginHeader)}</th>");
            html.Append($"<th scope=\"col\">{Text(strings.DiagnosticHintHeader)}</th>");
            html.Append("</tr>\n</thead>\n<tbody>\n");
            foreach (ErrorDiagnostic diagnostic in error.Diagnostics) {
                html.Append("<tr>");
                html.Append($"<td>{Text(diagnostic.PossibleCause)}</td>");
                html.Append($"<td>{Text(diagnostic.Origin.ToString())}</td>");
                html.Append($"<td>{Text(diagnostic.AnalysisHint)}</td>");
                html.Append("</tr>\n");
            }

            html.Append("</tbody>\n</table></div>\n</section>\n");
        }

        if (error.Examples.Count > 0) {
            html.Append($"<section class=\"examples\"><{hSub}>{Text(strings.ExamplesHeading)}</{hSub}>\n");
            foreach (ErrorDescription example in error.Examples) {
                html.Append("<div class=\"example\">\n");
                html.Append($"<p class=\"example-label\">{Text(strings.PublicResponseLabel)}</p>\n");
                html.Append($"<pre class=\"json\"><code>{Text(ProblemDetailsJson(example, error.Code))}</code></pre>\n");
                html.Append($"<p class=\"example-label\">{Text(strings.DiagnosticExampleLabel)}</p>\n");
                html.Append($"<pre class=\"log\"><code>{Text(DiagnosticLogLine(example, error.Code, error.Source))}</code></pre>\n");
                html.Append("</div>\n");
            }

            html.Append("</section>\n");
        }

        if (error.Context.Count > 0) {
            html.Append($"<section class=\"context\"><{hSub}>{Text(strings.ContextHeading)}</{hSub}>\n");
            html.Append("<div class=\"table-wrap\"><table class=\"data-table\">\n<thead>\n<tr>");
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

            string baseName = SafeStem(code);
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

    private static IReadOnlyList<Group> GroupBySource(IReadOnlyList<Entry> entries) {
        List<Group>               groups      = [];
        Dictionary<string, Group> byKey       = new(StringComparer.Ordinal);
        HashSet<string>           usedAnchors = new(StringComparer.OrdinalIgnoreCase);

        // Group by source (ProvidesErrorsFor target), preserving first-seen order of both groups and errors.
        foreach (Entry entry in entries) {
            string source = entry.Source ?? "Other";
            if (byKey.TryGetValue(source, out Group? group) is false) {
                group = new Group(source, UniqueAnchor(source, usedAnchors), []);
                byKey[source] = group;
                groups.Add(group);
            }

            group.Entries.Add(entry);
        }

        return groups;
    }

    /// <summary>Builds a unique <c>src-…</c> anchor for a source name (distinct names that share a stem stay distinct).</summary>
    private static string UniqueAnchor(string source, HashSet<string> used) {
        string stem = SafeStem(source);
        if (stem.Length == 0) { stem = "group"; }

        string anchor = "src-" + stem;
        int    suffix = 2;
        while (used.Add(anchor) is false) {
            anchor = $"src-{stem}-{suffix}";
            suffix++;
        }

        return anchor;
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

    /// <summary>Turns a code or source name into a safe file-name / anchor stem (letters, digits, <c>._-</c>).</summary>
    private static string SafeStem(string value) {
        StringBuilder builder  = new(value.Length);
        bool          lastDash = false;
        foreach (char character in value.Trim()) {
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

    #region Nested types

    /// <summary>A source group (from <c>[ProvidesErrorsFor]</c>): its label, anchor id, and the errors under it.</summary>
    private sealed record Group(string Label, string Anchor, List<Entry> Entries);

    /// <summary>An error paired with its stable code, display title, and file-name stem, plus derived catalog data.</summary>
    private sealed class Entry {

        public Entry(ErrorDocumentation error, string code, string title, string fileStem) {
            Error    = error;
            Code     = code;
            Title    = title;
            Anchor   = fileStem;
            FileName = fileStem + ".html";

            ErrorDescription? first = error.Examples.Count > 0 ? error.Examples[0] : null;
            Summary    = first?.ShortMessage ?? title;
            Source     = string.IsNullOrWhiteSpace(error.Source) ? null : error.Source!.Trim();
            SearchText = BuildSearchText(error, code, title);
        }

        public ErrorDocumentation Error { get; }
        public string Code { get; }
        public string Title { get; }

        /// <summary>The <c>#err-…</c> anchor id (single layout) and file-name stem (split layout), code-derived and unique.</summary>
        public string Anchor { get; }

        public string FileName { get; }
        public string Summary { get; }
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

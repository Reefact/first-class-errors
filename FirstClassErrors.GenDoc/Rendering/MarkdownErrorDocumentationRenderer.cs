#region Usings declarations

using System.Text;

#endregion

namespace FirstClassErrors.GenDoc.Rendering;

/// <summary>
///     Renders the catalog as Markdown, either as a single file (a table of contents followed by every error) or
///     split into one file per error plus an index (<c>README.md</c>). The output is deterministic so it diffs
///     cleanly in source control.
/// </summary>
public sealed class MarkdownErrorDocumentationRenderer : IErrorDocumentationRenderer {

    /// <inheritdoc />
    public string Format => "markdown";

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedLayouts { get; } = [RenderLayouts.Single, RenderLayouts.Split];

    /// <inheritdoc />
    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request) {
        if (catalog is null) { throw new ArgumentNullException(nameof(catalog)); }
        if (request is null) { throw new ArgumentNullException(nameof(request)); }

        if (SupportedLayouts.Contains(request.Layout, StringComparer.OrdinalIgnoreCase) is false) {
            throw new LayoutNotSupportedException(Format, request.Layout, SupportedLayouts);
        }

        // Materialize once, assigning each error a stable title and a unique slug used for file names and anchors.
        IReadOnlyList<Entry>    entries = BuildEntries(catalog);
        MarkdownRendererStrings strings = new(request.Culture);

        return string.Equals(request.Layout, RenderLayouts.Split, StringComparison.OrdinalIgnoreCase)
                   ? RenderSplit(entries, strings)
                   : RenderSingle(entries, strings);
    }

    private static IReadOnlyList<RenderedDocument> RenderSingle(IReadOnlyList<Entry> entries, MarkdownRendererStrings strings) {
        StringBuilder markdown = new();
        markdown.Append($"# {strings.ErrorCatalog}\n\n");

        if (entries.Count == 0) {
            markdown.Append($"_{strings.NoDocumentedErrors}_\n");

            return [new RenderedDocument("errors.md", markdown.ToString())];
        }

        IReadOnlyList<Group> groups = GroupBySource(entries, strings);

        // Table of contents: two levels — each source group, then the errors that belong to it.
        markdown.Append($"## {strings.TableOfContents}\n\n");
        foreach (Group group in groups) {
            markdown.Append($"- [{LinkText(group.Label)}](#{group.Anchor})\n");
            foreach (Entry entry in group.Entries) {
                markdown.Append($"  - [{LinkText(entry.Title)}](#err-{entry.Slug})\n");
            }
        }

        markdown.Append('\n');

        // Body: errors grouped under their source heading. Each group and error carries an explicit HTML anchor
        // (portable on GitHub) so the table-of-contents links are deterministic.
        foreach (Group group in groups) {
            markdown.Append($"<a id=\"{group.Anchor}\"></a>\n\n");
            markdown.Append($"## {Inline(group.Label)}\n\n");

            string? description = GroupDescription(group);
            if (description is not null) { markdown.Append(description).Append("\n\n"); }

            foreach (Entry entry in group.Entries) {
                markdown.Append($"<a id=\"err-{entry.Slug}\"></a>\n\n");
                AppendErrorBody(markdown, entry, headingLevel: 3, strings);
            }
        }

        return [new RenderedDocument("errors.md", markdown.ToString())];
    }

    private static IReadOnlyList<RenderedDocument> RenderSplit(IReadOnlyList<Entry> entries, MarkdownRendererStrings strings) {
        List<RenderedDocument> documents = [];

        StringBuilder index = new();
        index.Append($"# {strings.ErrorCatalog}\n\n");

        if (entries.Count == 0) {
            index.Append($"_{strings.NoDocumentedErrors}_\n");
            documents.Add(new RenderedDocument("README.md", index.ToString()));

            return documents;
        }

        IReadOnlyList<Group> groups = GroupBySource(entries, strings);

        // The index (table of contents): each source group linked to its own group file, then its errors.
        foreach (Group group in groups) {
            index.Append($"- [{LinkText(group.Label)}](./{group.FileName})\n");
            foreach (Entry entry in group.Entries) {
                index.Append($"  - [{LinkText(entry.Title)}](./{entry.Slug}.md)\n");
            }
        }

        documents.Add(new RenderedDocument("README.md", index.ToString()));

        // One file per source group: its (optional) description followed by the list of its errors.
        foreach (Group group in groups) {
            StringBuilder groupFile = new();
            groupFile.Append($"# {Inline(group.Label)}\n\n");

            string? description = GroupDescription(group);
            if (description is not null) { groupFile.Append(description).Append("\n\n"); }

            foreach (Entry entry in group.Entries) {
                groupFile.Append($"- [{LinkText(entry.Title)}](./{entry.Slug}.md)\n");
            }

            documents.Add(new RenderedDocument(group.FileName, groupFile.ToString()));
        }

        // One file per error.
        foreach (Entry entry in entries) {
            StringBuilder markdown = new();
            AppendErrorBody(markdown, entry, headingLevel: 1, strings);
            documents.Add(new RenderedDocument($"{entry.Slug}.md", markdown.ToString()));
        }

        return documents;
    }

    private static void AppendErrorBody(StringBuilder markdown, Entry entry, int headingLevel, MarkdownRendererStrings strings) {
        ErrorDocumentation error       = entry.Error;
        string             heading     = new('#', headingLevel);
        string             subHeading  = new('#', headingLevel + 1);

        markdown.Append($"{heading} {Inline(entry.Title)}\n\n");

        bool hasCode   = string.IsNullOrWhiteSpace(error.Code) is false;
        bool hasSource = string.IsNullOrWhiteSpace(error.Source) is false;
        if (hasCode) { markdown.Append($"- **{strings.CodeLabel}** `{error.Code!.Trim()}`\n"); }
        if (hasSource) { markdown.Append($"- **{strings.SourceLabel}** `{error.Source!.Trim()}`\n"); }
        if (hasCode || hasSource) { markdown.Append('\n'); }

        if (string.IsNullOrWhiteSpace(error.Explanation) is false) {
            // A paragraph: keep author line breaks intact.
            markdown.Append(error.Explanation!.Trim()).Append("\n\n");
        }

        if (string.IsNullOrWhiteSpace(error.BusinessRule) is false) {
            markdown.Append($"> **{strings.BusinessRuleLabel}** {Inline(error.BusinessRule)}\n\n");
        }

        if (error.Diagnostics.Count > 0) {
            markdown.Append($"{subHeading} {strings.DiagnosticsHeading}\n\n");
            foreach (ErrorDiagnostic diagnostic in error.Diagnostics) {
                markdown.Append($"- **{Inline(diagnostic.PossibleCause)}** — _{strings.OriginLabel}_ {diagnostic.Origin} — {Inline(diagnostic.AnalysisHint)}\n");
            }

            markdown.Append('\n');
        }

        if (error.Examples.Count > 0) {
            markdown.Append($"{subHeading} {strings.ExamplesHeading}\n\n");
            foreach (ErrorDescription example in error.Examples) {
                // Public, exposable form: an RFC 9457 problem detail built only from the controlled public messages.
                markdown.Append($"**{Inline(strings.ExamplePublicResponseLabel)}**\n\n");
                markdown.Append("```json\n");
                markdown.Append(ProblemDetailsJson(example, error.Code));
                markdown.Append("\n```\n\n");

                // Internal form: how the same failure reads in the logs. Never exposed to external clients.
                markdown.Append($"**{Inline(strings.ExampleDiagnosticLabel)}**\n\n");
                markdown.Append("```text\n");
                markdown.Append(DiagnosticLogLine(example, error.Code, error.Source));
                markdown.Append("\n```\n\n");
            }
        }

        if (error.Context.Count > 0) {
            markdown.Append($"{subHeading} {strings.ContextHeading}\n\n");
            markdown.Append($"| {strings.ContextKeyHeader} | {strings.ContextTypeHeader} | {strings.ContextDescriptionHeader} | {strings.ContextExampleValuesHeader} |\n");
            markdown.Append("| --- | --- | --- | --- |\n");
            foreach (ErrorContextEntryDocumentation contextEntry in error.Context) {
                markdown.Append($"| {CodeCell(contextEntry.Key)} | {CodeCell(contextEntry.ValueType)} | {Cell(contextEntry.Description)} | {ExampleValuesCell(contextEntry.ExampleValues)} |\n");
            }

            markdown.Append('\n');
        }
    }

    #region Helpers

    /// <summary>
    ///     Builds the public, exposable RFC 9457 (<c>problem+json</c>) representation of an example, using only the
    ///     controlled public messages — never the diagnostic message. <c>detail</c> is omitted when there is no public
    ///     detail. Neither <c>type</c> nor <c>status</c> is emitted: both are the application's concern (an absent
    ///     <c>type</c> defaults to <c>about:blank</c> per RFC 9457 §4.1, and the core model is HTTP-agnostic).
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
    ///     A fixed, illustrative timestamp used by the diagnostic log-line examples. A real logger injects the actual
    ///     time at runtime; this constant keeps the rendered documentation deterministic.
    /// </summary>
    private const string SampleLogTimestamp = "2026-07-04T13:42:18.734Z";

    /// <summary>
    ///     Builds a structured log-line rendering of an example's internal diagnostic message: a fixed illustrative
    ///     timestamp, the level, the source used as the logger category, the diagnostic message, and the error code as a
    ///     structured field. It is illustrative, stays in the author (invariant) language, and is never a public message.
    /// </summary>
    private static string DiagnosticLogLine(ErrorDescription example, string? code, string? source) {
        StringBuilder line = new();
        line.Append($"{SampleLogTimestamp} ERROR");
        if (string.IsNullOrWhiteSpace(source) is false) {
            line.Append($" [{source!.Trim()}]");
        }

        line.Append($" {Inline(example.DiagnosticMessage)}");
        if (string.IsNullOrWhiteSpace(code) is false) {
            line.Append($" error.code={code!.Trim()}");
        }

        return line.ToString();
    }

    /// <summary>Escapes a value for inclusion inside a JSON string literal (folding line breaks to spaces first).</summary>
    private static string JsonString(string value) {
        return Inline(value).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static IReadOnlyList<Entry> BuildEntries(IEnumerable<ErrorDocumentation> catalog) {
        List<Entry>     entries   = [];
        HashSet<string> usedSlugs = new(StringComparer.OrdinalIgnoreCase);

        int index = 0;
        foreach (ErrorDocumentation error in catalog) {
            index++;

            string title    = FirstNonEmpty(error.Title, error.Code) ?? $"Error {index}";
            string baseSlug = Slugify(FirstNonEmpty(error.Code, error.Title) ?? string.Empty);
            if (baseSlug.Length == 0) { baseSlug = $"error-{index}"; }

            string slug   = baseSlug;
            int    suffix = 2;
            while (usedSlugs.Add(slug) is false) {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            entries.Add(new Entry(error, title, slug));
        }

        return entries;
    }

    private static IReadOnlyList<Group> GroupBySource(IReadOnlyList<Entry> entries, MarkdownRendererStrings strings) {
        List<Group>               groups = [];
        Dictionary<string, Group> byKey  = new(StringComparer.Ordinal);

        // Group by ProvidesErrorsFor (ErrorDocumentation.Source), preserving first-seen order of both groups and errors.
        // The label is localized, but the anchor and file name stay culture-invariant so links are stable across languages.
        foreach (Entry entry in entries) {
            string source = FirstNonEmpty(entry.Error.Source) ?? "Other";
            if (byKey.TryGetValue(source, out Group? group) is false) {
                string slug = SlugifySource(source);
                group = new Group(strings.GroupLabel(source), $"src-{slug}", $"{slug}-errors.md", []);
                byKey[source] = group;
                groups.Add(group);
            }

            group.Entries.Add(entry);
        }

        return groups;
    }

    private static string? FirstNonEmpty(params string?[] values) {
        foreach (string? value in values) {
            if (string.IsNullOrWhiteSpace(value) is false) { return value.Trim(); }
        }

        return null;
    }

    private static string Slugify(string value) {
        StringBuilder builder  = new(value.Length);
        bool          lastDash = false;
        foreach (char character in value.Trim().ToLowerInvariant()) {
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9')) {
                builder.Append(character);
                lastDash = false;
            } else if (lastDash is false && builder.Length > 0) {
                builder.Append('-');
                lastDash = true;
            }
        }

        while (builder.Length > 0 && builder[^1] == '-') { builder.Length--; }

        return builder.ToString();
    }

    /// <summary>
    ///     Slugifies a source name, first splitting PascalCase/camelCase into words so that
    ///     <c>BankTransactionFileValidator</c> becomes <c>bank-transaction-file-validator</c>.
    /// </summary>
    private static string SlugifySource(string source) {
        StringBuilder spaced = new(source.Length + 8);
        for (int index = 0; index < source.Length; index++) {
            char character = source[index];
            if (index > 0 && char.IsUpper(character) && (char.IsLower(source[index - 1]) || char.IsDigit(source[index - 1]))) {
                spaced.Append(' ');
            }

            spaced.Append(character);
        }

        return Slugify(spaced.ToString());
    }

    /// <summary>Gets the group's source description (shared by its errors), or <c>null</c> when none is set.</summary>
    private static string? GroupDescription(Group group) {
        foreach (Entry entry in group.Entries) {
            if (string.IsNullOrWhiteSpace(entry.Error.SourceDescription) is false) {
                return entry.Error.SourceDescription!.Trim();
            }
        }

        return null;
    }

    /// <summary>Trims a value and folds any line breaks into spaces so it stays on one Markdown line.</summary>
    private static string Inline(string? value) {
        if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }

        return value.Trim().Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
    }

    /// <summary>Inline text safe to place inside a link label (escapes the brackets that would break it).</summary>
    private static string LinkText(string? value) {
        return Inline(value).Replace("[", "\\[").Replace("]", "\\]");
    }

    /// <summary>Inline text safe to place inside a table cell (escapes the pipe that would break the row).</summary>
    private static string Cell(string? value) {
        return Inline(value).Replace("|", "\\|");
    }

    /// <summary>A table cell holding an inline code span, or an empty cell when there is no value.</summary>
    private static string CodeCell(string? value) {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"`{Inline(value).Replace("`", "'")}`";
    }

    private static string ExampleValuesCell(IReadOnlyList<string?> values) {
        IEnumerable<string> rendered = values.Where(value => string.IsNullOrWhiteSpace(value) is false)
                                             .Select(value => CodeCell(value));

        return string.Join(", ", rendered);
    }

    #endregion

    #region Nested type: Entry

    /// <summary>An error paired with the display title and unique slug used for its file name and anchor.</summary>
    private sealed record Entry(ErrorDocumentation Error, string Title, string Slug);

    /// <summary>A source group (from <c>[ProvidesErrorsFor]</c>): its label, heading anchor, split file name, and errors.</summary>
    private sealed record Group(string Label, string Anchor, string FileName, List<Entry> Entries);

    #endregion

}

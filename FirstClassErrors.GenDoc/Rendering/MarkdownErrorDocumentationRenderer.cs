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

    #region Fields declarations

    private readonly MarkdownLayout _layout;

    #endregion

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarkdownErrorDocumentationRenderer" /> class.
    /// </summary>
    /// <param name="layout">The on-disk layout to produce. Defaults to <see cref="MarkdownLayout.Single" />.</param>
    public MarkdownErrorDocumentationRenderer(MarkdownLayout layout = MarkdownLayout.Single) {
        _layout = layout;
    }

    #endregion

    /// <inheritdoc />
    public string Format => "markdown";

    /// <inheritdoc />
    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog) {
        if (catalog is null) { throw new ArgumentNullException(nameof(catalog)); }

        // Materialize once, assigning each error a stable title and a unique slug used for file names and anchors.
        IReadOnlyList<Entry> entries = BuildEntries(catalog);

        return _layout == MarkdownLayout.Split
                   ? RenderSplit(entries)
                   : RenderSingle(entries);
    }

    private static IReadOnlyList<RenderedDocument> RenderSingle(IReadOnlyList<Entry> entries) {
        StringBuilder markdown = new();
        markdown.Append("# Error Catalog\n\n");

        if (entries.Count == 0) {
            markdown.Append("_No documented errors._\n");

            return [new RenderedDocument("errors.md", markdown.ToString())];
        }

        IReadOnlyList<Group> groups = GroupBySource(entries);

        // Table of contents: two levels — each source group, then the errors that belong to it.
        markdown.Append("## Table of contents\n\n");
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
            foreach (Entry entry in group.Entries) {
                markdown.Append($"<a id=\"err-{entry.Slug}\"></a>\n\n");
                AppendErrorBody(markdown, entry, headingLevel: 3);
            }
        }

        return [new RenderedDocument("errors.md", markdown.ToString())];
    }

    private static IReadOnlyList<RenderedDocument> RenderSplit(IReadOnlyList<Entry> entries) {
        List<RenderedDocument> documents = new(entries.Count + 1);

        StringBuilder index = new();
        index.Append("# Error Catalog\n\n");
        if (entries.Count == 0) {
            index.Append("_No documented errors._\n");
        } else {
            // The index is the table of contents: two levels — each source group, then its errors.
            foreach (Group group in GroupBySource(entries)) {
                index.Append($"- {Inline(group.Label)}\n");
                foreach (Entry entry in group.Entries) {
                    index.Append($"  - [{LinkText(entry.Title)}](./{entry.Slug}.md)\n");
                }
            }
        }

        documents.Add(new RenderedDocument("README.md", index.ToString()));

        foreach (Entry entry in entries) {
            StringBuilder markdown = new();
            AppendErrorBody(markdown, entry, headingLevel: 1);
            documents.Add(new RenderedDocument($"{entry.Slug}.md", markdown.ToString()));
        }

        return documents;
    }

    private static void AppendErrorBody(StringBuilder markdown, Entry entry, int headingLevel) {
        ErrorDocumentation error       = entry.Error;
        string             heading     = new('#', headingLevel);
        string             subHeading  = new('#', headingLevel + 1);

        markdown.Append($"{heading} {Inline(entry.Title)}\n\n");

        bool hasCode   = string.IsNullOrWhiteSpace(error.Code) is false;
        bool hasSource = string.IsNullOrWhiteSpace(error.Source) is false;
        if (hasCode) { markdown.Append($"- **Code:** `{error.Code!.Trim()}`\n"); }
        if (hasSource) { markdown.Append($"- **Source:** `{error.Source!.Trim()}`\n"); }
        if (hasCode || hasSource) { markdown.Append('\n'); }

        if (string.IsNullOrWhiteSpace(error.Explanation) is false) {
            // A paragraph: keep author line breaks intact.
            markdown.Append(error.Explanation!.Trim()).Append("\n\n");
        }

        if (string.IsNullOrWhiteSpace(error.BusinessRule) is false) {
            markdown.Append($"> **Business rule:** {Inline(error.BusinessRule)}\n\n");
        }

        if (error.Diagnostics.Count > 0) {
            markdown.Append($"{subHeading} Diagnostics\n\n");
            foreach (ErrorDiagnostic diagnostic in error.Diagnostics) {
                markdown.Append($"- **{Inline(diagnostic.PossibleCause)}** — _origin:_ {diagnostic.Origin} — {Inline(diagnostic.AnalysisHint)}\n");
            }

            markdown.Append('\n');
        }

        if (error.Examples.Count > 0) {
            markdown.Append($"{subHeading} Examples\n\n");
            foreach (ErrorDescription example in error.Examples) {
                markdown.Append($"- {Inline(example.DetailedMessage)}");
                if (string.IsNullOrWhiteSpace(example.ShortMessage) is false) {
                    markdown.Append($" _({Inline(example.ShortMessage)})_");
                }

                markdown.Append('\n');
            }

            markdown.Append('\n');
        }

        if (error.Context.Count > 0) {
            markdown.Append($"{subHeading} Context\n\n");
            markdown.Append("| Key | Type | Description | Example values |\n");
            markdown.Append("| --- | --- | --- | --- |\n");
            foreach (ErrorContextEntryDocumentation contextEntry in error.Context) {
                markdown.Append($"| {CodeCell(contextEntry.Key)} | {CodeCell(contextEntry.ValueType)} | {Cell(contextEntry.Description)} | {ExampleValuesCell(contextEntry.ExampleValues)} |\n");
            }

            markdown.Append('\n');
        }
    }

    #region Helpers

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

    private static IReadOnlyList<Group> GroupBySource(IReadOnlyList<Entry> entries) {
        List<Group>               groups = [];
        Dictionary<string, Group> byKey  = new(StringComparer.Ordinal);

        // Group by ProvidesErrorsFor (ErrorDocumentation.Source), preserving first-seen order of both groups and errors.
        foreach (Entry entry in entries) {
            string source = FirstNonEmpty(entry.Error.Source) ?? "Other";
            if (byKey.TryGetValue(source, out Group? group) is false) {
                group = new Group($"{source} errors", $"src-{Slugify(source)}", []);
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

    /// <summary>A source group (from <c>[ProvidesErrorsFor]</c>), its heading anchor, and the errors within it.</summary>
    private sealed record Group(string Label, string Anchor, List<Entry> Entries);

    #endregion

}

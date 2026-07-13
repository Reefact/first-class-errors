#region Usings declarations

using System.Text;
using System.Text.Json;

#endregion

namespace FirstClassErrors.GenDoc.Versioning;

/// <summary>
///     Renders a <see cref="CatalogDiff" /> as a human-readable report (plain text or Markdown, the latter suited to
///     a pull-request comment) or as a machine-readable JSON document for CI tooling.
/// </summary>
/// <remarks>
///     Every output is deterministic (the diff itself is ordered, and no timestamp is emitted), so a report can be
///     committed, posted or compared verbatim.
/// </remarks>
public static class CatalogDiffFormatter {

    #region Statics members declarations

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true
    };

    /// <summary>
    ///     Renders the diff as a plain-text report, grouped by impact.
    /// </summary>
    /// <param name="diff">The diff to render.</param>
    /// <returns>The plain-text report; a single line when the diff is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diff" /> is <c>null</c>.</exception>
    public static string ToText(CatalogDiff diff) {
        if (diff is null) { throw new ArgumentNullException(nameof(diff)); }

        if (diff.IsEmpty) { return "No catalog changes." + "\n"; }

        StringBuilder report = new();
        AppendTextSection(report, "Breaking changes",      diff.BreakingChanges);
        AppendTextSection(report, "Compatible changes",    diff.CompatibleChanges);
        AppendTextSection(report, "Documentation changes", diff.InformationalChanges);

        return report.ToString();
    }

    /// <summary>
    ///     Renders the diff as a Markdown report, grouped by impact — ready to be posted as a pull-request comment.
    /// </summary>
    /// <param name="diff">The diff to render.</param>
    /// <returns>The Markdown report; a single line when the diff is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diff" /> is <c>null</c>.</exception>
    public static string ToMarkdown(CatalogDiff diff) {
        if (diff is null) { throw new ArgumentNullException(nameof(diff)); }

        StringBuilder report = new();
        report.Append("## Error catalog changes\n");

        if (diff.IsEmpty) {
            report.Append("\nNo catalog changes.\n");

            return report.ToString();
        }

        AppendMarkdownSection(report, "💥 Breaking",      diff.BreakingChanges);
        AppendMarkdownSection(report, "✅ Compatible",    diff.CompatibleChanges);
        AppendMarkdownSection(report, "ℹ️ Documentation", diff.InformationalChanges);

        return report.ToString();
    }

    /// <summary>
    ///     Renders the diff as a machine-readable JSON document (camelCase properties, enum values as camelCase
    ///     strings).
    /// </summary>
    /// <param name="diff">The diff to render.</param>
    /// <returns>The JSON report, ending with a single newline.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diff" /> is <c>null</c>.</exception>
    public static string ToJson(CatalogDiff diff) {
        if (diff is null) { throw new ArgumentNullException(nameof(diff)); }

        // A curated projection: the anonymous shape fixes exactly which fields are published and their names,
        // without exposing the internal model (same approach as the JSON documentation renderer).
        var document = new {
            hasBreakingChanges = diff.BreakingChanges.Count > 0,
            counts = new {
                breaking      = diff.BreakingChanges.Count,
                compatible    = diff.CompatibleChanges.Count,
                informational = diff.InformationalChanges.Count
            },
            changes = diff.Changes.Select(change => new {
                impact      = CamelCase(change.Impact.ToString()),
                kind        = CamelCase(change.Kind.ToString()),
                code        = change.Code,
                description = change.Description
            })
        };

        // Normalize line endings to \n rather than JsonSerializerOptions.NewLine (.NET 9+ only): the tooling must
        // also build on the .NET 8 floor, and a deterministic report diffs and posts identically on every platform.
        return JsonSerializer.Serialize(document, JsonOptions).Replace("\r\n", "\n") + "\n";
    }

    private static void AppendTextSection(StringBuilder report, string heading, IReadOnlyList<CatalogChange> changes) {
        if (changes.Count == 0) { return; }

        if (report.Length > 0) { report.Append('\n'); }

        report.Append($"{heading} ({changes.Count}):\n");
        foreach (CatalogChange change in changes) {
            report.Append($"  - [{Label(change.Kind)}] {change.Code} — {change.Description}\n");
        }
    }

    private static void AppendMarkdownSection(StringBuilder report, string heading, IReadOnlyList<CatalogChange> changes) {
        if (changes.Count == 0) { return; }

        report.Append($"\n### {heading} ({changes.Count})\n\n");
        foreach (CatalogChange change in changes) {
            report.Append($"- **`{change.Code}`** — {change.Description}\n");
        }
    }

    private static string Label(CatalogChangeKind kind) {
        return kind switch {
            CatalogChangeKind.ErrorAdded                 => "added",
            CatalogChangeKind.ErrorRemoved               => "removed",
            CatalogChangeKind.ContextKeyAdded            => "context-added",
            CatalogChangeKind.ContextKeyRemoved          => "context-removed",
            CatalogChangeKind.ContextKeyValueTypeChanged => "context-retyped",
            CatalogChangeKind.TitleChanged               => "title",
            CatalogChangeKind.SourceChanged              => "source",
            _                                            => kind.ToString()
        };
    }

    private static string CamelCase(string value) {
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    #endregion

}

#region Usings declarations

using System.Text.Json;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Versioning.UnitTests;

[TestSubject(typeof(CatalogDiffFormatter))]
public sealed class CatalogDiffFormatterTests {

    private static CatalogDiff SampleDiff() {
        CatalogSnapshot baseline = new() {
            Errors = [
                new CatalogSnapshotEntry { Code = "PAYMENT_DECLINED", Title = "Payment declined" },
                new CatalogSnapshotEntry { Code = "SOME_CODE", Title = "Old title" }
            ]
        };
        CatalogSnapshot current = new() {
            Errors = [
                new CatalogSnapshotEntry { Code = "PAYMENT_REFUSED", Title = "Payment declined" },
                new CatalogSnapshotEntry { Code = "SOME_CODE", Title = "New title" }
            ]
        };

        return CatalogDiffer.Diff(baseline, current);
    }

    private static CatalogDiff EmptyDiff() {
        return CatalogDiffer.Diff(new CatalogSnapshot(), new CatalogSnapshot());
    }

    [Fact(DisplayName = "The text report groups the changes by impact with their counts.")]
    public void TheTextReportGroupsTheChangesByImpact() {
        // Exercise
        string report = CatalogDiffFormatter.ToText(SampleDiff());

        // Verify
        Check.That(report).Contains("Breaking changes (1):");
        Check.That(report).Contains("Compatible changes (1):");
        Check.That(report).Contains("Documentation changes (1):");
        Check.That(report).Contains("[removed] PAYMENT_DECLINED");
        Check.That(report).Contains("possibly renamed to 'PAYMENT_REFUSED'");
        Check.That(report).Contains("[added] PAYMENT_REFUSED");
        Check.That(report).Contains("[title] SOME_CODE");
    }

    [Fact(DisplayName = "The text report of an empty diff is a single 'no changes' line.")]
    public void TheTextReportOfAnEmptyDiffIsASingleLine() {
        // Exercise & verify
        Check.That(CatalogDiffFormatter.ToText(EmptyDiff())).IsEqualTo("No catalog changes.\n");
    }

    [Fact(DisplayName = "The Markdown report has one section per impact, ready for a pull-request comment.")]
    public void TheMarkdownReportHasOneSectionPerImpact() {
        // Exercise
        string report = CatalogDiffFormatter.ToMarkdown(SampleDiff());

        // Verify
        Check.That(report).StartsWith("## Error catalog changes");
        Check.That(report).Contains("### 💥 Breaking (1)");
        Check.That(report).Contains("### ✅ Compatible (1)");
        Check.That(report).Contains("### ℹ️ Documentation (1)");
        Check.That(report).Contains("- **`PAYMENT_DECLINED`**");
    }

    [Fact(DisplayName = "The Markdown report of an empty diff says so under the heading.")]
    public void TheMarkdownReportOfAnEmptyDiffSaysSo() {
        // Exercise
        string report = CatalogDiffFormatter.ToMarkdown(EmptyDiff());

        // Verify
        Check.That(report).Contains("## Error catalog changes");
        Check.That(report).Contains("No catalog changes.");
    }

    [Fact(DisplayName = "The JSON report exposes the counts, the breaking flag and camelCase change entries.")]
    public void TheJsonReportExposesCountsFlagAndChanges() {
        // Exercise
        string json = CatalogDiffFormatter.ToJson(SampleDiff());

        // Verify
        using JsonDocument parsed = JsonDocument.Parse(json);
        JsonElement        root   = parsed.RootElement;

        Check.That(root.GetProperty("hasBreakingChanges").GetBoolean()).IsTrue();
        Check.That(root.GetProperty("counts").GetProperty("breaking").GetInt32()).IsEqualTo(1);
        Check.That(root.GetProperty("counts").GetProperty("compatible").GetInt32()).IsEqualTo(1);
        Check.That(root.GetProperty("counts").GetProperty("informational").GetInt32()).IsEqualTo(1);

        JsonElement changes = root.GetProperty("changes");
        Check.That(changes.GetArrayLength()).IsEqualTo(3);
        JsonElement removal = changes.EnumerateArray().Single(change => change.GetProperty("kind").GetString() == "errorRemoved");
        Check.That(removal.GetProperty("impact").GetString()).IsEqualTo("breaking");
        Check.That(removal.GetProperty("code").GetString()).IsEqualTo("PAYMENT_DECLINED");
    }

    [Fact(DisplayName = "The JSON report of an empty diff has zero counts and no changes.")]
    public void TheJsonReportOfAnEmptyDiffHasZeroCounts() {
        // Exercise
        string json = CatalogDiffFormatter.ToJson(EmptyDiff());

        // Verify
        using JsonDocument parsed = JsonDocument.Parse(json);
        Check.That(parsed.RootElement.GetProperty("hasBreakingChanges").GetBoolean()).IsFalse();
        Check.That(parsed.RootElement.GetProperty("changes").GetArrayLength()).IsEqualTo(0);
    }

    [Fact(DisplayName = "A null diff is rejected by every formatter.")]
    public void ANullDiffIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogDiffFormatter.ToText(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => CatalogDiffFormatter.ToMarkdown(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => CatalogDiffFormatter.ToJson(null!)).Throws<ArgumentNullException>();
    }

}

#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Rendering.UnitTests;

[TestSubject(typeof(MarkdownErrorDocumentationRenderer))]
public sealed class MarkdownErrorDocumentationRendererTests {

    #region Statics members declarations

    private static ErrorDocumentation TemperatureError() {
        return new ErrorDocumentation {
            Code         = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title        = "Temperature below absolute zero",
            Explanation  = "A temperature was instantiated below absolute zero.",
            BusinessRule = "Temperature cannot go below absolute zero.",
            Source       = "Temperature",
            Diagnostics  = new[] { new ErrorDiagnostic("A value entered by a user is invalid.", ErrorOrigin.External, "Verify the user input.") },
            Examples     = new[] { new ErrorDescription("Failed to instantiate temperature: -300 is below absolute zero.", "Below absolute zero.") },
            Context = new[] {
                new ErrorContextEntryDocumentation {
                    Key           = "AttemptedValue",
                    ValueType     = "System.Double",
                    Description   = "The rejected value.",
                    ExampleValues = new[] { "-300" }
                }
            }
        };
    }

    private static ErrorDocumentation EmailError() {
        return new ErrorDocumentation {
            Code        = "INVALID_EMAIL",
            Title       = "Invalid email",
            Explanation = "The provided email address is not valid.",
            Source      = "Email"
        };
    }

    #endregion

    [Fact(DisplayName = "The Markdown renderer declares the 'markdown' format.")]
    public void TheMarkdownRendererDeclaresTheMarkdownFormat() {
        // Exercise & verify
        Check.That(new MarkdownErrorDocumentationRenderer().Format).IsEqualTo("markdown");
    }

    [Fact(DisplayName = "The single layout groups the table of contents by source and inlines every error.")]
    public void TheSingleLayoutGroupsTheTableOfContentsBySource() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Single).Render(new[] { TemperatureError(), EmailError() });

        // Verify
        Check.That(documents).HasSize(1);
        RenderedDocument document = documents[0];
        Check.That(document.RelativePath).IsEqualTo("errors.md");

        string markdown = document.Content;
        Check.That(markdown).StartsWith("# Error Catalog");
        Check.That(markdown).Contains("## Table of contents");

        // The table of contents is two levels: a source group, then the errors nested under it (linked to anchors).
        Check.That(markdown).Contains("- Temperature errors");
        Check.That(markdown).Contains("- Email errors");
        Check.That(markdown).Contains("  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)");
        Check.That(markdown).Contains("  - [Invalid email](#err-invalid-email)");
        Check.That(markdown).Contains("<a id=\"err-temperature-below-absolute-zero\"></a>");

        // Body: a source group heading (h2), the error as a sub-heading (h3), then its sections (h4).
        Check.That(markdown).Contains("## Temperature errors");
        Check.That(markdown).Contains("### Temperature below absolute zero");
        Check.That(markdown).Contains("- **Code:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`");
        Check.That(markdown).Contains("- **Source:** `Temperature`");
        Check.That(markdown).Contains("#### Diagnostics");
        Check.That(markdown).Contains("_origin:_ External");
        Check.That(markdown).Contains("#### Examples");
        Check.That(markdown).Contains("#### Context");
        Check.That(markdown).Contains("| `AttemptedValue` | `System.Double` | The rejected value. | `-300` |");
    }

    [Fact(DisplayName = "The table of contents groups several errors under one source heading.")]
    public void TheTableOfContentsGroupsSeveralErrorsUnderOneSourceHeading() {
        // Setup: two errors share a source, a third has another.
        ErrorDocumentation tooCold  = new() { Code = "TEMP_LOW", Title = "Too cold", Source = "Temperature" };
        ErrorDocumentation tooHot   = new() { Code = "TEMP_HIGH", Title = "Too hot", Source = "Temperature" };
        ErrorDocumentation badEmail = new() { Code = "BAD_EMAIL", Title = "Bad email", Source = "Email" };

        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Single).Render(new[] { tooCold, tooHot, badEmail });

        // Verify
        string markdown = documents[0].Content;
        Check.That(markdown).Contains("- Temperature errors");
        Check.That(markdown).Contains("  - [Too cold](#err-temp-low)");
        Check.That(markdown).Contains("  - [Too hot](#err-temp-high)");
        Check.That(markdown).Contains("- Email errors");
        Check.That(markdown).Contains("  - [Bad email](#err-bad-email)");
    }

    [Fact(DisplayName = "The split layout produces an index grouped by source plus one file per error.")]
    public void TheSplitLayoutProducesAnIndexGroupedBySource() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(new[] { TemperatureError(), EmailError() });

        // Verify
        Check.That(documents).HasSize(3);
        Check.That(documents.Select(document => document.RelativePath))
             .Contains("README.md", "temperature-below-absolute-zero.md", "invalid-email.md");

        RenderedDocument index = documents.Single(document => document.RelativePath == "README.md");
        Check.That(index.Content).StartsWith("# Error Catalog");
        Check.That(index.Content).Contains("- Temperature errors");
        Check.That(index.Content).Contains("- Email errors");
        Check.That(index.Content).Contains("  - [Temperature below absolute zero](./temperature-below-absolute-zero.md)");
        Check.That(index.Content).Contains("  - [Invalid email](./invalid-email.md)");

        RenderedDocument errorFile = documents.Single(document => document.RelativePath == "temperature-below-absolute-zero.md");
        Check.That(errorFile.Content).StartsWith("# Temperature below absolute zero");
        // In the split layout the per-error file title is h1, so its sections are h2.
        Check.That(errorFile.Content).Contains("## Context");
    }

    [Fact(DisplayName = "The split layout on an empty catalog still produces a valid index.")]
    public void TheSplitLayoutOnAnEmptyCatalogProducesAValidIndex() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render([]);

        // Verify
        Check.That(documents).HasSize(1);
        Check.That(documents[0].RelativePath).IsEqualTo("README.md");
        Check.That(documents[0].Content).Contains("_No documented errors._");
    }

    [Fact(DisplayName = "Errors that slugify to the same name get distinct, disambiguated file names.")]
    public void CollidingSlugsAreDisambiguated() {
        // Setup
        ErrorDocumentation first  = new() { Code = "DUP", Title = "First" };
        ErrorDocumentation second = new() { Code = "DUP", Title = "Second" };

        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(new[] { first, second });

        // Verify
        Check.That(documents.Select(document => document.RelativePath)).Contains("dup.md", "dup-2.md");
    }

    [Fact(DisplayName = "An error without a code or title falls back to a generated name and slug.")]
    public void AnErrorWithoutCodeOrTitleFallsBackToAGeneratedName() {
        // Setup: neither Code nor Title is set.
        ErrorDocumentation nameless = new();

        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(new[] { nameless });

        // Verify
        RenderedDocument errorFile = documents.Single(document => document.RelativePath == "error-1.md");
        Check.That(errorFile.Content).StartsWith("# Error 1");
    }

    [Fact(DisplayName = "Slugs strip leading, repeated and trailing separators.")]
    public void SlugsStripLeadingRepeatedAndTrailingSeparators() {
        // Setup: leading, doubled and trailing non-alphanumeric characters.
        ErrorDocumentation error = new() { Code = "!!FOO__BAR!!", Title = "Messy" };

        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Split).Render(new[] { error });

        // Verify
        Check.That(documents.Select(document => document.RelativePath)).Contains("foo-bar.md");
    }

    [Fact(DisplayName = "An empty catalog still produces a valid single document.")]
    public void AnEmptyCatalogProducesAValidSingleDocument() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer(MarkdownLayout.Single).Render([]);

        // Verify
        Check.That(documents).HasSize(1);
        Check.That(documents[0].Content).Contains("_No documented errors._");
    }

    [Fact(DisplayName = "The Markdown renderer guards against a null catalog.")]
    public void TheMarkdownRendererGuardsAgainstANullCatalog() {
        // Exercise & verify
        Check.ThatCode(() => new MarkdownErrorDocumentationRenderer().Render(null!))
             .Throws<ArgumentNullException>();
    }

}

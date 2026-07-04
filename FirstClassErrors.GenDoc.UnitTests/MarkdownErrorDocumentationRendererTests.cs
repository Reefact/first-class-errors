#region Usings declarations

using System.Globalization;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Rendering.UnitTests;

[TestSubject(typeof(MarkdownErrorDocumentationRenderer))]
public sealed class MarkdownErrorDocumentationRendererTests {

    #region Statics members declarations

    private static IReadOnlyList<RenderedDocument> RenderSingle(params ErrorDocumentation[] catalog) {
        return new MarkdownErrorDocumentationRenderer().Render(catalog, new RenderRequest(RenderLayouts.Single));
    }

    private static IReadOnlyList<RenderedDocument> RenderSplit(params ErrorDocumentation[] catalog) {
        return new MarkdownErrorDocumentationRenderer().Render(catalog, new RenderRequest(RenderLayouts.Split));
    }

    private static ErrorDocumentation TemperatureError() {
        return new ErrorDocumentation {
            Code         = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title        = "Temperature below absolute zero",
            Explanation  = "A temperature was instantiated below absolute zero.",
            BusinessRule = "Temperature cannot go below absolute zero.",
            Source       = "Temperature",
            Diagnostics  = new[] { new ErrorDiagnostic("A value entered by a user is invalid.", ErrorOrigin.External, "Verify the user input.") },
            Examples     = new[] { new ErrorDescription("Below absolute zero.", "Failed to instantiate temperature: -300 is below absolute zero.", "The temperature is invalid.") },
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

    [Fact(DisplayName = "The Markdown renderer declares the single and split layouts.")]
    public void TheMarkdownRendererDeclaresTheSingleAndSplitLayouts() {
        // Exercise & verify
        Check.That(new MarkdownErrorDocumentationRenderer().SupportedLayouts).Contains("single", "split");
    }

    [Fact(DisplayName = "The single layout groups the table of contents by source and inlines every error.")]
    public void TheSingleLayoutGroupsTheTableOfContentsBySource() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSingle(TemperatureError(), EmailError());

        // Verify
        Check.That(documents).HasSize(1);
        RenderedDocument document = documents[0];
        Check.That(document.RelativePath).IsEqualTo("errors.md");

        string markdown = document.Content;
        Check.That(markdown).StartsWith("# Error Catalog");
        Check.That(markdown).Contains("## Table of contents");

        // The table of contents is two levels: a source group, then the errors nested under it (linked to anchors).
        Check.That(markdown).Contains("- [Temperature errors](#src-temperature)");
        Check.That(markdown).Contains("- [Email errors](#src-email)");
        Check.That(markdown).Contains("  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)");
        Check.That(markdown).Contains("  - [Invalid email](#err-invalid-email)");
        Check.That(markdown).Contains("<a id=\"err-temperature-below-absolute-zero\"></a>");

        // Body: a source group heading (h2) preceded by its anchor, the error as a sub-heading (h3), sections (h4).
        Check.That(markdown).Contains("<a id=\"src-temperature\"></a>");
        Check.That(markdown).Contains("## Temperature errors");
        Check.That(markdown).Contains("### Temperature below absolute zero");
        Check.That(markdown).Contains("- **Code:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`");
        Check.That(markdown).Contains("- **Source:** `Temperature`");
        Check.That(markdown).Contains("#### Diagnostics");
        Check.That(markdown).Contains("_origin:_ External");
        Check.That(markdown).Contains("#### Examples");

        // Each example is shown two ways: a public RFC 9457 problem detail (short message as title, detailed as detail,
        // code as an extension), and the internal diagnostic rendered as a log line (invariant, flagged internal).
        Check.That(markdown).Contains("**Public response (RFC 9457)**");
        Check.That(markdown).Contains("```json");
        Check.That(markdown).Contains("\"title\": \"Below absolute zero.\"");
        Check.That(markdown).Contains("\"detail\": \"The temperature is invalid.\"");
        Check.That(markdown).Contains("\"code\": \"TEMPERATURE_BELOW_ABSOLUTE_ZERO\"");
        Check.That(markdown).Contains("**Diagnostic (internal — not for external exposure)**");
        Check.That(markdown).Contains("2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: -300 is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO");

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
        IReadOnlyList<RenderedDocument> documents = RenderSingle(tooCold, tooHot, badEmail);

        // Verify
        string markdown = documents[0].Content;
        Check.That(markdown).Contains("- [Temperature errors](#src-temperature)");
        Check.That(markdown).Contains("  - [Too cold](#err-temp-low)");
        Check.That(markdown).Contains("  - [Too hot](#err-temp-high)");
        Check.That(markdown).Contains("- [Email errors](#src-email)");
        Check.That(markdown).Contains("  - [Bad email](#err-bad-email)");
    }

    [Fact(DisplayName = "The split layout produces an index, a file per source group, and a file per error.")]
    public void TheSplitLayoutProducesIndexGroupFilesAndErrorFiles() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSplit(TemperatureError(), EmailError());

        // Verify: README + one group file per source + one file per error.
        Check.That(documents).HasSize(5);
        Check.That(documents.Select(document => document.RelativePath))
             .Contains("README.md", "temperature-errors.md", "email-errors.md",
                       "temperature-below-absolute-zero.md", "invalid-email.md");

        RenderedDocument index = documents.Single(document => document.RelativePath == "README.md");
        Check.That(index.Content).StartsWith("# Error Catalog");
        // The index groups link to the new group files; the errors nest under them.
        Check.That(index.Content).Contains("- [Temperature errors](./temperature-errors.md)");
        Check.That(index.Content).Contains("- [Email errors](./email-errors.md)");
        Check.That(index.Content).Contains("  - [Temperature below absolute zero](./temperature-below-absolute-zero.md)");
        Check.That(index.Content).Contains("  - [Invalid email](./invalid-email.md)");

        RenderedDocument groupFile = documents.Single(document => document.RelativePath == "temperature-errors.md");
        Check.That(groupFile.Content).StartsWith("# Temperature errors");
        Check.That(groupFile.Content).Contains("- [Temperature below absolute zero](./temperature-below-absolute-zero.md)");

        RenderedDocument errorFile = documents.Single(document => document.RelativePath == "temperature-below-absolute-zero.md");
        Check.That(errorFile.Content).StartsWith("# Temperature below absolute zero");
        // In the split layout the per-error file title is h1, so its sections are h2.
        Check.That(errorFile.Content).Contains("## Context");
    }

    [Fact(DisplayName = "A source description is rendered under the group heading (single body and split group file).")]
    public void ASourceDescriptionIsRenderedUnderTheGroupHeading() {
        // Setup
        ErrorDocumentation error = new() {
            Code              = "TEMP_LOW",
            Title             = "Too cold",
            Source            = "Temperature",
            SourceDescription = "Errors about temperature values."
        };

        // Single: the description sits under the group heading.
        string single = RenderSingle(error)[0].Content;
        Check.That(single).Contains("## Temperature errors");
        Check.That(single).Contains("Errors about temperature values.");

        // Split: the description sits in the group file.
        IReadOnlyList<RenderedDocument> split     = RenderSplit(error);
        RenderedDocument                groupFile = split.Single(document => document.RelativePath == "temperature-errors.md");
        Check.That(groupFile.Content).StartsWith("# Temperature errors");
        Check.That(groupFile.Content).Contains("Errors about temperature values.");
    }

    [Fact(DisplayName = "The split layout on an empty catalog still produces a valid index.")]
    public void TheSplitLayoutOnAnEmptyCatalogProducesAValidIndex() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSplit();

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
        IReadOnlyList<RenderedDocument> documents = RenderSplit(first, second);

        // Verify
        Check.That(documents.Select(document => document.RelativePath)).Contains("dup.md", "dup-2.md");
    }

    [Fact(DisplayName = "An error without a code or title falls back to a generated name and slug.")]
    public void AnErrorWithoutCodeOrTitleFallsBackToAGeneratedName() {
        // Setup: neither Code nor Title is set.
        ErrorDocumentation nameless = new();

        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSplit(nameless);

        // Verify
        RenderedDocument errorFile = documents.Single(document => document.RelativePath == "error-1.md");
        Check.That(errorFile.Content).StartsWith("# Error 1");
    }

    [Fact(DisplayName = "Slugs strip leading, repeated and trailing separators.")]
    public void SlugsStripLeadingRepeatedAndTrailingSeparators() {
        // Setup: leading, doubled and trailing non-alphanumeric characters.
        ErrorDocumentation error = new() { Code = "!!FOO__BAR!!", Title = "Messy" };

        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSplit(error);

        // Verify
        Check.That(documents.Select(document => document.RelativePath)).Contains("foo-bar.md");
    }

    [Fact(DisplayName = "An empty catalog still produces a valid single document.")]
    public void AnEmptyCatalogProducesAValidSingleDocument() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSingle();

        // Verify
        Check.That(documents).HasSize(1);
        Check.That(documents[0].Content).Contains("_No documented errors._");
    }

    [Fact(DisplayName = "The Markdown renderer guards against a null catalog.")]
    public void TheMarkdownRendererGuardsAgainstANullCatalog() {
        // Exercise & verify
        Check.ThatCode(() => new MarkdownErrorDocumentationRenderer().Render(null!, new RenderRequest(RenderLayouts.Single)))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The Markdown renderer rejects a layout it does not support.")]
    public void TheMarkdownRendererRejectsAnUnsupportedLayout() {
        // Exercise & verify
        Check.ThatCode(() => new MarkdownErrorDocumentationRenderer().Render(new[] { TemperatureError() }, new RenderRequest("pdf")))
             .Throws<LayoutNotSupportedException>();
    }

    [Fact(DisplayName = "The renderer localizes the template boilerplate for the requested culture.")]
    public void TheRendererLocalizesTheTemplateBoilerplate() {
        // Exercise: render the same catalog for French.
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer().Render(new[] { TemperatureError() },
                                                           new RenderRequest(RenderLayouts.Single, CultureInfo.GetCultureInfo("fr")));
        string markdown = documents[0].Content;

        // Verify: headings, labels and table headers come from the French resources.
        Check.That(markdown).StartsWith("# Catalogue des erreurs");
        Check.That(markdown).Contains("## Table des matières");
        Check.That(markdown).Contains("## Erreurs Temperature");
        Check.That(markdown).Contains("- **Code :**");
        Check.That(markdown).Contains("#### Diagnostics");
        Check.That(markdown).Contains("_origine :_ External");
        Check.That(markdown).Contains("#### Exemples");

        // The example block labels are localized, but the diagnostic log line stays in the invariant author language.
        Check.That(markdown).Contains("**Réponse publique (RFC 9457)**");
        Check.That(markdown).Contains("**Diagnostic (interne — non destiné à l'exposition externe)**");
        Check.That(markdown).Contains("2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: -300 is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        Check.That(markdown).Contains("#### Contexte");
        Check.That(markdown).Contains("| Clé | Type | Description | Exemples de valeurs |");

        // The group anchor stays culture-invariant so cross-language links remain stable.
        Check.That(markdown).Contains("<a id=\"src-temperature\"></a>");
        Check.That(markdown).Contains("- [Erreurs Temperature](#src-temperature)");
    }

}

#region Usings declarations

using System.Globalization;

using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.Usage.Model;

using NFluent;

using VerifyTests;
using VerifyXunit;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     End-to-end snapshots of the real <c>FirstClassErrors.Usage</c> catalog. The catalog is extracted
///     <b>in-process</b> with the same reader the worker runs, so this exercises the real reflection and the real
///     code execution (living examples) without spawning a worker or needing the SDK. The extraction is snapshotted
///     as a model; each rendered output is snapshotted as its own document (JSON / Markdown), not as a wrapper object.
/// </summary>
/// <remarks>
///     On the first run Verify writes <c>*.received.*</c> files and fails; review and approve them (they become
///     <c>*.verified.*</c>) to lock the golden output.
/// </remarks>
public sealed class UsageDocumentationSnapshotTests {

    #region Statics members declarations

    // The Usage catalog is now localized, so the snapshots pin the invariant (English) culture to stay deterministic
    // regardless of the machine's culture.
    private static ErrorDocumentationExtractionResult Extract() {
        return ExtractFor(CultureInfo.InvariantCulture);
    }

    private static ErrorDocumentationExtractionResult ExtractFor(CultureInfo culture) {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try {
            return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(Temperature).Assembly);
        } finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    #endregion

    [Fact(DisplayName = "The extraction of the Usage catalog matches its snapshot.")]
    public async Task TheExtractedUsageCatalog() {
        await Verifier.Verify(Extract());
    }

    [Fact(DisplayName = "The JSON rendering of the Usage catalog matches its snapshot.")]
    public async Task TheJsonRenderingOfTheUsageCatalog() {
        string json = new JsonErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Single))[0].Content;

        await Verifier.Verify(json, extension: "json");
    }

    [Fact(DisplayName = "The single-file Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSingleMarkdownRenderingOfTheUsageCatalog() {
        string markdown = new MarkdownErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Single))[0].Content;

        await Verifier.Verify(markdown, extension: "md");
    }

    [Fact(DisplayName = "Each file of the split Markdown rendering of the Usage catalog matches its snapshot.")]
    public async Task TheSplitMarkdownRenderingOfTheUsageCatalog() {
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Split));

        // A single Verify call that emits one snapshot file per produced document (each file its own pure Markdown,
        // no wrapper). A per-document loop would not work: the first Verify throws on a missing snapshot and aborts
        // the test before the next iteration.
        List<Target> files = documents
                            .Select(document => new Target("md", document.Content, Path.GetFileNameWithoutExtension(document.RelativePath)))
                            .ToList();

        await Verifier.Verify(files);
    }

    [Fact(DisplayName = "The Usage catalog is extracted in the requested language (French).")]
    public void TheUsageCatalogIsExtractedInFrench() {
        // Exercise: extract the real Usage catalog under the French culture. Temperature is intentionally the plain,
        // non-localized example, so this asserts on a source that opts into i18n: Amount.
        ErrorDocumentationExtractionResult result = ExtractFor(CultureInfo.GetCultureInfo("fr"));

        // Verify: the authored prose is localized (title/explanation), and the source group's description — provided
        // as a resource key via [ProvidesErrorsFor(DescriptionResourceType = …)] — is resolved to French too.
        ErrorDocumentation amount =
            result.Documentation.Single(document => document.Code == "AMOUNT_CURRENCY_MISMATCH");

        Check.That(amount.Source).IsEqualTo("Amount");
        Check.That(amount.Title).IsEqualTo("Incohérence de devise entre montants");
        Check.That(amount.Explanation).StartsWith("Cette erreur se produit");
        Check.That(amount.SourceDescription).StartsWith("Erreurs levées lors d'opérations combinant");
    }

}

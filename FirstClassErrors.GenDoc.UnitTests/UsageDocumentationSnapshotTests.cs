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

    // Amount opts into i18n (Temperature is deliberately the plain, non-localized example), so it proves the localized
    // content is resolved per requested culture: the authored prose (title/explanation) AND the source group's
    // description — provided as a resource key via [ProvidesErrorsFor(DescriptionResourceType = …)]. A passing case per
    // language also proves that language's satellite assembly is built and loaded.
    [Theory(DisplayName = "The Usage catalog is extracted in the requested language.")]
    [InlineData("fr", "Incohérence de devise entre montants", "Cette erreur se produit", "Erreurs levées lors d'opérations combinant")]
    [InlineData("es", "Discrepancia de moneda entre importes", "Este error se produce", "Errores generados al realizar operaciones")]
    [InlineData("de", "Währungskonflikt zwischen Beträgen", "Dieser Fehler tritt auf", "Fehler, die bei Operationen ausgelöst werden")]
    [InlineData("sv", "Valutakonflikt mellan belopp", "Det här felet uppstår", "Fel som uppstår vid operationer")]
    public void TheUsageCatalogIsExtractedInTheRequestedLanguage(string culture, string expectedTitle, string explanationPrefix, string sourceDescriptionPrefix) {
        // Exercise: extract the real Usage catalog under the requested culture.
        ErrorDocumentationExtractionResult result = ExtractFor(CultureInfo.GetCultureInfo(culture));

        // Verify
        ErrorDocumentation amount =
            result.Documentation.Single(document => document.Code == "AMOUNT_CURRENCY_MISMATCH");

        Check.That(amount.Source).IsEqualTo("Amount");
        Check.That(amount.Title).IsEqualTo(expectedTitle);
        Check.That(amount.Explanation).StartsWith(explanationPrefix);
        Check.That(amount.SourceDescription).StartsWith(sourceDescriptionPrefix);
    }

}

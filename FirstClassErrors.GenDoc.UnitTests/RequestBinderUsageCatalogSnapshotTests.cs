#region Usings declarations

using System.Globalization;

using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.RequestBinder.Usage.Model;

using VerifyTests;
using VerifyXunit;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     End-to-end snapshots of the real <c>FirstClassErrors.RequestBinder.Usage</c> catalog — the sibling of
///     <see cref="UsageDocumentationSnapshotTests" /> for the request-binder sample. The catalog is extracted in-process
///     with the same reader the worker runs, so this exercises the real reflection and the living examples (the binder
///     sample's documented errors) without spawning a worker or needing the SDK. This is what makes issue #154's goal
///     real for the binder: its errors appear in a generated, snapshot-tested catalog.
/// </summary>
/// <remarks>
///     On the first run Verify writes <c>*.received.*</c> files and fails; review and approve them (they become
///     <c>*.verified.*</c>) to lock the golden output.
/// </remarks>
public sealed class RequestBinderUsageCatalogSnapshotTests {

    #region Statics members declarations

    private const string SampleService = "booking-service";

    // The sample catalog is localized, so the snapshots pin the invariant (English) culture to stay deterministic
    // regardless of the machine's culture.
    private static ErrorDocumentationExtractionResult Extract() {
        return ExtractFor(CultureInfo.InvariantCulture);
    }

    private static ErrorDocumentationExtractionResult ExtractFor(CultureInfo culture) {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try {
            return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(PlaceBookingCommand).Assembly);
        } finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    #endregion

    [Fact(DisplayName = "The extraction of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheExtractedCatalog() {
        await Verifier.Verify(Extract());
    }

    [Fact(DisplayName = "The JSON rendering of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheJsonRendering() {
        string json = new JsonErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Single))[0].Content;

        await Verifier.Verify(json, extension: "json");
    }

    [Fact(DisplayName = "The single-file Markdown rendering of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheSingleMarkdownRendering() {
        string markdown = new MarkdownErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Single, CultureInfo.InvariantCulture, SampleService))[0].Content;

        await Verifier.Verify(markdown, extension: "md");
    }

    [Fact(DisplayName = "Each file of the split Markdown rendering of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheSplitMarkdownRendering() {
        IReadOnlyList<RenderedDocument> documents =
            new MarkdownErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Split, CultureInfo.InvariantCulture, SampleService));

        List<Target> files = documents
                            .Select(document => new Target("md", document.Content, Path.GetFileNameWithoutExtension(document.RelativePath)))
                            .ToList();

        await Verifier.Verify(files);
    }

    [Theory(DisplayName = "The Markdown rendering of the request-binder Usage catalog is localized per language.")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("de")]
    [InlineData("sv")]
    public async Task TheLocalizedMarkdownRendering(string culture) {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);

        string markdown = new MarkdownErrorDocumentationRenderer()
                         .Render(ExtractFor(cultureInfo).Documentation, new RenderRequest(RenderLayouts.Single, cultureInfo, SampleService))[0]
                         .Content;

        await Verifier.Verify(markdown, extension: "md").UseParameters(culture);
    }

    [Fact(DisplayName = "The single-page HTML rendering of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheSingleHtmlRendering() {
        string html = new HtmlErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Single, CultureInfo.InvariantCulture, SampleService))[0].Content;

        await Verifier.Verify(html, extension: "html");
    }

    [Fact(DisplayName = "Each file of the split HTML rendering of the request-binder Usage catalog matches its snapshot.")]
    public async Task TheSplitHtmlRendering() {
        IReadOnlyList<RenderedDocument> documents =
            new HtmlErrorDocumentationRenderer().Render(Extract().Documentation, new RenderRequest(RenderLayouts.Split, CultureInfo.InvariantCulture, SampleService));

        List<Target> files = documents
                            .Select(document => new Target(
                                        Path.GetExtension(document.RelativePath).TrimStart('.'),
                                        document.Content,
                                        Path.ChangeExtension(document.RelativePath, null).Replace('/', '-').Replace('\\', '-')))
                            .ToList();

        await Verifier.Verify(files);
    }

    [Theory(DisplayName = "The HTML rendering of the request-binder Usage catalog is localized per language.")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("de")]
    [InlineData("sv")]
    public async Task TheLocalizedHtmlRendering(string culture) {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);

        string html = new HtmlErrorDocumentationRenderer()
                     .Render(ExtractFor(cultureInfo).Documentation, new RenderRequest(RenderLayouts.Single, cultureInfo, SampleService))[0]
                     .Content;

        await Verifier.Verify(html, extension: "html").UseParameters(culture);
    }

}

#region Usings declarations

using System.Globalization;

using AngleSharp.Html.Parser;

using FirstClassErrors.GenDoc.Rendering;
using FirstClassErrors.Usage.Model;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     Well-formedness gate for the HTML the documentation generator emits. The snapshot tests only lock the output
///     byte-for-byte against an approved oracle, so a stably-malformed page would still pass them. These tests instead
///     parse every generated page with a strict HTML5 parser, so a regression that makes the generator emit invalid
///     markup — an unescaped <c>&lt;</c>, a broken attribute, a misnested or unclosed tag — fails the build rather than
///     being silently baked into a new snapshot.
/// </summary>
/// <remarks>
///     Strict parsing has teeth: it throws on exactly those malformations (verified against unescaped <c>&lt;</c>,
///     unterminated tags, misnesting, stray end tags and duplicate attributes). It is not over-strict either — every
///     document the generator currently emits parses cleanly. The emitted CSS/JS live inside the page, so this also
///     guards the <c>&lt;style&gt;</c>/<c>&lt;script&gt;</c> islands, which SonarJS cannot see from the C# string
///     constants that hold them.
/// </remarks>
public sealed class GeneratedHtmlWellFormednessTests {

    #region Statics members declarations

    private const string SampleService = "sample-service";

    private static ErrorDocumentationExtractionResult ExtractFor(CultureInfo culture) {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try {
            return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(Temperature).Assembly);
        } finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // A fresh parser per call keeps the check safe under xUnit's parallelism (HtmlParser is not meant to be shared
    // across concurrent parses). Strict mode turns any HTML5 parse error into an HtmlParseException.
    private static void CheckWellFormed(string label, string html) {
        HtmlParser parser = new(new HtmlParserOptions { IsStrictMode = true });
        Check.ThatCode(() => parser.ParseDocument(html))
             .As($"generated HTML document '{label}'")
             .DoesNotThrow();
    }

    #endregion

    [Fact(DisplayName = "The single-page HTML rendering of the Usage catalog is well-formed.")]
    public void TheSinglePageHtmlIsWellFormed() {
        string html = new HtmlErrorDocumentationRenderer()
                     .Render(ExtractFor(CultureInfo.InvariantCulture).Documentation, new RenderRequest(RenderLayouts.Single, CultureInfo.InvariantCulture, SampleService))[0]
                     .Content;

        CheckWellFormed("single", html);
    }

    [Fact(DisplayName = "Every HTML page of the split rendering of the Usage catalog is well-formed.")]
    public void EachSplitHtmlPageIsWellFormed() {
        IReadOnlyList<RenderedDocument> documents =
            new HtmlErrorDocumentationRenderer().Render(ExtractFor(CultureInfo.InvariantCulture).Documentation, new RenderRequest(RenderLayouts.Split, CultureInfo.InvariantCulture, SampleService));

        List<RenderedDocument> htmlPages = documents
                                          .Where(document => document.RelativePath.EndsWith(".html", StringComparison.Ordinal))
                                          .ToList();

        // Guard against a vacuous pass: the split layout must produce the index plus one page per error.
        Check.That(htmlPages).Not.IsEmpty();

        foreach (RenderedDocument page in htmlPages) {
            CheckWellFormed(page.RelativePath, page.Content);
        }
    }

    [Theory(DisplayName = "The localized HTML rendering of the Usage catalog is well-formed per language.")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("de")]
    [InlineData("sv")]
    public void TheLocalizedHtmlIsWellFormed(string culture) {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);

        string html = new HtmlErrorDocumentationRenderer()
                     .Render(ExtractFor(cultureInfo).Documentation, new RenderRequest(RenderLayouts.Single, cultureInfo, SampleService))[0]
                     .Content;

        CheckWellFormed($"single:{culture}", html);
    }

}

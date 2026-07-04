#region Usings declarations

using System.Globalization;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Rendering.UnitTests;

[TestSubject(typeof(HtmlErrorDocumentationRenderer))]
public sealed class HtmlErrorDocumentationRendererTests {

    #region Statics members declarations

    private static IReadOnlyList<RenderedDocument> RenderSingle(params ErrorDocumentation[] catalog) {
        return new HtmlErrorDocumentationRenderer().Render(catalog, new RenderRequest(RenderLayouts.Single));
    }

    private static IReadOnlyList<RenderedDocument> RenderSplit(params ErrorDocumentation[] catalog) {
        return new HtmlErrorDocumentationRenderer().Render(catalog, new RenderRequest(RenderLayouts.Split));
    }

    private static string ContentOf(IReadOnlyList<RenderedDocument> documents, string relativePath) {
        return documents.Single(document => document.RelativePath == relativePath).Content;
    }

    private static ErrorDocumentation TemperatureError() {
        return new ErrorDocumentation {
            Code         = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title        = "Temperature below absolute zero",
            Explanation  = "A temperature was instantiated below absolute zero.",
            BusinessRule = "Temperature cannot go below absolute zero.",
            Source       = "Temperature",
            Diagnostics  = new[] { new ErrorDiagnostic("A value entered by a user is invalid.", ErrorOrigin.External, "Verify the user input.") },
            Examples     = new[] { new ErrorDescription("Temperature is invalid.", "Failed to instantiate temperature: -300 is below absolute zero.", "The temperature is invalid.") },
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
            Source      = "Email",
            Examples    = new[] { new ErrorDescription("The email is invalid.", "The email 'x@' is not a valid address.") }
        };
    }

    #endregion

    [Fact(DisplayName = "The HTML renderer declares the 'html' format.")]
    public void TheHtmlRendererDeclaresTheHtmlFormat() {
        Check.That(new HtmlErrorDocumentationRenderer().Format).IsEqualTo("html");
    }

    [Fact(DisplayName = "The HTML renderer declares the single and split layouts.")]
    public void TheHtmlRendererDeclaresTheSingleAndSplitLayouts() {
        Check.That(new HtmlErrorDocumentationRenderer().SupportedLayouts).Contains("single", "split");
    }

    [Fact(DisplayName = "The HTML renderer guards against a null catalog.")]
    public void TheHtmlRendererGuardsAgainstANullCatalog() {
        Check.ThatCode(() => new HtmlErrorDocumentationRenderer().Render(null!, new RenderRequest(RenderLayouts.Single)))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The HTML renderer rejects a layout it does not support.")]
    public void TheHtmlRendererRejectsAnUnsupportedLayout() {
        Check.ThatCode(() => new HtmlErrorDocumentationRenderer().Render(new[] { TemperatureError() }, new RenderRequest("pdf")))
             .Throws<LayoutNotSupportedException>();
    }

    [Fact(DisplayName = "Each page is self-contained: the CSS and JS are inlined, with no external dependency.")]
    public void EachPageIsSelfContained() {
        // Exercise
        IReadOnlyList<RenderedDocument> single = RenderSingle(TemperatureError());
        IReadOnlyList<RenderedDocument> split  = RenderSplit(TemperatureError());

        // Verify: every layout emits its page(s) plus the external search index (the only external asset).
        foreach (IReadOnlyList<RenderedDocument> documents in new[] { single, split }) {
            Check.That(documents.Select(d => d.RelativePath)).Contains("index.html", "assets/search-index.json");
        }

        // The CSS and JS are inlined into the page — no external stylesheet/script link and no remote URL.
        string html = ContentOf(single, "index.html");
        Check.That(html).Contains("<style>");
        Check.That(html).Contains("</style>");
        Check.That(html).Contains("<script>");
        Check.That(html).Not.Contains("<link rel=\"stylesheet\"");
        Check.That(html).Not.Contains("app.css");
        Check.That(html).Not.Contains("http://");
        Check.That(html).Not.Contains("https://");
    }

    [Fact(DisplayName = "The single layout groups a two-level table of contents by source and inlines every error.")]
    public void TheSingleLayoutGroupsAndInlinesEveryError() {
        // Exercise
        string html = ContentOf(RenderSingle(TemperatureError(), EmailError()), "index.html");

        // Verify
        Check.That(html).StartsWith("<!doctype html>");

        // Two-level table of contents: a source group, then the error codes under it (linked to in-page anchors).
        Check.That(html).Contains("<nav class=\"toc\"");
        Check.That(html).Contains("<a class=\"toc-source\" href=\"#src-Temperature\">Temperature</a>");
        Check.That(html).Contains("<li class=\"toc-item\"");
        Check.That(html).Contains("<a href=\"#err-TEMPERATURE_BELOW_ABSOLUTE_ZERO\"><code>TEMPERATURE_BELOW_ABSOLUTE_ZERO</code></a>");

        // Body grouped by source (h2), each error keyed by its code (h3).
        Check.That(html).Contains("<h2 class=\"group-title\">Temperature</h2>");
        Check.That(html).Contains("<h3 class=\"error-title\"><code>TEMPERATURE_BELOW_ABSOLUTE_ZERO</code>");
        Check.That(html).Contains("id=\"err-INVALID_EMAIL\"");

        // Diagnostics render as a table.
        Check.That(html).Contains("<table class=\"data-table\"");
        Check.That(html).Contains("A value entered by a user is invalid.");

        // The dual example rendering (RFC 9457 + internal diagnostic log). The redundant top messages panel is gone.
        Check.That(html).Contains("Public response (RFC 9457)");
        Check.That(html).Contains("Diagnostic (internal");
        Check.That(html).Not.Contains("Internal diagnostic message");
        Check.That(html).Contains("&quot;title&quot;: &quot;Temperature is invalid.&quot;");
        Check.That(html).Contains("2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: -300 is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO");
    }

    [Fact(DisplayName = "The split layout produces a home page and one page per error, linked by code.")]
    public void TheSplitLayoutProducesAHomePageAndOnePagePerError() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSplit(TemperatureError(), EmailError());

        // Verify
        Check.That(documents.Select(d => d.RelativePath))
             .Contains("index.html", "errors/TEMPERATURE_BELOW_ABSOLUTE_ZERO.html", "errors/INVALID_EMAIL.html");

        string home = ContentOf(documents, "index.html");
        Check.That(home).Contains("<a href=\"errors/TEMPERATURE_BELOW_ABSOLUTE_ZERO.html\"");
        Check.That(home).Not.Contains("id=\"err-TEMPERATURE_BELOW_ABSOLUTE_ZERO\"");

        string page = ContentOf(documents, "errors/TEMPERATURE_BELOW_ABSOLUTE_ZERO.html");
        Check.That(page).StartsWith("<!doctype html>");
        Check.That(page).Contains("id=\"err-TEMPERATURE_BELOW_ABSOLUTE_ZERO\"");
        Check.That(page).Contains("<style>");
        Check.That(page).Contains("../index.html");
    }

    [Fact(DisplayName = "The HTML renderer escapes catalog content.")]
    public void TheHtmlRendererEscapesCatalogContent() {
        // Setup: a title and a message carrying HTML metacharacters.
        ErrorDocumentation error = new() {
            Code     = "XSS_TEST",
            Title    = "<script>alert(1)</script>",
            Source   = "Danger & Co",
            Examples = new[] { new ErrorDescription("A <b>bold</b> summary.", "diag \"quoted\" & <angled>") }
        };

        // Exercise
        string html = ContentOf(RenderSingle(error), "index.html");

        // Verify: no raw injected markup survives.
        Check.That(html).Not.Contains("<script>alert(1)</script>");
        Check.That(html).Contains("&lt;script&gt;alert(1)&lt;/script&gt;");
        Check.That(html).Contains("&lt;b&gt;bold&lt;/b&gt;");
        Check.That(html).Contains("Danger &amp; Co");
    }

    [Fact(DisplayName = "The HTML output is deterministic and orders errors by code.")]
    public void TheHtmlOutputIsDeterministicAndOrdersErrorsByCode() {
        // Exercise: render the same catalog twice, in different input order.
        string first  = ContentOf(RenderSplit(TemperatureError(), EmailError()), "index.html");
        string second = ContentOf(RenderSplit(EmailError(), TemperatureError()), "index.html");

        // Verify: identical output regardless of input order, and INVALID_EMAIL sorts before TEMPERATURE_…
        Check.That(first).IsEqualTo(second);
        Check.That(first.IndexOf("INVALID_EMAIL", StringComparison.Ordinal))
             .IsStrictlyLessThan(first.IndexOf("TEMPERATURE_BELOW_ABSOLUTE_ZERO", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "The HTML renderer localizes the labels for the requested culture.")]
    public void TheHtmlRendererLocalizesTheLabels() {
        // Exercise: render for French.
        IReadOnlyList<RenderedDocument> documents =
            new HtmlErrorDocumentationRenderer().Render(new[] { TemperatureError() },
                                                        new RenderRequest(RenderLayouts.Single, CultureInfo.GetCultureInfo("fr")));
        string html = ContentOf(documents, "index.html");

        // Verify: the title and lang come from the French resources; the diagnostic log line stays invariant.
        Check.That(html).Contains("<html lang=\"fr\">");
        Check.That(html).Contains("Catalogue des erreurs");
        Check.That(html).Contains("2026-07-04T13:42:18.734Z ERROR [Temperature]");
    }

    [Fact(DisplayName = "The HTML renderer produces a valid page for an empty catalog.")]
    public void TheHtmlRendererProducesAValidPageForAnEmptyCatalog() {
        // Exercise
        IReadOnlyList<RenderedDocument> documents = RenderSingle();

        // Verify
        string html = ContentOf(documents, "index.html");
        Check.That(html).StartsWith("<!doctype html>");
        Check.That(html).Contains("No documented errors.");
        Check.That(html).Contains("<style>");
        Check.That(documents.Select(d => d.RelativePath)).Contains("assets/search-index.json");
    }

}

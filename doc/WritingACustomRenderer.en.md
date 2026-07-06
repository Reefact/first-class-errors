# Writing a custom renderer

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./WritingACustomRenderer.fr.md)

The built-in `json` and `markdown` formats cover the common cases, but any output format — HTML, CSV, a company documentation template — can be added as a **custom renderer**. A renderer depends only on the documentation model, not on how the catalog was produced, so writing one is small and self-contained.

## The contract

A renderer implements `IErrorDocumentationRenderer` (shipped in the `FirstClassErrors` package, namespace `FirstClassErrors.GenDoc.Rendering`):

```csharp
public interface IErrorDocumentationRenderer {
    // The value selected with `fce generate --format <…>`.
    string Format { get; }

    // The layouts this renderer can produce, e.g. "single", "split" (see RenderLayouts).
    IReadOnlyCollection<string> SupportedLayouts { get; }

    // Turn the catalog into one or more output files for the requested layout and culture.
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request);
}
```

`RenderedDocument` is a `(RelativePath, Content)` pair. Return a single document for a one-file format, or several (an index plus one file per error) for a multi-file one — the `RelativePath` is used as the file name when the output target is a directory.

`RenderRequest` carries the two per-call choices:

* **`Layout`** — the value of `fce generate --layout <…>`. Declare the layouts you support in `SupportedLayouts` and reject any other with `LayoutNotSupportedException` (the built-in `json` renderer supports only `single`; `markdown` supports `single` and `split`). A layout is a free-form string, so a renderer may define its own.
* **`Culture`** — the target language. Localize any boilerplate you emit for `request.Culture` (the error *content* is already localized upstream by the extractor, so a renderer only localizes its own template text). See [Internationalization](Internationalization.en.md).

The contract and the model (`ErrorDocumentation`, `ErrorDiagnostic`, …) ship in the `FirstClassErrors` package, which targets **.NET Standard 2.0** — so a renderer needs only that one reference, which most projects already have.

## A minimal example

```csharp
using System.Linq;

using FirstClassErrors;
using FirstClassErrors.GenDoc.Rendering;

public sealed class CsvErrorDocumentationRenderer : IErrorDocumentationRenderer {

    public string Format => "csv";

    // A single CSV file — this renderer supports only the "single" layout.
    public IReadOnlyCollection<string> SupportedLayouts { get; } = new[] { RenderLayouts.Single };

    public IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request) {
        if (!SupportedLayouts.Contains(request.Layout, StringComparer.OrdinalIgnoreCase)) {
            throw new LayoutNotSupportedException(Format, request.Layout, SupportedLayouts);
        }

        var rows    = catalog.Select(error => $"{error.Code},{Quote(error.Title)}");
        var content = "code,title\n" + string.Join("\n", rows);

        return new[] { new RenderedDocument("errors.csv", content) };
    }

    private static string Quote(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
```

That is a complete renderer. (This CSV has no boilerplate to translate; a renderer that emits headings or labels would read them from resources keyed by `request.Culture`.)

## Plugging it into the CLI

Build your renderer into a library, then register it:

```bash
fce config renderer add ./plugins/MyCompany.Renderers.dll
fce generate --solution MyApp.sln --format csv --output errors.csv
```

`fce config renderer add` records the library path in `fce.json` (you can also edit the file by hand). At generation time the CLI loads the referenced assemblies, discovers every public renderer with a parameterless constructor, and selects the one whose `Format` matches `--format`. `fce config renderer list` shows the built-in and configured formats, and an unknown `--format` lists what is available.

```json
{
  "renderers": ["./plugins/MyCompany.Renderers.dll"]
}
```

Paths are absolute or relative to `fce.json`, so a configuration is portable with its plugins.

### Things to know

* **Parameterless constructor** — the CLI instantiates renderers by reflection.
* **Shared contract** — reference `FirstClassErrors`, but do not ship your own copy of it next to the CLI: the renderer type must resolve to the CLI's contract assembly. Reference it without copying (e.g. `<Private>false</Private>` on the reference), or rely on the identical version already sitting beside the CLI.
* **Target framework** — the CLI loads the plugin into its own process, so build it for a framework the CLI can load.
* **Built-ins win ties** — if a custom renderer declares `json` or `markdown`, the built-in one is used.
* **Failures are tolerated** — a plugin that cannot be loaded is reported as a warning and skipped; it does not abort generation.

## Using a renderer without the CLI

The CLI is optional — a renderer is just a class. If you obtain a catalog yourself (for instance via `SolutionErrorDocumentationGenerator`, in `FirstClassErrors.GenDoc`), rendering it is:

```csharp
// Use one culture for both levels: it localizes the extracted content and the rendered boilerplate.
CultureInfo culture = CultureInfo.GetCultureInfo("en");

IEnumerable<ErrorDocumentation> catalog =
    SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(
        "MyApp.sln", new SolutionGenerationOptions { Culture = culture });

RenderRequest request = new(RenderLayouts.Single, culture);
foreach (RenderedDocument document in new CsvErrorDocumentationRenderer().Render(catalog, request)) {
    File.WriteAllText(document.RelativePath, document.Content);
}
```

---

<table width="100%">
<tr>
<td align="left">Previous section: <a href="ArchitectureOfTheDocumentationPipeline.en.md">Architecture of the Documentation Pipeline</a></td>
<td align="center"><a href="../README.md#-next-steps">📚 Table of contents</a></td>
<td align="right">Next section: <a href="Internationalization.en.md">Internationalization</a></td>
</tr>
</table>

---

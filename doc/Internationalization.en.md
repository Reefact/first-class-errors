# Internationalization

FirstClassErrors can produce the error catalog in several languages. Internationalization is **opt-in and granular**: with no setup the documentation is English, and you localize only what you choose.

Two things can be localized, at two different stages of the pipeline:

| What | Localized when | How |
| --- | --- | --- |
| **Error content** — titles, explanations, rules, diagnostics, messages, source and context descriptions | at **extraction** | your factories read localized resources under the current UI culture |
| **Renderer templates** — headings, labels, table headers | at **rendering** | the renderer reads its own boilerplate for `RenderRequest.Culture` |

Everything else stays **culture-invariant**, so links never break across languages: error codes, source names (`nameof(...)`), `ErrorOrigin` values, and the generated file names and anchors.

The `.Usage` sample ships five languages — English, French, Spanish, German and Swedish (`en`, `fr`, `es`, `de`, `sv`).

## Choosing the language

Pass `--language` (alias `-l`) to `fce generate`, or set a `language` default in `fce.json`; a command-line value overrides the configuration, exactly like the other options. The default is English.

```bash
fce generate --solution ./MyApp.sln --format markdown --language sv --output ./docs/errors
```

```json
{
  "solution": "./MyApp.sln",
  "language": "sv"
}
```

## Level 1 — localizing the error content

Error content is localized at **extraction time**. The generator runs each assembly's worker with `CultureInfo.CurrentUICulture` set to the requested language, so any factory that reads localized resources produces that language. In the sample, the prose is read from a small `ResourceManager` wrapper (`UsageErrorMessages`) backed by a `.resx` per language:

```csharp
private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
    return DescribeError.WithTitle(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Title"))
                        .WithDescription(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Description"))
                        // …rules, diagnostics, examples read the same way
                        ;
}
```

You are free to author plain string literals instead — that error is then simply always in that one language (see [Opt-in and partial localization](#opt-in-and-partial-localization)).

### The source-group description

`[ProvidesErrorsFor]` accepts a `DescriptionResourceType`. When it is set, the extractor treats `Description` as a **resource key** resolved against that type — the same pattern as `[Display(ResourceType = …)]` in DataAnnotations. When it is absent, `Description` is literal text.

```csharp
[ProvidesErrorsFor(nameof(Amount),
                   Description = "Amount_Source",                        // a resource key…
                   DescriptionResourceType = typeof(UsageErrorMessages))] // …resolved against these resources
```

### Context-key descriptions

An `ErrorContextKey` is registered once by its name, but its description can be resolved lazily so it follows the current culture. Use the `Func<string?>` overload of `Create`:

```csharp
public static readonly ErrorContextKey<DateOnly> TransactionDate =
    ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", () => UsageErrorMessages.Get("Bank_TransactionDate_Context"));
```

The key's identity (its name) stays fixed; only the description text is deferred and read under the culture in effect when the documentation is extracted.

## Level 2 — localizing the renderer templates

A renderer's own boilerplate (headings, labels, table headers) is localized at **rendering time**, from `RenderRequest.Culture`. The built-in Markdown renderer reads its strings from a `.resx` set for that culture; the JSON renderer has no boilerplate to translate — its field names are a machine schema, not prose.

A custom renderer localizes its template the same way — see [Writing a custom renderer](WritingACustomRenderer.en.md). The error *content* it receives is already localized upstream, so a renderer only ever localizes its own text.

## Opt-in and partial localization

Internationalization is never forced:

* An error whose `[ProvidesErrorsFor]` has no `DescriptionResourceType` keeps its literal `Description`.
* A factory that authors plain strings (rather than reading resources) is always in that one language.
* Without `--language`, everything renders in English (the invariant culture), byte-for-byte as before i18n existed.

So a project internationalizes only where it wants to. The `.Usage` sample shows both ends: `Temperature` is a plain, non-localized example, while `Amount` and `BankTransactionFileValidator` are fully localized across the five languages.

## Using it without the CLI

When you drive the pipeline yourself, set the **same** culture on both stages so the content and the templates match:

```csharp
CultureInfo culture = CultureInfo.GetCultureInfo("sv");

IEnumerable<ErrorDocumentation> catalog =
    SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(
        "MyApp.sln", new SolutionGenerationOptions { Culture = culture });

RenderRequest request = new(RenderLayouts.Single, culture);
IReadOnlyList<RenderedDocument> documents = new MarkdownErrorDocumentationRenderer().Render(catalog, request);
```

## How the culture flows through the pipeline

| Stage | Culture source | What it localizes |
| --- | --- | --- |
| Worker / extraction | `CultureInfo.CurrentUICulture` (set from `--language`) | error content (titles, explanations, rules, diagnostics, messages, source and context descriptions) |
| Renderer | `RenderRequest.Culture` | the renderer's own templates (headings, labels, table headers) |

Content is localized at extraction; boilerplate at rendering. File names and anchors stay culture-invariant.

---

Previous section: [Writing a custom renderer](WritingACustomRenderer.en.md) | Next section: [FAQ](FAQ.en.md)

---

# Architecture of the Documentation Pipeline

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ArchitectureOfTheDocumentationPipeline.fr.md)

FirstClassErrors does not treat documentation as an external artifact.
Documentation is derived directly from the code and flows through a structured pipeline.

The pipeline separates **knowledge definition**, **extraction**, and **rendering**.

## 🧱 1. Knowledge lives in the code

Error knowledge is written where errors are defined:

* A static class annotated with `[ProvidesErrorsFor(...)]` groups the errors that belong to a given model
* `Error` subtypes (`DomainError`, `PrimaryPortError`, `SecondaryPortError`, ...) represent categories of errors
* Factory methods represent specific error situations
* The `DescribeError` DSL describes meaning, rules, diagnostics, and examples

At this stage, documentation is **structured data**, not text files.

## 🔗 2. Errors are anchored and linked to documentation

A static class declares that it owns the errors of a given model:

```csharp
[ProvidesErrorsFor(nameof(Temperature))]
public static class InvalidTemperatureError { ... }
```

This attribute is the primary anchor of the documentation model: it marks the class as a source of errors and supplies `ErrorDocumentation.Source` (the model name passed via `nameof(...)`). It can also carry an optional `Description`, rendered as an introduction to that source's group in the generated documentation:

```csharp
[ProvidesErrorsFor(nameof(Temperature),
                   Description = "Errors raised when constructing a Temperature value from an out-of-range input.")]
```

The `Description` is literal text by default; set `DescriptionResourceType` to have it resolved as a resource key instead, for localization (see [Internationalization](Internationalization.en.md)).

Inside that class, each factory method is linked to its documentation method using:

```csharp
[DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
```

This creates an explicit connection between:

* how an error is created
* how it is described

## 🔎 3. Extraction

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` scans a single assembly and:

* finds any class annotated with `[ProvidesErrorsFor(...)]` (these are plain static classes, not exception types)
* finds factory methods marked with `[DocumentedBy]`
* **invokes** the linked documentation methods — and the example factories they reference. Documentation is *executable*, so the examples reflect the real code rather than a copy that can drift. A factory that throws, or a `[DocumentedBy]` reference that cannot be resolved, is recorded as a failure instead of aborting the whole scan.
* returns an `ErrorDocumentationExtractionResult`: the `ErrorDocumentation` collection (deduped by `Code`, ordered by `Code`) together with the list of extraction `Failures`

At this stage, documentation becomes a structured in-memory model.

## 🧪 4. Extraction runs out of process

Because extraction **executes** the target's code, each assembly is documented by a short-lived **worker process**, spawned by the generator (`dotnet exec`, using the target's own dependency file). This buys:

* **living examples** — the example factories run against the real code, not a stale description
* a **fresh static registry** per assembly — no state leaks from one assembly to the next
* **version isolation** — each target binds its own FirstClassErrors version
* **fault isolation** — a crashing or hanging assembly is killed on a timeout and recorded as a failure, without taking the whole run down

The worker writes its `ErrorDocumentationExtractionResult` as JSON; the generator reads it back and moves on to the next assembly.

## 🧩 5. Aggregation at solution level

`SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solutionPath[, options])` — or `GetErrorDocumentationFromAssemblies(paths, options)` for pre-built binaries — works at a higher level and:

* discovers the projects (via `dotnet sln list`) and, unless told not to, builds them
* runs a worker for each output assembly
* aggregates all extracted `ErrorDocumentation` (deduped by `Code`, ordered by `Code`)

This produces a **global error catalog** for the application or system.

## 🖨️ 6. Rendering to output formats

A renderer turns the in-memory catalog into published documentation. Because the model is plain data, rendering is decoupled behind a single contract:

```csharp
public interface IErrorDocumentationRenderer {
    string Format { get; }
    IReadOnlyCollection<string> SupportedLayouts { get; }
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog, RenderRequest request);
}
```

Each renderer declares the layouts it can produce and is asked for one per call through the `RenderRequest` (which also carries the target culture); an unsupported layout is rejected with a `LayoutNotSupportedException`. Three renderers ship in the box:

* **json** — a curated, stable JSON schema (`single` layout only)
* **markdown** — a single file, or (with `--layout split`) a README index plus one file per source group and one file per error (`single`/`split`)
* **html** — a self-contained static site: a searchable table of contents grouped by source and, in `split`, one page per error (`single`/`split`). See [The HTML renderer](TheHtmlRenderer.en.md).

Any other format (CSV, a company template, …) is a **custom renderer**: implement the interface and register it. See [Writing a custom renderer](WritingACustomRenderer.en.md).

## 🧰 7. CLI orchestration

The `fce` CLI orchestrates the whole process:

```bash
fce generate --solution ./MyApp.sln --format markdown --layout split --output ./docs/errors
```

It handles the solution build, extraction (via workers), aggregation and rendering. Common options can be stored in a configuration file (`fce.json`) so they need not be repeated, and custom renderers are referenced there too:

```bash
fce config init
fce config renderer add ./plugins/MyCompany.Renderers.dll
fce generate            # uses the configured solution, format, output, renderers…
```

A value passed on the command line overrides the configuration.

## 🌍 8. Internationalization

The pipeline is culture-aware at two levels: the extractor localizes error *content* (under the requested UI culture) and each renderer localizes its own *templates* (from `RenderRequest.Culture`), while file names and anchors stay culture-invariant so links never break. It is opt-in and driven by `fce generate --language`.

See **[Internationalization](Internationalization.en.md)** for the full story — choosing the language, the `DescriptionResourceType` hook, localizing renderer templates, and driving it without the CLI.

## 🔁 Why this architecture matters

This separation ensures:

| Layer     | Responsibility                        |
| --------- | ------------------------------------- |
| Code      | Define error knowledge                |
| Reader    | Extract structured documentation      |
| Worker    | Execute the code in isolation         |
| Generator | Build and aggregate across assemblies |
| Renderer  | Turn the catalog into a target format |
| CLI       | Orchestrate the process               |

Documentation remains:

* close to the code
* always up to date
* structured
* tool-friendly

## 🎯 The key idea

> Error documentation is not written *about* the system.
> It is derived *from* the system.

The code is the source of truth.

---

<table width="100%">
<tr>
<td align="left"><a href="OperationalIntegration.en.md">← CI/CD and Operational Integration</a></td>
<td align="center"><a href="../README.md#-next-steps">↑ Table of contents</a></td>
<td align="right"><a href="WritingACustomRenderer.en.md">Writing a custom renderer →</a></td>
</tr>
</table>

---

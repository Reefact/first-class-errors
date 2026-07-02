# Architecture of the Documentation Pipeline

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

This attribute is the primary anchor of the documentation model: it marks the class as a source of errors and supplies `ErrorDocumentation.Source` (the model name passed via `nameof(...)`).

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
    IReadOnlyList<RenderedDocument> Render(IEnumerable<ErrorDocumentation> catalog);
}
```

Two renderers ship in the box:

* **json** — a curated, stable JSON schema
* **markdown** — a single file, or one file per error plus an index (`--layout single|split`)

Any other format (HTML, CSV, a company template, …) is a **custom renderer**: implement the interface and register it. See [Writing a custom renderer](WritingACustomRenderer.en.md).

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

Previous section: [CI/CD and Operational Integration](OperationalIntegration.en.md) | Next section: [Writing a custom renderer](WritingACustomRenderer.en.md)

---

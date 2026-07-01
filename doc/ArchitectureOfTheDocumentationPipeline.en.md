# Architecture of the Documentation Pipeline

FirstClassErrors does not treat documentation as an external artifact.
Documentation is derived directly from the code and flows through a structured pipeline.

The pipeline separates **knowledge definition**, **extraction**, and **rendering**.

## 🧱 1️. Knowledge lives in the code

Error knowledge is written where errors are defined:

* A static class annotated with `[ProvidesErrorsFor(...)]` groups the errors that belong to a given model
* `Error` subtypes (`DomainError`, `PrimaryPortError`, `SecondaryPortError`, ...) represent categories of errors
* Factory methods represent specific error situations
* The `DescribeError` DSL describes meaning, rules, diagnostics, and examples

At this stage, documentation is **structured data**, not text files.

## 🔗 2️. Errors are anchored and linked to documentation

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

## 🔎 3. Assembly scanning

`AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(assembly)` scans an assembly and:

* finds any class annotated with `[ProvidesErrorsFor(...)]` (these are plain static classes, not exception types)
* finds factory methods marked with `[DocumentedBy]`
* invokes the linked documentation methods
* builds a collection of `ErrorDocumentation` objects (deduped by `Code`, ordered by `Code`)

At this stage, documentation becomes a structured in-memory model.

## 🧩 4️. Aggregation at solution level

`SolutionErrorDocumentationGenerator.GetErrorDocumentationFrom(solutionPath[, options])` works at a higher level and:

* builds a solution
* loads all assemblies
* aggregates all extracted `ErrorDocumentation` (deduped by `Code`, ordered by `Code`)

This produces a **global error catalog** for the application or system.

## 🖨️ 5️. Transformation to output formats (planned)

The library ships the structured in-memory model only; **no exporter is shipped today**. Because the model is plain data, an exporter can be built on top of this library to transform it into:

* Markdown
* HTML
* JSON
* or any other format

Such a transformation layer would be independent of the core model.

## 🧰 6️. CLI orchestration (planned)

There is **no shipped CLI today** (the CLI project is currently a Hello-World stub). A CLI could be built on top of this library to orchestrate the full process, for example:

```
errdocgen --solution ./MyApp.sln --export html
```

Such a CLI would handle:

* solution build
* assembly loading
* extraction
* transformation
* export

## 🔁 Why this architecture matters

This separation ensures:

| Layer              | Responsibility                   |
| ------------------ | -------------------------------- |
| Code               | Define error knowledge           |
| Reader             | Extract structured documentation |
| Builder            | Aggregate across assemblies      |
| Exporter (planned) | Render documentation             |
| CLI (planned)      | Orchestrate the process          |

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

Previous section: [CI/CD and Operational Integration](OperationalIntegration.en.md) | Next section: [FAQ](FAQ.en.md)

---
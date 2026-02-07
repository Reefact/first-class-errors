# Architecture of the Documentation Pipeline

DiagnosableExceptions does not treat documentation as an external artifact.
Documentation is derived directly from the code and flows through a structured pipeline.

The pipeline separates **knowledge definition**, **extraction**, and **rendering**.

## 🧱 1️. Knowledge lives in the code

Error knowledge is written where errors are defined:

* Exception types represent categories of errors
* Factory methods represent specific error situations
* The `DescribeError` DSL describes meaning, rules, diagnostics, and examples

At this stage, documentation is **structured data**, not text files.

## 🔗 2️. Factories are linked to documentation

Each factory method is linked to its documentation using:

```csharp
[DocumentedBy(nameof(CurrencyMismatchDocumentation))]
```

This creates an explicit connection between:

* how an error is created
* how it is described

Factories become the anchor points of the documentation model.

## 🔎 3. Assembly scanning

The `AssemblyErrorDocumentationReader` scans assemblies and:

* finds exception types deriving from `DiagnosableException`
* finds factory methods marked with `[DocumentedBy]`
* invokes documentation methods
* builds a collection of `ErrorDocumentation` objects

At this stage, documentation becomes a structured in-memory model.

## 🧩 4️. Aggregation at solution level

A higher-level tool can:

* build a solution
* load all assemblies
* aggregate all extracted `ErrorDocumentation`

This produces a **global error catalog** for the application or system.

## 🖨️ 5️. Transformation to output formats

The structured model can be transformed into:

* Markdown
* HTML
* JSON
* or any other format

The transformation layer is independent of the core model.

## 🧰 6️. CLI orchestration

A CLI tool can orchestrate the full process:

```
errdocgen --solution ./MyApp.sln --export html
```

It handles:

* solution build
* assembly loading
* extraction
* transformation
* export

## 🔁 Why this architecture matters

This separation ensures:

| Layer    | Responsibility                   |
| -------- | -------------------------------- |
| Code     | Define error knowledge           |
| Reader   | Extract structured documentation |
| Builder  | Aggregate across assemblies      |
| Exporter | Render documentation             |
| CLI      | Orchestrate the process          |

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
# Documentation map

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./DocumentationMap.fr.md)

FirstClassErrors documentation is organized by **reader intent**, not by implementation namespace.

The project README introduces the library and lists every document by area. This page is different: it helps you choose *what to read next* based on what you are trying to accomplish.

Start with the question you are trying to answer. The primary pages then point you toward the focused guides and references for advanced details.

## I am discovering the library

Follow this path when you are deciding whether FirstClassErrors fits your application.

1. [Getting Started](GettingStarted.en.md) — install the library, create an error, and generate a first human-readable error catalog.
2. [Design Principles](DesignPrinciples.en.md) — understand why the error is the model and how it travels (exception or return value) is a separate choice.
3. [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) — identify cases where the library would add more ceremony than value.
4. [Comparison with error-handling libraries](ComparisonWithOtherLibraries.en.md) — compare FirstClassErrors with ErrorOr and FluentResults through one concrete scenario.

## I need to understand the model

Read these pages to understand the model before defining your own usage or a project's conventions.

- [Core Concepts](CoreConcepts.en.md) — `Error`, factories, documentation, exceptions, and `Outcome`.
- [Error Taxonomy and Composition](ErrorTaxonomy.en.md) — domain, infrastructure, and port errors, and how errors nest as structured causes.
- [Error Context Guide](ErrorContext.en.md) — structured facts attached to one occurrence.

## I am writing an error

Use these pages when adding or reviewing an application error.

- [Writing Errors Guide](WritingErrorsGuide.en.md) — code, title, description, rule, diagnostics, and examples.
- [Best Practices](BestPractices.en.md) — project and pull-request review checklist.

Then, as optional complements:

- [Internationalization](Internationalization.en.md) — localize public and documentation content while keeping stable identifiers invariant.
- [Analyzer rules](analyzers/README.md) — the compile-time checks that protect the model and its documentation links.

## I am using errors in application code

- [Usage Patterns](UsagePatterns.en.md) — select the right representation for common situations.
- [Error Context Guide](ErrorContext.en.md) — attach useful, safe occurrence-level facts.
- [Testing Guide](Testing.en.md) — assert outcomes and errors without manual plumbing.

## I am integrating delivery and operations

1. [CI/CD and Operational Integration](OperationalIntegration.en.md) — generate and publish the catalog as part of delivery.
2. [Catalog Versioning — overview and workflow](CatalogVersioning.en.md) — understand snapshots, baselines, and compatibility.
3. [Catalog Versioning — command reference](CatalogVersioningReference.en.md) — find exact CLI options and exit codes.
4. [Catalog Versioning — CI/CD integration](CatalogVersioningCI.en.md) — implement read-only contract checks in pipelines.

To connect runtime failures back to the catalog:

- [Integrate with structured logging](LoggingIntegration.en.md) — what to log, how to preserve inner errors, and how to link an occurrence to the generated catalog.

## I am extending the documentation pipeline

- [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md) — understand the end-to-end model and component responsibilities.
- [Extraction and Project Discovery Reference](DocumentationExtractionReference.en.md) — select projects and assemblies, configure isolated extraction, and handle its failures.
- [Writing a custom renderer](WritingACustomRenderer.en.md) — implement and register another output format.
- [Internationalization](Internationalization.en.md) — understand the extraction/rendering culture boundary.

## I need a reference, not a tutorial

Use these pages when you already understand the model and need exact behavior.

- [Extraction and Project Discovery Reference](DocumentationExtractionReference.en.md) — projects, assemblies, isolated extraction, and failure handling.
- [Catalog Versioning command reference](CatalogVersioningReference.en.md) — exact CLI options and exit codes.
- [Analyzer rules (FCExxx)](analyzers/README.md) — the full list of compile-time diagnostics.

## I am stuck or unsure about a decision

Use these pages when you need to make a design call or weigh options.

- [FAQ](FAQ.en.md) — resolve common design questions and find the relevant focused guide.
- [Usage Patterns](UsagePatterns.en.md) — choose the right representation for a situation.
- [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) — recognize when the library adds more ceremony than value.
- [Comparison with error-handling libraries](ComparisonWithOtherLibraries.en.md) — compare FirstClassErrors with ErrorOr and FluentResults.

## Suggested team reading order

For a team adopting FirstClassErrors, a practical sequence is:

1. Getting Started;
2. Design Principles;
3. Core Concepts;
4. Writing Errors Guide;
5. Usage Patterns;
6. Testing Guide;
7. CI/CD and Operational Integration;
8. Catalog Versioning.

After that shared foundation, specialists can read the architecture, renderer, internationalization, logging, analyzer, and command-reference material relevant to their work.

## Keep one source of truth

Avoid copying large explanations between project guidelines and this documentation.

Project-specific rules should state the local decision and link to the relevant guide. For example:

```text
Application errors must be created through named factories.
See Writing Errors Guide and Best Practices.
```

This keeps local conventions short while allowing the library documentation to evolve without creating several conflicting explanations.

---

<div align="center">
<a href="../README.md">← Project README</a> · <a href="GettingStarted.en.md">Start with Getting Started →</a>
</div>

---
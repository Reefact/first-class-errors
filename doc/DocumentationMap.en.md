# Documentation map

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./DocumentationMap.fr.md)

FirstClassErrors documentation is organized by **reader intent**, not by implementation namespace.

Start with the question you are trying to answer. Each primary page may link to more focused guides or references for advanced details.

## I am discovering the library

Follow this path when you are deciding whether FirstClassErrors fits your application.

1. [Getting Started](GettingStarted.en.md) — install the library, create an error, and generate a first catalog.
2. [Design Principles](DesignPrinciples.en.md) — understand why the error is the model and transport is a separate choice.
3. [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) — identify cases where the library would add more ceremony than value.
4. [Comparison with error-handling libraries](ComparisonWithOtherLibraries.en.md) — compare FirstClassErrors with ErrorOr and FluentResults through one concrete scenario.

## I need to understand the model

Use these pages before defining project-wide conventions.

- [Core Concepts](CoreConcepts.en.md) — `Error`, factories, documentation, exceptions, and `Outcome`.
- [Error Context Guide](ErrorContext.en.md) — structured facts attached to one occurrence.
- [Usage Patterns](UsagePatterns.en.md) — choose between exceptions, outcomes, domain errors, and infrastructure errors.

The primary pages link to any dedicated taxonomy or composition guides available in the current documentation version.

## I am writing an error

Use this path when adding or reviewing an application error.

1. [Writing Errors Guide](WritingErrorsGuide.en.md) — code, title, description, rule, diagnostics, and examples.
2. [Best Practices](BestPractices.en.md) — project and pull-request review checklist.
3. [Internationalization](Internationalization.en.md) — localize public and documentation content while keeping stable identifiers invariant.
4. [Analyzer rules](analyzers/README.md) — understand the compile-time checks that protect the model and documentation links.

## I am using errors in application code

- [Usage Patterns](UsagePatterns.en.md) — select the right representation for common situations.
- [Error Context Guide](ErrorContext.en.md) — attach useful, safe occurrence-level facts.
- [Testing Guide](Testing.en.md) — assert outcomes and errors without manual plumbing.
- [FAQ](FAQ.en.md) — resolve common design questions and find the relevant focused guide.

## I am integrating delivery and operations

1. [CI/CD and Operational Integration](OperationalIntegration.en.md) — generate and publish the catalog as part of delivery.
2. [Catalog Versioning — overview and workflow](CatalogVersioning.en.md) — understand snapshots, baselines, and compatibility.
3. [Catalog Versioning — command reference](CatalogVersioningReference.en.md) — find exact CLI options and exit codes.
4. [Catalog Versioning — CI/CD integration](CatalogVersioningCI.en.md) — implement read-only contract checks in pipelines.

The operational integration pages link to any dedicated logging guidance available in the current documentation version.

## I am extending the documentation pipeline

- [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md) — understand the end-to-end model and component responsibilities.
- [Writing a custom renderer](WritingACustomRenderer.en.md) — implement and register another output format.
- [Internationalization](Internationalization.en.md) — understand the extraction/rendering culture boundary.

The architecture page links to any focused extraction and project-discovery reference available in the current documentation version.

## I need a reference, not a tutorial

Use these pages when you already understand the model and need exact behavior.

- [Catalog Versioning command reference](CatalogVersioningReference.en.md)
- [Analyzer rules](analyzers/README.md)
- [FAQ](FAQ.en.md)

Architecture, renderer, testing, and operational pages may also link to focused reference pages introduced by newer documentation versions.

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
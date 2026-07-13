# CI/CD and Operational Integration

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./OperationalIntegration.fr.md)

FirstClassErrors reaches its full value when it is integrated into the delivery pipeline and operational tooling. The goal is not only to define error knowledge, but to make it automatically available to the people who need it: developers, support teams, and operators.

## 📦 Documentation as a build artifact

Error documentation should be generated automatically during CI.

The `fce` generator is distributed as a .NET tool
([`FirstClassErrors.Cli`](https://www.nuget.org/packages/FirstClassErrors.Cli)), so a
pipeline installs it and runs it as build steps:

1. Install the generator: `dotnet tool install --global FirstClassErrors.Cli`
2. Build the solution
3. Run the generator (`fce generate`) to produce the error catalog (Markdown, HTML or JSON)
4. Publish it as a pipeline artifact or deploy it to a documentation portal

Concretely:

```bash
dotnet tool install --global FirstClassErrors.Cli
dotnet build MyApp.sln -c Release
fce generate --solution MyApp.sln --no-build \
             --output artifacts/errors.md --format markdown --service-name my-api
```

Generation is **opt-in per project**: only projects whose project file (`.csproj`) sets `<GenerateErrorDocumentation>true</GenerateErrorDocumentation>` are scanned; a project without it is silently skipped. The marker must sit in the `.csproj` itself — it is read straight from the project XML, so a value inherited from a shared `Directory.Build.props` is not picked up. When not a single project opts in, the generator logs a warning naming the property rather than producing an empty catalog silently. If a fresh pipeline produces an empty catalog, check the opt-in first. See [Opting a project in](ArchitectureOfTheDocumentationPipeline.en.md#opting-a-project-in).

This ensures that documentation always matches the version of the system that is deployed. No manual updates are required, and no drift can occur.

You can emit the catalog per locale by adding `--language <…>` (e.g. a CI matrix over `en`, `fr`, `sv`); file names and anchors stay stable across languages. See [Internationalization](Internationalization.en.md).

The public RFC 9457 examples carry a problem `type` of the shape `urn:problem:{service}:{code}`. Provide the service segment with `--service-name <name>` (or `serviceName` in `fce.json`); it is required for the `markdown` and `html` formats — `fce generate` fails with a clear message when it is missing — while `json` (which carries no such example) does not need it.

## 🛡️ Guarding the error contract

Error codes and context keys are a public contract: clients branch on them, dashboards alert on them, support procedures reference them. A CI step can guard that contract by comparing the current catalog against a committed baseline and failing the build when a code disappears by accident:

```bash
fce catalog diff --solution MyApp.sln
```

See [Catalog Versioning](CatalogVersioning.en.md) for the baseline workflow, the change classification and the exit-code semantics.

## 🌍 Publishing documentation

The generated documentation can be:

* published to an internal documentation portal
* exposed via a static site
* attached to release artifacts

The important principle is:

> The documentation must be reachable by the people investigating production issues.

## 📜 Logging integration

FirstClassErrors are designed to integrate naturally with structured logging.

Logs can include:

* the error code
* the unique instance ID
* the occurrence timestamp
* the error context

This makes logs not only readable but also correlatable across systems.

## 🔍 Logging inner errors

By default, most logging setups treat exceptions as flat messages or stack traces. They do not automatically traverse and structure the diagnostic information carried by a `DiagnosableException` in a meaningful way for analysis.

A `DiagnosableException` does not set `Exception.InnerException`; instead, the diagnostic chain lives on its `Error`. Through `exception.Error.InnerErrors` (a list of `Error`), a logging filter or middleware should explicitly traverse and log the chain. Without this step, part of the diagnostic information carried by the model may remain unused in logs.

This filter can:

* detect `DiagnosableException`
* read its `.Error`
* traverse `Error.InnerErrors` and log the full chain in a structured form

This preserves diagnostic depth and ensures that the richness of the error model is actually visible in operational logs.

## 🔗 Linking logs to documentation

A powerful pattern is to enrich the thrown exception (via its `Error`) with a documentation URL.

During the documentation generation step, each error can be associated with a page or anchor. A logging filter can then populate:

```
exception.HelpLink = "https://docs.mycompany/errors/AMOUNT_CURRENCY_MISMATCH"
```

This makes production logs navigable: support can move directly from a log entry to the corresponding error documentation.

## 🧩 Complementary to structured logging

FirstClassErrors do not replace structured logging, scopes, or correlation IDs.

They complement them:

* structured logging → technical context
* scopes → execution context
* FirstClassErrors → semantic error meaning

Together, they provide a complete picture of what happened.

## 🎯 The objective

Industrial integration turns errors into a shared operational language.

Errors become:

* documented
* traceable
* searchable
* actionable

**automatically**, as part of the build and delivery process — without relying on manual documentation efforts.

---

<div align="center">
<a href="Testing.en.md">← Testing Guide</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="CatalogVersioning.en.md">Catalog Versioning →</a>
</div>

---
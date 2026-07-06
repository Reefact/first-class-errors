# CI/CD and Operational Integration

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./OperationalIntegration.fr.md)

FirstClassErrors reaches its full value when it is integrated into the delivery pipeline and operational tooling. The goal is not only to define error knowledge, but to make it automatically available to the people who need it: developers, support teams, and operators.

## 📦 Documentation as a build artifact

Error documentation should be generated automatically during CI.

A typical pipeline step:

1. Build the solution
2. Run the `fce` documentation generator (`fce generate`)
3. Generate the error catalog (Markdown or JSON)
4. Publish it as a pipeline artifact or deploy it to a documentation portal

This ensures that documentation always matches the version of the system that is deployed. No manual updates are required, and no drift can occur.

You can emit the catalog per locale by adding `--language <…>` (e.g. a CI matrix over `en`, `fr`, `sv`); file names and anchors stay stable across languages. See [Internationalization](Internationalization.en.md).

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

<table width="100%">
<tr>
<td align="left">Previous section: <a href="BestPractices.en.md">Best Practices</a></td>
<td align="center"><a href="../README.md#-next-steps">📚 Table of contents</a></td>
<td align="right">Next section: <a href="ArchitectureOfTheDocumentationPipeline.en.md">Architecture of the Documentation Pipeline</a></td>
</tr>
</table>

---
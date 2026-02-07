# CI/CD and Operational Integration

DiagnosableExceptions reaches its full value when it is integrated into the delivery pipeline and operational tooling. The goal is not only to define error knowledge, but to make it automatically available to the people who need it: developers, support teams, and operators.

## 📦 Documentation as a build artifact

Error documentation should be generated automatically during CI.

A typical pipeline step:

1. Build the solution
2. Run the documentation extraction tool
3. Generate the error catalog (HTML/Markdown)
4. Publish it as a pipeline artifact or deploy it to a documentation portal

This ensures that documentation always matches the version of the system that is deployed. No manual updates are required, and no drift can occur.

## 🌍 Publishing documentation

The generated documentation can be:

* published to an internal documentation portal
* exposed via a static site
* attached to release artifacts

The important principle is:

> The documentation must be reachable by the people investigating production issues.

## 📜 Logging integration

DiagnosableExceptions are designed to integrate naturally with structured logging.

Logs can include:

* `ErrorCode`
* `ShortMessage`
* `InstanceId`
* `OccurredAt`
* diagnostics context

This makes logs not only readable but also correlatable across systems.

## 🔍 Logging inner exceptions

By default, most logging setups treat exceptions as flat messages or stack traces. They do not automatically traverse and structure multiple inner exceptions in a meaningful way for analysis.

Since `DiagnosableException` can aggregate several inner exceptions, a logging filter or middleware should explicitly extract and log them. Without this step, part of the diagnostic information carried by the model may remain unused in logs.

This filter can:

* detect `DiagnosableException`
* extract its inner exceptions
* log the full chain in a structured form

This preserves diagnostic depth and ensures that the richness of the error model is actually visible in operational logs.

## 🔗 Linking logs to documentation

A powerful pattern is to enrich diagnosable exceptions with a documentation URL.

During the documentation generation step, each error can be associated with a page or anchor. A logging filter can then populate:

```
exception.HelpLink = "https://docs.mycompany/errors/AMOUNT_CURRENCY_MISMATCH"
```

This makes production logs navigable: support can move directly from a log entry to the corresponding error documentation.

## 🧩 Complementary to structured logging

DiagnosableExceptions do not replace structured logging, scopes, or correlation IDs.

They complement them:

* structured logging → technical context
* scopes → execution context
* DiagnosableExceptions → semantic error meaning

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

Previous section: [Best Practices](BestPractices.en.md) | Next section: [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md)

---
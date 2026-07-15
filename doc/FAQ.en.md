# FAQ

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./FAQ.fr.md)

## Why not just use normal exceptions?

You can. FirstClassErrors still uses standard .NET exceptions as the mechanism that signals and propagates failures.

FirstClassErrors attaches a structured `Error` to that exception — carrying a stable code, structured context, diagnostics, and linked documentation. See [Core Concepts](CoreConcepts.en.md).

## Why not use a generic `Result<T, Error>`?

You could. Carrying the library’s `Error` in a general-purpose result type — for example [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions)’ `Result<T, E>` — keeps the structure a bare `Result<T, string>` would lose.

`Outcome` is that idea specialized for this error model. Its failure side is always an `Error` — not a second type parameter to thread through every signature — and its small API (`Then`, `Recover`, `Finally`) is named for intent rather than for functional-programming mechanics (`Map`, `Bind`, `Match`). The goal is domain code and use cases that read as a business flow, not as generic result plumbing.

See [Usage Patterns](UsagePatterns.en.md) and [Comparison with error-handling libraries](ComparisonWithOtherLibraries.en.md).

## Does FirstClassErrors replace logging?

No. It structures errors and their documentation; your logging system records their occurrences and runtime context. A first-class error gives each logged occurrence a stable code and a shared explanation, but it neither stores nor replaces the log itself.

See [Integrate with structured logging](LoggingIntegration.en.md).

## Is this too heavy for a simple application?

It can be. Small scripts, prototypes, and systems without long-term support needs may be better served by standard exceptions.

See [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) for the decision criteria.

## Can I adopt FirstClassErrors incrementally?

Yes. You do not need a global migration. Introduce it where errors are worth modeling — one domain, module, or use case at a time — and leave the rest on standard exceptions until they earn a first-class error. Because a factory is just a typed way to build an `Error`, existing code keeps working while new or reworked paths adopt the model.

See [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md).

## When should I use `Outcome<T>`?

Use it when failure is an expected branch of normal flow, such as validation, parsing, batch processing, or partial success.

Use an exception when the failure should interrupt the operation at that level. Both paths can carry the same `Error` created by the same factory.

See [Usage Patterns](UsagePatterns.en.md).

## Does `Outcome<T>` preserve a stack trace?

It does not create or throw an exception while the error is carried as data. If the failure is later escalated with `GetResultOrThrow()` or `error.ToException()`, the exception and its stack trace start at that escalation point.

## Should every exception become a first-class error?

No. Model the meaningful application errors: recognized situations, rules, constraints, or boundary failures that benefit from a stable identity and a shared explanation. Framework exceptions, accidental crashes, and low-level implementation faults usually stay as plain technical exceptions.

See [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md).

## How do I handle an exception from a third-party library?

Catch it at the boundary — typically an adapter or secondary port — and decide whether it represents a stable application failure. If it does, model it with a factory as an `InfrastructureError` (or a `SecondaryPortError` for an outgoing dependency, a `PrimaryPortError` for an incoming one), put the technical detail in the `DiagnosticMessage`, and add safe facts through `ErrorContext`.

The first-class error captures the *meaning* of the failure; it does not store the caught exception object. Keep the original exception where it belongs — recorded by your logging pipeline at the catch site. Do not turn every technical exception into a first-class error: only the boundary failures that deserve a stable identity.

See [Error Taxonomy and Composition](ErrorTaxonomy.en.md) and [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md).

## Why use error factories instead of `new`?

A factory gives one error situation a name, centralizes its code and messages, and acts as the anchor for living documentation. Compared with a `new DomainError(...)` at each call site, it avoids repeating codes, messages, and metadata across use cases, and keeps error construction out of the happy path.

See [Getting Started](GettingStarted.en.md).

## Should I document every first-class error?

Yes — every first-class error you define is meant to be documented. An undocumented error never reaches the generated catalog, and the analyzer [FCE009](analyzers/FCE009.en.md) flags any error factory you leave without documentation.

See [Writing Error Documentation](WritingErrorsGuide.en.md).

## What is the difference between error documentation and runtime messages?

Documentation is the part of an error that does not depend on any single occurrence: it is identical for every instance and never changes at runtime. It exists to help you *understand* the error — title, meaning, rule, diagnostic hypotheses, and representative examples.

Runtime messages are carried by the error instance itself and help you *investigate* that occurrence:

- `ShortMessage` is the safe public summary;
- `DetailedMessage` is optional controlled public detail;
- `DiagnosticMessage` is internal detail for logs and support.

See [Writing Error Documentation](WritingErrorsGuide.en.md) and [Writing Error Messages](WritingErrorMessages.en.md).

## Can error messages be shown to users?

`ShortMessage`, and optionally `DetailedMessage`, are the controlled public messages: they are safe to surface to users or API clients. `DiagnosticMessage` is internal and must never be exposed.

“Public” means safe to expose, not necessarily final UI copy: if you need fully localized, product-styled wording, your presentation layer still owns that. The error guarantees a safe, stable message you can show or map.

See [Writing Error Messages](WritingErrorMessages.en.md).

## When should I add `ErrorContext`?

Use it for safe, occurrence-specific facts that materially improve diagnosis or observability, such as a business identifier, measured value, or relevant boundary.

Do not use it for secrets, large payloads, generic documentation, or operational procedures. See [Error Context](ErrorContext.en.md).

## Are diagnostics the same as root causes?

No. Diagnostics are plausible hypotheses and investigation starting points. They describe what may explain the error without claiming certainty or assigning blame.

## Should diagnostics contain support procedures?

No. Keep ticketing, escalation, and team-contact instructions outside application error documentation. Analysis leads should say where to investigate, not prescribe an organizational workflow.

See [Writing Error Documentation](WritingErrorsGuide.en.md#6-write-diagnostics-as-hypotheses).

## Why is documentation written in code?

Because the documentation is linked to the same factories that create the errors. It can be extracted automatically and evolves beside the behavior it describes, reducing drift.

See [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md).

## Is FirstClassErrors tied to Domain-Driven Design?

No. Its vocabulary aligns well with DDD and hexagonal architecture, but any long-lived system that needs explicit error semantics, supportability, and living documentation can use it.

## How do I nest the different error categories?

A `DomainError` states that a business rule was violated. Nesting an infrastructure failure inside it would describe a technical outage as part of the domain vocabulary.

A port or infrastructure error may contain a `DomainError` when a boundary-level failure is caused by a domain rejection—for example, an incoming request that cannot be mapped into a valid value object. This preserves both facts without making domain code depend on HTTP, messaging, or another adapter technology.

See [Error Taxonomy and Composition](ErrorTaxonomy.en.md).

---

<div align="center">
<a href="Internationalization.en.md">← Internationalization</a> · <a href="../README.md#-next-steps">↑ Table of contents</a>
</div>

---

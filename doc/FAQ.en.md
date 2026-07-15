# FAQ

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./FAQ.fr.md)

## Why not just use normal exceptions?

You can. FirstClassErrors still uses standard .NET exceptions as the mechanism that signals and propagates failures.

The library enriches the `Error` carried by that exception with a stable code, structured context, diagnostics, and linked documentation. See [Core Concepts](CoreConcepts.en.md).

## Why not use a generic `Result<T, Error>`?

You could. Carrying the library’s `Error` in a general-purpose result type — for example [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions)’ `Result<T, E>` — keeps the structure a bare `Result<T, string>` would lose.

`Outcome` is that idea specialized for this error model. Its failure side is always an `Error` — not a second type parameter to thread through every signature — and its small API (`Then`, `To`, `Recover`, `Finally`) is named for intent rather than for functional-programming mechanics (`Map`, `Bind`, `Match`). The goal is domain code and use cases that read as a business flow, not as generic result plumbing.

See [Usage Patterns](UsagePatterns.en.md) and [Comparison with error-handling libraries](ComparisonWithOtherLibraries.en.md).

## Is this too heavy for a simple application?

It can be. Small scripts, prototypes, and systems without long-term support needs may be better served by standard exceptions.

See [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) for the decision criteria.

## Why use error factories instead of `new`?

A factory gives one error situation a name, centralizes its code and messages, keeps construction out of the happy path, and acts as the anchor for living documentation.

See [Getting Started](GettingStarted.en.md).

## What is the difference between error documentation and runtime messages?

Documentation is the part of an error that does not depend on any single occurrence: it is identical for every instance and never changes at runtime. It exists to help you *understand* the error — title, meaning, rule, diagnostic hypotheses, and representative examples.

Runtime messages are carried by the error instance itself and help you *investigate* that occurrence:

- `ShortMessage` is the safe public summary;
- `DetailedMessage` is optional controlled public detail;
- `DiagnosticMessage` is internal detail for logs and support.

See [Writing Error Documentation](WritingErrorsGuide.en.md) and [Writing Error Messages](WritingErrorMessages.en.md).

## Are diagnostics the same as root causes?

No. Diagnostics are plausible hypotheses and investigation starting points. They describe what may explain the error without claiming certainty or assigning blame.

## Should diagnostics contain support procedures?

No. Keep ticketing, escalation, and team-contact instructions outside application error documentation. Analysis leads should say where to investigate, not prescribe an organizational workflow.

See [Writing Error Documentation](WritingErrorsGuide.en.md#6-write-diagnostics-as-hypotheses).

## Why is documentation written in code?

Because the documentation is linked to the same factories that create the errors. It can be extracted automatically and evolves beside the behavior it describes, reducing drift.

See [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md).

## When should I add `ErrorContext`?

Use it for safe, occurrence-specific facts that materially improve diagnosis or observability, such as a business identifier, measured value, or relevant boundary.

Do not use it for secrets, large payloads, generic documentation, or operational procedures. See [Error Context](ErrorContext.en.md).

## When should I use `Outcome<T>`?

Use it when failure is an expected branch of normal flow, such as validation, parsing, batch processing, or partial success.

Use an exception when the failure should interrupt the operation at that level. Both paths can carry the same `Error` created by the same factory.

See [Usage Patterns](UsagePatterns.en.md).

## Does `Outcome<T>` preserve a stack trace?

It does not create or throw an exception while the error is carried as data. If the failure is later escalated with `GetResultOrThrow()` or `error.ToException()`, the exception and its stack trace start at that escalation point.

## Should I document every error?

Yes. Every error you model is meant to be documented: an undocumented error never reaches the generated catalog, and the analyzer [FCE009](analyzers/FCE009.en.md) flags any error factory you leave without documentation.

See [Writing Error Documentation](WritingErrorsGuide.en.md).

## Should every exception become a first-class error?

No. Model the meaningful application errors: recognized situations, rules, constraints, or boundary failures that benefit from a stable identity and a shared explanation. Framework exceptions, accidental crashes, and low-level implementation faults usually stay as plain technical exceptions.

See [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md).

## Is FirstClassErrors tied to Domain-Driven Design?

No. Its vocabulary aligns well with DDD and hexagonal architecture, but any long-lived system that needs explicit error semantics, supportability, and living documentation can use it.

## Why can a domain error contain only domain errors?

A `DomainError` states that a business rule was violated. Nesting an infrastructure failure inside it would describe a technical outage as part of the domain vocabulary.

A port or infrastructure error may contain a `DomainError` when a boundary-level failure is caused by a domain rejection—for example, an incoming request that cannot be mapped into a valid value object. This preserves both facts without making domain code depend on HTTP, messaging, or another adapter technology.

See the taxonomy and nesting rules in [Core Concepts](CoreConcepts.en.md#error-taxonomy).

---

<div align="center">
<a href="Internationalization.en.md">← Internationalization</a> · <a href="../README.md#-next-steps">↑ Table of contents</a>
</div>

---
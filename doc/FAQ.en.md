# FAQ

## ❓ Why not just use normal exceptions?

You can — and DiagnosableExceptions still uses standard .NET exceptions.

The difference is that this library adds:

* stable error codes
* structured diagnostics
* linked documentation
* a consistent model

It turns exceptions from *technical signals* into *documented knowledge units*.

## ❓ Why not use `Result<T, string>` instead?

A string loses structure.

DiagnosableExceptions keeps:

* error codes
* rich messages
* diagnostics
* context

while still allowing errors to be transported without throwing via `TryOutcome<T>`.

You get the advantages of result-based flow without losing the power of exceptions.

## ❓ Isn’t this too heavy for simple applications?

For small scripts or prototypes, yes, it may be unnecessary.

This library shines in systems that are:

* domain-heavy
* long-lived
* support-critical
* used by multiple teams

It is an investment in clarity and supportability.

## ❓ Why use exception factories instead of `new`?

Factories:

* make error situations explicit
* keep construction out of the happy path
* centralize messages and codes
* act as anchors for documentation

They improve readability and enable living documentation.

## ❓ Are diagnostics the same as root causes?

No.

Diagnostics describe **plausible explanations** and guide investigation.
They are hypotheses, not guarantees.

## ❓ Do diagnostics blame developers or users?

No.

Diagnostics should describe states or conditions, not assign blame.

The goal is to support analysis, not responsibility attribution.

## ❓ Why is documentation written in code?

Because documentation in code:

* evolves with the system
* stays close to behavior
* can be extracted automatically

This prevents drift between code and documentation.

## ❓ When should I use `TryOutcome<T>`?

Use it when failure is expected and part of normal flow:

* input validation
* parsing
* batch processing

Use exceptions directly when:

* invariants are violated
* the system cannot proceed

## ❓ Does `TryOutcome<T>` lose the stack trace?

Yes — intentionally.

When using `TryOutcome<T>`, the exception is treated as structured error information, not a runtime crash.
If you later call `GetOrThrow()`, the exception is thrown at that point.

## ❓ Can I document every exception?

No.

Focus on meaningful, domain-relevant errors.
Do not document:

* framework exceptions
* accidental crashes
* low-level technical faults

The DSL is for errors that carry semantic meaning in your system.

## ❓ Is this tied to Domain-Driven Design?

It aligns very well with DDD, but it is not limited to it.

Any system that benefits from:

* clear rules
* explicit error semantics
* supportability

can use this library.

---

Previous section: [Architecture of the Documentation Pipeline](ArchitectureOfTheDocumentationPipeline.en.md)

---
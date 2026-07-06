# FAQ

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./FAQ.fr.md)

## ❓ Why not just use normal exceptions?

You can — and FirstClassErrors still uses standard .NET exceptions.

The difference is that this library adds:

* stable error codes
* structured diagnostics
* linked documentation
* a consistent model

The exception stays what it has always been: the mechanism that signals and propagates a failure — the library doesn’t replace it. What it transforms is the **error** the exception carries: from a mere *technical signal*, it becomes a *documented knowledge unit*.

## ❓ Why not use `Result<T, string>` instead?

A string loses structure.

FirstClassErrors keeps:

* error codes
* rich messages
* diagnostics
* context

while still allowing errors to be transported without throwing via `Outcome<T>`.

You get the advantages of result-based flow without losing the power of exceptions.

## ❓ Isn’t this too heavy for simple applications?

For small scripts or prototypes, yes, it may be unnecessary.

This library shines in systems that are:

* domain-heavy
* long-lived
* support-critical
* used by multiple teams

It is an investment in clarity and supportability.

## ❓ Why use error factories instead of `new`?

Error factories return `Error` objects (thrown via `.ToException()` when you need an exception). They:

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

## ❓ When should I add `ErrorContext` to an error?

Use `ErrorContext` for **instance-specific facts** that help diagnosis and observability. Context lives on the `Error`, so it travels with the error whether it is transported via `Outcome<T>` or thrown as an exception.

Good candidates:

* business identifiers used during investigation
* values that violated a rule
* timestamps or boundaries relevant to the failure

Avoid adding:

* sensitive data
* large payloads
* information already present in the stable error documentation

A good rule: if the data helps explain this occurrence in logs, and is safe to expose, add it.

## ❓ When should I use `Outcome<T>`?

Use it when failure is expected and part of normal flow:

* input validation
* parsing
* batch processing

Use exceptions directly when:

* invariants are violated
* the system cannot proceed

## ❓ Does `Outcome<T>` lose the stack trace?

Yes — intentionally.

When using `Outcome<T>`, the exception is treated as structured error information, not a runtime crash.
If you later call `GetResultOrThrow()`, the exception is thrown at that point.

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

## ❓ Why can a domain error only nest domain errors, while an infrastructure error can also nest a domain error?

Because an error's **type follows the nature of the failure — which rule was violated — not the place where the failure is detected.**

A `DomainError` means a business rule was broken; its cause is only ever another business-rule failure, so it nests only `DomainError`s. Giving it an infrastructure cause would leak a technical concern into the domain vocabulary — and mislabel a technical failure as a business one.

An infrastructure / port error *can* nest a `DomainError`, because a boundary legitimately reports a *request-level* failure whose *cause* is a domain rule. The textbook case: an incoming (primary-port) adapter maps a DTO into value objects, and a value object refuses to build because an invariant is violated. Two distinct facts coexist:

* the **cause** — “this value is invalid” — belongs to the domain (the value object produced a `DomainError`);
* the **boundary condition** — “this request is rejected at the edge” — belongs to the adapter (a `PrimaryPortError`).

Nesting the domain error inside the port error keeps both. It is an *infrastructure error **caused by** a domain error* — not one or the other.

Mind the direction: the value object stays domain code and emits the `DomainError`; the adapter is what classifies the boundary condition as infrastructure. A value object must never emit an infrastructure error itself — that would make the domain depend on infrastructure.

Why does the distinction earn its keep?

* **Transport independence** — the same `DomainError` renders as an HTTP 400/422, a gRPC `INVALID_ARGUMENT`, or a CLI exit code. The HTTP status is a *rendering* of the error, not its identity.
* **Operations** — you alert and retry on `InteractionDirection` and `Transience`, not on the error type alone. A user who typed a bad email produces a non-transient *inbound* port error that must never page anyone; a database timeout produces a *transient* *outbound* one that might. Collapsing invalid input into the same bucket as real outages would poison that signal.

---

<div align="center">
<a href="Internationalization.en.md">← Internationalization</a> · <a href="../README.md#-next-steps">↑ Table of contents</a>
</div>

---
# Core Concepts

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./CoreConcepts.fr.md)

FirstClassErrors is not just a utility library.
It introduces a different way to think about application errors.

Instead of reducing a failure to a mere technical incident, it turns the **error** behind it into **structured knowledge about what went wrong** — while the exception stays the mechanism that signals and propagates that failure.

## 🧠 An error is not just a message

In many systems, exceptions are reduced to:

> a type + a string message

In FirstClassErrors, an **error** represents:

* a **specific error situation**
* identified by a **stable error code**
* described with three purpose-built messages (a public summary, an optional public detail, and an internal diagnostic message)
* optionally enriched with context
* associated with structured diagnostics

An **error** becomes a semantic object, not just a runtime signal.

### Three messages, two audiences

An error carries three messages but deliberately splits them across just **two audiences** — a public one (end users / API clients) and an internal one (logs, support, developers). The separation is guaranteed by construction, so what reaches a caller can never leak what is meant for developers and support:

* **`ShortMessage`** (mandatory) — a short public summary, safe to expose to an end user or an API client (e.g. the `title` of an RFC 9457 problem detail).
* **`DetailedMessage`** (optional) — a controlled public detail, exposable **only** when the application explicitly chooses to (e.g. the `detail` of an RFC 9457 problem detail). It must never carry sensitive or internal information.
* **`DiagnosticMessage`** (mandatory) — the internal diagnostic message for logs, support and developers. It may contain technical/operational detail (identifiers, offending values, internal state) and is **never** exposed to external clients by default. `error.ToException()` uses it as the exception's `Message`.

The core model stays HTTP-agnostic: the diagnostic message is never a default HTTP response body.

## 🧩 A factory represents an error situation

**Error** factories are central to the model.

A factory method:

* represents one precise error scenario
* gives it a **name** in the code
* centralizes error creation
* becomes the anchor for documentation

This means:

> Each factory = one documented error case.

Factories improve readability and make error situations explicit, while keeping construction details out of the business logic.

## 📘 Documentation lives with the code

Error documentation is written using the `DescribeError` DSL and linked directly to **error** factories.

This creates:

* structured descriptions
* violated rules
* diagnostics
* realistic examples

Because documentation is code:

* it evolves with the system
* it does not drift
* it can be extracted automatically

This is **living documentation**.

## 🔎 Diagnostics describe hypotheses, not blame

Diagnostics answer:

* What might have caused this error?
* Is it likely input-related, system-related, or both?
* Where should investigation start?

Diagnostics are:

* structured
* human-oriented
* guidance for analysis

They do not encode operational processes. They provide **direction**, not procedures.

## 🧭 Error taxonomy

Errors are modeled as a hierarchy rooted in the abstract `Error` type:

* **`DomainError`** — a violation of a domain rule (the domain layer).
* **`InfrastructureError`** — a failure at a technical boundary. It carries a `Transience` (`Unknown` / `NonTransient` / `Transient`) and an `InteractionDirection`.
  * **`PrimaryPortError`** — incoming boundary (`Direction` fixed to `Incoming`).
  * **`SecondaryPortError`** — outgoing boundary (`Direction` fixed to `Outgoing`).

The Port errors replace the old Adapter exceptions. When a port failure wraps several causes, `PrimaryPortInnerErrors` / `SecondaryPortInnerErrors` aggregate the inner errors and compute the overall transience.

**Nesting rules.** Inner errors capture *causes*, and the model enforces what may nest in what — by construction, not by convention:

* A **`DomainError`** nests **only** other `DomainError`s. A domain failure is only ever caused by — or aggregated from — other domain failures; it never carries an infrastructure cause (that would leak a technical concern into the domain vocabulary).
* A **`PrimaryPortError`** / **`SecondaryPortError`** nests **infrastructure errors of its own direction** (a primary port nests primary-port errors, a secondary port nests secondary-port errors) **and/or** `DomainError`s — for example a boundary rejection whose cause is a domain-invariant violation surfaced while mapping an incoming request. The typed `PrimaryPortInnerErrors` / `SecondaryPortInnerErrors` builders make anything else unrepresentable: they only expose `Add(DomainError)` and `Add(`_same-direction port error_`)`.
* The base **`InfrastructureError`** is the permissive general case and accepts any `Error` as inner. Prefer the Port types in real code: they pin the `InteractionDirection` and keep nesting consistent.

In short: **a domain error contains only domain errors; an infrastructure error contains same-direction infrastructure errors and/or domain errors.** The [FAQ](FAQ.en.md) explains *why* this asymmetry exists.

Each error has a paired exception reached via `error.ToException()`: `DomainException`, `InfrastructureException`, `PrimaryPortException`, `SecondaryPortException`. You never `new` these directly; the exception exposes its `Error` (and through it the context and inner errors).

## 🔁 Error or data? Both are supported

Traditionally, exceptions are always thrown.
FirstClassErrors supports two complementary models:

* **Exception as control flow** (classic throw)
* **Error as data** (`Outcome<T>`, or non-generic `Outcome` when there is no value)

This allows errors to be:

* raised immediately
* transported through validation pipelines
* escalated later

The same error situation can serve both roles.

The non-throwing model is `Outcome` / `Outcome<T>`: the `Error` is carried as data (`IsSuccess` / `IsFailure` / `Error`) and can be converted into an exception on demand via `error.ToException()`.

## 🎯 From failures to knowledge

With this model, errors are no longer:

> isolated technical failures

They become:

> shared, structured knowledge about how the system can fail.

This bridges:

* development
* support
* documentation
* operations

All based on the same source of truth: the code.

---

<div align="center">
<a href="WhenNotToUseFirstClassErrors.en.md">← When Not to Use FirstClassErrors</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="ErrorContext.en.md">Error Context Guide →</a>
</div>

---
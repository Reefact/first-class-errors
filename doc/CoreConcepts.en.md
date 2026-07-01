# Core Concepts

FirstClassErrors is not just a utility library.
It introduces a different way to think about application errors.

Instead of treating exceptions as technical incidents, it treats them as **structured knowledge about what went wrong**.

## 🧠 An error is not just a message

In many systems, exceptions are reduced to:

> a type + a string message

In FirstClassErrors, an **error** represents:

* a **specific error situation**
* identified by a **stable error code**
* described with meaningful messages
* optionally enriched with context
* associated with structured diagnostics

An **error** becomes a semantic object, not just a runtime signal.

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

Previous section: [When Not to Use FirstClassErrors](WhenNotToUseFirstClassErrors.en.md) | Next section: [Error Context Guide](ErrorContext.en.md)

---
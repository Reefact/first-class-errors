# Core Concepts

DiagnosableExceptions is not just a utility library.
It introduces a different way to think about application errors.

Instead of treating exceptions as technical incidents, it treats them as **structured knowledge about what went wrong**.

## 🧠 An exception is not just a message

In many systems, exceptions are reduced to:

> a type + a string message

In DiagnosableExceptions, an exception represents:

* a **specific error situation**
* identified by a **stable error code**
* described with meaningful messages
* optionally enriched with context
* associated with structured diagnostics

An exception becomes a **semantic object**, not just a runtime signal.

## 🧩 A factory represents an error situation

Exception factories are central to the model.

A factory method:

* represents one precise error scenario
* gives it a **name** in the code
* centralizes error creation
* becomes the anchor for documentation

This means:

> Each factory = one documented error case.

Factories improve readability and make error situations explicit, while keeping construction details out of the business logic.

## 📘 Documentation lives with the code

Error documentation is written using the `DescribeError` DSL and linked directly to exception factories.

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

## 🔁 Exception or data? Both are supported

Traditionally, exceptions are always thrown.
DiagnosableExceptions supports two complementary models:

* **Exception as control flow** (classic throw)
* **Exception as data** (`TryOutcome<T>`)

This allows errors to be:

* raised immediately
* transported through validation pipelines
* escalated later

The same exception type can serve both roles.

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

Previous section: [When Not to Use DiagnosableExceptions](WhenNotToUseDiagnosableExceptions.en.md) | Next section: [Writing Errors Guide](WritingErrorsGuide.en.md)

---
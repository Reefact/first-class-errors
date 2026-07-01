# Comparison with error-handling libraries

[ErrorOr](https://github.com/amantinband/error-or) and [FluentResults](https://github.com/altmann/FluentResults) are excellent, mature libraries. If your goal is a lightweight *Result* type — returning errors as values instead of throwing — they are focused, well-adopted choices for exactly that.

FirstClassErrors answers a **different question**. It is not primarily a *Result* library: it is a way to make errors **first-class, documented and diagnosable knowledge** about your system — errors you can *carry* as values **or** *throw* as exceptions, using one and the same model.

This page highlights what FirstClassErrors does differently.

## 🎯 A different centre of gravity

| Library | The question it answers |
|---|---|
| **ErrorOr** | *How do I return one or more errors as a value instead of throwing?* |
| **FluentResults** | *How do I return a result carrying errors, successes and causal reasons?* |
| **FirstClassErrors** | *How do I turn errors into documented, diagnosable knowledge — and move them through my system however each layer needs?* |

For ErrorOr and FluentResults, the **error is a payload of the result type**. For FirstClassErrors, the **error is the model**, and the result type (`Outcome`) is just one of several ways to transport it.

## 🧩 One error model, three transports

The `Error` model is decoupled from the way it travels. The *same* error can be:

- kept as **data** — an `Error` value you inspect, log or enrich;
- **thrown** — turned into a typed exception with `error.ToException()`, then caught and routed by type;
- **carried** — wrapped in an `Outcome` / `Outcome<T>` and composed without throwing.

Bridges connect all three, so you are never locked into one style. You can *carry* errors inside your domain and *throw* them at a boundary — with **the same error object**, no re-modeling in between.

ErrorOr and FluentResults are, by design, *errors-as-values only*: the error is coupled to the result type and the model deliberately avoids throwing. FirstClassErrors treats the exception path as a **first-class citizen** alongside the value path.

## 📖 Errors that carry meaning, not just an identifier

An ErrorOr `Error` is a code, a description, a `Type` and a metadata bag. A FluentResults error is a message with metadata and nested reasons. That is enough to *handle* an error at runtime.

A FirstClassErrors error is described for **humans**: a title, a plain-language explanation, the **business rule** that was violated, and representative examples. The error stops being a technical token and becomes something a developer — or a support engineer — can actually *understand*.

## 🔎 Diagnostics built for investigation

Where the others *classify* an error (an `ErrorType` enum, a metadata entry), FirstClassErrors lets an error declare **how to investigate it**:

- one or more **possible causes**;
- the likely **origin** of each one (`Internal`, `External`, `InternalOrExternal`);
- an **analysis lead** — where to start looking.

This turns the error from *"what failed"* into *"what probably went wrong, and where to begin"* — the difference between an error message and an on-call runbook.

## 📚 Documentation generated from your code

Because errors are described in code with the `DescribeError` DSL, their documentation is **generated automatically into an error catalog** — a living reference that stays in sync with the code and is ready for developers and support teams alike.

Neither ErrorOr nor FluentResults produces documentation from your error definitions; the description lives, at best, in scattered strings and metadata. Here, **documenting an error and defining it are the same act**.

## 🎚️ Fluent where it helps, plain code where it doesn't

`Outcome` offers a fluent pipeline (`Then`, `To`, `Recover`, `Finally`) to compose steps without throwing — use it when it genuinely makes the flow clearer.

But that pipeline is an **optional transport, not the centre of gravity**. When a plain `if` returning a well-named domain error reads closer to the business, FirstClassErrors encourages you to *write that instead*. Your error handling stays at **business altitude**; you are never pushed into long fluent chains just to remain "idiomatic".

Railway-oriented result libraries tend to make the fluent pipeline the primary idiom. Here, the pipeline serves the error — not the other way around.

## 🏛️ Architecture- and operations-aware

The model speaks the language of layered / hexagonal design out of the box:

- a taxonomy of `DomainError`, `InfrastructureError`, and primary / secondary **port** errors;
- infrastructure concerns such as `Transience` (transient / non-transient) and `InteractionDirection` (incoming / outgoing);
- an **occurrence identity** on every error — a unique `InstanceId` and a UTC timestamp — to correlate logs and diagnostic events.

ErrorOr and FluentResults are deliberately architecture-agnostic and keep the error lightweight; these concepts are simply outside their scope.

## 📊 At a glance

| | FirstClassErrors | ErrorOr | FluentResults |
|---|:---:|:---:|:---:|
| Return errors as values (railway style) | ✅ (optional) | ✅ | ✅ |
| Throw the *same* error as a typed exception | ✅ | ➖ | ➖ |
| Human-facing error model (title, business rule, explanation) | ✅ | ➖ | ➖ |
| Diagnostics: cause + origin + analysis lead | ✅ | ➖ | ➖ |
| Documentation generated from the code | ✅ | ➖ | ➖ |
| Architecture taxonomy (domain / infrastructure / port) | ✅ | ➖ | ➖ |
| Per-occurrence identity (id + timestamp) | ✅ | ➖ | ➖ |
| Fluent pipeline | Optional, by design | Central | Central |

*➖ means "not a goal of that library", not "done badly": ErrorOr and FluentResults are focused on being lean result types.*

## 🧭 Which one should you pick?

- Reach for **ErrorOr** when you want a tiny, ergonomic result type with clean, HTTP-friendly error categorization.
- Reach for **FluentResults** when you want a result carrying rich reason chains and metadata.
- Reach for **FirstClassErrors** when you want your errors to be **documented, diagnosable knowledge** — described once in code, carried as values or thrown as exceptions, and turned into a catalog your whole team can rely on.

They are not really competing for the same job: the first two make errors easy to *return*; FirstClassErrors makes them easy to *understand, support and document*.

# Comparison with error-handling libraries

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ComparisonWithOtherLibraries.fr.md)

> Comparison reviewed on **2026-07-14** against the public documentation of [ErrorOr](https://github.com/error-or/error-or) and [FluentResults](https://github.com/altmann/FluentResults). Their APIs and goals may evolve; verify their current documentation before making a long-term choice.

ErrorOr, FluentResults, and FirstClassErrors all help applications represent failure explicitly, but they optimize for different primary goals.

This page does not try to rank them in general.

The scenario below deliberately emphasizes error stability, diagnosis, and documentation: that is the primary focus of FirstClassErrors, so it naturally plays to its strengths. Other scenarios, such as heavy multi-error validation or intensive functional composition, can favor ErrorOr or FluentResults.

The goal is to show the same situation through three different centres of gravity, so you can choose the model that best matches your system.

## The scenario

A payment provider refuses an authorization request. The application needs to:

- return a failure without crashing the process;
- expose a safe message to the caller;
- preserve an internal diagnostic message;
- keep a stable code for logs and support;
- possibly document how the failure should be investigated.

## ErrorOr: a discriminated union of value or errors

ErrorOr centres the API on `ErrorOr<T>`, which carries either a successful value or one or more errors.

```csharp
private static readonly Error PaymentDeclined = Error.Failure(
    code: "PAYMENT_DECLINED",
    description: "The payment was declined.");

public ErrorOr<Receipt> Pay(Order order)
{
    if (provider.Declines(order))
    {
        return PaymentDeclined;
    }

    return new Receipt(order.Id);
}
```

A caller can inspect, match, switch on, or compose the result. Built-in error types and metadata support categorization and application-specific information.

Choose this style when the central need is an ergonomic value-or-errors flow, especially when multiple validation errors are common, or when error types are mapped to HTTP responses.

## FluentResults: a result with reasons and metadata

FluentResults centres the API on `Result` and `Result<T>`. Failures carry one or more reasons, and reasons can contain metadata and nested causes.

```csharp
public Result<Receipt> Pay(Order order)
{
    if (provider.Declines(order))
    {
        return Result.Fail<Receipt>(
            new Error("The payment was declined.")
                .WithMetadata("Code", "PAYMENT_DECLINED"));
    }

    return Result.Ok(new Receipt(order.Id));
}
```

The reasons model is useful when an application wants to enrich both successes and failures, keep hierarchical causes, and compose several results.

Choose this style when the result and its reason graph are the primary abstraction you want to compose.

## FirstClassErrors: an error model with several transports

FirstClassErrors centres the API on the error itself. In the recommended usage, a named factory defines and creates the error; an `Outcome<T>` (the library's success-or-failure result type) is one way to carry — or transport — that error as a value:

```csharp
public Outcome<Receipt> Pay(Order order)
{
    if (provider.Declines(order, out string providerCode))
    {
        return Outcome<Receipt>.Failure(
            PaymentError.PaymentDeclined(providerCode, order.PaymentId));
    }

    return Outcome<Receipt>.Success(new Receipt(order.Id));
}
```

At this level the three libraries look alike: a method returns a success or a failure the caller inspects. The difference is where the failure's meaning lives — [what FirstClassErrors adds](#what-firstclasserrors-adds-to-the-result-model) is shown below.

Choose this style when the error definition itself must remain stable, documented, diagnosable, and independent from whether a caller returns or throws it.

## What FirstClassErrors adds to the result model

The transport above looks like the other two. The difference is that the error is defined once, in a named factory that carries its code, messages, occurrence context, and a link to documentation:

```csharp
internal static DomainError PaymentDeclined(
    string providerCode,
    Guid paymentId)
{
    // Code.PaymentDeclined and ContextKey.PaymentId are application-defined
    // constants: ErrorCode.Create("PAYMENT_DECLINED") and a typed context key.
    return DomainError.Create(
            Code.PaymentDeclined,
            diagnosticMessage:
                $"Provider refused payment {paymentId} with code {providerCode}.",
            configureContext: context =>
                context.Add(ContextKey.PaymentId, paymentId))
        .WithPublicMessage(
            shortMessage: "The payment was declined.",
            detailedMessage:
                "Use another payment method or contact your bank.");
}
```

The same factory feeds exception flow. The error travels as a structured, category-typed exception (`DomainException` here) carrying the same `Error`:

```csharp
throw PaymentError
    .PaymentDeclined(providerCode, paymentId)
    .ToException();
```

The error can additionally be linked to structured documentation describing its title, rule, possible causes, analysis leads, and examples. The generated catalog (a human-readable reference of every documented error) and the versioning workflow that guards it against breaking changes are part of the library's intended use.

None of this changes the `Pay` method above: the transport stays small, and the durable knowledge lives with the error's definition, not in each call site.

## What changes between the approaches?

| Concern | ErrorOr | FluentResults | FirstClassErrors |
| --- | --- | --- | --- |
| Primary abstraction | value or errors | result with reasons | structured error |
| Value-based failure flow | central | central | available through `Outcome` |
| Multiple errors or reasons | built in | built in | structured causes; aggregating independent errors is left to the application |
| Metadata / occurrence facts | metadata | metadata | typed `ErrorContext` |
| Dedicated public vs internal messages | application-defined | application-defined | explicit in the core model |
| Exception transport from the same error definition | not the primary model | not the primary model | built in via `ToException()` — a structured, category-typed exception |
| Domain / infrastructure / port (incoming/outgoing boundary) taxonomy | application-defined | application-defined | built in |
| Transience (is retrying meaningful?) and interaction direction (incoming vs outgoing) | application-defined | application-defined | built in for infrastructure errors |
| Generated human documentation | outside the library's main scope | outside the library's main scope | built in |
| Catalog compatibility checks | outside the library's main scope | outside the library's main scope | built in |

“Application-defined” or “outside the main scope” does not mean impossible. It means the concern is not the library's central abstraction and may be implemented by application conventions or surrounding tooling.

## Decision guide

Choose **ErrorOr** when:

- you primarily want a concise `T`-or-errors union;
- multiple validation errors are common;
- matching and functional composition are the main interaction style;
- a lightweight error representation is sufficient.

Choose **FluentResults** when:

- you want both success and failure reasons;
- nested reason chains and metadata are central;
- the result graph itself is the model you want to enrich and compose.

Choose **FirstClassErrors** when:

- errors are durable concepts used by developers, support, operations, or clients;
- public and diagnostic messages must have explicit audiences;
- the same error must travel as data, an `Outcome`, or an exception;
- domain and infrastructure failures require different operational meaning;
- generated documentation and compatibility checks are part of the requirement.

## A combination may also be valid

These choices are not always exclusive. An application may use a general-purpose result library at some boundaries while keeping a separate documented error catalog.

Before combining models, decide which type owns the stable error identity. Duplicating codes, messages, metadata, and mappings across two competing error models usually creates more work than it saves.

## Questions to ask before choosing

1. Is the primary problem **control flow**, **reason composition**, or **shared error knowledge**?
2. Must one error definition support both return and exception paths?
3. Who consumes the error model: only code, or also support and operations?
4. Are stable codes and context keys treated as a versioned contract?
5. Is generated documentation a requirement or an external concern?
6. Does the application need built-in domain and infrastructure semantics?
7. How much convention are you prepared to build around a smaller result type?

The best choice is the smallest model that covers the real requirements without forcing the application to recreate its missing semantics elsewhere.

---

<div align="center">
<a href="Internationalization.en.md">← Internationalization</a> · <a href="DocumentationMap.en.md">Documentation map</a> · <a href="FAQ.en.md">FAQ →</a>
</div>

---
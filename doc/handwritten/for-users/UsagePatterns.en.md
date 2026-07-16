# Usage Patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./UsagePatterns.fr.md)

FirstClassErrors supports both exceptions and errors carried as data. The important decision is not which syntax you prefer, but **whether the failure belongs to the operation's normal contract**.

This guide helps you choose the appropriate pattern. For the complete `Outcome` API and pipeline composition, see [Composing with Outcome](OutcomeGuide.en.md).

## 🧭 Is failure part of the operation's contract?

A given failure can legitimately be represented by a thrown exception or by a returned `Outcome`; the error's category alone does not determine which:

- **Use `Outcome`** when the failure is part of the operation's normal contract and the caller must explicitly handle or propagate it.
- **Throw an exception** when the operation cannot fulfill its contract and no local failure branch is normally expected at this point.

Throwing does not remove the caller's agency — a thrown exception can still be caught, translated, retried, or logged. It only removes the failure from the operation's return type. So once the contract question above is settled, a second, useful question remains: who holds the next useful decision, this method or its caller? That question refines the choice; it should not replace the contract question.

Both paths can start from the very same documented error factory. The factory describes **what happened**; throwing or returning it describes **how this particular caller chooses to propagate it**.

The sections below walk through common contexts. Where both contracts are genuinely useful in a context, both are shown side by side; where one clearly dominates, only that one is.

## 🧱 Constructing a valid value object

A value object must not enter an invalid state. Whether failure belongs to the constructing method's contract, or is excluded from it entirely, is a propagation choice, not a property of the invariant itself — the same check in `Temperature` supports both:

```csharp
// Contract: returns a valid Temperature or fails — failure has no place in this method's return type.
public static Temperature FromKelvinOrThrow(decimal kelvin) {
    return FromKelvin(kelvin).GetResultOrThrow();
}
```

```csharp
// Contract: an out-of-range value is part of the normal contract — the caller must handle it explicitly.
public static Outcome<Temperature> FromKelvin(decimal kelvin) {
    if (kelvin < 0) { return Outcome<Temperature>.Failure(InvalidTemperatureError.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin)); }

    return Outcome<Temperature>.Success(new Temperature(kelvin));
}
```

Both report the exact same documented error, `InvalidTemperatureError.BelowAbsoluteZero`. `FromKelvinOrThrow` does not repeat the check; it escalates `FromKelvin`'s failure with `GetResultOrThrow()`. When both contracts are genuinely useful, centralize validation in the `Outcome`-returning version and derive the throwing one from it — not the reverse.

## 🧮 Domain operations

Operations between valid domain objects may still violate a rule. The choice here is a matter of API contract, not of who is theoretically allowed to react — a caller of `AddOrThrow` could still catch and handle a thrown mismatch, just as a caller of `Add` could still ignore a returned one. What differs is what each method promises to its caller:

```csharp
// Contract: the amounts passed in must already share the same currency.
public Amount AddOrThrow(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

```csharp
// Contract: a currency mismatch is an expected outcome the caller must handle.
public Outcome<Amount> Add(Amount other) {
    if (Currency != other.Currency) { return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(this, other)); }

    return Outcome<Amount>.Success(new Amount(Value + other.Value, Currency));
}
```

Both report the same `InvalidAmountOperationError.CurrencyMismatch`. Use a precise factory name rather than a generic exception such as `InvalidOperationException`: the code should say which domain situation occurred, regardless of which method carries it.

## 📦 Batch and file processing

Each item in a batch can fail independently — but whether that failure is part of the loop's own contract, or invalidates the whole file, is a policy decision, not a property of batch processing as a category.

```csharp
// Contract: a per-line failure is expected and handled locally — log it and move on.
foreach (string line in file) {
    ParseAmount(line).Finally(
        onSuccess: Process,
        onFailure: Log);
}
```

```csharp
// Contract: any invalid line invalidates the whole file — stop immediately.
foreach (string line in file) {
    Amount amount = ParseAmount(line).GetResultOrThrow();
    Process(amount);
}
```

The first version never touches the exception channel: `Finally` dispatches directly to `Process` or `Log` from the `Outcome`, with no intermediate `IsFailure` check and no null-forgiving `Error!`. The second calls `GetResultOrThrow()` directly, so the first invalid line throws and stops the loop. Choose per-item recovery when a bad line is only ever about that line; choose the immediate throw when file-level integrity is what is actually being protected.

## 🌐 Incoming boundaries

An incoming adapter may reject an interaction because mapping, validation, or domain construction failed. The domain error explains the violated rule; the primary-port error explains the boundary-level outcome — see [Error Taxonomy and Composition](ErrorTaxonomy.en.md) for the nesting rules. The direction of the interaction decides the port type: `PrimaryPortError` applies whether the failure is thrown or returned.

```csharp
DomainError invalidAmount = InvalidMoneyTransferError.AmountNotPositive(request.Amount);

PrimaryPortError rejection = PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"Request {request.Id} contains an invalid amount.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "The request contains invalid data.");
```

```csharp
// Contract: failure is part of this handler's return type — an Outcome-aware pipeline maps it to a response.
return Outcome<Receipt>.Failure(rejection);
```

```csharp
// Contract: failure crosses this boundary as an exception — a filter or middleware catches it.
throw rejection.ToException();
```

Either propagation carries the same `rejection`; only how it leaves this method differs.

## 🔌 Outgoing dependencies

A database, broker, filesystem, or remote API failure is an outgoing interaction; the direction is what makes it a `SecondaryPortError`, regardless of how the failure is propagated.

```csharp
SecondaryPortError unavailable = SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "The payment provider timed out after 5 seconds.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "The payment service is temporarily unavailable.");
```

```csharp
// Contract: failure is part of this call's return type — the caller inspects Transience to decide whether to retry.
return Outcome<Receipt>.Failure(unavailable);
```

```csharp
// Contract: failure crosses this boundary as an exception — a resilience policy (e.g. a retry filter) catches it.
throw unavailable.ToException();
```

`Transience` indicates whether trying the same operation later may help; it does not itself implement a retry policy, and it says nothing about which propagation to use — both examples above carry the same `Transience.Transient`.

## 🔁 Multi-step application flows

When several expected-failure operations must run in sequence, compose their outcomes rather than repeatedly checking and unpacking them.

```csharp
Outcome<Receipt> result =
    CreateAmount(value, currencyCode)
        .Then(CheckLimits)
        .Then(Charge);
```

The first failure short-circuits the remaining steps and is propagated unchanged. Use a fluent chain only when it reads more clearly than ordinary branching.

The full behavior of `Then`, `Recover`, `Finally`, async overloads, and the escape hatches back to exceptions (`ThrowIfFailure()`, `GetResultOrThrow()`) is documented in [Composing with Outcome](OutcomeGuide.en.md).

## 🧩 Logging and support

At the point where a failure is handled, log the structured error rather than only a public message.

Useful fields include:

- `Code` for grouping and dashboards;
- `InstanceId` for correlating one occurrence;
- `OccurredAt` for timing;
- `DiagnosticMessage` for internal analysis;
- `Context` for occurrence-specific facts;
- `InnerErrors` for the causal chain.

Public messages are for callers. Diagnostic information is for logs, support, and developers.

## 📌 Decision checklist

### Choosing the contract

- Is the failure part of this operation's expected outcomes?
- Must the caller explicitly handle or propagate it?
- Can the caller reasonably act on this failure, or only observe it?
- Does it need to be composed with other operations, aggregated, or recovered locally?
- Would it be acceptable for the caller to ignore it by accident?
- Is this operation a `Try...`-style API, a use case, a boundary, or an internal primitive?

### Classifying the error

- A domain rule?
- An incoming interaction?
- An outgoing dependency?

### Consuming an `Outcome`

- Explicit branching for a local decision?
- A fluent chain for a linear succession of steps?

Choose the contract from the first list, the error type from the second, and the consumption style from the third — not from a single blanket rule that every error must be thrown or every error must be returned.

---

<div align="center">
<a href="WritingErrorsGuide.en.md">← Writing Errors Guide</a> · <a href="../../../README.md#-next-steps">↑ Table of contents</a> · <a href="OutcomeGuide.en.md">Composing with Outcome →</a>
</div>

---
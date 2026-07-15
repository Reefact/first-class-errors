# Usage Patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./UsagePatterns.fr.md)

FirstClassErrors supports both exceptions and errors carried as data. The important decision is not which syntax you prefer, but **who is expected to decide what happens next: this method, or its caller**.

This guide helps you choose the appropriate pattern. For the complete `Outcome` API and pipeline composition, see [Composing with Outcome](OutcomeGuide.en.md).

## 🧭 Who decides: this method, or its caller?

The same kind of failure — an invalid value, a violated business rule, an unreachable dependency — honestly supports both a thrown exception and a returned `Outcome`. What decides is not the category of situation, but who is expected to act on it next:

- **This method decides, right here.** An invalid state must simply not exist, and the caller has nothing useful to add. Throw the documented error through `.ToException()`; the exception interrupts the current operation immediately.
- **The caller decides.** The failure is a normal branch the caller should inspect, log, retry, aggregate, or recover from. Return the documented error as `Outcome<T>.Failure(...)` (or `Outcome.Failure(...)`); nothing is thrown, and the failure travels back as data.

Both paths can start from the very same documented error factory. The factory describes **what happened**; throwing or returning it describes **how this particular caller chooses to propagate it**.

The sections below walk through common contexts. Where both intents are genuinely useful in a context, both are shown side by side; where one clearly dominates, only that one is.

## 🧱 Constructing a valid value object

A value object must not enter an invalid state. Whether that stops the current method immediately or becomes a decision for the caller is a propagation choice, not a property of the invariant itself — the same check in `Temperature` supports both:

```csharp
// Intent: the caller has nothing to decide — a temperature below absolute zero must not exist.
public static Temperature FromKelvin(decimal kelvin) {
    return TryFromKelvin(kelvin).GetResultOrThrow();
}
```

```csharp
// Intent: the caller — a sensor reading, a parsed file, a form field — must react itself.
public static Outcome<Temperature> TryFromKelvin(decimal kelvin) {
    if (kelvin < 0) { return Outcome<Temperature>.Failure(InvalidTemperatureError.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin)); }

    return Outcome<Temperature>.Success(new Temperature(kelvin));
}
```

Both report the exact same documented error, `InvalidTemperatureError.BelowAbsoluteZero`. `FromKelvin` does not repeat the check; it escalates `TryFromKelvin`'s failure with `GetResultOrThrow()`. Write the `Outcome`-returning version first, and derive the throwing one from it, not the other way around.

## 🧮 Domain operations

Operations between valid domain objects may still violate a rule — and again, throwing or returning is a choice about who should react, not about the rule itself.

```csharp
// Intent: the caller has nothing to decide — mixing currencies inside one Amount must not happen.
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

```csharp
// Intent: the caller — e.g. a statement reconciliation job summing many lines — decides what a mismatch means.
public Outcome<Amount> TryAdd(Amount other) {
    if (Currency != other.Currency) { return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(this, other)); }

    return Outcome<Amount>.Success(new Amount(Value + other.Value, Currency));
}
```

Both report the same `InvalidAmountOperationError.CurrencyMismatch`. Use a precise factory name rather than a generic exception such as `InvalidOperationException`: the code should say which domain situation occurred, regardless of which method carries it.

## 📦 Batch and file processing

Each item in a batch can fail independently — but whether one failure should stop the whole batch is, once again, an intent, not a property of batch processing as a category.

```csharp
// Intent: the caller (the batch job) decides per item — log and keep going.
foreach (string line in file) {
    Outcome<Amount> result = TryParseAmount(line);
    if (result.IsFailure) { Log(result.Error!); continue; }

    Process(result.GetResultOrThrow());
}
```

```csharp
// Intent: one invalid line makes the whole file untrustworthy — stop immediately.
foreach (string line in file) {
    Amount amount = TryParseAmount(line).GetResultOrThrow();
    Process(amount);
}
```

Both reuse the same `GetResultOrThrow()`: the first version calls it only after `IsFailure` has been ruled out for that one item, so it never throws there; the second calls it directly, so the first invalid line throws and stops the loop. Choose per-item recovery when a bad line is only ever about that line; choose the immediate throw when file-level integrity is what is actually being protected.

## 🌐 Incoming boundaries

An incoming adapter may reject an interaction because mapping, validation, or domain construction failed. Here the situation itself — an incoming interaction — decides the port type; `PrimaryPortError` applies either way.

```csharp
DomainError invalidAmount = InvalidMoneyTransferError.AmountNotPositive(request.Amount);

return PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"Request {request.Id} contains an invalid amount.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "The request contains invalid data.");
```

The domain error explains the violated rule. The primary-port error explains the boundary-level outcome. See [Error Taxonomy and Composition](ErrorTaxonomy.en.md) for the nesting rules. Whether this `PrimaryPortError` then crosses the boundary as a thrown exception or a returned `Outcome` still follows the same question as above — who is expected to decide next, this adapter or its caller.

## 🔌 Outgoing dependencies

A database, broker, filesystem, or remote API failure is an outgoing interaction; the direction is what makes it a `SecondaryPortError`, not an intent.

```csharp
return SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "The payment provider timed out after 5 seconds.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "The payment service is temporarily unavailable.");
```

`Transience` indicates whether trying the same operation later may help; it does not itself implement a retry policy. As elsewhere, throwing this error or returning it as a failed `Outcome` is a separate propagation choice, independent of what `Transience` says about the dependency.

## 🔁 Multi-step application flows

When several expected-failure operations must run in sequence, compose their outcomes rather than repeatedly checking and unpacking them.

```csharp
Outcome<Receipt> result =
    TryCreateAmount(value, currencyCode)
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

Before choosing a pattern, ask:

1. Is this failure expected in the normal flow?
2. Must the current operation stop immediately?
3. Is the failure a domain rule, an incoming boundary condition, or an outgoing dependency failure?
4. Who is expected to make the next decision: this method or its caller?
5. Would a fluent `Outcome` chain be clearer than explicit branching?

Choose the representation from those answers, not from a blanket rule that every error must be thrown or every error must be returned.

---

<div align="center">
<a href="WritingErrorsGuide.en.md">← Writing Errors Guide</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="OutcomeGuide.en.md">Composing with Outcome →</a>
</div>

---
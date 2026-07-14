# Usage Patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./UsagePatterns.fr.md)

FirstClassErrors supports both exceptions and errors carried as data. The important decision is not which syntax you prefer, but **what the failure means in the current flow**.

This guide helps you choose the appropriate pattern. For the complete `Outcome` API and pipeline composition, see [Composing with Outcome](OutcomeGuide.en.md).

## 🧭 Choose from the meaning of the failure

| Situation | Recommended representation | Why |
| --- | --- | --- |
| A domain invariant is violated and the operation cannot continue | throw a `DomainError` through `.ToException()` | the failure interrupts the current operation and belongs to the domain |
| Invalid input is an expected result of validation or parsing | return `Outcome<T>.Failure(...)` | callers can handle the failure without exception-based control flow |
| One item fails inside a batch | return an `Outcome<T>` for each item | one failure does not need to abort the whole batch |
| An incoming boundary rejects an interaction | use a `PrimaryPortError` | it preserves the incoming direction and any underlying domain cause |
| An outgoing dependency fails | use a `SecondaryPortError` | it preserves outgoing direction and transience |
| A failure must cross an application boundary as an exception | call `ThrowIfFailure()` or `GetResultOrThrow()` at that boundary | the error remains data until the chosen escalation point |

The same documented error factory may be used in more than one transport. The error describes **what happened**; throwing or returning it describes **how this caller chooses to propagate it**.

## 🧱 Domain invariants

A value object or entity must not enter an invalid state. When construction or an operation cannot continue, throw the documented domain error.

```csharp
public static Amount From(decimal value, Currency currency) {
    if (value < 0) {
        throw InvalidAmountOperationError.NegativeAmount(value).ToException();
    }

    return new Amount(value, currency);
}
```

The happy path stays readable, while the factory centralizes the error code, messages, context, and documentation.

## 🧮 Domain operations

Operations between valid domain objects may still violate a rule.

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) {
        throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
    }

    return new Amount(Value + other.Value, Currency);
}
```

Use a precise factory name rather than a generic exception such as `InvalidOperationException`. The code should say which domain situation occurred.

## 📥 Expected validation failures

User input, parsing, and business validation often fail as part of normal application flow. Return the error as data when the caller is expected to decide what happens next.

```csharp
public Outcome<Amount> TryCreateAmount(decimal value, string currencyCode) {
    if (!Currency.TryParse(currencyCode, out Currency currency)) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.UnknownCurrency(currencyCode));
    }

    if (value < 0) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.NegativeAmount(value));
    }

    return Outcome<Amount>.Success(new Amount(value, currency));
}
```

The failure still carries the same structured `Error`; it is simply not thrown at this stage.

## 📦 Batch and file processing

In a batch, each item can fail independently. Handle each `Outcome<T>` and continue when that matches the business requirement.

```csharp
foreach (string line in file) {
    Outcome<Amount> result = TryParseAmount(line);

    if (result.IsFailure) {
        Log(result.Error!);
        continue;
    }

    Process(result.Value);
}
```

This pattern is appropriate only when continuing is intentional. If one invalid item invalidates the entire file, return or throw a file-level error instead.

## 🌐 Incoming boundaries

An incoming adapter may reject an interaction because mapping, validation, or domain construction failed.

```csharp
DomainError invalidAmount = InvalidAmountOperationError.NegativeAmount(request.Amount);

return PrimaryPortError.Create(
        Code.RequestRejected,
        diagnosticMessage: $"Request {request.Id} contains an invalid amount.",
        new PrimaryPortInnerErrors().Add(invalidAmount),
        configureContext: ctx => ctx.Add(ErrCtxKey.RequestId, request.Id))
    .WithPublicMessage(
        shortMessage: "The request contains invalid data.");
```

The domain error explains the violated rule. The primary-port error explains the boundary-level outcome. See [Error Taxonomy and Composition](ErrorTaxonomy.en.md) for the nesting rules.

## 🔌 Outgoing dependencies

A database, broker, filesystem, or remote API failure is an outgoing interaction.

```csharp
return SecondaryPortError.Create(
        Code.PaymentProviderUnavailable,
        diagnosticMessage: "The payment provider timed out after 5 seconds.",
        transience: Transience.Transient)
    .WithPublicMessage(
        shortMessage: "The payment service is temporarily unavailable.");
```

`Transience` indicates whether trying the same operation later may help. It does not itself implement a retry policy.

## 🔁 Multi-step application flows

When several expected-failure operations must run in sequence, compose their outcomes rather than repeatedly checking and unpacking them.

```csharp
Outcome<Receipt> result =
    TryCreateAmount(value, currencyCode)
        .Then(CheckLimits)
        .Then(Charge);
```

The first failure short-circuits the remaining steps and is propagated unchanged. Use a fluent chain only when it reads more clearly than ordinary branching.

The full behavior of `Then`, `To`, `Recover`, `Finally`, async overloads, and escape hatches is documented in [Composing with Outcome](OutcomeGuide.en.md).

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
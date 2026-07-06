# Usage Patterns

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./UsagePatterns.fr.md)

FirstClassErrors is most useful when errors are not just technical failures, but **meaningful events in the life of the system**.
Below are common patterns where the library brings clarity and structure.

## 🧱 1️. Value Object invariants

When creating a value object, invalid states must be rejected.

```csharp
public static Amount From(decimal value, Currency currency)
{
    if (value < 0)
    {
        throw InvalidAmountOperationError.NegativeAmount(value).ToException();
    }

    return new Amount(value, currency);
}
```

Here:

* the domain rule is explicit
* the exception represents a precise invariant violation
* documentation describes the rule and diagnostics

This keeps domain code expressive and self-explanatory.

## 📥 2. Input validation (API / UI)

User or external inputs may be invalid, but not exceptional in the technical sense.

```csharp
public Outcome<Amount> TryCreateAmount(decimal value, string currencyCode)
{
    if (!Currency.TryParse(currencyCode, out var currency))
    {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.UnknownCurrency(currencyCode));
    }

    return Outcome<Amount>.Success(new Amount(value, currency));
}
```

Errors are:

* captured
* transportable
* diagnosable

without interrupting the flow.

## 🧮 3️. Domain operations

Operations between domain objects often have semantic constraints.

```csharp
public Amount Add(Amount other)
{
    if (Currency != other.Currency)
    {
        throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
    }

    return new Amount(Value + other.Value, Currency);
}
```

The code reads like domain language, while the error remains structured and documented.

## 📦 4️. Batch or file processing

In batch processing, many items may fail independently.

```csharp
foreach (var line in file)
{
    var result = TryParseAmount(line);

    if (result.IsFailure)
    {
        Log(result.Error);
        continue;
    }

    Process(result.Value);
}
```

Errors are:

* collected
* logged with full diagnostics
* not disruptive to the entire process

## 🌐 5️. Integration boundaries

When interacting with external systems:

* data may be inconsistent
* formats may change
* assumptions may break

Using first-class errors helps distinguish:

* domain issues
* input issues
* system or transformation issues

Diagnostics guide where investigation should start.

## 🔁 6️. Validation pipelines

Complex validations often involve multiple checks.

```csharp
var result = ValidateAmount(amount)
             .Then(CheckCurrency)
             .Then(CheckLimits);
```

Each failure carries an `Error`, keeping the model consistent while avoiding uncontrolled throwing.

## 🧩 7️. Support-oriented logging

Because errors carry structured diagnostics, logs become more useful:

* stable error codes
* meaningful short messages
* documented causes

Support teams can relate runtime events to documented error cases.

## 🛠️ 8. Composing with the `Outcome` pipeline

`Outcome` and `Outcome<T>` let you compose success and failure paths without throwing.
A failure carries an `Error` (never an `Exception`), so the whole chain stays diagnosable.

* **`Then(...)`** — chain the next step only when the previous one succeeded (short-circuits on failure).
* **`To(...)`** — map the carried value to another value (`Outcome<T>` only), preserving any failure.
* **`Recover(...)`** — provide a fallback when the chain has failed.
* **`Finally(...)`** — run terminal handling for both success and failure.

```csharp
Outcome<Receipt> outcome =
    TryCreateAmount(value, currencyCode)         // Outcome<Amount>
        .Then(amount => CheckLimits(amount))     // Outcome<Amount>, runs only on success
        .To(amount => amount.WithVat())          // map the value, failures pass through
        .Recover(error => Amount.Zero)           // fallback value if the chain failed
        .Then(amount => Charge(amount))          // Outcome<Receipt>
        .Finally(
            onSuccess: receipt => Log($"Charged {receipt}"),
            onFailure: error => Log(error));      // error is an Error, fully diagnosable
```

### Escape hatches

When you need to leave the `Outcome` world (e.g. at an application boundary), two escape hatches turn a failure back into a throw:

* **`ThrowIfFailure()`** — throws the failure's exception (via `error.ToException()`) when the outcome failed; otherwise does nothing.
* **`GetResultOrThrow()`** — returns the carried value on success, or throws the failure's exception (`Outcome<T>` only).

```csharp
Outcome<Amount> outcome = TryCreateAmount(value, currencyCode);

outcome.ThrowIfFailure();            // throws error.ToException() on failure
Amount amount = outcome.GetResultOrThrow(); // value on success, otherwise throws
```

### Async composition

For asynchronous flows, `OutcomeTaskExtensions` provides `Then` / `To` / `Recover` / `Finally`
overloads over `Task<Outcome>` and `Task<Outcome<T>>`. Each overload accepts an optional
`CancellationToken`, so you can await the whole pipeline:

```csharp
Outcome<Receipt> outcome =
    await TryLoadAmountAsync(orderId, cancellationToken)   // Task<Outcome<Amount>>
        .Then(amount => CheckLimitsAsync(amount), cancellationToken)
        .To(amount => amount.WithVat())
        .Recover(error => Amount.Zero)
        .Then(amount => ChargeAsync(amount), cancellationToken)
        .Finally(
            onSuccess: receipt => LogAsync(receipt),
            onFailure: error => LogAsync(error),
            cancellationToken);
```

## 🎯 Summary

FirstClassErrors shines when:

| Situation         | Benefit                     |
| ----------------- | --------------------------- |
| Domain invariants | Clear semantic violations   |
| Validation        | Errors as data              |
| Operations        | Readable domain code        |
| Batch processing  | Non-blocking error handling |
| Integration       | Better troubleshooting      |
| Support           | Structured knowledge        |

The library helps you express not just that something failed —
but **what it means, why it might have happened, and where to look**.

---

<div align="center">
<a href="WritingErrorsGuide.en.md">← Writing Errors Guide</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="BestPractices.en.md">Best Practices →</a>
</div>

---
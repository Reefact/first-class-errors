# Usage Patterns

DiagnosableExceptions is most useful when errors are not just technical failures, but **meaningful events in the life of the system**.
Below are common patterns where the library brings clarity and structure.

## 🧱 1️. Value Object invariants

When creating a value object, invalid states must be rejected.

```csharp
public static Amount From(decimal value, Currency currency)
{
    if (value < 0)
    {
        throw InvalidAmountException.NegativeValue(value, currency);
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
public TryOutcome<Amount> TryCreateAmount(decimal value, string currencyCode)
{
    if (!Currency.TryParse(currencyCode, out var currency))
    {
        return TryOutcome<Amount>.Failure(
            InvalidAmountException.UnknownCurrency(currencyCode));
    }

    return TryOutcome<Amount>.Success(new Amount(value, currency));
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
        throw InvalidAmountOperationException.CurrencyMismatch(this, other);
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
        Log(result.Exception);
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

Using diagnosable exceptions helps distinguish:

* domain issues
* input issues
* system or transformation issues

Diagnostics guide where investigation should start.

## 🔁 6️. Validation pipelines

Complex validations often involve multiple checks.

```csharp
var result = ValidateAmount(amount)
             .Bind(CheckCurrency)
             .Bind(CheckLimits);
```

Each failure can carry a diagnosable exception, keeping the model consistent while avoiding uncontrolled throwing.

## 🧩 7️. Support-oriented logging

Because exceptions carry structured diagnostics, logs become more useful:

* stable error codes
* meaningful short messages
* documented causes

Support teams can relate runtime events to documented error cases.

## 🎯 Summary

DiagnosableExceptions shines when:

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

Previous section : [Writing Errors Guide](WritingErrorsGuide.en.md) | Next section: [Best Practices](BestPractices.en.md)

---
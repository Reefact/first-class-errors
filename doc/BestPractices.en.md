# Best Practices

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./BestPractices.fr.md)

FirstClassErrors is most effective when used consistently and with intention.
These practices help keep errors meaningful, readable, and useful.

## 🧠 1. One error situation per factory

Each factory method should represent **one precise error situation**.

Avoid:

* factories that cover multiple different causes
* generic “InvalidOperation” factories

A factory should answer:

> “What exactly went wrong?”

**Why:**
Clear boundaries between error situations make diagnostics precise and documentation reliable.

## 🏷️ 2. Keep error codes stable

Error codes are part of the contract.

* Do not change codes casually
* Do not reuse a code for another situation
* Treat them as long-lived identifiers

**Why:**
Error codes are used in logs, documentation, and support workflows. Stability preserves traceability over time.

## ✂️ 3. Keep the happy path clean

Error factories should keep error construction out of domain logic.

Prefer:

```csharp
throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();
```

Over:

```csharp
throw new DomainException(/* manually assembled Error */);
```

**Why:**
This keeps business logic readable and separates domain intent from error construction details.

## 📘 4. Write documentation for humans

Error documentation is not for the compiler — it is for:

* developers
* support
* operators

Avoid technical noise. Focus on:

* meaning
* rule
* plausible causes

## 🔎 5. Diagnostics are hypotheses, not blame

Diagnostics should describe possible states, not accuse actors.

Prefer:

> “Amounts were used without conversion.”

Avoid:

> “The developer forgot to convert.”

**Why:**
Diagnostics guide investigation. Blame-oriented wording harms collaboration and does not help troubleshooting.

## 🧭 6. Analysis leads guide, they don’t prescribe

Do not include operational processes or support procedures.

Avoid:

* “Open a ticket”
* “Contact team X”

Focus on investigation direction, not workflow.

**Why:**
Operational processes depend on organizational context, not on the application itself. Encoding them in error documentation couples your code to external procedures and makes documentation brittle when processes change.

## 🔁 7. Use Outcome where failure is expected

Use exceptions for:

* invariant violations
* unexpected states

Use `Outcome<T>` when:

* validating input
* processing batches
* partial failure is normal

**Why:**
This keeps exception flow meaningful while still allowing rich error information in non-exceptional scenarios.

## 🧩 8. Don’t document technical accidents

Avoid documenting:

* NullReferenceExceptions
* framework exceptions
* low-level technical failures

The DSL is for **meaningful application errors**, not incidental crashes.

**Why:**
The goal is to document system behavior and rules, not unpredictable technical incidents.

## 🧪 9. Examples should educate, not stress test

Examples are not unit tests.

Use:

* simple
* realistic
* clear values

Avoid edge cases or pathological data.

## 🧱 10. Keep documentation close to the factory

Documentation methods should live in the same error factory class as the factory.

This keeps:

* intent
* error creation
* documentation

in the same conceptual place.

**Why:**
Keeping documentation next to the factory ensures it evolves with the code. This prevents drift and preserves the core idea of living documentation: knowledge stays where the behavior is defined.

## 🧩 11. Group errors in a dedicated factory class

Application-specific errors should be grouped in a `static` factory class annotated with `[ProvidesErrorsFor(...)]`, with one `internal static` factory method per error situation.

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Cannot operate on amounts with different currencies: {left.Currency} and {right.Currency}.")
            .WithPublicMessage(
                shortMessage: "Amounts use different currencies.",
                detailedMessage: "The amounts involved use different currencies.");
    }

    // ... documentation method and error codes ...
}
```

**Why:**
Each factory method represents a well-defined error category. Grouping them in a dedicated class keeps related error situations, their codes, and their documentation in one place. Note that the core types (`DomainError`, `DomainException`, …) are **not** sealed — inheritance is intentionally allowed so you can model your own error hierarchies — but in practice you author error situations through these factory classes rather than by subclassing.

## 🏭 12. Build errors through factories, throw via `ToException()`

You never `new` a `DiagnosableException` in user code: an exception's only constructor takes an `Error`. Errors themselves are no longer built through public constructors either — those are now internal. An error is assembled through the staged builder — `Type.Create(code, diagnosticMessage, …)` captures the mandatory internal information and returns an intermediate stage, and `.WithPublicMessage(shortMessage, detailedMessage)` finalizes the real error (there is no `.Build()`). Factory methods encapsulate that call, so you simply invoke the factory and turn its result into an exception with `ToException()`.

```csharp
// Build an Error through the factory, then throw it as an exception:
throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();
```

When failure is expected rather than exceptional, return the same factory's `Error` inside an `Outcome<T>`:

```csharp
return Outcome<Amount>.Failure(InvalidAmountOperationError.NegativeAmount(value));
```

**Why:**
Routing every error through a factory ensures that all errors of a given category are created in a controlled, documented, and semantically consistent way, whether they are thrown as exceptions or carried as `Outcome` failures.

## 🎯 Final thought

FirstClassErrors is about **expressing knowledge**, not just handling errors.

Well-written errors improve:

* code readability
* troubleshooting
* documentation
* shared understanding of the system

---

Previous section: [Usage Patterns](UsagePatterns.en.md) | Next section: [CI/CD and Operational Integration](OperationalIntegration.en.md)

---
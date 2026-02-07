# Best Practices

DiagnosableExceptions is most effective when used consistently and with intention.
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

Exception factories should keep error construction out of domain logic.

Prefer:

```csharp
throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);
```

Over:

```csharp
throw new InvalidAmountOperationException(...);
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

## 🔁 7. Use TryOutcome where failure is expected

Use exceptions for:

* invariant violations
* unexpected states

Use `TryOutcome<T>` when:

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

Documentation methods should live in the same exception class as the factory.

This keeps:

* intent
* error creation
* documentation

in the same conceptual place.

**Why:**
Keeping documentation next to the factory ensures it evolves with the code. This prevents drift and preserves the core idea of living documentation: knowledge stays where the behavior is defined.

## 🧩 11. Seal application exception types

Application-specific exceptions should be declared as `sealed`.

```csharp
public sealed class InvalidAmountOperationException : DomainException
```

**Why:**
Each exception type represents a well-defined error category. Allowing inheritance tends to blur semantics, create unclear hierarchies, and make diagnostics harder to reason about. Sealing the type ensures that the meaning of the exception remains stable and explicit.

## 🏭 12. Use private constructors and factory methods

Exception constructors should be `private` and only the required ones should be implemented as necessary.

```csharp
private InvalidAmountOperationException(string errorCode, string errorMessage)
    : base(errorCode, errorMessage) { }
```

Instances should always be created through factory methods:

```csharp
throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);
```

**Why:**
By restricting constructors, you ensure that all exceptions of this type are created in a controlled, documented, and semantically consistent way.

## 🎯 Final thought

DiagnosableExceptions is about **expressing knowledge**, not just handling errors.

Well-written errors improve:

* code readability
* troubleshooting
* documentation
* shared understanding of the system

---

Previous section: [Usage Patterns](UsagePatterns.en.md) | Next section: [CI/CD and Operational Integration](OperationalIntegration.en.md)

---
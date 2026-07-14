# Error Context: When and Why to Use It

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ErrorContext.fr.md)

`ErrorContext` attaches **structured, typed, and stable** metadata to an `Error`.

It answers one question:

> What was true when this specific occurrence happened?

The error code identifies the situation. The context records the facts that make this occurrence diagnosable.

## ✅ When to use `ErrorContext`

Use it when the information:

- varies from one occurrence to another;
- helps diagnosis, correlation, or observability;
- is safe to record in logs;
- can be represented as a small, stable value.

Typical examples include:

- a business identifier used during investigation (`OrderId`, `StatementId`, `CustomerId`);
- a value that violated a rule (`ProvidedTemperature`, `DeclaredAmount`);
- a relevant date or boundary (`TransactionDate`, `PeriodStart`, `PeriodEnd`);
- an external correlation identifier.

In short: use context for **instance-level facts**, not for the stable meaning of the error.

## ❌ When not to use it

Do not put in context:

- information that belongs to the stable error definition, such as its title, rule, or diagnostics;
- full request or response bodies;
- large objects or files;
- passwords, tokens, secrets, or unnecessary personal data;
- operational instructions such as “open a ticket” or “contact team X”;
- values that nobody can use during investigation.

If data is noisy, sensitive, unstable, oversized, or not actionable, keep it out.

## 🎯 Code, messages, and context have different roles

| Element | Question it answers |
| --- | --- |
| `ErrorCode` | Which recognized error situation occurred? |
| public messages | What may be explained safely to the caller? |
| `DiagnosticMessage` | What internal detail explains this occurrence? |
| `ErrorContext` | Which structured facts should logs and tooling be able to query? |

Do not duplicate every value from the diagnostic message into context. Add a context entry when the value should be searchable, filterable, correlated, or consumed by tooling.

## 🧱 Define named, reusable keys

Define keys once in a stable location:

```csharp
internal static class ErrCtxKey {
    public static readonly ErrorContextKey<Guid> OrderId =
        ErrorContextKey.Create<Guid>(
            "ORDER_ID",
            "Identifier of the order being processed.");

    public static readonly ErrorContextKey<Guid> StatementId =
        ErrorContextKey.Create<Guid>(
            "STATEMENT_ID",
            "Identifier of the statement being processed.");

    public static readonly ErrorContextKey<DateOnly> TransactionDate =
        ErrorContextKey.Create<DateOnly>(
            "TRANSACTION_DATE",
            "Date of the transaction being processed.");
}
```

A named key gives the value a stable identity and type. Dashboards, log queries, and generated documentation can rely on that contract.

## 🏭 Add context in the error factory

Attach context where the error is created so every occurrence uses the same keys.

```csharp
return PrimaryPortError.Create(
        Code.DateOutOfStatementPeriod,
        diagnosticMessage: $"Transaction dated {transactionDate} is outside [{periodStart}; {periodEnd}].",
        transience: Transience.NonTransient,
        configureContext: ctx => ctx
            .Add(ErrCtxKey.TransactionDate, transactionDate)
            .Add(ErrCtxKey.StatementId, statementId))
    .WithPublicMessage(
        shortMessage: "The transaction date is outside the statement period.",
        detailedMessage: "The transaction cannot be accepted for this statement period.");
```

Adding context in an adapter, catch block, or logging middleware after the fact risks inconsistent keys and missing data. Prefer the factory whenever the information is available there.

## 🔁 Context travels with the error

Context belongs to the `Error`, not to a particular transport.

```mermaid
flowchart LR
    A[Error factory] --> B[Error with Context]
    B --> C[Outcome<T>]
    B --> D[error.ToException()]
    C --> E[result.Error.Context]
    D --> F[exception.Error.Context]
```

The same context is preserved when the error:

- is returned inside `Outcome` or `Outcome<T>`;
- is propagated through `Then`, `To`, or other outcome operations;
- is converted into an exception through `ToException()`;
- is nested as an inner error.

This is why context should describe the occurrence itself rather than one transport such as HTTP or exceptions. See [Usage Patterns](UsagePatterns.en.md) and [Composing with Outcome](OutcomeGuide.en.md).

## 📦 Keep values small and serializable

Prefer primitives, enums, identifiers, dates, and small value objects that serialize predictably.

Good context:

```text
ORDER_ID = 7f7a7f30-3b28-44d6-b956-f85ef8f70b03
TRANSACTION_DATE = 2026-07-14
PROVIDED_AMOUNT = 127.33
```

Poor context:

```text
ORDER = <complete aggregate>
REQUEST_BODY = <full JSON document>
CUSTOMER = <entire personal profile>
```

If several values describe the same failure, use several explicit keys rather than one opaque object.

## 🔒 Treat context as log data

Even when context is not public API data, assume it may appear in logs, traces, support tools, or exported telemetry.

Before adding a value, consider:

- data-protection requirements;
- retention duration;
- access to operational tooling;
- whether hashing or partial redaction is sufficient;
- whether the value is truly necessary.

A technically useful value is not automatically appropriate to record.

## 📌 Practical checklist

Before adding a context entry, ask:

1. Does it vary per occurrence?
2. Does it help someone investigate faster?
3. Should logs or tooling be able to query it independently?
4. Is the key name stable and reusable?
5. Is the value small and predictably serializable?
6. Is it safe to retain in operational systems?

If the answer is yes to all six, it is a strong context candidate.

---

<div align="center">
<a href="CoreConcepts.en.md">← Core Concepts</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="WritingErrorsGuide.en.md">Writing Errors Guide →</a>
</div>

---
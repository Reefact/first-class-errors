# Error Context: When and Why to Use It

`ErrorContext` lets you attach **structured, typed, and stable** metadata to a `DiagnosableException` instance.

It complements the error code and messages by answering:

> What was true at the moment this specific error happened?

## ✅ When to use `ErrorContext`

Use it when the information is useful for diagnosis and observability, and varies per occurrence.

Typical examples:

* a transaction date that is outside an allowed period
* a business identifier useful for investigation (`OrderId`, `StatementId`, `CustomerId`)
* a measured value that violated a rule (`ProvidedTemperature`, `DeclaredAmount`)

In short: use context for **instance-level facts**, not for generic error semantics.

## ❌ When not to use it

Do not put in context:

* information that already belongs to the stable error definition (title, rule, diagnostics)
* large payloads (full files, huge objects, full request/response bodies)
* secrets or sensitive data (passwords, tokens, full personal data)
* operational instructions (“open ticket”, “contact team X”)

If data is unstable, noisy, sensitive, or not actionable, keep it out.

## 🎯 Why it improves observability

With `ErrorCode` you can group errors by type.
With `ErrorContext` you can understand **why this occurrence** happened.

This enables:

* better triage in logs
* faster correlation across systems
* easier dashboards and filtering (by context key)
* less back-and-forth between dev and support

Think of it this way:

* `ErrorCode` = *which error category is this?*
* `ErrorContext` = *what are the key facts for this occurrence?*

## 🧱 Design guidelines

### 1) Use named, reusable keys

Define context keys once in a central place:

```csharp
internal static class ErrCtxKey {
    public static readonly ErrorContextKey<DateOnly> TransactionDate =
        ErrorContextKey.Create<DateOnly>("TRANSACTION_DATE", "Date of the transaction being processed.");
}
```

### 2) Add context at factory level

Attach context where the exception is created, so every occurrence is consistent:

```csharp
return new NonCompliantBankTransactionFileException(
    Code.DateOutOfStatementPeriod,
    $"Transaction dated {transactionDate} is outside statement period [{periodStart};{periodEnd}].",
    "Transaction date is outside the statement period.",
    ctx => ctx.Add(ErrCtxKey.TransactionDate, transactionDate));
```

### 3) Keep values simple and serializable

Prefer primitives and small value objects that log cleanly.

### 4) Keep key names stable

Context keys become part of your operational vocabulary. Renaming them frequently hurts dashboards and queries.

## 📌 Practical checklist

Before adding a context entry, ask:

* Does it help someone diagnose this error faster?
* Is it safe to expose in logs?
* Is it specific to this occurrence?
* Can support/ops act on it?

If yes to most of these, it is a good candidate.

---

Previous section: [Core Concepts](CoreConcepts.en.md) | Next section: [Writing Errors Guide](WritingErrorsGuide.en.md)

---

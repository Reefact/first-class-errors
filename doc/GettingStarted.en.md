# Getting Started

FirstClassErrors helps you treat errors as **structured, diagnosable knowledge** instead of simple exception messages.

In a few minutes, you will see how to:

* define an error (with a factory)
* use error factories (required for living documentation)
* document the error in a structured way
* attach diagnostics
* optionally use the error without throwing

## 1️⃣ Define an error (with a factory)

To benefit from **living documentation**, errors are not created directly with `new`. Instead, they are created through **static factory methods** inside the error class.

This pattern is essential because:

* each factory method represents a **specific error situation**
* it is the anchor point for the documentation DSL
* the documentation generator links factories to documentation

Note:

*Using factory methods to create errors is a well-established .NET pattern for centralizing and standardizing error creation. FirstClassErrors builds on this idea and makes error factories the anchor point for structured, living error documentation. Beyond documentation, factories significantly improve code readability: they keep error construction (error codes, messages, formatting, and wording) out of the “happy path,” allowing domain logic to remain focused on business rules rather than technical details. A call such as `throw InvalidAmountOperationError.CurrencyMismatch(a1, a2).ToException();` expresses intent far more clearly than inlined error construction. This approach aligns with clean code principles by separating concerns, reducing duplication, and giving each error situation a named, explicit representation in the codebase — while also providing a single, consistent place to attach diagnostics and documentation.*

Example:

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount amount1, Amount amount2) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Failed to perform the monetary operation because the involved amounts are expressed in different currencies: {amount1} and {amount2}.")
            .WithPublicMessage(
                shortMessage: "Currency mismatch",
                detailedMessage: "The amounts involved in the operation are expressed in different currencies.");
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch = ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }

}
```

Here:

* The **error type** represents a category of domain errors.
* The **factory method** represents a precise error case.
* The **error code** is stable and machine-readable.
* The factory method is what will be documented.

The error is built in two stages: `DomainError.Create(...)` captures the mandatory internal information (the error `code` and the internal `diagnosticMessage`), and `.WithPublicMessage(shortMessage, detailedMessage)` supplies the public-facing messages and produces the final error. The mandatory `shortMessage` is a safe public summary, the optional `detailedMessage` is a controlled public detail, and the `diagnosticMessage` is for logs, support and developers — never exposed to clients by default. There is no `.Build()`, and the public constructors are internal, so an error can never be left without its public message.

You never `new` the error or the exception yourself: the error is created through the staged builder inside the factory, and when you need to throw, you call `error.ToException()` (see section 4).

## 2️⃣ Link the factory to structured documentation

Each factory method is linked to documentation using `[DocumentedBy]`.

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Amount currency mismatch")
                        .WithDescription("This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.")
                        .WithRule("All monetary operations must involve amounts expressed in the same currency.")
                        .WithDiagnostic(
                            "Amounts were used in a monetary operation without having been converted to the same currency.",
                            ErrorOrigin.Internal,
                            "Verify whether all amounts involved in the operation were converted to a common currency before being used together."
                        )
                        .AndDiagnostic(
                            "Amounts expected to be expressed in the same currency were provided with different currencies.",
                            ErrorOrigin.InternalOrExternal,
                            "Check the currencies associated with each amount and confirm whether a common currency was expected for this operation."
                        )
                        .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
}
```

Each diagnostic declares an **origin** via `ErrorOrigin`, whose values are `Internal`, `External`, and `InternalOrExternal` — indicating whether the cause lies inside the system, outside it (input), or could be either.

This documentation:

* explains what the error means
* states the violated rule
* provides diagnostic hypotheses
* gives realistic example messages

This is structured knowledge, not a comment.

## 3️⃣ Add structured error context (`ErrorContext`)

When information helps diagnose **a specific occurrence**, attach it as context.

```csharp
return PrimaryPortError.Create(
        Code.DateOutOfStatementPeriod,
        diagnosticMessage: $"Transaction dated {transactionDate} is outside the statement period [{periodStart};{periodEnd}].",
        transience: Transience.NonTransient,
        configureContext: ctx => ctx.Add(ErrCtxKey.TransactionDate, transactionDate))
    .WithPublicMessage(
        shortMessage: "Transaction date is outside the statement period.",
        detailedMessage: "The transaction date falls outside the allowed statement period.");
```

The context lives on the `Error`; when an exception is later produced with `error.ToException()`, it is reached through `exception.Error.Context`.

Best practices:

* use named, stable keys (`ErrorContextKey<T>`)
* add context at factory level
* avoid sensitive or oversized data

## 4️⃣ Use the exception in domain code

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }

    return new Amount(Value + other.Value, Currency);
}
```

Domain logic remains clean and expressive.

## 5️⃣ Or use it without throwing (`Outcome<T>`)

For validation or batch scenarios:

```csharp
public static Outcome<Amount> TryAdd(Amount a1, Amount a2) {
    if (a1.Currency != a2.Currency) { 
        return Outcome<Amount>.Failure(InvalidAmountOperationError.CurrencyMismatch(a1, a2)); 
    }

    return Outcome<Amount>.Success(new Amount(a1.Value + a2.Value, a1.Currency));
}
```

Note: `Failure(...)` takes an **`Error`** — the factory returns one directly, so no exception is involved.

You can inspect:

```csharp
if (result.IsFailure) {
    Log(result.Error);
}
```

Or escalate:

```csharp
var amount = result.GetResultOrThrow();
```

## 6️⃣ Generate documentation

Because factories are linked to structured documentation:

* errors can be extracted from assemblies
* documentation can be generated automatically
* support and developers share the same source of truth

## ✅ What you gain

With FirstClassErrors:

* errors are consistent
* documentation is close to the code
* diagnostics guide troubleshooting
* knowledge does not drift

You move from:

> “An exception happened”

to

> “This specific, documented error occurred, here is what it means and where to look.”

---

Next section: [Design Principles](DesignPrinciples.en.md)

---
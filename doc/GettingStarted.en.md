# Getting Started

DiagnosableExceptions helps you treat errors as **structured, diagnosable knowledge** instead of simple exception messages.

In a few minutes, you will see how to:

* define a diagnosable exception
* use exception factories (required for living documentation)
* document the error in a structured way
* attach diagnostics
* optionally use the error without throwing

## 1️. Define a diagnosable exception (with a factory)

To benefit from **living documentation**, exceptions are not created directly with `new`. Instead, they are created through **static factory methods** inside the exception class.

This pattern is essential because:

* each factory method represents a **specific error situation**
* it is the anchor point for the documentation DSL
* the documentation generator links factories to documentation

Note:

*Using factory methods to create exceptions is a well-established .NET pattern for centralizing and standardizing exception creation. DiagnosableExceptions builds on this idea and makes exception factories the anchor point for structured, living error documentation. Beyond documentation, factories significantly improve code readability: they keep error construction (error codes, messages, formatting, and wording) out of the “happy path,” allowing domain logic to remain focused on business rules rather than technical details. A call such as `throw InvalidAmountOperationException.CurrencyMismatch(a1, a2);` expresses intent far more clearly than inlined exception construction. This approach aligns with clean code principles by separating concerns, reducing duplication, and giving each error situation a named, explicit representation in the codebase — while also providing a single, consistent place to attach diagnostics and documentation.*

Example:

```csharp
[ProvidesErrorsFor(typeof(Amount))]
public sealed class InvalidAmountOperationException : DomainException {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    public static InvalidAmountOperationException CurrencyMismatch(Amount amount1, Amount amount2) {
        return new InvalidAmountOperationException(
            "AMOUNT_CURRENCY_MISMATCH",
            $"Failed to perform the monetary operation because the involved amounts are expressed in different currencies: {amount1} and {amount2}.",
            "Currency mismatch"
        );
    }

    private InvalidAmountOperationException(string errorCode, string errorMessage, string shortMessage)
        : base(errorCode, errorMessage, shortMessage) { }

}
```

Here:

* The **exception type** represents a category of domain errors.
* The **factory method** represents a precise error case.
* The **error code** is stable and machine-readable.
* The factory method is what will be documented.

## 2️. Link the factory to structured documentation

Each factory method is linked to documentation using `[DocumentedBy]`.

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Amount currency mismatch")
                        .WithDescription("This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.")
                        .WithRule("All monetary operations must involve amounts expressed in the same currency.")
                        .WithDiagnostic(
                            "Amounts were used in a monetary operation without having been converted to the same currency.",
                            ErrorCauseType.System,
                            "Verify whether all amounts involved in the operation were converted to a common currency before being used together."
                        )
                        .AndDiagnostic(
                            "Amounts expected to be expressed in the same currency were provided with different currencies.",
                            ErrorCauseType.SystemOrInput,
                            "Check the currencies associated with each amount and confirm whether a common currency was expected for this operation."
                        )
                        .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
}
```

This documentation:

* explains what the error means
* states the violated rule
* provides diagnostic hypotheses
* gives realistic example messages

This is structured knowledge, not a comment.

## 3️. Use the exception in domain code

```csharp
public Amount Add(Amount other) {
    if (Currency != other.Currency) { throw InvalidAmountOperationException.CurrencyMismatch(this, other); }

    return new Amount(Value + other.Value, Currency);
}
```

Domain logic remains clean and expressive.

## 4️. Or use it without throwing (`TryOutcome<T>`)

For validation or batch scenarios:

```csharp
public static TryOutcome<Amount> TryAdd(Amount a1, Amount a2) {
    if (a1.Currency != a2.Currency) { 
        return TryOutcome<Amount>.Failure(InvalidAmountOperationException.CurrencyMismatch(a1, a2)); 
    }

    return TryOutcome<Amount>.Success(new Amount(a1.Value + a2.Value, a1.Currency));
}
```

You can inspect:

```csharp
if (result.IsFailure) {
    Log(result.Exception);
}
```

Or escalate:

```csharp
var amount = result.GetOrThrow();
```

## 5️. Generate documentation

Because factories are linked to structured documentation:

* errors can be extracted from assemblies
* documentation can be generated automatically
* support and developers share the same source of truth

## ✅ What you gain

With DiagnosableExceptions:

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
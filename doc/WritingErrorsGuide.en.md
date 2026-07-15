# Writing Error Documentation

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./WritingErrorsGuide.fr.md)

A documented error should help someone understand a failure without opening the source code first.

This guide explains how to describe the **stable meaning** of an error: its code, title, description, rule, diagnostics, and examples. Runtime messages are covered separately in [Writing Error Messages](WritingErrorMessages.en.md).

> Documentation text may be authored as literals or read from localized resources. See [Internationalization](Internationalization.en.md).

## The model in one minute

Each documented factory answers six questions:

| Element | Question it answers |
| --- | --- |
| Error code | How can software identify this situation? |
| Title | What happened, in a few words? |
| Description | What does this error mean? |
| Rule | What should normally be true? |
| Diagnostics | What may explain it, and where should investigation start? |
| Examples | What does a real occurrence look like? |

The goal is not to describe every technical detail. It is to capture the knowledge that remains useful across logs, support investigations, releases, and refactorings.

## 1. Start with one precise error situation

A factory should represent one situation in which the system cannot continue as expected.

Avoid broad categories such as:

- `INVALID_OPERATION`
- `PROCESSING_ERROR`
- `UNEXPECTED_FAILURE`

Prefer situations that a developer or support engineer can recognize immediately:

- `AMOUNT_CURRENCY_MISMATCH`
- `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- `TRANSACTION_DATE_OUTSIDE_STATEMENT_PERIOD`

A useful test is:

> Could two occurrences with genuinely different meanings share this documentation without making it vague?

If the answer is no, they probably need separate factories and codes.

## 2. Choose a stable error code

The code is the machine-readable identity of the error.

Use `UPPER_SNAKE_CASE`, include enough domain scope to avoid ambiguity, and keep the code independent from class names or implementation details.

Good:

```text
AMOUNT_CURRENCY_MISMATCH
```

Avoid:

```text
INVALID_AMOUNT_OPERATION_ERROR
ADD_METHOD_FAILED
```

Treat the code as a contract. Clients, dashboards, alerts, and support procedures may depend on it. Renaming or removing it is therefore a breaking change; see [Catalog Versioning](CatalogVersioning.en.md).

## 3. Write a title that names the situation

The title is a short human-readable label.

Good titles:

- “Amount currency mismatch”
- “Temperature below absolute zero”
- “Transaction date outside statement period”

Avoid titles that merely announce failure:

- “Operation failed”
- “Invalid value”
- “Unexpected error”

A title should still make sense when displayed in a catalog index without its description.

## 4. Explain the meaning in plain language

The description explains when the error occurs and what the situation means.

A reliable pattern is:

> “This error occurs when…”

Write for someone who understands the business system but may not know the implementation. Describe the situation, not the stack trace, class, method, or exception-handling behavior.

Good:

> “This error occurs when an operation combines monetary amounts expressed in different currencies.”

Avoid:

> “This exception is thrown by `Amount.Add` when the currency fields are different.”

## 5. State the violated rule

The rule expresses the invariant or constraint that should normally hold.

Write it as a general truth:

> “All monetary operations must involve amounts expressed in the same currency.”

A rule should not repeat the description. The description says what happened; the rule says what must be true.

Omit the rule when no meaningful invariant exists rather than inventing one.

## 6. Write diagnostics as hypotheses

A diagnostic contains three parts:

| Part | Purpose |
| --- | --- |
| Cause | A plausible state or condition that may explain the error |
| Origin | Whether that cause is internal, external, or either |
| Analysis lead | Where investigation should start |

Causes are hypotheses, not proven root causes. Describe conditions without assigning blame.

Good cause:

> “Amounts were used before they had been converted to a common currency.”

Avoid:

> “The developer forgot to convert the amounts.”

A useful analysis lead starts with a neutral verb such as *Check*, *Verify*, or *Review*:

> “Verify whether every amount was converted to the operation currency before calculation.”

Do not encode organizational procedures such as “open a ticket” or “contact team X”. Those processes change independently from the application.

## 7. Use examples to make the rule visible

Examples should use simple, realistic values that make the violation obvious.

```csharp
.WithExamples(
    () => CurrencyMismatch(
        new Amount(127.33m, Currency.EUR),
        new Amount(84.10m, Currency.USD)))
```

Examples are educational catalog content, not boundary-value tests. Prefer one or two representative occurrences over pathological values.

Because the example invokes the real factory, it also keeps the generated documentation connected to the actual runtime error.

## Complete example

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Cannot combine {left.Currency} and {right.Currency} amounts: {left} and {right}.")
            .WithPublicMessage(
                shortMessage: "The amounts use different currencies.",
                detailedMessage: "All amounts in this operation must use the same currency.");
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError
            .WithTitle("Amount currency mismatch")
            .WithDescription("This error occurs when an operation combines monetary amounts expressed in different currencies.")
            .WithRule("All monetary operations must involve amounts expressed in the same currency.")
            .WithDiagnostic(
                "Amounts were used before they had been converted to a common currency.",
                ErrorOrigin.Internal,
                "Verify whether every amount was converted to the operation currency before calculation.")
            .AndDiagnostic(
                "An external request supplied amounts in incompatible currencies.",
                ErrorOrigin.External,
                "Check the currencies supplied by the caller and the currency expected by the operation.")
            .WithExamples(() => CurrencyMismatch(
                new Amount(127.33m, Currency.EUR),
                new Amount(84.10m, Currency.USD)));
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch =
            ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }
}
```

The documentation describes the stable error situation. The factory separately creates a concrete occurrence with runtime messages and values.

## Review checklist

Before accepting new error documentation, verify that:

- the factory represents one precise situation;
- the code is specific, stable, and written in `UPPER_SNAKE_CASE`;
- the title names the situation rather than announcing failure;
- the description is understandable without reading the implementation;
- the rule is a genuine invariant, or is omitted;
- diagnostic causes are plausible conditions rather than accusations;
- analysis leads guide investigation without prescribing support workflow;
- examples are simple, realistic, and call the documented factory;
- technical noise and sensitive data are absent.

For choosing public and internal runtime text, continue with [Writing Error Messages](WritingErrorMessages.en.md). For a compact project-wide review list, see [Best Practices](BestPractices.en.md).

---

<div align="center">
<a href="ErrorContext.en.md">← Error Context Guide</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="WritingErrorMessages.en.md">Writing Error Messages →</a>
</div>

---
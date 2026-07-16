# Getting Started

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./GettingStarted.fr.md)

This guide takes you from an empty project to your first generated error catalog.

You will:

1. install the library and CLI;
2. opt a project into documentation generation;
3. define one documented error;
4. use it in code;
5. generate the catalog.

## 1. Install the package and CLI

In the application project:

```bash
dotnet add package FirstClassErrors
```

Install the documentation CLI once on your machine:

```bash
dotnet tool install --global FirstClassErrors.Cli
```

## 2. Opt the project into documentation generation

Add this property directly to the project file that contains your errors:

```xml
<PropertyGroup>
  <GenerateErrorDocumentation>true</GenerateErrorDocumentation>
</PropertyGroup>
```

The marker must be present in the `.csproj` itself. Projects without it are skipped when the CLI scans a solution.

## 3. Define one error situation

Create a static factory class. Each factory method represents one precise situation the system recognizes. The examples use a small `Amount` value type with a `Currency`; substitute any type from your own domain.

```csharp
using FirstClassErrors;

[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                Code.CurrencyMismatch,
                diagnosticMessage: $"Cannot add {left} and {right} because their currencies differ.")
            .WithPublicMessage(
                shortMessage: "The amounts use different currencies.",
                detailedMessage: "Both amounts must use the same currency.");
    }

    private static class Code {
        public static readonly ErrorCode CurrencyMismatch =
            ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
    }
}
```

The four important parts are:

- `[ProvidesErrorsFor]` ties the factory class to the concept whose errors it declares; the generator uses it to find and group errors;
- the factory name expresses the situation in code;
- the error code is its stable, machine-readable identity;
- the diagnostic message is internal, while the public messages are safe for callers.

## 4. Add the structured documentation

The `[DocumentedBy]` attribute links the factory to a documentation method in the same class:

```csharp
private static ErrorDocumentation CurrencyMismatchDocumentation() {
    return DescribeError.WithTitle("Amount currency mismatch")
                        .WithDescription(
                            "This error occurs when an operation combines amounts expressed in different currencies.")
                        .WithRule(
                            "A monetary operation must use one common currency.")
                        .WithDiagnostic(
                            "The amounts reached the operation without being converted to one currency.",
                            ErrorOrigin.Internal,
                            "Verify where the amounts should have been converted before this operation.")
                        .AndDiagnostic(
                            "The caller provided amounts in currencies that cannot be combined directly.",
                            ErrorOrigin.External,
                            "Check the currencies supplied by the caller.")
                        .WithExamples(() => CurrencyMismatch(
                            new Amount(10m, Currency.EUR),
                            new Amount(12m, Currency.USD)));
}
```

This is structured knowledge rather than a comment: the generator can extract the title, explanation, rule, diagnostics, and real example produced by the factory.

Each diagnostic is a hypothesis: a plausible cause, an origin (whether the suspected cause lies inside the system or with an external caller), and an investigation lead.

## 5. Use the error

When the failure is exceptional, turn the error into its paired exception:

```csharp
public Amount AddOrThrow(Amount other) {
    if (Currency != other.Currency) {
        throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
    }

    return new Amount(Value + other.Value, Currency);
}
```

The business code names the situation without repeating codes or messages.

When failure is an expected part of the flow, carry the same `Error` without throwing:

```csharp
public static Outcome<Amount> Add(Amount left, Amount right) {
    if (left.Currency != right.Currency) {
        return Outcome<Amount>.Failure(
            InvalidAmountOperationError.CurrencyMismatch(left, right));
    }

    return Outcome<Amount>.Success(
        new Amount(left.Value + right.Value, left.Currency));
}
```

These two usages do not define two different errors. They transport the same error situation in different ways.

## 6. Generate the catalog

Build the solution, then generate a Markdown catalog:

```bash
dotnet build MyApp.sln -c Release

fce generate \
  --solution MyApp.sln \
  --configuration Release \
  --no-build \
  --format markdown \
  --service-name my-api \
  --output artifacts/errors.md
```

`--configuration Release` matches the build above: with `--no-build`, the generator reads the assemblies produced by that exact configuration (it defaults to `Debug` otherwise).

The generated document contains an entry for `AMOUNT_CURRENCY_MISMATCH`, including its description, rule, diagnostic hypotheses, and the messages produced by the example factory.

A shortened result looks like this:

```markdown
## Amount currency mismatch

**Code:** `AMOUNT_CURRENCY_MISMATCH`

This error occurs when an operation combines amounts expressed in different currencies.

**Rule:** A monetary operation must use one common currency.
```

Commit or publish this generated catalog according to your delivery workflow. For automatic generation in CI, see [CI/CD and Operational Integration](OperationalIntegration.en.md).

## Optional next steps

- Add occurrence-specific facts with [Error Context](ErrorContext.en.md).
- Understand the available error categories in [Error Taxonomy and Composition](ErrorTaxonomy.en.md).
- Learn when to throw or return an `Outcome<T>` in [Usage Patterns](UsagePatterns.en.md).
- Protect stable codes and context keys with [Catalog Versioning](CatalogVersioning.en.md).

---

<div align="center">
<a href="../README.md#-documentation">↑ Table of contents</a> · <a href="DesignPrinciples.en.md">Design Principles →</a>
</div>

---

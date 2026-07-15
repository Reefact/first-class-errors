# FirstClassErrors

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./doc/README.fr.md)

|  |  |
| :-- | :-- |
| **Build** | [![ci](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/ci.yml) |
| **Quality** | [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=reefact_first-class-errors&metric=coverage)](https://sonarcloud.io/summary/new_code?id=reefact_first-class-errors) |
| **Security** | [![codeql](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/Reefact/first-class-errors/actions/workflows/codeql.yml) [![OpenSSF Best Practices](https://www.bestpractices.dev/projects/13567/badge)](https://www.bestpractices.dev/projects/13567) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Reefact/first-class-errors/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Reefact/first-class-errors) |
| **Package** | [![NuGet](https://img.shields.io/nuget/vpre/FirstClassErrors?logo=nuget)](https://www.nuget.org/packages/FirstClassErrors) ![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) |
| **Project** | [![License](https://img.shields.io/github/license/Reefact/first-class-errors)](LICENSE) [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-fe5196?logo=conventionalcommits&logoColor=white)](https://www.conventionalcommits.org) |

---

**Turn your errors into structured, living knowledge about your system.**

![FirstClassErrors](./doc/images/first-class-errors.png "FirstClassErrors")

An application error too often boils down to a bare code or string, with nothing that explains what it actually means. FirstClassErrors turns it into a first-class concept of your domain. You describe each error situation once, with its rules, in one place in the code; the whole library then builds on that model.

From there, it covers the full chain:

**Model the error**
An error becomes an object that carries its own meaning — a violated business rule, a rejected incoming request, or a failing external service, transient or not.

**Handle the failure**
You choose how it travels: thrown as an ordinary exception, or returned as an explicit result the caller inspects without a try/catch.

**Document the error**
Each error can carry its explanation, its rule, and its likely causes right there in the code. FirstClassErrors generates a human-readable catalog from that same code — so it never drifts.

**Check the model**
Roslyn analyzers enforce these rules at compile time: a duplicated error code or inconsistent documentation becomes a build error, caught before it ever reaches production.

**Bind external input**
A form or a JSON request must become a valid domain object. Where classic .NET binding restates those rules separately, in a validator or attributes, FirstClassErrors binds that input directly to your value objects, reusing their own construction rules instead of duplicating them.

## 🚨 The problem

A production error is rarely useful as only a type and a string:

```text
Invalid operation.
```

Developers and support still need to discover:

- which situation actually occurred;
- which rule was violated;
- which facts belong to this occurrence;
- what might have caused it;
- where to start investigating.

When that knowledge lives in logs, tickets, comments, and people's memories, it drifts away from the code.

## 💡 The FirstClassErrors approach

A factory gives the error situation a stable identity, keeps its construction in one place, and attaches structured documentation that will feed a generated, human-readable catalog:

```csharp
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount left, Amount right) {
        return DomainError.Create(
                ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH"),
                diagnosticMessage: $"Cannot add {left} and {right} because their currencies differ.")
            .WithPublicMessage(
                shortMessage: "The amounts use different currencies.",
                detailedMessage: "Both amounts must use the same currency.");
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError.WithTitle("Amount currency mismatch")
                            .WithDescription("This error occurs when an operation combines amounts expressed in different currencies.")
                            .WithRule("A monetary operation must use one common currency.")
                            .WithDiagnostic(
                                "The amounts reached the operation without being converted to one currency.",
                                ErrorOrigin.Internal,
                                "Verify where the amounts should have been converted before this operation.")
                            .WithExamples(() => CurrencyMismatch(
                                new Amount(10, Currency.EUR),
                                new Amount(12, Currency.USD)));
    }
}
```

The domain code stays focused on intent:

```csharp
if (Currency != other.Currency) {
    throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException();
}
```

The factory returns an `Error` (`DomainError` is one of its categories), so expected failures can use the same model without throwing:

```csharp
return Outcome<Amount>.Failure(
    InvalidAmountOperationError.CurrencyMismatch(left, right));
```

From these factories, FirstClassErrors can generate a human-readable catalog for developers, support teams, and operations.

## 📦 Installation

```bash
dotnet add package FirstClassErrors
```

The package targets **.NET Standard 2.0**. Its Roslyn analyzers are bundled automatically; no separate analyzer package is required.

To generate documentation, install the CLI:

```bash
dotnet tool install --global FirstClassErrors.Cli
```

Then follow the [Getting Started guide](doc/GettingStarted.en.md) to create and generate your first documented error.

## 🎯 When it is a good fit

FirstClassErrors is especially useful for long-lived application or domain code where:

- errors represent rules, constraints, or boundary failures;
- several teams or systems depend on stable error codes;
- support and operations investigate production failures;
- documentation must remain aligned with behavior.

For prototypes, tiny utilities, or low-level technical code, standard exceptions may be enough. See [When Not to Use FirstClassErrors](doc/WhenNotToUseFirstClassErrors.en.md).

## 🔍 Analyzers and supply-chain information

The package includes Roslyn rules with stable `FCExxx` identifiers. They detect duplicate or malformed error codes, invalid documentation wiring, missing examples, and common API misuse. See the [analyzer rules reference](doc/analyzers/README.md).

Released packages include signed build provenance and an embedded SPDX SBOM. See the release and verification details in the [supply-chain documentation](SECURITY.md).

## 🐛 Feedback and contributing

Found a bug or want to request a feature? Open an issue on the [GitHub issue tracker](https://github.com/Reefact/first-class-errors/issues). Contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

For security vulnerabilities, follow the private process in [SECURITY.md](SECURITY.md).

## 📚 Documentation

### Discover

- [Getting Started](doc/GettingStarted.en.md)
- [Design Principles](doc/DesignPrinciples.en.md)
- [When Not to Use FirstClassErrors](doc/WhenNotToUseFirstClassErrors.en.md)

### Understand the model

- [Core Concepts](doc/CoreConcepts.en.md)
- [Error Taxonomy and Composition](doc/ErrorTaxonomy.en.md)
- [Error Context Guide](doc/ErrorContext.en.md)

### Write and use errors

- [Writing Errors Guide](doc/WritingErrorsGuide.en.md)
- [Usage Patterns](doc/UsagePatterns.en.md)
- [Best Practices](doc/BestPractices.en.md)
- [Testing Guide](doc/Testing.en.md)

### Generate and operate the catalog

- [CI/CD and Operational Integration](doc/OperationalIntegration.en.md)
- Catalog versioning
  - [Overview and workflow](doc/CatalogVersioning.en.md)
  - [Command reference](doc/CatalogVersioningReference.en.md)
  - [CI/CD integration](doc/CatalogVersioningCI.en.md)
- [Architecture of the Documentation Pipeline](doc/ArchitectureOfTheDocumentationPipeline.en.md)
- [Writing a custom renderer](doc/WritingACustomRenderer.en.md)
- [Internationalization](doc/Internationalization.en.md)

### Evaluate and troubleshoot

- [Comparison with error-handling libraries](doc/ComparisonWithOtherLibraries.en.md)
- [Analyzer rules (FCExxx)](doc/analyzers/README.md)
- [FAQ](doc/FAQ.en.md)

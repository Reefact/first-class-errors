# FirstClassErrors

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./doc/README.fr.md)

![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4) [![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](./LICENSE)

---

**Turn your exceptions into structured, living knowledge about your system.**

![FirstClassErrors](./doc/images/first-class-errors.png "FirstClassErrors")

FirstClassErrors is a .NET library that treats errors as first-class, documented, and diagnosable concepts — not just strings thrown at runtime.

It helps you:

* express errors in a consistent and structured way
* attach meaningful diagnostics to each error
* keep error documentation close to the code
* generate human-readable error documentation automatically

## 🚨 The problem

In most systems, errors are:

* scattered across the codebase
* described by ad-hoc messages
* poorly documented
* hard to troubleshoot
* disconnected from support and operations

Over time, this leads to:

* duplicated investigations
* tribal knowledge
* support teams guessing
* developers reinventing error explanations

## 💡 The idea

What if:

> **Every error in your system was explicitly described, structured, and documented — directly in code — and that documentation could be generated automatically?**

FirstClassErrors introduces:

* a **rich error model**
* a **structured diagnostic system**
* a **DSL to document errors**
* a **documentation extraction pipeline**

Errors become:

> not just failures,
> but **documented knowledge units**.

## 🧱 What this library provides

### 1️⃣ A richer error model

The `Error` carries:

* a stable error code
* a timestamp
* optional short and full messages
* contextual data
* structured diagnostics

The exception itself only exposes its `.Error`, and is designed to be:

* logged consistently
* understood by humans
* used by tooling

### 2️⃣ Structured diagnostics

Each error can declare **possible causes** and **analysis leads**:

* What might have caused this error?
* Is it likely input-related, system-related, or both?
* Where should investigation start?

Diagnostics guide troubleshooting without hardcoding operational processes.

### 3️⃣ A DSL to describe errors

Errors are documented directly in code using a fluent API:

```csharp
return DescribeError.WithTitle("Temperature below absolute zero")
                    .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                    .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                    .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                    .WithExamples(
                        () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                        () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
```

This is not just comments — it is **structured, executable documentation**.

### 4️⃣ Documentation extraction

The library includes a mechanism to scan assemblies and extract all declared error documentation:

* linked to error factory classes
* linked to factory methods
* enriched with examples
* ready to be rendered

This enables:

* Markdown or JSON error catalogs (or any custom format via a renderer)
* support-oriented documentation
* living documentation generated from code
* multi-language catalogs (opt-in: English, French, Spanish, German, Swedish)

## 🔁 Exception or not? You choose.

The library supports both:

* **throwing errors** (traditional exception flow)
* **transporting errors without throwing** via `Outcome` and `Outcome<T>`

This allows you to use exceptions as:

> runtime signals
> or structured error data

depending on the context (domain logic, validation, pipelines, etc.).

## 🧩 Example

From the `FirstClassErrors.Usage` project:

```csharp
[ProvidesErrorsFor(nameof(Temperature))]
public static class InvalidTemperatureError {

    [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
    internal static DomainError BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        return new DomainError(
            Code.TemperatureBelowAbsoluteZero,
            $"Failed to instantiate temperature: the value {invalidValue} {invalidValueUnit} is below absolute zero.",
            "Temperature is below absolute zero.");
    }

    private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
        return DescribeError.WithTitle("Temperature below absolute zero")
                            .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                            .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                            .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                            .WithExamples(
                                () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                                () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
    }

    private static class Code {
        public static readonly ErrorCode TemperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
    }
}
```

The factory returns a structured `Error`. When you need to throw it, you turn it into an exception with `.ToException()`:

```csharp
throw InvalidTemperatureError.BelowAbsoluteZero(-1, TemperatureUnit.Kelvin).ToException();
```

Here, the error, its meaning, its rule, its diagnostics, and example messages are all defined together — in code.

## 🎯 Who is this for?

FirstClassErrors is especially useful if:

* you build complex business systems
* you care about supportability
* you want consistent error handling
* you want documentation that doesn’t drift from code
* you design with domain-driven thinking

## 📚 Next steps

See the full documentation:

- [Getting Started](doc/GettingStarted.en.md)
- [Design Principles](doc/DesignPrinciples.en.md)
- [When Not to Use FirstClassErrors](doc/WhenNotToUseFirstClassErrors.en.md)
- [Core Concepts](doc/CoreConcepts.en.md)
- [Error Context Guide](doc/ErrorContext.en.md)
- [Writing Errors Guide](doc/WritingErrorsGuide.en.md)
- [Usage Patterns](doc/UsagePatterns.en.md)
- [Best Practices](doc/BestPractices.en.md)
- [CI/CD and Operational Integration](doc/OperationalIntegration.en.md)
- [Architecture of the Documentation Pipeline](doc/ArchitectureOfTheDocumentationPipeline.en.md)
- [Writing a custom renderer](doc/WritingACustomRenderer.en.md)
- [Comparison with error-handling libraries](doc/ComparisonWithOtherLibraries.en.md)
- [FAQ](doc/FAQ.en.md)

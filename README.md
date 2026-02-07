# DiagnosableExceptions

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./docs/README.fr.md)

---

**Turn your exceptions into structured, living knowledge about your system.**

![Diagnosable Exceptions](./doc/images/diagnosable-exceptions.png "Diagnosable Exceptions")

DiagnosableExceptions is a .NET library that treats errors as first-class, documented, and diagnosable concepts — not just strings thrown at runtime.

It helps you:

* express errors in a consistent and structured way
* attach meaningful diagnostics to each error
* keep error documentation close to the code
* generate human-readable error documentation automatically

---

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

---

## 💡 The idea

What if:

> **Every error in your system was explicitly described, structured, and documented — directly in code — and that documentation could be generated automatically?**

DiagnosableExceptions introduces:

* a **rich exception model**
* a **structured diagnostic system**
* a **DSL to document errors**
* a **documentation extraction pipeline**

Errors become:

> not just failures,
> but **documented knowledge units**.

---

## 🧱 What this library provides

### 1️⃣ A richer exception model

Exceptions carry:

* a stable error code
* a timestamp
* optional short and full messages
* contextual data
* structured diagnostics

They are designed to be:

* logged consistently
* understood by humans
* used by tooling

---

### 2️⃣ Structured diagnostics

Each error can declare **possible causes** and **analysis leads**:

* What might have caused this error?
* Is it likely input-related, system-related, or both?
* Where should investigation start?

Diagnostics guide troubleshooting without hardcoding operational processes.

---

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

---

### 4️⃣ Documentation extraction

The library includes a mechanism to scan assemblies and extract all declared error documentation:

* linked to exception types
* linked to factory methods
* enriched with examples
* ready to be rendered

This enables:

* Markdown or HTML error catalogs
* support-oriented documentation
* living documentation generated from code

---

## 🔁 Exception or not? You choose.

The library supports both:

* **throwing errors** (traditional exception flow)
* **transporting errors without throwing** via `TryOutcome<T>`

This allows you to use exceptions as:

> runtime signals
> or structured error data

depending on the context (domain logic, validation, pipelines, etc.).

---

## 🧩 Example

From the `DiagnosableExceptions.Usage` project:

```csharp
[ProvidesErrorsFor(typeof(Temperature))]
public sealed class InvalidTemperatureException : DomainException {

    [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
    internal static InvalidTemperatureException BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        return new InvalidTemperatureException(
            "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            $"Failed to instantiate temperature: the value {invalidValue}{invalidValueUnit} is below absolute zero.",
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
}
```

Here, the exception, its meaning, its rule, its diagnostics, and example messages are all defined together — in code.

---

## 🎯 Who is this for?

DiagnosableExceptions is especially useful if:

* you build complex business systems
* you care about supportability
* you want consistent error handling
* you want documentation that doesn’t drift from code
* you design with domain-driven thinking

---

## 📚 Next steps

See the **DiagnosableExceptions.Usage** project in this repository for practical examples.

More advanced tooling (documentation generation, exporters, CLI) can be built on top of the structured model provided by this library.

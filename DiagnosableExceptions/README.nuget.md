# DiagnosableExceptions

**DiagnosableExceptions** is a lightweight .NET library that turns exceptions into **structured, documented, and diagnosable errors**.

Instead of throwing ad-hoc messages, the library helps you define errors as **explicit concepts with diagnostics and documentation directly in code**.

It is especially useful for systems where **supportability, troubleshooting, and operational clarity matter**.

## Key ideas

DiagnosableExceptions allows you to:

- define **structured exceptions with stable error codes**
- attach **diagnostics and investigation leads**
- keep **error documentation close to the code**
- generate **human-readable documentation automatically**

Errors become **documented knowledge about your system**, not just runtime failures.

## Example

    return DescribeError.WithTitle("Temperature below absolute zero")
                        .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                        .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                        .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                        .WithExamples(
                            () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                            () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));

This produces **structured documentation tied directly to the exception definition**.

## When to use DiagnosableExceptions

This library is particularly useful when:

- building **complex business systems**
- designing **domain-driven models**
- improving **error observability**
- supporting **production troubleshooting**
- generating **living documentation from code**

## Documentation

Full documentation and guides are available on GitHub:

https://github.com/Reefact/diagnosable-exceptions
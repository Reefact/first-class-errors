# ADR-0072 | Allow custom error subtypes

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0072-allow-custom-error-subtypes.fr.md)

**Status:** Proposed
**Proposed:** 2026-10-22
**Decision Makers:** Reefact

## Context

FirstClassErrors provides a typed hierarchy for domain and infrastructure errors, including primary and secondary ports.
Application platform teams can define more specific, reusable categories of errors, such as persistence failures, for both operational support and consistent diagnostics.

The existing categories can be instantiated through staged factories, but their constructors are inaccessible to subclasses in consuming assemblies.
Consequently, a custom category cannot extend the established taxonomy without replacing it or duplicating its behavior.
Consumers also cannot create a staged factory for a specialized error type.

## Decision

FirstClassErrors will allow applications to derive specialized error categories from each concrete built-in error type while retaining the staged public-message construction model.

## Rationale

An extensible type hierarchy allows shared platform libraries to introduce meaningful operational categories without requiring a concrete class for every individual error code.
Keeping specialized errors within the existing hierarchy preserves their architectural direction, transience classification, and diagnosable metadata.
Preserving staged construction provides a consistent way to supply public messages and minimizes divergence between built-in and user-defined error factories.

## Alternatives Considered

### Keep the built-in hierarchy closed to extension

This preserves a smaller public surface but prevents platform teams from defining common error families with normal type relationships.

### Model every specialization through an error code only

Codes identify individual failure cases well, but they do not provide reusable type categories for cross-cutting support and telemetry conventions.

## Consequences

### Positive

* Consumers can add semantic categories without changing FirstClassErrors.
* Existing factories and error codes remain valid.
* Custom types can use the same two-stage message contract.

### Negative

* The public API now exposes subclass construction points, increasing compatibility obligations.
* Custom types must maintain their own factory methods.

### Risks

* Custom subclasses may violate intended construction conventions; examples and tests should emphasize the two-stage factory pattern.
* Derived types may require explicit exception conversion behavior when a more specific exception is desired.

## Follow-up Actions

* Document the extension pattern and run the consumer-style inheritance tests across supported target frameworks.

## References

* [Built-in error hierarchy](../../../../FirstClassErrors/Error.cs)
* [Staged public messages](../../../../FirstClassErrors/PublicMessageStage.cs)

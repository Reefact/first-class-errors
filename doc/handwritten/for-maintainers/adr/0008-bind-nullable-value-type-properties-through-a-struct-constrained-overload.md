# ADR-0008 | Bind nullable value-type properties through a struct-constrained overload

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The request binder lets a DTO property be selected and then converted by a value-object factory, commonly supplied as a method group.

For nullable value-type properties, the original unconstrained generic selector inferred `Nullable<T>` as the converter input. A factory over the underlying value type therefore failed to bind as a method group even though the DTO correctly used a nullable property to distinguish absence from a legitimate default value.

C# cannot overload methods solely by changing generic constraints, but a struct-constrained selector can expose `Nullable<T>` as a distinct parameter shape while keeping the converter on the underlying non-nullable type.

Lists of nullable value types also require explicit null-element handling before conversion.

## Decision

The request binder binds nullable value-type DTO properties and list elements through dedicated `where TArgument : struct` selector paths whose converters operate on the underlying non-nullable value type.

## Rationale

The dedicated path restores the same method-group ergonomics that reference-type properties already have while preserving the semantic distinction between a missing argument and a supplied default value.

The overload belongs at the selector boundary because that is where C# type inference otherwise chooses `Nullable<T>` and produces an opaque compile-time failure for consumers.

Nullable list elements cannot safely reuse the reference-element implementation because they require their own absence handling and unwrapping before conversion.

Settling the additive API before the first stable release avoids introducing a later source-compatibility hazard through overload-resolution changes.

Exact overload signatures, converter types, null-element behavior, and examples are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts) and the Request Binder user documentation.

## Alternatives Considered

### Require adapter lambdas at each call site

Considered because it requires no new public API. Rejected because it exposes an unintuitive inference failure to consumers and duplicates binder-owned unwrapping logic throughout application code.

### Use one selector for both reference and value types

Considered because it minimizes the surface. Rejected because C# cannot infer the underlying value type from an unconstrained nullable annotation while preserving reference-type behavior.

### Reuse the reference-list converter

Considered because the conversion flow is otherwise similar. Rejected because nullable value-type elements require distinct missing-element handling and unwrapping.

## Consequences

### Positive

* Nullable value-type properties bind with the same method-group ergonomics as reference properties.
* Absence remains observable while converters receive the meaningful underlying value.
* The public shape is settled before the stable API freeze.

### Negative

* The binder exposes additional selector and converter surface.
* The overload-resolution mechanism is subtle and requires regression tests.

### Risks

* Future selector shapes could reintroduce ambiguity. Mitigation: keep targeted compile-time regression coverage for reference, string, scalar value, and list cases.

## Follow-up Actions

* Keep the bilingual Request Binder documentation aligned with the accepted behavior.

## References

* [ADR implementation reference — Request Binder implementation contracts](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.md)
* Issue #144 and pull requests #126 and #141.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

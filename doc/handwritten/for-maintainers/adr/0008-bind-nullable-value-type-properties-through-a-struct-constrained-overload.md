# ADR-0008 | Bind nullable value-type properties through a struct-constrained overload

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The Request Binder converts nullable DTO properties through value-object factory
method groups. With the original unconstrained selector, a property such as
`int?` inferred the generic argument as `Nullable<int>`, making a factory over the
underlying `int` fail method-group inference with `CS0411`.

A non-nullable value-type property cannot represent absence and is already a
programming error. C# constraints alone do not distinguish overload signatures,
but a struct-constrained selector carrying `Nullable<TArgument>` has a different
and more specific parameter type. Lists add an independent concern because each
nullable element can itself be absent.

The package was pre-release, so the overload shape could be settled before
consumer call sites existed.

## Decision

The Request Binder binds nullable value-type DTO properties and their lists through dedicated `where TArgument : struct` selector paths whose converters receive the underlying non-nullable value.

## Rationale

The dedicated path removes the inference failure at the API boundary and preserves
the same method-group ergonomic as reference properties. The DTO remains nullable
so absence is observable, while a present value is unwrapped before conversion.
Lists require corresponding element handling so a `null` element is recorded at
its indexed path rather than passed to the converter.

The overload and converter mechanics are maintained in the
[Request Binder specification](../specifications/request-binder.en.md), allowing
implementation refactoring without reopening the decision.

## Alternatives Considered

### Require an adapter lambda at every call site

Considered because it adds no API. Rejected because it leaks a generic-inference
limitation to every consumer and makes value-type properties less ergonomic than
reference properties.

### Use one selector for reference and value types

Considered for the smallest surface. Rejected because C# cannot make one
unconstrained `T?` infer the underlying value type while also preserving reference
semantics.

### Reuse the reference-list converter

Considered because most binding behaviour is shared. Rejected because nullable
value elements require explicit absence recording and unwrapping.

## Consequences

### Positive

* Nullable value properties and lists bind through factories over the underlying type.
* Method-group ergonomics match reference-property binding.
* The additive shape was settled before the public v1 surface froze.

### Negative

* The public selector and converter surface is larger.
* The overload-resolution mechanism is subtle and needs regression tests.

### Risks

* Future selector shapes could introduce ambiguity. Mitigation: tests pin reference,
  string, scalar-value and list-value overload resolution.

## Follow-up Actions

* Keep the Request Binder user guide and specification aligned with the public API.

## References

* [Request Binder specification](../specifications/request-binder.en.md).
* [Request Binder guide](../../for-users/RequestBinder.en.md).
* ADR-0007 — terminal naming.
* Issue #144 and pull requests #126 / #141.

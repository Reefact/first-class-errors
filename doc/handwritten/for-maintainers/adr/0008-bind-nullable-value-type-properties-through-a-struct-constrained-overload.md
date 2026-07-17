# ADR-0008 | Bind nullable value-type properties through a struct-constrained overload

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.fr.md)

**Status:** Proposed
**Date:** 2026-07-16
**Decision Makers:** Reefact

## Context

* The request binder selects each DTO property with `SimpleProperty(r => r.X)` or
  `ListOfSimpleProperties(r => r.X)`, then a converter binds it — a value-object
  factory `Func<TArgument, Outcome<TProperty>>`, typically a method group such as
  `EmailAddress.Parse`.
* The original selector is generic over `TArgument`, with an unconstrained
  `Expression<Func<TRequest, TArgument?>>` parameter.
* When the DTO property is a nullable value type (`int?`), the unconstrained selector
  infers `TArgument = Nullable<int>`, because for an unconstrained type parameter the
  `?` is a no-op annotation on value types. The converter stage then expects
  `Func<Nullable<int>, Outcome<T>>`, so a method group over the underlying type
  (`int -> Outcome<T>`) does not match and the call fails to compile with **CS0411**.
* A non-nullable value-type property is already rejected at bind time (review finding
  #4, shipped in #141): such a property must be declared nullable so a missing
  argument is distinguishable from a legitimately-sent default.
* C# does not treat two methods that differ only by a `class` versus `struct`
  constraint as distinct signatures — a constraint is not part of the signature
  (CS0111).
* Under a `where TArgument : struct` constraint, `TArgument?` denotes the constructed
  type `Nullable<TArgument>`. That is a different parameter type from the unconstrained
  selector's bare `TArgument`, and — being a constructed type — is structurally more
  specific for overload resolution.
* A `Nullable<TArgument>` list element can independently be `null`; a converter over
  the non-nullable underlying type cannot represent that element, and the reference
  list converter dereferences elements without unwrapping a `Nullable`.
* The library is pre-release, unpublished on NuGet with no external consumers. Additive
  overloads settled before the v1 freeze cannot shift inference at consumer call sites
  that do not yet exist; the same overloads added after consumers write value-type
  bindings could change resolution at those sites.

## Decision

The request binder binds a nullable value-type DTO property through a dedicated
`where TArgument : struct` selector overload whose selector carries
`Nullable<TArgument>` and whose converter runs over the underlying non-nullable type.

## Rationale

* The struct-constrained overload's `Nullable<TArgument>` parameter is a genuinely
  different — and structurally more specific — type than the unconstrained overload's
  bare `TArgument`, so the two coexist without CS0111 and the value-type overload wins
  for a nullable-value-type property, while reference and string properties keep
  resolving to the unconstrained overload. The CS0411 failure is removed at the
  selector rather than pushed onto the consumer as an adapter lambda.
* Surfacing the underlying non-nullable type (`int`, not `int?`) lets a value-object
  factory bind as a method group exactly as it does for a reference property, keeping
  one fluent ergonomic across both property kinds: the property stays declared nullable
  so absence remains observable, and the underlying type is what the converter
  meaningfully operates on.
* A dedicated list converter is required rather than a reuse, because a `Nullable`
  element needs its own null handling — a `null` element is a missing argument recorded
  under its indexed path — and an unwrap before conversion, neither of which the
  reference converter performs.
* Deciding before the v1 freeze settles the API shape while it is still free: the
  overloads are additive now, with no call sites to disturb, whereas deferring them
  past the freeze would make the same addition a source-breaking change to consumers'
  value-type bindings.

## Alternatives Considered

### Require consumers to pass an adapter lambda

Considered because it needs no new API: `AsRequired(v => PositiveInt.From(v))` binds
where the bare method group does not.

Rejected because it silently degrades the method-group ergonomics for the common
nullable-value-type case, surfaces a CS0411 compile error with no obvious cause, and
duplicates at every call site the unwrap the binder can perform once.

### A single selector unifying reference and value types

Considered because one overload is the smallest surface.

Rejected because C# cannot express it: an unconstrained `TArgument?` cannot both infer
the underlying type for a value-type property and stay a reference for a reference-type
property, and a constraint cannot be varied within one method.

### Reuse the reference list converter for value-type lists

Considered because the binding logic is otherwise identical.

Rejected because a `Nullable<T>` element and a reference element need different null
handling, and the value-type path must unwrap each present element before conversion;
folding both into one converter would obscure that a `null` element is a recorded
missing argument.

## Consequences

### Positive

* A nullable-value-type DTO property (`int?`, `bool?`, ...) and lists of them bind via
  a method group over the underlying type, with the same ergonomics as reference
  properties.
* The fix is additive and settled before the v1 freeze, so it never becomes a
  source-breaking change to a consumer's existing value-type binding.
* Reference and string properties are unaffected: they keep resolving to the original
  overload.

### Negative

* Two more public selector overloads and one more public converter type to document and
  maintain, doubling the selector surface of `SimpleProperty` and
  `ListOfSimpleProperties`.
* The mechanism — a `struct` constraint turning `TArgument?` into a more-specific
  `Nullable<TArgument>` — is subtle; a maintainer unfamiliar with the overload-
  resolution rule may not immediately see why the two selectors coexist.

### Risks

* A future third selector shape could interact with the two overloads in ways that
  reintroduce ambiguity; mitigated by this ADR and by regression tests that pin
  reference and string resolution to the original overload.

### Follow-up Actions

* Keep the RequestBinder guide (EN and French) in sync with the value-type binding
  ergonomics.

## References

* ADR-0007 — name the binder terminals New and Create, the sibling public-API decision
  on the same binder.
* Pull requests #126 and #141 — the request binder feature and the non-nullable
  value-type guard this decision complements.
* Issue #144 — the CS0411 finding this decision resolves.

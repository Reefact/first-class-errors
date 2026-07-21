# ADR-0032 | Draw arbitrary values from an explicit, top-level choice pool

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0032-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md)

**Status:** Proposed
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

`Dummies` supplies arbitrary yet valid values from a single seedable source, so any run is reproducible from a
reported seed (ADR-0006). A recurring need is a value whose domain is a **closed set the caller already holds** — one
of the currencies a context is configured with, one of the orders already in a fixture, one of a handful of domain
states the test does not assert on. The library generates structural shapes (a length, an interval, a pattern); it
cannot synthesize such a real-world set, and the caller owns the values.

Several existing facts frame the choice:

* The most common way to pick from a caller-held set today is hand-rolled — `pool[new Random().Next(pool.Count)]` —
  which draws from a fresh `Random`, ignores the ambient seeded source, and therefore cannot replay under
  `Any.Reproducibly(...)`: exactly the trap the library exists to remove.
* The scalar and enum builders expose `OneOf(params T[])`, but only **within their own domain** — it narrows a
  scalar's interval or pool and cross-validates against the other constraints. There is no top-level combinator to
  draw from a pool of arbitrary domain objects.
* ADR-0030 added `Any.String().OneOf(...)` as a **terminal** value-set generator chained off the string entry point:
  it implements `ICardinalityHint<string>`, deduplicates under an ordinal comparison, draws uniformly and
  reproducibly, and **rejects a `null` element**, directing the caller to `OrNull()`. It is string-specific; a
  type-agnostic pool of domain objects has no typed builder to chain off.
* Distinct collections gate on an element generator's advertised cardinality at declaration time (ADR-0013), through
  the internal `ICardinalityHint<T>`; a generator that advertises a cardinality also answers membership.
* `OrNull()` is the library's orthogonal decorator for nullability, for both value and reference types.
* With a `params T[]`-only method, passing a single held collection binds the type parameter to the **collection
  type**, not its elements; and when the element type is itself enumerable, an overload that also accepts
  `IEnumerable<T>` makes `OneOf(collection)` ambiguous between "a pool holding the collection" and "a pool of its
  elements".
* A factory that takes raw values rather than an `IAny<>` operand does not inherit a random context from an operand,
  so — unlike `Combine`/`ListOf`/`SetOf` — it must exist on both `Any` (ambient) and `AnyContext` (seeded); the
  surface-parity guard treats such a factory as a scalar factory and requires the mirror.
* The 2026-07-20 Dummies architecture & design audit (§10) ranks this the highest-leverage Must-Have: every consumer,
  most weeks.

## Decision

`Any.OneOf<T>(params T[])` and `Any.ElementOf<T>(IReadOnlyList<T>)`/`Any.ElementOf<T>(IEnumerable<T>)` — mirrored on
`AnyContext` — draw one value uniformly from an explicit, caller-supplied pool as a terminal generator, rejecting an
empty pool and any `null` element, and deduplicating, sizing and testing membership of the pool under
`EqualityComparer<T>.Default`.

## Rationale

* **It closes the reproducibility trap that is the library's reason to exist.** A seed-aware pool draw replaces the
  hand-rolled `Random`, so the choice replays under `Any.Reproducibly(...)` and `Any.WithSeed(...)` like every other
  draw (ADR-0006). The audit names this the single highest-leverage addition.
* **Rejecting `null` keeps nullability orthogonal and the surface symmetric.** `OrNull()` is the one way to express an
  optional value, so a `null` pool member would reintroduce the "is `null` a value or an absence" ambiguity that
  decorator exists to remove. It also matches the shipped string generator (ADR-0030): the two value-set combinators
  stay symmetric on their `null` contract instead of diverging — the kind of asymmetry the audit warns against. A
  caller who wants an occasional `null` still writes `OneOf(...).OrNull()`.
* **`EqualityComparer<T>.Default` is the type-agnostic analogue of the string generator's ordinal dedup, and it is the
  sound choice for the cardinality contract.** A downstream distinct collection carrying a coarser custom comparer can
  only *merge* pool values, never create new ones, so the advertised distinct count stays a conservative upper bound
  and membership never claims a value the pool lacks — the collection keeps gating correctly (ADR-0013).
* **A terminal generator that advertises its cardinality composes for free.** The pool is the whole specification —
  there is no scalar domain to narrow — so the generator exposes no further constraints, yet it flows through
  `As(...)`, `OrNull()`, `Combine(...)` and the collection generators as any `IAny<T>` does, and a distinct collection
  over it gates eagerly like the other countable-domain generators.
* **Two names beat one overloaded name.** `OneOf` takes inline literals; `ElementOf` takes a held collection. The
  split removes the generic-inference footgun: inline values never bind the type parameter to a container, and a held
  collection is never confused with its own elements. `ElementOf`'s sequence overload materializes once so a lazy
  query is not re-enumerated per draw.
* **The `AnyContext` mirror is required, not optional.** The pool carries no operand from which to inherit a seeded
  context, so without a mirror the seeded surface would have a silent hole; the parity guard makes the omission a
  failing test.

## Alternatives Considered

### Allow `null` as a pool member

Considered because a `null` domain object is arguably a valid arbitrary choice, and a `null` member (weight `1/n`) is
distributionally different from `OrNull()`'s independent coin flip — the direction the filed issue first proposed.

Rejected because it would contradict the shipped `Any.String().OneOf(...)` (ADR-0030), reintroduce the
value-versus-absence ambiguity `OrNull()` removes, and split the two value-set combinators on their `null` contract —
exactly the surface asymmetry the audit flags. The occasional-`null` case is still served by `OneOf(...).OrNull()`.

### A single overloaded `OneOf(params T[])` plus `OneOf(IEnumerable<T>)`, no `ElementOf`

Considered for surface economy, mirroring the string builder's two overloads.

Rejected because generic inference turns it into a footgun: passing a held collection to the `params` form pools the
container itself, and when the element type is enumerable the two overloads make `OneOf(collection)` ambiguous between
the container and its elements. A distinct `ElementOf` name makes the intent unambiguous at the call site.

### Add an `IEqualityComparer<T>` overload, as `SetOf` has

Considered because a caller might want pool identity decided by a custom comparer.

Rejected as unneeded for v1: the default comparer already yields a sound cardinality bound under any downstream
comparer, and a pool-specific comparer can be added later on evidence of need without changing the default contract.

### Chain it off a typed entry point, like the string `OneOf`

Considered for consistency with `Any.String().OneOf(...)`.

Rejected because an arbitrary domain object has no `Any.X()` builder to chain from — the whole point is a
type-agnostic, top-level factory — so a static factory on `Any`/`AnyContext` is the only shape that fits.

## Consequences

### Positive

* The audit's first-ranked gap is closed: picking from a caller-held set becomes a one-line, seed-reproducible dummy
  that composes into value objects (`As`), optionals (`OrNull`) and collections like every other generator.
* The string and generic value-set combinators now share one `null` contract (rejected, via `OrNull()`), removing an
  asymmetry rather than adding one.
* A distinct collection over the pool is gated eagerly by its cardinality, consistent with the other
  countable-domain generators.

### Negative

* A new public type (`AnyOneOf<T>`) and two entry-point names (`OneOf`/`ElementOf`) to maintain, document, and keep
  mirrored on `AnyContext`.
* The library does not check that the pooled values meet any external format: they are the caller's content, and a
  value object still needs `As(...)` to enforce its invariant.

### Risks

* A caller may expect `null` to be a legal pool member and be surprised it is refused. Mitigated by the exception
  message pointing at `OrNull()` — the same guidance the string generator gives.
* A caller may pass a held collection to `OneOf` and get a pool of one element. Mitigated by `ElementOf` being the
  documented path for a held collection, and by `OneOf`'s summary directing there.

## Follow-up Actions

* Document `OneOf`/`ElementOf` in the Dummies user guide (`ArbitraryTestValues.en.md`) and its French translation, and
  in the package README (`README.nuget.md`), with an example.
* Keep the `Any`↔`AnyContext` mirror green (enforced by `SurfaceParityTests`).
* Consider aligning the string generator's and the generic generator's `null`-element messages when the string
  surface is next revised.

## References

* ADR-0030 — Draw arbitrary strings from an explicit, terminal value set (the string sibling; the terminal-generator
  and `null`-rejection precedent).
* ADR-0013 — Gate distinct collections by cardinality, otherwise by a bounded draw (the `ICardinalityHint` contract).
* ADR-0006 — Supply arbitrary test values from a single seedable source (reproducibility).
* ADR-0031 — Name Any's scalar factories after their CLR type (why `OneOf`/`ElementOf`, as combinators, are exempt).
* ADR-0020 — Materialize dummies only through `Generate()`.
* Issue [#223](https://github.com/Reefact/first-class-errors/issues/223) and the 2026-07-20 Dummies architecture &
  design audit (§10 Must-Have).
* The `AnyOneOf<T>` type, the `Any.OneOf`/`Any.ElementOf` and `AnyContext.OneOf`/`AnyContext.ElementOf` factories, and
  their tests in the `Dummies` project and `Dummies.UnitTests`.

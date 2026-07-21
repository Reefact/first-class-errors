# ADR-0030 | Draw arbitrary strings from an explicit, terminal value set

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.fr.md)

**Status:** Accepted
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

`Dummies` supplies arbitrary yet valid values, with constraints that express what the surrounding code requires of
a value. A recurring need is a value whose domain is a **fixed, closed list** the test does not assert on — a
currency code drawn from a short table, a status label, a well-known company name. The library generates
**structural** shapes (a length, a character family, a regular pattern); it cannot synthesize such a real-world set,
and the caller holds the values.

Several existing facts frame the choice:

* The scalar and enum generators already expose `OneOf(params T[])` — draw uniformly from an explicit allow-list —
  but there it is a **composable** constraint: it narrows *within* the type's interval or pool and cross-validates
  against the other constraints. `AnyString` has no `OneOf` at all.
* `Any.StringMatching` (ADR-0025) is a **terminal** generator: the pattern is the whole specification, so it exposes
  no further shape or length constraints, yet it still composes through `As`, `OrNull`, `Combine` and the collection
  generators as any `IAny<string>` does.
* A string's shaping surface is far wider than a scalar's interval: prefix, suffix, contained fragments, character
  family, letter casing and length, each already cross-validated against the others.
* Distinct collections gate on an element generator's advertised cardinality at declaration time (ADR-0013), through
  the internal `ICardinalityHint<T>`; a generator that does not advertise one falls back to a bounded dedup draw.
* The library draws from a single seedable source so any run is reproducible (ADR-0006), builds values to satisfy
  the constraints rather than generate-and-filter, and ships with **zero runtime dependencies and no datasets** —
  its README lists "no realistic fake data (names, emails, addresses)" as an explicit non-goal (ADR-0011).
* The requested call shape is `Any.String().OneOf(...)` — chained off the string entry point.

## Decision

`Any.String().OneOf(...)` draws the string from an explicit set of caller-supplied values as a **terminal**
generator — the set is the whole specification and does not combine with the other string constraints — rather than
as a composable constraint like the scalar generators' `OneOf`.

## Rationale

* **A terminal set keeps the surface small and contradiction-free.** Reconciling an explicit value set with a
  string's prefix, suffix, fragments, character family, casing and length would multiply contradictory combinations
  and their conflict messages, for a combination nobody needs — a caller who supplies literal values already fixes
  their shape. Making the set the whole specification removes that whole class at once. `Any.StringMatching` reached
  the same conclusion for the same reason (ADR-0025); matching that precedent keeps the two string terminals
  coherent.
* **It stays on `Any.String()` for discoverability, and stays honest through fail-fast.** A caller reaches for
  `Any.String()` and finds `OneOf` beside the other ways to obtain a string. The terminal nature is enforced two
  ways: the returned generator carries no shaping methods, and declaring `OneOf` after another constraint raises a
  `ConflictingAnyConstraintException` at declaration time — the same "an impossible Arrange is a test defect" rule
  the library applies to every other conflict.
* **Caller-supplied values preserve the library's identity.** The realistic content lives in the consumer's test,
  not in the package, so the "no realistic fake data" non-goal holds and no dataset, dependency, or network call is
  introduced. `OneOf` is the dependency-free, deterministic answer to "give me a plausible value from a known set".
* **Advertising cardinality keeps distinct collections eager.** An explicit set is a small countable domain, so the
  generator implements `ICardinalityHint<string>`; a distinct collection over it gates eagerly (ADR-0013), exactly
  as it does over `AnyChar` or `AnyEnum`, instead of silently relying on the bounded dedup-draw fallback.
* **Reproducibility is preserved.** The value is a uniform pick from the deduplicated set through the same seedable
  source as every other generator, so a run replays under a seed (ADR-0006); collapsing duplicates keeps a listed
  value from being implicitly weighted.

## Alternatives Considered

### A composable `OneOf` on `AnyString`, like the scalar generators

Considered for surface symmetry with `AnyInt32.OneOf` and its peers. Rejected because a string's shaping constraints
intersect an explicit value set in many ways, each needing its own eager conflict analysis and message, for a
combination a caller supplying literals never needs — the terminal form removes the whole class, consistent with
ADR-0025.

### A static factory `Any.StringOneOf(...)` (or `Any.OneOf(...)`), parallel to `Any.StringMatching`

Considered because a static factory is terminal from the first call and sidesteps any "a constraint is already
declared" case. Rejected because the requested and more discoverable surface is `Any.String().OneOf(...)`, which
keeps the string entry points together; the prior-constraint case is covered by a clear declaration-time conflict,
the mechanism the library already uses for every impossible combination.

### Ship curated realistic datasets (`Any.CompanyName()`, `Any.FirstName()`, ...)

Considered because it answers "give me a plausible value" directly. Rejected because it contradicts the stated
non-goal of shipping no realistic fake data, and would make the library own, grow and localize an open-ended dataset;
the consumer supplies the set and `OneOf` draws from it instead.

### Generate the set on first run through an external service and cache it

Considered as a way to author the set without hand-writing it. Rejected because it would add a runtime dependency and
a non-deterministic, non-hermetic first run to a library whose identity is zero-dependency, deterministic generation
(ADR-0006, ADR-0011); authoring the set is a design-time concern that belongs outside the library.

## Consequences

### Positive

* A value whose domain is a short, closed list becomes a one-line, dependency-free, reproducible dummy that composes
  into value objects (`As`), optionals (`OrNull`) and collections like every other generator.
* The terminal shape keeps the string surface small and free of a new class of contradictory constraint
  combinations.
* A distinct collection over the set is gated eagerly by its cardinality, consistent with the other countable-domain
  generators.

### Negative

* A new public type (`AnyStringOneOf`) and method to maintain and document, and a second `OneOf` shape in the library
  — terminal for strings, composable for scalars — that the documentation must explain.
* The library does not check that the supplied values meet any external format: they are the caller's content, and a
  value object still needs `As(...)` to enforce its invariant.

### Risks

* A caller may expect the scalar `OneOf`'s composability and be surprised the string one is terminal. Mitigated by
  the returned type carrying no shaping methods and by the declaration-time conflict when a constraint precedes it —
  both make the terminal nature explicit at the call site.

## Follow-up Actions

* Document the generator in the `Dummies` package README (done) and in the user documentation when the string surface
  is next revised.
* Keep the "no realistic fake data" non-goal in the README accurate: `OneOf` draws from caller-supplied values and
  ships no dataset.

## References

* ADR-0025 — Generate matching strings from a home-grown regular subset (the terminal-generator precedent).
* ADR-0013 — Gate distinct collections by cardinality, otherwise by a bounded draw (the `ICardinalityHint` contract).
* ADR-0006 — Supply arbitrary test values from a single seedable source (reproducibility).
* ADR-0011 — Host Dummies as a standalone package in this repository (the zero-dependency, no-dataset boundary).
* The `AnyStringOneOf` type, the `AnyString.OneOf` method, and their tests in the `Dummies` project and
  `Dummies.UnitTests`.

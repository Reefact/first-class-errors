# ADR-0013 | Gate distinct collections by cardinality, otherwise by a bounded draw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)

**Status:** Proposed
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

`Dummies` carries a core contract: a constraint expresses what a value must
satisfy, contradictory constraints fail at the moment they are declared with a
`ConflictingAnyConstraintException` naming both sides, and a value is built to
satisfy its constraints in a single pass — never generated then filtered, and
never behind a retry loop.

The collection increment adds distinct collections: `SetOf`, `ListOf(...).Distinct()`
and the like, and a dictionary's keys. A distinct collection of *N* elements is
satisfiable only if its element generator can yield at least *N* distinct values,
which is a property of that generator's domain.

Element generators fall into two groups. Some draw from a small, countable
domain: a boolean has two values, an enum has its declared members, a narrow
integer range or a restricted character pool has a fixed size. Others draw from a
domain that is effectively unbounded or simply unknowable to the collection:
unconstrained integers, strings and identifiers, and — decisively — any foreign
`IAny<T>` implementation or any derived generator (`As`, `Combine`), which carries
no domain information at all. `IAny<T>` is a public interface, so the library
cannot assume every generator can report its cardinality.

A custom equality comparer can only merge distinct values into fewer equivalence
classes; it can never manufacture new ones.

## Decision

A distinct collection rejects, at declaration time, any element count that
exceeds the element generator's advertised cardinality, and otherwise builds its
elements by a bounded deduplicating draw that fails at generation, with a
replayable seed, if the element domain proves too small.

## Rationale

* **Fail eagerly wherever the domain is knowable.** The declaration-time conflict
  is the library's signature: a count that exceeds a countable element domain is
  a contradiction in the test's `Arrange`, and it must read as one, named on both
  sides, exactly like every scalar conflict — not surface later as a puzzling
  runtime failure.
* **A bounded draw is the only honest option where it is not.** Because an
  arbitrary generator's cardinality is generally unknowable, the only universal
  way to obtain *N* distinct values is to draw and deduplicate. Keeping that draw
  bounded honours the no-retry-loop principle; on exhaustion it reports the real
  shortfall as an `AnyGenerationException` naming the seed — the same failure
  channel a factory rejection already uses — rather than looping.
* **The advertised bound stays sound under a comparer.** Since a comparer only
  merges values, the advertised cardinality remains a valid *upper* bound, so the
  eager check never rejects a request that was actually satisfiable; a comparer
  that collapses the domain below the requested count is caught by the bounded
  draw instead.
* **One principle, applied where its information exists.** Splitting the failure
  between declaration time (when the domain is countable) and generation time
  (when it is not) is not a dilution of the eager-conflict principle but its
  faithful extension to the only place where the information needed to be eager is
  absent.

## Alternatives Considered

### Always fail at generation, dropping the eager cardinality check

Considered because a single failure channel is simpler to explain and to
implement. Rejected because it discards the library's signature diagnostic
precisely where it is cheap and certain — a set of three booleans, an enum asked
for more members than it declares — turning an obvious `Arrange` contradiction
into a runtime surprise.

### Make cardinality part of `IAny<T>`, so every request is decided eagerly

Considered because a mandatory cardinality on every generator would let every
distinct request fail or pass at declaration time. Rejected because `IAny<T>` is
a public contract with foreign implementations and with derived generators
(`As`, `Combine`) that cannot honestly report a bound; the guarantee would be
unenforceable and frequently wrong, and it would burden every implementer with a
value most of them cannot supply.

### Draw without a bound until *N* distinct values appear

Considered because an unbounded draw always terminates when the request is
satisfiable. Rejected because it never terminates when the request is *not*
satisfiable, which is exactly the case this decision must diagnose: it would turn
an impossible request into a hang instead of an error, breaking the library's
bounded-work principle.

## Consequences

### Positive

* The signature declaration-time diagnostic now reaches distinct collections
  wherever the element domain is countable.
* Requests over unknown or comparer-reduced domains still fail safely and
  reproducibly, with a seed to replay, never as a hang.
* The cardinality capability is internal and opt-in, so the public `IAny<T>`
  contract is unchanged and foreign generators keep working unmodified.

### Negative

* Failure timing is not uniform: the same logical contradiction surfaces at
  declaration for a known-small domain and at generation for an unknowable one,
  which a user must understand.
* The bounded draw runs to a chosen budget; a request pushed pathologically close
  to an unknown domain's true size could in principle fail although it was
  satisfiable — astronomically unlikely for the dummy-sized collections the
  library targets.

### Risks

* **Overstated hint** — a generator could advertise a cardinality larger than the
  distinct values it truly yields, so the eager check misses a real conflict.
  Mitigated because the hint is defined as an upper bound and the bounded draw
  catches any residual shortfall at generation.
* **Budget mis-tuning** — too small a draw budget would yield spurious generation
  failures. Mitigated by scaling the budget to a known cardinality and keeping a
  generous floor for unknown domains.

## Follow-up Actions

* Document the two failure channels in the user documentation once the collection
  surface stabilizes.
* Revisit the draw budget if real usage ever surfaces a spurious exhaustion.

## References

* ADR-0011 — Host Dummies as a standalone package in this repository.
* The distinct-collection engine and the cardinality capability, in the `Dummies`
  project (`CollectionState`, `ICardinalityHint`).

# ADR-0036 | Draw lattice-constrained scalars on the grid

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0036-draw-lattice-constrained-scalars-on-the-grid.fr.md)

**Status:** Accepted
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

Dummies builds a scalar directly to satisfy its constraints — never generated-then-filtered — detects contradictions eagerly at declaration, and avoids hidden unbounded retry loops. A scalar generator that exists can always generate, in one draw. The ordinal-mapped types (the integers, the temporals) draw the k-th non-excluded value of the domain in one pass over an order-preserving, affine ordinal space; `decimal` draws a candidate and nudges it within a bounded budget.

A recurring dummy need is a value that must lie on a regular grid: a multiple of a unit (an amount in whole cents, a quantity in dozens), a `decimal` expressible in a fixed number of places (a currency amount), or a round instant (a whole second, a quarter-hour, a whole day). These are invariants of the code under test — a value object or contract precondition the value must satisfy — not what the test asserts.

Today such a value can only be reached by projecting a constrained one after the fact, `As(x => x * k)`. The projection distorts the declared range (a range stated in the pre-projection unit no longer means what it says) and drops the value out of the constraint algebra: the projected generator can no longer exclude values, cannot conflict-check, and carries no cardinality hint for distinct collections. Tick-precision temporal dummies additionally surprise tests that serialize through a second- or day-granular format, where the round-trip silently drops precision.

Because the ordinal map is affine, the multiples of a step form an arithmetic progression in ordinal space, so a grid is expressible as a first-class dimension of the interval engines without leaving the constructive model. Binary floating-point types have no exact base-ten (or general rational) grid — `0.1` is not representable — so the same construction cannot hold there. Issue #226 records `MultipleOf`/`WithScale` as a demand-driven addition; the temporal granularity need was noted alongside it.

## Decision

A lattice constraint — `MultipleOf` on the integers, `WithScale` on `decimal`, `WithGranularity` on the temporals — restricts a scalar to a regular grid drawn constructively in one pass, composes with the existing bounds, exclusions and allow-list, is declared once per generator, and is deliberately withheld from the binary floating-point types.

## Rationale

Drawing on the grid keeps the library's single-draw, no-retry invariant: the affine ordinal map makes the multiples of a step an arithmetic progression, so the grid becomes another dimension the interval engine samples directly rather than a post-filter that would reintroduce rejection. Keeping the value first-class — rather than an `As` projection — is the whole point: the declared range keeps its meaning, and exclusions, allow-lists, eager conflict detection and the cardinality hint continue to apply, so a distinct collection over a narrow grid still fails eagerly.

`WithScale` is a *value* lattice — a multiple of `10⁻ⁿ` — not a representation contract that would pad trailing zeros, because the invariant callers actually need is "a value the domain accepts" (a money factory that rejects a third decimal place), which is a fact about value, not rendering. A representation guarantee would not compose with value equality and would surprise anyone comparing `12.30` with `12.3`.

The lattice is withheld from the binary floats because a base-ten grid is not exactly representable there; offering it would hand back off-grid values under a promise the type cannot keep. It is declared once — a second, different grid conflicts rather than silently intersecting — mirroring the "declared once" rule the allow-list already uses and sparing a least-common-multiple combination the demand does not justify. Surfacing one engine capability as `MultipleOf` on integers and `WithGranularity` on temporals is what lets a single dimension serve both families, so a fix to the grid logic reaches every type at once.

The step arithmetic, the decimal snap-and-nudge, and the conflict-message wording are implementation, documented in the `Dummies` code (`OrdinalIntervalSpec`, `WideIntervalSpec`, `DecimalIntervalSpec`) and the Dummies user documentation — not here.

## Alternatives Considered

### Keep the `As(x => x * k)` projection as the only way

Considered because it needs no new API and already works. Rejected because it distorts the declared range, drops the value out of the constraint algebra (no exclusion, no conflict check, no cardinality hint), and — for temporal precision — does not address the serialization surprise at all.

### Generate then filter off-grid draws

Considered because it is the obvious way to honour an arbitrary grid. Rejected because it reintroduces an unbounded retry loop, contradicting the constructive, no-hidden-loops model the library is built on.

### Extend the lattice to the binary floating-point types

Considered for surface symmetry with the integers and `decimal`. Rejected because a base-ten (or general rational) grid is not exactly representable in binary floating point, so the constraint would return off-grid values — a false promise worse than a deliberate, documented gap.

### Make `WithScale` a representation contract

Considered because the name evokes `decimal.Scale` and a database `DECIMAL(p, s)` column. Rejected because the invariant callers need is value-level, a representation guarantee does not compose with value equality, and it would surprise on `12.30 == 12.3`.

### Combine repeated lattices by least common multiple

Considered because "multiple of 4 and of 6" is mathematically "multiple of 12", not a contradiction. Rejected as disproportionate: it opens an overflow-prone corner for a combination the demand does not show, whereas "declared once" is simple, safe, and consistent with the allow-list.

## Consequences

### Positive

* The "value on a grid" invariant is expressible constructively, so the declared range stays honest and the value keeps full composition — bounds, exclusions, allow-list, eager conflict, and the cardinality hint that lets a distinct collection over a narrow grid fail eagerly.
* One engine capability serves the integers and the temporals (and `decimal` through its own engine), so a fix to the grid logic reaches every type at once.
* The `As(x => x * k)` workaround and the tick-precision serialization surprise both go away for the covered types.

### Negative

* A new commutative dimension now lives in three interval engines (ordinal, wide, decimal) and must be maintained in step across them.
* The surface is deliberately asymmetric: the binary floats carry the sign and bound vocabulary but no lattice — a gap users must learn rather than infer.

### Risks

* `WithScale`'s value-versus-representation distinction may surprise users expecting a padded scale. Mitigation: state it as a value lattice in the builder documentation and the readme.
* The decimal grid draws-and-snaps rather than enumerating, so the mass at the two extreme grid points is approximate. Mitigation: reachability of both bounds is preserved and tested, consistent with the existing decimal draw.

## Follow-up Actions

* Document `MultipleOf`/`WithScale`/`WithGranularity` in the Dummies readme and the builder documentation (done in the implementing pull request).
* Ship the `WholeSeconds()`/`WholeDays()` temporal sugar only if demand appears; the general `WithGranularity(TimeSpan)` covers it in the meantime.
* Revisit least-common-multiple combination of repeated lattices only if real usage shows the "declared once" rule is too strict.

## References

* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — the demand-driven backlog that lists `MultipleOf`/`WithScale` and temporal granularity.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the cardinality hint a lattice feeds, and the bounded-draw sibling.
* [ADR-0020](0020-materialize-dummies-only-through-generate.md) — dummies materialize only through `Generate()`.
* `OrdinalIntervalSpec`, `WideIntervalSpec`, `DecimalIntervalSpec` and the affected builders in the `Dummies` project; the Dummies NuGet readme.

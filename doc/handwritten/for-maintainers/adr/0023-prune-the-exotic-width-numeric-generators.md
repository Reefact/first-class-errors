# ADR-0023 | Prune the exotic-width numeric generators (128-bit and Half)

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0023-prune-the-exotic-width-numeric-generators.fr.md)

**Status:** Proposed
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

`Dummies` ships a wide numeric matrix so that a test can ask for an arbitrary
value of almost any built-in number type: `Byte`/`SByte`, the 16/32/64-bit
integers, `Int128`/`UInt128`, `Decimal`, and `Double`/`Single`/`Half`. The goal
is producing dummies *easily* — cover every simple case and a majority of the
plausible complex ones — without becoming a constraint solver.

Two of these types carry a cost out of proportion to their realistic use:

* **`Int128`/`UInt128`** are backed by their own spec engine,
  `WideIntervalSpec` — a ~185-line `UInt128` clone of `OrdinalIntervalSpec`,
  the library's *fourth* interval engine, existing only to serve these two
  generators. A constrained 128-bit dummy (a bounded range, an allow-list, an
  exclusion) is a rounding-error fraction of real usage; the type is net8-only,
  so it is not even a beginner's reach.
* **`Half`** rides the shared `ContinuousIntervalSpec`, so it costs no engine —
  only ~207 lines of generator surface — but a `Half` dummy is a rare
  ML/GPU-interop need.

The cost became concrete while extending the eager cardinality perimeter so
that *every* finite-domain generator gates distinct collections uniformly (the
"hold the promise everywhere" work behind [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md)).
Bringing these types into the perimeter revealed that `WideIntervalSpec` had
already **silently drifted** out of the earlier cardinality capability — it
advertised neither a cardinality nor membership — so the clone was a live
maintenance liability, not merely dormant lines. Every interval engine we keep
is a place the next cross-cutting invariant must be threaded by hand.

Removing public generator types is a **breaking change** and touches the
supported-type floor, so it is a deliberate product decision, not a code
cleanup — hence this ADR.

## Decision

Remove `AnyInt128`, `AnyUInt128`, and the `WideIntervalSpec` engine that exists
only for them; and remove `AnyHalf`. Keep the full integer matrix up to 64 bits
and the `Decimal`/`Double`/`Single` floating-point generators — the breadth
people actually reach for. A test needing a constrained 128-bit or `Half` dummy
uses a literal, or a cast from the next-widest generator
(`(Half)Any.Single().Between(...)`, an `Int64` cast).

## Rationale

* **Scope, not completeness.** A dummies library earns its keep by making the
  common cases trivial and the majority of complex ones easy — not by covering
  every BCL numeric type to the same depth. `Int128`/`UInt128`/`Half` are the
  tail where constrained-dummy demand rounds to zero.
* **One fewer engine to keep honest.** Deleting `WideIntervalSpec` removes an
  entire interval engine — a real concept, not just lines — and with it one of
  the four places every future cross-cutting invariant (cardinality, membership,
  a future capability) would have to be re-implemented and kept in sync. The
  already-observed drift is the evidence.
* **The escape hatch is cheap.** The regression is bounded and local: a cast or
  a literal at the call site covers every case the removed generators served, so
  no realistic test loses a capability it cannot trivially recover.

## Alternatives Considered

### Keep everything as-is

The status quo. Rejected because it preserves a whole engine and ~560 lines for
near-null realistic benefit, and — as the silent `WideIntervalSpec` drift
showed — that surface actively rots, taxing every future invariant.

### Cut only the 128-bit generators, keep `Half`

Tenable: `Half` rides the shared continuous engine, so it costs no *concept*,
only lines, and our concept-weighted yardstick treats legible breadth on a
shared engine as nearly free (the same reasoning that keeps `Byte`…`UInt64`).
This is the natural fallback if `Half` turns out to have real users; the 128-bit
cut (which removes the engine) is the load-bearing part.

### Unify the interval engines instead of pruning

Fold `WideIntervalSpec` back into a generic-math interval engine shared with
`OrdinalIntervalSpec`. Rejected: generic-math (`INumber<T>`) is net8-only, so on
the .NET Standard 2.0 floor there is no shared abstraction to unify *into* —
the honest options are "keep the clone" or "drop the types", not "merge".

## Consequences

### Positive

* One of the four interval engines disappears; the cardinality/membership
  invariant (and any future one) has fewer generators to reach.
* The library's surface tracks realistic dummy usage more closely.

### Negative

* A breaking change for any caller using `Any.Int128()`/`Any.UInt128()`/`Any.Half()`
  — mitigated by a cast or literal at the call site, and called out in the
  release notes.
* Loss of uniformity in the numeric matrix: the type list no longer mirrors the
  BCL's numeric types one-for-one.

### Risks

* **Underestimated demand** — if constrained 128-bit or `Half` dummies turn out
  to be used more than expected, the cut is reversible (the generators are
  mechanical), but at the cost of the churn this ADR is trying to avoid.
  Mitigated by shipping the removal in a clearly-noted major version.

## Follow-up Actions

* On acceptance, remove the generators, `WideIntervalSpec`, their `Any.*`
  factory entries, and their tests; note the breaking change in the release
  notes.
* If only the 128-bit cut is accepted (Half kept), leave `AnyHalf` on the shared
  engine untouched.

## References

* [ADR-0011](0011-host-dummies-as-a-standalone-package.md) — Host Dummies as a standalone package.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the eager cardinality perimeter these types were brought into.
* The generators and engine proposed for removal, in the `Dummies` project
  (`AnyInt128`, `AnyUInt128`, `WideIntervalSpec`, `AnyHalf`).

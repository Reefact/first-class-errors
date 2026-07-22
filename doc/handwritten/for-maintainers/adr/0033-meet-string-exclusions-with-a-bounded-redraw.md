# ADR-0033 | Meet string exclusions with a bounded redraw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0033-meet-string-exclusions-with-a-bounded-redraw.fr.md)

**Status:** Accepted
**Date:** 2026-07-22
**Decision Makers:** Reefact

## Context

Dummies builds a scalar directly to satisfy its constraints — never generated-then-filtered — and detects contradictions eagerly at declaration, so a scalar generator that exists can always generate. It also avoids hidden unbounded retry loops.

Every scalar builder but one exposes an exclusion trio (`OneOf`/`Except`/`DifferentFrom`). For the ordinal-mapped types (integers, temporal types, `char`, `Guid`) an exclusion is built into construction: the draw is mapped onto the k-th non-excluded value of the domain in one pass, and whether the exclusions leave the domain non-empty is counted cheaply at declaration.

Strings have no ordinal mapping. An `AnyString` is assembled by layout — prefix, filler, contained values, suffix — over an effectively unbounded domain. An excluded value cannot be projected out of that domain by construction, and whether a set of exclusions leaves a shape satisfiable is not cheaply decidable in general: it is trivial for a fixed one-character length, but grows combinatorially with length and character set.

`AnyString` was the only scalar builder with no exclusion constraints, even though "a value different from the one I already hold" — testing an inequality path with a string identifier while keeping its format — is a common dummy-string need (issue #224). Hand-rolling it with a retry loop typically forgets the seeded source and breaks reproducibility, the exact trap the library exists to prevent.

The library already accepts one place where a value that a caller declared may still fail to materialize: a distinct collection over an uncountable domain draws-and-deduplicates under a bounded budget and fails at generation, reproducibly, when it cannot (ADR-0013). `AnyString.OneOf` is a separate, terminal generator that does not combine with other constraints (ADR-0030).

## Decision

`AnyString.DifferentFrom`/`Except` are satisfied by a bounded redraw of the constructive layout, and an exclusion that leaves the shape unsatisfiable fails at generation with a reproducible, seed-bearing error rather than eagerly at declaration.

## Rationale

Because a string carries no ordinal mapping, an exclusion cannot be built into the layout the way it is for ordinal types, so a redraw is the only general strategy — the same escape a distinct collection already uses when it cannot count its domain. Bounding it preserves termination and turns an unsatisfiable exclusion into a diagnosable, reproducible failure rather than a hang; carrying the seed keeps that failure within the library's reproducibility contract.

The failure is deferred rather than eager because string satisfiability under exclusion is not cheaply decidable in general. A complete declaration-time check is therefore infeasible, and a partial one would diagnose some unsatisfiable specs at declaration and others only at generation — an inconsistent seam worse than a single, predictable rule. Deferring uniformly is the honest choice, and it confines the departure to exclusions alone: every other string constraint stays constructive and eagerly validated.

Accepting that departure is warranted because the alternatives are worse: leaving the gap keeps the most-used builder the only scalar that cannot exclude and pushes users back to seed-breaking retry loops, while forcing an eager verdict demands a decision procedure the domain does not cheaply admit. The cost — one narrowly-scoped, documented case where a string generator that exists may still fail — is the trade already accepted for distinct collections, and expected collisions are ≈ 0 for any non-trivial shape, so the constructive fast path is preserved in practice.

The redraw budget, the exception payload, and the seed propagation are implementation, documented in the `Dummies` code (`StringSpec`) and the Dummies user documentation — not here.

## Alternatives Considered

### Leave `AnyString` without exclusion constraints

Considered because it preserved the pure constructive rule for scalars and needed no new failure channel. Rejected because it left the most-used builder the only scalar that cannot express exclusion, forcing hand-rolled retry loops that silently break seeding.

### Decide satisfiability eagerly, as the ordinal builders do

Considered because declaration-time diagnosis is the library's norm for contradictory constraints. Rejected because string satisfiability under exclusion is not cheaply decidable in general; a partial eager check would be an inconsistent seam, diagnosing some specs early and others late.

### Redraw without a bound

Considered because a satisfiable exclusion would eventually succeed. Rejected because an unsatisfiable one would loop forever, violating the no-hidden-unbounded-loops principle.

### Spec-aware layout avoidance

Considered because constructing the string to dodge the excluded set would keep exclusion constructive and eager. Rejected as disproportionate: correctly avoiding an arbitrary excluded set across the layout's free positions is complex for a path the redraw takes virtually never; it can be revisited if evidence warrants.

## Consequences

### Positive

* The exclusion pair is now uniform across every scalar builder; the common "different identifier, same shape" need is served, seeded and reproducible.
* The constructive fast path is unchanged for every spec without exclusions, and in practice for exclusions too (collisions ≈ 0).
* An unsatisfiable exclusion fails safely, reproducibly, and names the seed to replay — consistent with ADR-0013.

### Negative

* "An `AnyString` that exists can always generate" no longer holds unconditionally: an over-tight exclusion is the one case deferred to generation.
* Failure timing for an unsatisfiable string exclusion differs from the eager, declaration-time diagnosis the ordinal builders give.

### Risks

* A poorly tuned budget could fail a theoretically satisfiable but extremely tight shape. Mitigation: keep the budget documented and revise it on evidence, rather than describing failure as impossible (the posture of ADR-0013).
* Users may expect string exclusion to be constructive like the numeric builders. Mitigation: state the redraw and its deferred failure explicitly in the builder documentation and the Dummies readme.

## Follow-up Actions

* Document the redraw and the deferred, seed-bearing failure in the Dummies readme and the builder documentation (done in the implementing pull request).
* Revisit the budget if real usage reveals false exhaustion.
* Consider spec-aware avoidance only if evidence shows the bounded redraw is inadequate.

## References

* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the sibling bounded-draw-with-deferred-failure channel.
* [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md) — `AnyString.OneOf` stays terminal and does not combine with exclusions.
* [ADR-0020](0020-materialize-dummies-only-through-generate.md) — dummies materialize only through `Generate()`.
* `StringSpec` and `AnyString` in the `Dummies` project; the Dummies NuGet readme.
* Issue [#224](https://github.com/Reefact/first-class-errors/issues/224).

# ADR-0013 | Gate distinct collections by cardinality, otherwise by a bounded draw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

Dummies treats contradictory constraints as arrangement errors and avoids hidden unbounded retry loops.

A distinct collection of `N` elements is satisfiable only when at least `N` distinct values can be assembled from its effective domain: the element generator's own domain, widened by any values pinned outside it and by opaque externally-supplied values the generator itself could never draw. The generator's own cardinality therefore bounds only the elements that must come from it, not the whole request.

Some generators expose a domain the library can count cheaply — a small fixed set, or a value pinned to one member of it. Others cannot honestly report their domain size, either because counting it is disproportionately expensive (a floating-point range, for example) or because it is genuinely unbounded or unknowable, including foreign `IAny<T>` implementations and composed generators.

A custom equality comparer can reduce the number of effective equivalence classes even when the generator's nominal domain is larger.

## Decision

A distinct collection rejects a requested count immediately when it exceeds a known effective element-domain cardinality, and otherwise uses a bounded deduplicating draw that fails explicitly and reproducibly when enough distinct values cannot be obtained.

## Rationale

When the domain size is known, the contradiction is certain and belongs at declaration time with the rest of Dummies' constraint validation.

Counting only the generator's own cardinality would eagerly reject requests that are actually satisfiable once already-accounted-for values are considered; the eager check therefore compares against the domain size net of the values already pinned or opaquely supplied outside it, so it stays sound: it never rejects a request that was truly satisfiable, and a comparer that collapses the effective domain below the requested count is still caught by the bounded draw.

When the domain size is unknown, drawing and deduplicating is the only general strategy available. Bounding the work preserves termination and turns an impossible or practically unreachable request into a diagnosable generation failure rather than a hang.

The cardinality capability remains optional so public and foreign generators are not forced to provide information they cannot know. A comparer-induced reduction is then handled by the generation-time bound.

The exact hint interface, collection state, draw budget, exception payload, and seed propagation are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#dummies-generation-contracts) and the Dummies user documentation.

## Alternatives Considered

### Always fail at generation

Considered because one failure point is simpler. Rejected because it discards an exact declaration-time diagnosis for generators whose domain size is known.

### Require every generator to expose cardinality

Considered because it would make every request decidable up front. Rejected because many valid generators cannot provide a trustworthy bound and the public interface supports foreign implementations.

### Draw without a bound

Considered because a satisfiable request would eventually complete. Rejected because an unsatisfiable request could loop forever.

## Consequences

### Positive

* Known contradictions fail early and clearly.
* Unknown domains still fail safely, reproducibly, and without hanging.
* Foreign generators remain compatible without implementing cardinality metadata.

### Negative

* Failure timing differs between known and unknown domains.
* A bounded draw can fail for a theoretically satisfiable but heavily biased generator.

### Risks

* A generator may advertise an inaccurate upper bound. Mitigation: the bounded draw remains the final safety net.
* A poorly tuned budget may cause spurious failures. Mitigation: keep the budget documented, test representative biased generators, and revise it based on evidence rather than describing failure as impossible.

## Follow-up Actions

* Document both failure channels and the replay seed in the Dummies guide.
* Revisit the budget if real usage reveals false exhaustion.

## References

* [ADR implementation reference — Dummies generation contracts](../specifications/adr-implementation-reference.md#dummies-generation-contracts)
* [ADR-0011](0011-host-dummies-as-a-standalone-package.md)
* `CollectionState` and `ICardinalityHint` in the `Dummies` project.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

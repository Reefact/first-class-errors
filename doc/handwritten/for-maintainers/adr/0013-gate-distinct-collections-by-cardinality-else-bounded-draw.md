# ADR-0013 | Gate distinct collections by cardinality, otherwise by a bounded draw

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

Dummies rejects contradictory constraints when they are declared and otherwise
builds values from the complete specification without an unbounded retry loop.
Distinct collections add a requirement that can be proved eagerly only when the
element generator's domain size is known.

Some internal generators have a small countable domain; arbitrary or foreign
`IAny<T>` implementations, derived generators and custom comparers may have an
unknown or distribution-dependent number of equivalence classes. Cardinality
therefore cannot be required on the public generator interface.

## Decision

A distinct collection rejects a requested count eagerly when it exceeds an advertised cardinality and otherwise uses a bounded deduplicating draw that fails reproducibly at generation if it cannot obtain enough distinct values.

## Rationale

When a sound upper bound is known, exceeding it is a declaration-time conflict and
should fail with the same diagnostic quality as scalar contradictions. When the
domain is unknown, drawing and deduplicating is the only general mechanism; it
must be bounded so an impossible request cannot hang indefinitely.

The split failure timing reflects available information rather than two competing
principles. A comparer can reduce the effective domain, so the bounded path remains
necessary even after an eager cardinality check. The current hint model, collision
budget and replay diagnostics are maintained in the
[Dummies generation specification](../specifications/dummies-generation.en.md).

## Alternatives Considered

### Always fail during generation

Considered for one failure channel. Rejected because it postpones contradictions
that are cheap and certain for booleans, enums and other known finite domains.

### Require every `IAny<T>` to report cardinality

Considered for universal eager validation. Rejected because foreign and derived
generators often cannot report a sound bound.

### Draw until enough values appear

Considered because it eventually succeeds for a satisfiable request. Rejected
because it never terminates for an unsatisfiable one.

## Consequences

### Positive

* Known impossible distinct requests fail in the test arrangement.
* Unknown domains fail safely and replayably instead of hanging.
* Public `IAny<T>` remains implementable without domain metadata.

### Negative

* Failure timing differs between known and unknown domains.
* A finite budget can reject a satisfiable but highly biased generator.

### Risks

* A cardinality hint can be too generous or a comparer can collapse more values
  than expected; the bounded draw then detects the shortfall at generation.
* Budget tuning can produce false exhaustion for pathological distributions.
  Mitigation: report the seed and revisit the documented budget if real usage
  demonstrates the problem; no universal probability guarantee is claimed.

## Follow-up Actions

* Keep the user documentation explicit about the two failure channels.
* Revisit the budget or capability model if reproducible false exhaustion occurs.

## References

* [Dummies generation specification](../specifications/dummies-generation.en.md).
* ADR-0011 — standalone Dummies package.
* `Dummies/CollectionState.cs` and `Dummies/ICardinalityHint.cs`.

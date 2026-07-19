# ADR-0023 | Allow a one-time editorial refactoring of accepted ADRs

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The ADR corpus defines accepted records as immutable historical decisions and also defines ADRs as decision records rather than implementation specifications.

Several accepted ADRs predate or imperfectly apply that separation. They contain exact project paths, configuration properties, workflow steps, command sequences, API signatures, algorithm mechanics, or maintenance procedures that can change while the underlying architectural decision remains valid.

Leaving those details in place creates two conflicting forms of governance: accepted ADRs cannot be edited, but implementation details embedded in them inevitably become stale. It also makes it difficult to distinguish the durable decision from the current technical realization.

The maintainer has reviewed the corpus and authorized a one-time editorial migration provided that it does not change any decision, rationale, alternative, consequence, historical status, or attribution.

## Decision

The repository will permit one traceable editorial refactoring of existing accepted ADRs to move implementation specifications into dedicated reference documentation without changing their architectural meaning.

## Rationale

The migration resolves a contradiction inside the current governance model while preserving the historical value of the records. The durable decision and its reasoning remain in each ADR; volatile mechanics move to documentation that is expected to evolve with the implementation.

Treating the migration as an explicit architectural decision keeps the exception visible and bounded. It prevents the work from becoming an informal precedent for silently rewriting accepted decisions.

A single thematic reference is preferable to scattering implementation details across replacement ADRs because those details describe current contracts and procedures rather than new architectural choices.

## Alternatives Considered

### Leave accepted ADRs unchanged

This would preserve strict immutability, but it would also preserve stale or overly detailed implementation material and continue violating the repository's own distinction between decisions and specifications.

### Supersede every affected ADR

This would maintain strict historical immutability, but it would create many artificial successor ADRs despite no decision having changed. The resulting history would suggest architectural reversals where only editorial separation occurred.

### Remove the details without recording an exception

This would be simpler, but it would make the repository's governance internally inconsistent and establish an undocumented precedent for rewriting accepted records.

## Consequences

### Positive

* Existing ADRs become shorter, more durable, and easier to review as architectural records.
* Implementation details gain a maintainable home that can evolve without rewriting history.
* The repository's ADR policy becomes internally consistent.
* Cross-links can make refinements and later decisions explicit without changing the original meaning.

### Negative

* The historical text of affected ADR files changes once, even though their decisions do not.
* Reviewers must verify that no architectural meaning was lost during extraction.
* The migration creates and maintains an additional reference document.

### Risks

* Editorial rewriting could accidentally alter the force or scope of a decision. Mitigation: preserve each decision sentence, rationale, alternatives, consequences, status, date, and decision makers unless a separately authorized governance correction applies.
* The exception could be reused later as justification for rewriting accepted decisions. Mitigation: this ADR authorizes only the migration identified in its references; future decision changes still require a superseding ADR.

## Follow-up Actions

* Extract implementation-specific material from the affected ADRs into the bilingual ADR implementation reference.
* Add explicit links between ADRs that refine, revisit, or update the API shape of earlier decisions.
* Correct statuses for decisions already implemented and approved by the maintainer.
* Review the final diff specifically for semantic changes to accepted decisions.

## References

* [ADR implementation reference](../specifications/adr-implementation-reference.md)
* [ADR corpus and conventions](README.md)

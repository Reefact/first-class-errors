# ADR-0023 | Extract specifications from accepted ADRs

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0023-extract-specifications-from-accepted-adrs.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

Accepted ADRs are immutable historical records: a changed decision is represented
by a successor, not by rewriting the original. The ADR corpus nevertheless
contains implementation mechanics, exact configuration, maintenance procedures,
and API walkthroughs that change more frequently than the decisions they explain.

That mixture conflicts with the repository's own ADR rule: if implementation
changes while the decision stands, the ADR should not need editing. It also makes
future maintenance ambiguous, because a reader cannot tell whether an exact flag,
method shape, draw budget, or workflow step is the durable decision or merely the
current implementation.

A review of the complete ADR base identified a bounded set of accepted records
that can be normalised without changing their decision, rationale, alternatives,
consequences, or historical meaning. Reefact explicitly authorises that editorial
normalisation and the correction of already-implemented `Proposed` statuses in the
same pull request.

## Decision

The accepted ADR corpus may undergo one editorial migration that extracts implementation specifications into mutable maintainer references, adds non-semantic relation metadata, and corrects already-implemented statuses without materially changing any recorded decision or its reasoning.

## Rationale

The migration restores the intended separation: ADRs remain stable explanations
of choices, while specifications become the maintained source for current API,
workflow, algorithm, and compatibility mechanics. Creating superseding ADRs would
misrepresent an editorial correction as a change of architecture, and copying the
mechanics without removing them would preserve the ambiguity.

The exception is deliberately one-time and bounded. It applies only to the corpus
reviewed in the migration pull request, requires English and French pages to stay
aligned, and can be checked as a pure documentation diff. Future accepted ADRs
remain immutable under the ordinary rule.

Correcting a `Proposed` status is included only where the corresponding decision
is already fully implemented on `main` and the maintainer has explicitly accepted
the regularisation. No agent infers acceptance from implementation alone.

## Alternatives Considered

### Leave the corpus unchanged

Considered because it preserves literal immutability. Rejected because it leaves
known specification leakage in the historical records and makes the problem
permanent.

### Supersede every affected ADR

Considered because it follows the normal mechanism for changing an accepted ADR.
Rejected because no decision is changing; successor records would falsely imply
new architecture and fragment one decision across editorial-only replacements.

### Copy the mechanics to references without shortening the ADRs

Considered because it adds mutable documentation without touching accepted files.
Rejected because the duplicated text would immediately create two competing
sources of truth and would not restore the decision/specification boundary.

## Consequences

### Positive

* Accepted ADRs become shorter and stable under implementation refactoring.
* Current mechanics have explicit, bilingual, mutable maintainer references.
* Cross-ADR refinement and revisiting relationships become discoverable.
* Status metadata matches the decisions already accepted and implemented.

### Negative

* The migration produces a large documentation diff across historical files.
* Reviewers must verify semantic preservation rather than relying on the usual
  rule that accepted ADRs never change.

### Risks

* Editorial rewriting could accidentally alter a decision or its reasoning.
  Mitigation: the pull request links each extracted specification, keeps the
  decision sentences stable, and is reviewed as an exceptional migration.
* The exception could be cited to justify later in-place changes. Mitigation: this
  ADR limits the permission to the migration pull request; future changes follow
  the normal superseding process.

## Follow-up Actions

* Maintain the specification index under
  [`../specifications/`](../specifications/README.md).
* Apply the ordinary immutability rule again after this migration is merged.

## References

* [ADR format and index](README.md).
* [Maintainer specifications](../specifications/README.md).

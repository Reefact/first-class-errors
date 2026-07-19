# ADR-0004 | Check every pull request against the ADR base

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0004-check-every-pull-request-against-the-adr-base.fr.md)

**Status:** Accepted
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

Pull requests are where new decisions enter the repository. Without a deliberate
review, a change can introduce an unrecorded decision, silently replace an
accepted one, or contradict the ADR base.

Deterministic invariants are already enforced by tests, analyzers and CI. What
remains is a judgement question over the whole diff: whether a significant,
lasting decision exists and how it relates to recorded decisions. The repository
is primarily changed through instructed coding sessions, but it also accepts
other contributors. A model judgement has cost, is non-deterministic, and cannot
replace the maintainer's authority.

## Decision

The repository requires every pull request to receive an advisory, non-blocking review against the ADR base, normally within the instructed coding session and otherwise through a manually initiated fallback, while the maintainer alone controls ADR status and merge decisions.

## Rationale

The review belongs at pull-request time because the change context is still
available. It remains advisory because architectural significance and conflict are
human judgements, whereas enforceable invariants belong in deterministic checks.

The normal in-session path has the richest context and minimal duplicate work. A
manual fallback covers other contribution paths without turning a floating model
verdict into an autonomous gate. The exact outcomes, prompts, checklist and
limitations are maintained in the
[ADR review process specification](../specifications/adr-review-process.en.md).

The requirement is procedural, not a claim of perfect automatic coverage: a pull
request created outside an instructed session still depends on someone running or
performing the fallback review.

## Alternatives Considered

### Run a model automatically on every pull request

Considered for complete automatic coverage. Rejected because it adds cost and
non-determinism to every change, duplicates a better-context session review, and
would tempt maintainers to treat a model opinion as a gate.

### Encode every ADR as a machine-readable invariant

Considered for deterministic conflict detection. Rejected because lasting choices
cannot generally be reduced to mechanical predicates; the predicates that can be
expressed belong in tests or CI, not in the ADR itself.

### Rely on memory

Considered as the zero-effort status quo. Rejected because it is precisely how
unrecorded, silently superseded, or conflicting decisions escape review.

## Consequences

### Positive

* Architectural significance is considered while the change context is fresh.
* Agents can draft proposed ADRs with the complete diff available.
* The maintainer retains sole decision and merge authority.
* Contributors outside the normal agent workflow have a documented fallback.

### Negative

* Coverage is best-effort rather than mechanically guaranteed.
* Model reviews are non-reproducible and can produce false alarms or omissions.

### Risks

* The review can be skipped or treated as a checkbox. Mitigation: instructions are
  repeated in `CLAUDE.md`, `AGENTS.md`, the PR template, and the process
  specification.

## Follow-up Actions

* If real usage shows inadequate coverage, improve the advisory mechanism without
  turning it into an autonomous blocking decision.

## References

* [ADR review process specification](../specifications/adr-review-process.en.md).
* `AGENTS.md` and `CLAUDE.md` — agent instructions.
* [`adr-check` workflow reference](../workflows/adr-check.en.md).

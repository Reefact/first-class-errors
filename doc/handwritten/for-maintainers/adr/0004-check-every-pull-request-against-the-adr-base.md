# ADR-0004 | Check every pull request against the ADR base

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0004-check-every-pull-request-against-the-adr-base.fr.md)

**Status:** Accepted
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

The repository records significant decisions as ADRs under `doc/handwritten/for-maintainers/adr/`, but
nothing confronts a change with that base at the moment the change is made. A pull
request is where new decisions enter the codebase: it can embark a decision that is
never recorded, replace a decision an existing ADR holds without saying so, or
contradict an accepted ADR unnoticed.

Hard, mechanical invariants are already guarded elsewhere: the value-object `class`
rule, the analyzer's Roslyn floor (ADR-0001), and the tooling runtime floor
(ADR-0002) are enforced by unit tests and CI jobs that fail deterministically. What
no test expresses is the softer question a reader of a diff must still ask: is there
an architectural decision here, and does it fit what has already been decided?

The repository's work is largely produced through Claude Code coding sessions, which
load `CLAUDE.md` (and, when directed, `AGENTS.md`) as instructions and hold,
in-session, the full diff, the ADR base, and the reasoning that produced the change.
It is also open to contributors who do not use Claude Code. A GitHub Actions workflow
can run a model on manual dispatch, as the `changelog` workflow already does. Such
a model call has a per-request cost, and the model is referenced by a floating alias,
so its verdict is not reproducible.

The maintainer (`Reefact`) is the sole authority who merges a pull request and who
accepts an ADR; no agent merges, and an ADR is immutable once accepted (a decision is
revisited by a superseding ADR).

## Decision

Every pull request is checked against the ADR base as an advisory, non-blocking
recommendation — automatically within a Claude Code coding session and on manual
dispatch for contributors without Claude Code — with an agent drafting any ADR as
`Proposed` and the maintainer alone accepting, superseding, or deprecating it.

## Rationale

The check belongs at the pull request because that is where decisions enter the
codebase; asking "should this be recorded?" while the context is fresh is what the
ADR base needs and does not yet get.

It is advisory, never blocking, because the decisions it surfaces are matters of
judgement, not conditions a machine settles: the hard invariants that *can* be
settled mechanically are already gated by tests and CI, and gating a merge on a
model's opinion would contradict the rule that the maintainer alone merges.

The automatic path runs inside a Claude Code session because the agent there is the
best-placed checker — it already holds the diff, the ADR base, and the reason the
change was made, so the check adds little to work the session is already doing, with
more context than any separate call could carry.

The manual workflow exists so a contributor without Claude Code is still covered;
keeping it manual — like the sibling `changelog` workflow — gives that coverage
without turning a non-reproducible LLM verdict into an autonomous gate on every pull
request.

An agent drafts and proposes; it never sets an ADR's status. This keeps the maintainer
as the decision authority, consistent with "no agent merges" and with ADR immutability.

## Alternatives Considered

### An automatic per-pull-request model check in CI

Considered because it would cover every pull request — human- and agent-authored,
Claude Code or not — deterministically and without anyone remembering to run it.

Rejected because an autonomous model call on every pull request carries a per-request
cost, introduces a non-deterministic check on a near-mandatory surface, and duplicates
— with less context — what a Claude Code session already performs; the coverage it
would add is met instead by the session check plus the manual dispatch.

### Encode each ADR's invariant in a machine-checkable field

Considered because a crisp, declared invariant would make conflict detection more
reliable than reasoning over prose.

Rejected because the hard invariants that lend themselves to mechanical checking are
already enforced by tests and CI, which do it better and deterministically; and adding
a specification field to an ADR contradicts the principle that an ADR is a decision
record, not a specification, eroding the human-readability that is the point of the
format.

### Rely on memory, with no check

Considered because it is the zero-effort status quo.

Rejected because it is exactly the gap the ADR base exists to close: decisions embarked
in a pull request then go unrecorded, silently supersede an earlier one, or contradict
an accepted ADR.

## Consequences

### Positive

* The question "is there a decision to record?" is asked on every pull request, while
  the context that produced the change is still fresh.
* Drafting is cheap: the in-session agent already has everything it needs.
* Nothing blocks a merge; the maintainer keeps sole authority over ADR status.
* Contributors without Claude Code have a first-class fallback.

### Negative

* The in-session check is best-effort: it is guidance the agent follows, not a hard
  gate.
* Coverage of a pull request opened without Claude Code depends on someone dispatching
  the workflow.
* The advisory verdict is non-deterministic — it uses a floating model alias — so it is
  not reproducible.

### Risks

* An agent skips the in-session check. Mitigation: the essentials are in `CLAUDE.md`
  (reliably loaded), a checklist item sits on every pull request, and the manual
  workflow is an independent path.
* False alarms train the team to ignore the check. Mitigation: the prompt is biased
  hard toward silence on routine changes.

## Follow-up Actions

* None blocking. If the in-session guidance proves unreliable in practice, add a
  narrowly scoped, non-blocking Claude Code hook that runs the same check — not built
  pre-emptively.

## References

* `AGENTS.md` — "Architecture decisions" (the agent procedure).
* `CLAUDE.md` — the inlined per-session essentials.
* [`adr-check` workflow reference](../workflows/adr-check.en.md).
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md), [ADR-0002](0002-floor-the-tooling-runtime.md)
  — examples of the hard invariants this check deliberately leaves to tests and CI.

# ADR review process specification

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](adr-review-process.fr.md)

This page implements [ADR-0004](../adr/0004-check-every-pull-request-against-the-adr-base.md).
The repository process **requires** an ADR review for every pull request, but the
review is advisory and the current automation does not mechanically guarantee
that every pull request receives one.

## Review question

Before finalising a pull request, compare the complete change with the accepted
ADR base and ask:

* Does the change introduce a significant, lasting decision?
* Does it replace or refine a recorded decision?
* Does it contradict an accepted decision?
* Does it only change implementation mechanics while the decision remains true?

The significance test is: if the implementation changed but the decision stood,
the ADR should not need editing.

## Outcomes

| Outcome | Required action |
|---|---|
| No decision | State that no architectural decision is introduced. |
| Create | Draft one ADR per new decision as `Proposed`, index it, and link it from the pull request. |
| Supersede | Draft a successor as `Proposed`; identify the accepted ADR it would replace. Do not rewrite the accepted ADR. |
| Alert | Flag the exact conflict with an accepted ADR and leave the resolution to the maintainer. |

An automated agent may draft and recommend. Only the maintainer may accept,
supersede, deprecate, merge, or waive a conflict.

## Execution paths

### Agent-authored work

`CLAUDE.md` and `AGENTS.md` carry the mandatory review instructions. The agent
performing the change already has the diff and the reasoning context, so it is
the primary review path and records the outcome in the pull-request description.

### Other contributors

The `adr-check` workflow is a manually dispatched fallback. It provides an
independent review path when the change was not produced in an instructed coding
session. Its result remains advisory and non-reproducible because it is a model
judgement, not a deterministic build invariant.

### Pull-request checklist

The pull-request template records the declared outcome. The checklist makes the
review visible but does not prove that the analysis was complete.

## What remains mechanical

Deterministic invariants belong in tests and required CI checks, not in an LLM
opinion. The ADR review may identify that a guard is missing, but it must not
replace guards such as compatibility-floor tests, architecture tests, analyzers,
or release gates.

## Limitations and escalation

* A pull request opened outside an instructed session is covered only if someone
  dispatches the fallback or performs the review manually.
* A model verdict can be wrong and must never block by itself.
* A conflict, uncertain significance, or status change is escalated to `@reefact`.
* The exact workflow permissions and prompt live in the
  [`adr-check` workflow reference](../workflows/adr-check.en.md).

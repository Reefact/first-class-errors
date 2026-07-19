# ADR-0004 | Check every pull request against the ADR base

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0004-check-every-pull-request-against-the-adr-base.fr.md)

**Status:** Accepted
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

Pull requests are where new architectural decisions enter the repository. A change can introduce an unrecorded decision, replace an existing one without acknowledgement, or conflict with an accepted ADR.

Mechanical invariants are already enforced by tests and CI. The remaining question — whether a diff contains or conflicts with an architectural decision — requires judgement and context rather than a deterministic rule.

The maintainer is the sole authority who merges pull requests and changes ADR statuses. Agents may analyse changes and draft proposed ADRs, but they do not accept, supersede, deprecate, or merge them.

The repository supports both agent-assisted work and contributions that do not use an in-session agent. A model-based check is advisory and non-reproducible, so it must not become an autonomous merge gate.

## Decision

Every pull request is subject to an advisory review against the accepted ADR base, performed in-session when an agent owns the change or explicitly invoked by a contributor otherwise, with agents limited to drafting `Proposed` ADRs and the maintainer retaining sole decision authority.

## Rationale

The review belongs at pull-request time because that is when the implementation context is freshest and when a decision can still be recorded or challenged before merge.

The review remains advisory because architectural significance is a judgement call. Deterministic invariants should continue to be enforced mechanically, while the maintainer remains responsible for accepting the recommendation and for every status transition.

Using the in-session agent when available avoids a lower-context duplicate analysis. An explicit fallback for other contributors preserves accessibility without turning a non-deterministic model verdict into an automatic gate.

The current agent instructions, checklist, and workflow mechanics are documented in `AGENTS.md`, `CLAUDE.md`, the [ADR implementation reference](../specifications/adr-implementation-reference.md#adr-pull-request-check), and the [`adr-check` workflow reference](../workflows/adr-check.en.md).

## Alternatives Considered

### Run an automatic model check on every pull request

Considered because it would maximize nominal coverage. Rejected because it would add cost, duplicate higher-context in-session analysis, and place a non-deterministic judgement on a near-mandatory CI surface.

### Encode each ADR as a machine-checkable invariant

Considered because deterministic checks are reliable. Rejected because only a subset of architectural decisions can be expressed mechanically; those that can already belong in tests or CI, while the ADR itself must remain a human decision record.

### Rely on memory

Considered because it requires no process. Rejected because it leaves exactly the gap the ADR corpus is intended to close.

## Consequences

### Positive

* Architectural significance is considered before merge while the reasoning is still available.
* Agents can draft records cheaply without acquiring decision authority.
* Contributors without an in-session agent have an explicit fallback.
* No model opinion blocks a maintainer from merging.

### Negative

* Coverage is procedural rather than mechanically guaranteed.
* The review is non-deterministic and may produce false positives or omissions.
* A contributor can forget to invoke the fallback review.

### Risks

* The phrase "every pull request" could be interpreted as an automated guarantee. Mitigation: this ADR defines an obligation of process; the current workflow is manually invoked and does not itself prove universal execution.
* Repeated low-value findings could cause the review to be ignored. Mitigation: keep prompts biased toward silence on routine implementation changes.

## Follow-up Actions

* Revisit automated enforcement only if procedural coverage proves insufficient, and keep any future model-based check advisory unless a separate decision changes that rule.

## References

* `AGENTS.md` — the agent procedure and status authority.
* `CLAUDE.md` — the in-session guidance.
* [ADR implementation reference — ADR pull-request check](../specifications/adr-implementation-reference.md#adr-pull-request-check)
* [`adr-check` workflow reference](../workflows/adr-check.en.md)
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

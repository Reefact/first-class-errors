# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited through a **new** ADR that supersedes the old one.

[ADR-0023](0023-extract-specifications-from-accepted-adrs.md) authorises one
bounded exception: the editorial migration that extracted implementation details
from the existing corpus, added relationship metadata, and regularised decisions
that Reefact had already accepted and implemented. After that migration, the
ordinary immutability rule applies again.

## When is an ADR written?

The repository process requires every pull request to be reviewed against this
base when decisions enter the codebase. Most pull requests embark no architectural
decision and add no ADR; the review is what is required, not the artifact. The
current review paths are advisory and do not mechanically prove that every pull
request received one; their operational contract is documented in the
[ADR review process specification](../specifications/adr-review-process.en.md).

The test for significance is: *if the implementation changed but the decision
stood, the ADR should not need editing.* A new decision is **recorded** here, a
decision that replaces another is written as a **superseding** ADR, and a change
that **conflicts** with an accepted ADR is raised for the maintainer. The agent
procedure — draft as *Proposed*, never flip a status without maintainer authority —
is in [`AGENTS.md`](../../../../AGENTS.md).

## An ADR is a decision record, not a specification

An ADR captures a **decision and the reasoning behind it** — not how that decision
is implemented. Implementation mechanics, exact configuration, code, YAML,
commands, maintenance procedures, and API walkthroughs live in mutable reference
documentation.

The [maintainer specification index](../specifications/README.md) is the starting
point for current cross-cutting technical contracts. Workflow structure and
permissions remain in the [workflow reference](../workflows/README.md). A useful
test is unchanged: if implementation changes while the decision stands, update the
specification, not the accepted ADR.

## File conventions

* One file per decision, under `doc/handwritten/for-maintainers/adr/`, named
  `NNNN-short-title.md` — a four-digit sequence number and a lowercase,
  kebab-case title.
* ADRs are written in **English** — the canonical version — with a French
  translation alongside as `NNNN-short-title.fr.md`.
* Each language file links to its counterpart.
* Every ADR follows [`template.md`](template.md).

## Format

### Title and header

```markdown
# ADR-{number} | {Short Title}

**Status:** Proposed | Accepted | Superseded | Deprecated
**Date:** YYYY-MM-DD
**Decision Makers:** {Names or team}
```

The date is the day the decision reached its current status. A *Superseded* ADR
links to its successor next to the status.

### Context

Describe the facts that made the decision necessary. Include business,
functional, technical, architectural, operational, security, performance, cost,
team, organizational, dependency, delivery, and risk constraints when relevant.
Do not justify the chosen option here.

### Decision

State the decision in **one sentence**, without justification, alternatives, or
implementation detail unless the detail is itself the durable decision.

### Rationale

Explain why the decision is the best choice given the Context. Every argument
must be traceable to a fact already stated there. Rationale is argument, not a
design document; link to a specification for current mechanics.

### Alternatives Considered

Document every serious alternative, why it was considered, and why it was
rejected.

### Consequences

Record positive effects, accepted negative effects, and risks with their
mitigations.

### Follow-up Actions

List work made necessary by the decision, such as documentation, migration,
tests, monitoring, or a future review.

### References

Link related ADRs, specifications, benchmarks, design documents, pull requests,
issues, and diagrams.

## Decision relationships

These relationships clarify the corpus without changing the historical decisions:

| Earlier decision | Relationship | Later decision |
|---|---|---|
| [ADR-0002](0002-floor-the-tooling-runtime.md) | Its incidental .NET Framework 4.6.1 statement is refined; the tooling decision remains unchanged. | [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) |
| [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md) | A rejected process-wide default is revisited with freezing and test-isolation mitigations; the fixed-entry decision remains unchanged. | [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) |
| [ADR-0007](0007-name-the-binder-terminals-new-and-create.md), [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md), [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) | Their illustrative DTO-first entry shape is updated; their naming and option-lifetime decisions remain unchanged. | [ADR-0021](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md) |
| [ADR-0016](0016-make-the-binders-structural-error-codes-configurable.md) | Superseded. | [ADR-0018](0018-bundle-the-binders-structural-error-code-and-messages.md) |

## Index

| ADR | Title | Status |
|---|---|---|
| [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) | Lock the analyzer's Roslyn floor | Accepted |
| [ADR-0002](0002-floor-the-tooling-runtime.md) | Floor the tooling runtime at the oldest supported LTS | Accepted |
| [ADR-0003](0003-unify-outcome-value-mapping-under-then.md) | Unify Outcome value mapping under Then | Accepted |
| [ADR-0004](0004-check-every-pull-request-against-the-adr-base.md) | Check every pull request against the ADR base | Accepted |
| [ADR-0005](0005-reserve-the-plain-factory-name-for-the-outcome-returning-variant.md) | Reserve the plain factory name for the Outcome-returning variant | Accepted |
| [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md) | Supply arbitrary test values from a single seedable source | Accepted |
| [ADR-0007](0007-name-the-binder-terminals-new-and-create.md) | Name the binder terminals New and Create | Accepted |
| [ADR-0008](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) | Bind nullable value-type properties through a struct-constrained overload | Accepted |
| [ADR-0009](0009-report-the-toolings-failures-as-first-class-errors.md) | Report the tooling's failures as first-class errors | Accepted |
| [ADR-0010](0010-treat-gendocs-error-catalog-as-a-versioned-contract.md) | Treat GenDoc's error catalog as a versioned contract | Accepted |
| [ADR-0011](0011-host-dummies-as-a-standalone-package.md) | Host Dummies as a standalone package in this repository | Accepted |
| [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md) | Fix the binder options before binding begins | Accepted |
| [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) | Gate distinct collections by cardinality, otherwise by a bounded draw | Accepted |
| [ADR-0014](0014-bind-a-required-list-by-presence-not-cardinality.md) | Bind a required list by presence, not cardinality | Accepted |
| [ADR-0015](0015-cap-any-combine-at-arity-eight.md) | Cap Any.Combine at arity eight | Accepted |
| [ADR-0016](0016-make-the-binders-structural-error-codes-configurable.md) | Make the binder's structural error codes configurable | Superseded |
| [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) | Provide a configurable application-wide default for the binder options | Accepted |
| [ADR-0018](0018-bundle-the-binders-structural-error-code-and-messages.md) | Bundle the binder's structural error code and messages in one definition | Accepted |
| [ADR-0019](0019-document-overridden-binder-errors-in-the-consumers-catalog.md) | Document overridden binder errors in the consumer's own catalog | Accepted |
| [ADR-0020](0020-materialize-dummies-only-through-generate.md) | Materialize dummies only through Generate() | Accepted |
| [ADR-0021](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md) | Bind out-of-DTO arguments as peers through a source-agnostic untyped entry | Accepted |
| [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) | Floor the library's .NET Framework support at 4.7.2 | Accepted |
| [ADR-0023](0023-extract-specifications-from-accepted-adrs.md) | Extract specifications from accepted ADRs | Accepted |

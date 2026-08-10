# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited by writing a **new** ADR that supersedes the old
one, and the old one's status changes to *Superseded* with a link to its
successor.

[ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md)
authorizes one bounded exception to this rule: a traceable editorial migration
that moves implementation specifications out of existing ADRs without changing
any decision, rationale, alternative, consequence, status, date, or attribution.
It is not a precedent for changing accepted decisions in place.

## When is an ADR written?

Every pull request is checked against this base — the moment new decisions enter
the codebase. Most pull requests embark no architectural decision and add no ADR;
the check is what is mandatory, not the artifact. The test for "significant": *if
the implementation changed but the decision stood, the ADR should not need
editing.* A new decision is **recorded** here, a decision that replaces another is
written as a **superseding** ADR, and a change that **conflicts** with an accepted
ADR is raised for the maintainer. The agent procedure — draft as *Proposed*, never
flip a status unilaterally — is in [`AGENTS.md`](../../../../AGENTS.md).

## An ADR is a decision record, not a specification

An ADR captures a **decision and the reasoning behind it** — not how that
decision is implemented. Implementation mechanics (code, configuration, YAML,
exact flags, XML or command snippets, guard-by-guard or step-by-step
walkthroughs) live in the code and in the reference documentation the ADR links
to — for example the [workflow reference](../workflows/README.md) and the
[ADR implementation reference](../specifications/adr-implementation-reference.md)
— never in the ADR itself. In particular, **Rationale is argument, not a design
document**: if a paragraph explains *how something is built* rather than *why the
decision is right*, it belongs in the reference docs, and the ADR links to it. A
useful test: if the implementation changed but the decision stood, the ADR
should not need editing.

## File conventions

* One file per decision, under `doc/handwritten/for-maintainers/adr/`, named
  `NNNN-short-title.md` — a four-digit sequence number and a lowercase,
  kebab-case title: `0001-lock-the-analyzer-roslyn-floor.md`.
* ADRs are written in **English** — the canonical version — with a French
  translation kept alongside as `NNNN-short-title.fr.md`, matching the rest of
  the repository's bilingual documentation. The English version is
  authoritative; the French one is a convenience for the project's maintainers
  and follows it. Each file carries a language banner linking to its
  counterpart.
* Every ADR follows the format below; [`template.md`](template.md) is a
  copy-ready skeleton.

## Format

### Title and header

```markdown
# ADR-{number} | {Short Title}

**Status:** Proposed | Accepted | Superseded | Deprecated
**Proposed:** YYYY-MM-DD
**Accepted:** YYYY-MM-DD
**Decision Makers:** {Names or team}
```

The header carries **one dated line per state the decision actually reached in
this repository**, and no date is ever overwritten (decision: ADR-0057). A record
drafted as *Proposed* carries `Proposed:` alone; accepting it adds `Accepted:`
below and leaves the first line untouched. Both dates then stay for good: when
the thinking happened and when it was ratified are different facts, and a log
that keeps only the second cannot say how long a decision waited, nor which ones
were ratified on sight.

A supersession adds nothing — **it moves no date and introduces none**. The
decision was taken when it was taken, and that is what the record keeps; the new
date belongs to the successor. What connects the two is the link, not the date: a
*Superseded* ADR links to the ADR that supersedes it, next to the status.

Records written before this format carried a single `Date:` line and have been
converted, so the whole base reads the same way. Where that single date is all
the base ever held — most of these were entered already *Accepted*, having never
been *Proposed* here — it is written to both lines. Two identical dates therefore
say "one date is known, and it stands for both states", not that a proposal and
an acceptance were separately recorded a day apart. Nothing was invented to fill
a gap; the gap is stated by the repetition.

### Context

Describe all information that led to the decision. The objective is that
someone unfamiliar with the project can understand why this decision had to be
made.

Include every relevant aspect when applicable:

* business context;
* functional requirements;
* technical constraints;
* architectural constraints;
* operational constraints;
* security requirements;
* performance requirements;
* cost considerations;
* team skills and experience;
* existing system limitations;
* organizational or political constraints;
* external dependencies;
* deadlines or delivery constraints;
* known risks.

This section contains **facts only**. It does not justify or explain the
chosen solution.

### Decision

Describe the decision in **one single sentence**.

Rules:

* one sentence only;
* no justification;
* no alternatives;
* no historical explanation;
* no implementation details unless they are part of the decision itself.

Example:

> The application will use PostgreSQL as its primary relational database.

### Rationale

Explain why this decision is the best choice given the context. Each argument
must be traceable to information already described in the Context section; if
an argument is missing from the Context, add the missing factual information
there first.

This section explains:

* why the decision satisfies the requirements;
* which constraints it addresses;
* which trade-offs were accepted;
* why the expected benefits outweigh the drawbacks.

It is **argument only**. It does **not** contain implementation detail — no
code, configuration, YAML, exact flags, or XML/command snippets, and no
guard-by-guard or step-by-step "how it is built". That is specification: link
to where it actually lives (the code, the [workflow
reference](../workflows/README.md), or the [ADR implementation
reference](../specifications/adr-implementation-reference.md)) instead of
pasting it here. Naming a guard's *role* and *why it exists* is argument and
belongs here; documenting *how the guard is wired* is specification and does
not.

### Alternatives Considered

Document every serious alternative that was evaluated. Each alternative
explains **why it was considered** and **why it was ultimately rejected** —
not simply that it was rejected.

```markdown
### {Alternative 1}

Why it was considered.

Why it was ultimately rejected.
```

### Consequences

Describe the consequences of adopting this decision — both positive and
negative impacts — under three subheadings:

* **Positive** — the benefits the decision delivers;
* **Negative** — the costs and limitations accepted with it;
* **Risks** — what could go wrong later, and any mitigation in place.

### Follow-up Actions

List any work that becomes necessary because of this decision. Examples:

* update documentation;
* migrate existing components;
* create technical guidelines;
* monitor performance after deployment;
* add automated tests;
* schedule a future review.

### References

Optional supporting material:

* related ADRs;
* RFCs;
* specifications;
* benchmarks;
* design documents;
* pull requests;
* issue trackers;
* diagrams.

## Index

| ADR | Title | Status |
|---|---|---|
| [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) | Lock the analyzer's Roslyn floor | Accepted |
| [ADR-0002](0002-floor-the-tooling-runtime.md) | Floor the tooling runtime at the oldest supported LTS | Accepted |
| [ADR-0003](0003-unify-outcome-value-mapping-under-then.md) | Unify Outcome value mapping under Then | Accepted |
| [ADR-0004](0004-check-every-pull-request-against-the-adr-base.md) | Check every pull request against the ADR base | Accepted |
| [ADR-0005](0005-reserve-the-plain-factory-name-for-the-outcome-returning-variant.md) | Reserve the plain factory name for the Outcome-returning variant | Accepted |
| [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md) | Supply arbitrary test values from a single seedable source | Superseded |
| [ADR-0007](0007-name-the-binder-terminals-new-and-create.md) | Name the binder terminals New and Create | Accepted |
| [ADR-0008](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) | Bind nullable value-type properties through a struct-constrained overload | Accepted |
| [ADR-0009](0009-report-the-toolings-failures-as-first-class-errors.md) | Report the tooling's failures as first-class errors | Accepted |
| [ADR-0010](0010-treat-gendocs-error-catalog-as-a-versioned-contract.md) | Treat GenDoc's error catalog as a versioned contract | Accepted |
| [ADR-0011](0011-host-dummies-as-a-standalone-package.md) | Host JustDummies as a standalone package in this repository | Accepted |
| [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md) | Fix the binder options before binding begins | Accepted |
| [ADR-0014](0014-bind-a-required-list-by-presence-not-cardinality.md) | Bind a required list by presence, not cardinality | Accepted |
| [ADR-0016](0016-make-the-binders-structural-error-codes-configurable.md) | Make the binder's structural error codes configurable | Superseded |
| [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) | Provide a configurable application-wide default for the binder options | Accepted |
| [ADR-0018](0018-bundle-the-binders-structural-error-code-and-messages.md) | Bundle the binder's structural error code and messages in one definition | Accepted |
| [ADR-0019](0019-document-overridden-binder-errors-in-the-consumers-catalog.md) | Document overridden binder errors in the consumer's own catalog | Accepted |
| [ADR-0021](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md) | Bind out-of-DTO arguments as peers through a source-agnostic untyped entry | Accepted |
| [ADR-0023](0023-keep-expression-tree-selectors-for-the-v1-binder-api.md) | Keep expression-tree selectors for the v1 binder API | Accepted |
| [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) | Allow a one-time editorial refactoring of accepted ADRs | Accepted |
| [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) | Rebase the testing package's arbitrary values on JustDummies | Accepted |
| [ADR-0027](0027-repair-dependabot-pull-requests-within-a-risk-boundary.md) | Repair Dependabot pull requests within a risk boundary | Accepted |
| [ADR-0028](0028-bridge-throwing-code-into-outcomes-through-a-guarded-try.md) | Bridge throwing code into outcomes through a guarded Try | Accepted |
| [ADR-0029](0029-complete-the-outcome-try-async-surface-with-token-less-overloads.md) | Complete the Outcome.Try async surface with token-less overloads | Accepted |
| [ADR-0034](0034-require-a-scope-on-the-version-driving-commit-types.md) | Require a scope on the version-driving commit types | Accepted |
| [ADR-0043](0043-gate-pull-requests-on-the-mutation-score-of-the-diff.md) | Gate pull requests on the mutation score of what they changed | Accepted |
| [ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md) | Make the per-pull-request mutation gate advisory; the weekly full sweep is the enforced bar | Accepted |
| [ADR-0055](0055-enforce-the-style-rules-the-compiler-can-express.md) | Enforce the style rules the compiler can express, and keep the DotSettings authoritative for the rest | Accepted |
| [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.md) | State the coding rules where an agent can act on them, and check them at the edit | Accepted |
| [ADR-0057](0057-keep-one-dated-line-per-state-an-adr-reached.md) | Keep one dated line per state an ADR reached, and never overwrite one | Accepted |
| [ADR-0060](0060-let-stated-intent-outrank-generic-analyzer-advice.md) | Let stated intent outrank generic analyzer advice, and record the refusal beside the rule | Accepted |
| [ADR-0061](0061-run-the-justdummies-analyzers-on-the-repository-s-own-code.md) | Run the JustDummies analyzers on the repository's own code, so the rules are verified against code not written to please them | Accepted |
| [ADR-0062](0062-derive-the-build-rule-set-from-the-quality-profile.md) | Derive the build's Sonar rule set from the quality profile: generated membership, hand-written exceptions, weekly drift check | Accepted |
| [ADR-0067](0067-treat-the-cli-s-exit-codes-as-a-closed-published-contract.md) | Treat the CLI's exit codes as a closed, published contract | Accepted |
| [ADR-0069](0069-consume-justdummies-from-its-own-repository.md) | Consume JustDummies from its own repository | Accepted |
| [ADR-0070](0070-land-pull-requests-by-rebasing-and-keep-main-linear.md) | Land pull requests by rebasing, and keep `main`'s history linear | Accepted |
| [ADR-0071](0071-refresh-an-open-branch-only-by-rebasing-it.md) | Refresh an open branch only by rebasing it onto `main` | Accepted |

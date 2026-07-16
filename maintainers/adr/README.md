# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited by writing a **new** ADR that supersedes the old
one, and the old one's status changes to *Superseded* with a link to its
successor.

## When is an ADR written?

Every pull request is checked against this base — the moment new decisions enter
the codebase. Most pull requests embark no architectural decision and add no ADR;
the check is what is mandatory, not the artifact. The test for "significant": *if
the implementation changed but the decision stood, the ADR should not need
editing.* A new decision is **recorded** here, a decision that replaces another is
written as a **superseding** ADR, and a change that **conflicts** with an accepted
ADR is raised for the maintainer. The agent procedure — draft as *Proposed*, never
flip a status unilaterally — is in [`AGENTS.md`](../../AGENTS.md).

## An ADR is a decision record, not a specification

An ADR captures a **decision and the reasoning behind it** — not how that
decision is implemented. Implementation mechanics (code, configuration, YAML,
exact flags, XML or command snippets, guard-by-guard or step-by-step
walkthroughs) live in the code and in the reference documentation the ADR links
to — for example the [workflow reference](../workflows/README.md) — never in the
ADR itself. In particular, **Rationale is argument, not a design document**: if
a paragraph explains *how something is built* rather than *why the decision is
right*, it belongs in the reference docs, and the ADR links to it. A useful
test: if the implementation changed but the decision stood, the ADR should not
need editing.

## File conventions

* One file per decision, under `maintainers/adr/`, named
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
**Date:** YYYY-MM-DD
**Decision Makers:** {Names or team}
```

The date is the day the decision reached its current status. A *Superseded*
ADR links to the ADR that supersedes it, next to the status.

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
to where it actually lives (the code, or the [workflow
reference](../workflows/README.md)) instead of pasting it here. Naming a
guard's *role* and *why it exists* is argument and belongs here; documenting
*how the guard is wired* is specification and does not.

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
| [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md) | Supply arbitrary test values from a single seedable source | Accepted |
| [ADR-0007](0007-name-the-binder-terminals-new-and-create.md) | Name the binder terminals New and Create | Accepted |
| [ADR-0008](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) | Bind nullable value-type properties through a struct-constrained overload | Proposed |

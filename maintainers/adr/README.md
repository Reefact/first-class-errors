# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited by writing a **new** ADR that supersedes the old
one, and the old one's status changes to *Superseded* with a link to its
successor.

## File conventions

* One file per decision, under `maintainers/adr/`, named
  `NNNN-short-title.md` — a four-digit sequence number and a lowercase,
  kebab-case title: `0001-lock-the-analyzer-roslyn-floor.md`.
* ADRs are written in **English**, like all repository content.
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

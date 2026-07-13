# AGENTS.md — FirstClassErrors

Instructions for automated agents (OpenAI Codex and others) in this repository.
Two roles are covered: **writing code** and **reviewing pull requests**.

## Project orientation (code changes)

- .NET Standard 2.0 library. Errors are first-class, documented, diagnosable concepts.
- Build: `dotnet build FirstClassErrors.sln`
- Test: `dotnet test FirstClassErrors.sln` (analyzer tests: `dotnet test FirstClassErrors.Analyzers.UnitTests`).
- Repository language is **English** (code, comments, commits, PRs, issues, and
  review comments). French lives only in `doc/README.fr.md` and must stay in sync
  with the English README.
- `Error` and its hierarchy, `ErrorCode`, `ErrorContextKey`, `Outcome`/`Outcome<T>`
  and any value object are **`class`, never `struct`** — a struct exposes a
  zero-initialized default that bypasses validating constructors. Enums
  (`Transience`, `ErrorOrigin`) are the only value-type exception.
- Keep changes small and focused. Treat renamed error codes, diagnostic IDs and
  public types as breaking changes.

## Review guidelines (pull request reviews)

READ THIS BEFORE REVIEWING. The full specification is in `code_review.md`; the
rules below are mandatory and inlined so they are never missed.

### Output format — mandatory

Every inline comment MUST use exactly this shape, with nothing around it:

```text
<label> [(decorations)]: <subject on one line>

<optional discussion>
```

In this shape, `< >` marks a placeholder to replace and `[ ]` marks an optional
part — write neither the angle brackets nor the square brackets literally.
Decorations, when present, go in parentheses (for example `(security)`).

- The entire comment is written in **English** — label, decorations, subject and
  discussion. Code identifiers, API names and exception messages are quoted verbatim.
- Never publish an unlabelled comment.
- Exactly **one label** and **one independent finding** per comment. At most two decorations.
- Do **NOT** add a severity/priority prefix — no `P0`, `P1`, `P2`, `P3`,
  `critical`, `major`, `minor`, anywhere in the comment. Blocking status is
  carried only by the label and the `(blocking)` / `(non-blocking)` decoration.
- No introduction or conclusion around the comment. Place it on the smallest
  relevant code range. Do not repeat the same finding on multiple lines.

Canonical example:

```text
issue (security): The raw connection string is copied into the error context and reaches the logs.

`ConnectionError.Create` stores the full connection string in the `ErrorContext`, and the
log sink serializes every `Values` entry. The password therefore appears verbatim in any
aggregated log output.

Store only the host, or a redacted form, in the context — never the credential itself.
```

### Labels (one per comment)

- `issue:` confirmed defect that must be addressed — *blocking*.
- `todo:` small, obvious, local, non-debatable required change — *blocking*.
- `chore:` mandatory process step before merge; name the command/file — *blocking*.
- `question:` code looks suspicious but evidence is insufficient to assert a defect — *non-blocking*.
- `suggestion:` concrete optional improvement (never for incorrect code — use `issue:`) — *non-blocking*.
- `nitpick:` purely subjective, optional preference; should be rare — *non-blocking*.
- `note:` relevant information, no change expected — *non-blocking*.
- `thought:` design/architecture observation out of scope; must state no change is required here — *non-blocking*.
- `praise:` genuinely good and worth preserving; explain what and why — *non-blocking*.

Override a default only when the finding genuinely differs, e.g.
`suggestion (blocking):` or `issue (non-blocking):`. Never restate a default
(`issue (blocking):`, `nitpick (non-blocking):`).

Allowed decorations: `(blocking)`, `(non-blocking)`, `(if-minor)`, `(security)`,
`(perf)`, `(test)`, `(archi)`. One normally, never more than two.

### What to report (priority order)

Correctness → security → data integrity → regressions → public API / compatibility
→ concurrency / reliability → significant performance → missing tests for a
demonstrated risk → violations of an explicit repository rule (e.g. a value object
converted to `struct`).

Do NOT report: formatter-enforced style, analyzer-detected issues already flagged
by an `FCExxx` rule, naming already enforced by tooling, speculative problems with
no execution path, broad refactors unrelated to the PR, personal style presented as
a requirement, or pre-existing issues the PR does not materially affect.

If there is no relevant finding, approve without manufacturing comments.

### Final summary

Keep it concise. Report only: the number of blocking findings, the number of
non-blocking findings, and the main risk areas. Do not restate every inline
comment. If nothing was found, state clearly that no blocking issue was found.
The summary is not a Conventional Comment and needs no label.

## Responding to review feedback (acting agent)

This section governs the agent that *fixes* a pull request in response to a
review (for example `@claude`), not the reviewer. The human maintainer `@reefact`
is the only authority that merges a pull request; no agent merges, and no agent
enables auto-merge on its own pull request.

For each review finding, take exactly one route:

- **You agree and the fix is clear and local** — implement it, push, and reply on
  the thread with `Resolved in <sha>`. You MAY ask the reviewer (`@codex`) for a
  single confirming re-review; never open a back-and-forth.
- **You believe the finding is wrong** — reply on the thread with the concrete
  technical reason and mention `@reefact` to arbitrate. Do not ping `@codex` to
  argue: a peer reviewer has no authority to settle the disagreement.
- **The finding needs a human judgement** — architecture, a product trade-off, an
  ambiguous requirement, a security or compatibility policy — mention `@reefact`
  and wait. Do not decide unilaterally.

Rules:

- Never mention both `@codex` and `@reefact` on the same thread: a bot round-trip
  or a human decision, never both.
- At most two fix / re-review cycles per finding. If it is still open after that,
  stop and mention `@reefact` instead of continuing.
- Keep replies short and factual; the diff is the record.

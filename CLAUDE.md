# FirstClassErrors — guide for Claude Code

FirstClassErrors is a .NET library that treats errors as first-class,
documented, and diagnosable concepts. Keep changes aligned with that goal:
errors should stay structured, documented, and close to the code.

## Language

* The repository language is **English** by default:
  source code, code comments, commit messages, branch names,
  PR titles and descriptions, and issues.
* The English documentation is canonical.
* The French documentation in `doc/handwritten/for-users/README.fr.md` is an intentional translation
  and must stay in sync with the English documentation when user-facing behavior changes.
* You may reply to me in French in the chat, but never write repository content in French
  unless you are updating the French documentation.

## Build & test

* Target framework: **.NET Standard 2.0**.
* Build: `dotnet build FirstClassErrors.sln`
* Test: `dotnet test FirstClassErrors.sln`
* Run the analyzer tests when touching analyzers:
  `dotnet test FirstClassErrors.Analyzers.UnitTests`
* Only report tests as passing if you actually ran the corresponding command.
* If you did not run a relevant command, say so explicitly.

## Project layout

* `FirstClassErrors`               — main library
* `FirstClassErrors.Analyzers`     — Roslyn analyzers (+ `.UnitTests`)
* `FirstClassErrors.GenDoc`        — error-documentation generator (+ `.UnitTests`)
* `FirstClassErrors.GenDoc.Worker` — background worker for doc generation
* `FirstClassErrors.Cli`           — command-line tool
* `FirstClassErrors.RequestBinder` — request binder for the primary-adapter boundary (+ `.UnitTests`)
* `FirstClassErrors.Usage`         — usage examples
* `doc/`                           — documentation: `handwritten/` (`for-users`, `for-maintainers`) and `generated/` (CI/CD living docs)

## Change guidelines

* Keep changes small, focused, and aligned with the requested task.
* Do not introduce new dependencies without a clear reason.
* Do not make public API changes unless they are required by the task.
* Treat renamed error codes, diagnostic IDs, and public types as breaking changes unless explicitly stated otherwise.
* **Value objects and results are reference types (`class`), never structs.**
  Types that enforce invariants — `Error` and its hierarchy, `ErrorCode`,
  `ErrorContextKey`, `Outcome`/`Outcome<T>`, and any future value object — must be
  declared `class`. A `struct` always exposes an unsuppressable default/parameterless
  constructor (`default(T)`, `new T[]`, uninitialized fields) that yields a
  zero-initialized instance bypassing every validating constructor; nullable
  reference types only warn at compile time and cannot prevent it. A validating
  class keeps its constructor/factory as the single entry point. Do not convert
  these types to `struct`/`readonly struct` for allocation reasons: error/result
  paths are not hot loops, and invariant correctness takes precedence. (Enums such
  as `Transience` and `ErrorOrigin` are the legitimate value-type case — they carry
  no invariant to bypass.)
* Preserve compatibility with **.NET Standard 2.0**.
* Code style and inspection severities are defined in `FirstClassErrors.sln.DotSettings`
  (ReSharper/Rider). Follow it; do not reformat code against these settings.

## Error and documentation conventions

* When you add or change an error, update its documentation accordingly.
* When you change user-facing behavior, keep the English README and the French translation
  (`doc/handwritten/for-users/README.fr.md`) in sync.
* When you change analyzers, update or add analyzer tests.
* When you change diagnostics, keep diagnostic IDs, messages, documentation, and tests consistent.

## Architecture decisions (ADRs)

Before finalizing a pull request, check the change against the ADR base under
`doc/handwritten/for-maintainers/adr/`. This is **advisory**: produce a recommendation, never a
blocker. Full procedure in [`AGENTS.md`](AGENTS.md) ("Architecture decisions");
format and conventions in [`doc/handwritten/for-maintainers/adr/README.md`](doc/handwritten/for-maintainers/adr/README.md).
The essentials, inlined so they hold even if `AGENTS.md` is not read:

* An ADR records a **significant, lasting decision** — one a future maintainer
  would question. Test: *if the implementation changed but the decision stood, the
  ADR should not need editing.* Most pull requests need none; the **check** is the
  habit, the **ADR** is the exception.
* **Create** — a new lasting decision (public API contract, cross-cutting invariant,
  supported-platform floor, dependency or security/compatibility policy): draft one
  ADR per decision as `Status: Proposed`, index it, and link it from the PR.
* **Supersede** — the change replaces a recorded decision: draft the successor as
  `Proposed`; never edit an accepted ADR in place or flip its status yourself.
* **Alert** — the change contradicts an accepted ADR: flag it in the PR description
  (`⚠️ Conflicts with ADR-NNNN`); do not proceed silently.
* You **draft and propose**; you never accept, supersede, or deprecate an ADR — the
  maintainer decides, exactly as no agent merges a pull request. When unsure whether
  a change is significant enough, say so and let `@reefact` judge.

## Git and pull requests

* Follow `.github/pull_request_template.md` for every pull request.
* Do not open a pull request unless I explicitly ask for one.
* PR titles, descriptions, commits, and branch names must be written in English.
* Write every commit message per [`CONTRIBUTING.md`](CONTRIBUTING.md): Conventional Commits, a closed type list, the scopes `core, analyzers, binder, cli, dummies, gendoc, testing`, an imperative header within 72 characters, and `Refs: #NN` in a footer when a GitHub issue exists (issue-closing keywords belong in the PR description, not the commit).
* Write every pull request title per [`CONTRIBUTING.md`](CONTRIBUTING.md): name the whole change in English; a single-intention PR mirrors its commit header (`type(scope): description`), a multi-intention PR uses a short descriptive title, and issue references stay in the description, not the title.
* Enable the local commit-message hook once per clone with `git config core.hooksPath .githooks`; the same check runs in CI on every pull request.
* In PR descriptions, do not invent testing results. Only check items that were actually run.

## Responding to pull request review feedback

When you act on review feedback on a pull request (for example a Codex review),
follow the escalation rules in [`AGENTS.md`](AGENTS.md) ("Responding to review
feedback"). The essentials, inlined so they hold even if `AGENTS.md` is not read:

* If you agree and the fix is clear and local, implement it, push, and reply
  `Resolved in <sha>`.
* If you believe a finding is wrong, reply with the concrete technical reason and
  mention `@reefact` to arbitrate — do not argue with the reviewer bot.
* If a finding needs a human judgement (architecture, a trade-off, an ambiguous
  requirement, a security or compatibility policy), mention `@reefact` and wait.
* Never mention both the reviewer bot and `@reefact` on the same thread; cap at
  two fix/re-review cycles, then escalate to `@reefact`.
* No agent merges a pull request or enables auto-merge on it — the human
  maintainer merges.

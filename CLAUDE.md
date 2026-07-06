# FirstClassErrors — guide for Claude Code

FirstClassErrors is a .NET library that treats errors as first-class,
documented, and diagnosable concepts. Keep changes aligned with that goal:
errors should stay structured, documented, and close to the code.

## Language

* The repository language is **English** by default:
  source code, code comments, commit messages, branch names,
  PR titles and descriptions, and issues.
* The English documentation is canonical.
* The French documentation in `doc/README.fr.md` is an intentional translation
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
* `FirstClassErrors.Usage`         — usage examples
* `doc/`                           — documentation, including the French translation

## Change guidelines

* Keep changes small, focused, and aligned with the requested task.
* Do not introduce new dependencies without a clear reason.
* Do not make public API changes unless they are required by the task.
* Treat renamed error codes, diagnostic IDs, and public types as breaking changes unless explicitly stated otherwise.
* Preserve compatibility with **.NET Standard 2.0**.
* Code style and inspection severities are defined in `FirstClassErrors.sln.DotSettings`
  (ReSharper/Rider). Follow it; do not reformat code against these settings.

## Error and documentation conventions

* When you add or change an error, update its documentation accordingly.
* When you change user-facing behavior, keep the English README and the French translation
  (`doc/README.fr.md`) in sync.
* When you change analyzers, update or add analyzer tests.
* When you change diagnostics, keep diagnostic IDs, messages, documentation, and tests consistent.

## Git and pull requests

* Follow `.github/pull_request_template.md` for every pull request.
* Do not open a pull request unless I explicitly ask for one.
* PR titles, descriptions, commits, and branch names must be written in English.
* In PR descriptions, do not invent testing results. Only check items that were actually run.

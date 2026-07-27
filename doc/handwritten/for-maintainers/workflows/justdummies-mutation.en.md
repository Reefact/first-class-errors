# `justdummies-mutation` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](justdummies-mutation.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/justdummies-mutation.yml`](../../../../.github/workflows/justdummies-mutation.yml)

## What it is for

Mutation testing for the **two JustDummies packages** — `JustDummies` and its
xUnit v3 adapter `JustDummies.Xunit` ([ADR-0039](../adr/0039-adapt-dummies-to-xunit-v3-through-a-companion-package.md)).
On a pull request it mutates only the files the pull request changed and fails
when the score falls under the library's threshold; a weekly sweep measures
everything else. What mutation testing *is*, and why this repository gates on
it, is explained once on the [`mutation`](mutation.en.md) page — this workflow is
the same machine with a different matrix.

## Why it is a separate workflow

`JustDummies` is a standalone, error-agnostic package that deliberately holds no
reference to `FirstClassErrors` ([ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.md)),
and it is headed for a repository of its own. Splitting the mutation gate along
that future boundary now means the move is a **file move rather than an edit**:
nothing in this workflow names a FirstClassErrors project, and nothing in
[`mutation`](mutation.en.md) names a JustDummies one.

It also gives JustDummies its **own required check**,
**`JustDummies mutation gate`**, independent of the FirstClassErrors one. Two
gates, two branch-protection entries, two bars that move independently — which is
what two libraries at different levels of test maturity need anyway.

## When it runs

- On every **pull request targeting `main`** — diff-scoped. **This is the gate.**
- **Weekly** on a schedule (Monday, 03:47 UTC) — the full sweep, advisory. The
  slot is offset from `mutation`'s so the two sweeps do not contend for runners.
- On demand via **`workflow_dispatch`** — the full sweep.

## How it runs

Identically to [`mutation`](mutation.en.md), whose page documents the mechanism
in full: `changed` mutates the diff from the fork point, `gate` collapses the
matrix into one stable check name, `full` sweeps everything with the threshold
disabled. The per-library Stryker configurations are
[`build/stryker/justdummies.json`](../../../../build/stryker/justdummies.json)
and [`build/stryker/justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json).

Two points from that page matter more here than anywhere else:

- **`JustDummies` is the largest library in the repository** — a few thousand
  mutants — so its full sweep is the longest job the repository runs. That is the
  whole reason the gate is diff-scoped rather than a full sweep per pull request.
- **`"test-runner": "mtp"` and `"coverage-analysis": "off"` are not tuning knobs.**
  With Stryker's default VSTest runner these suites score 0 % — every mutant
  reported as survived, because the runner cannot activate a mutant in an xUnit v3
  test project. Read
  [that section](mutation.en.md#two-settings-that-are-not-tuning-knobs) before
  changing either.

## `JustDummies` has no score threshold yet

Every other library's bar was set from a measured full sweep of that library
([how and why](mutation.en.md#where-the-thresholds-come-from)). `JustDummies` was
not: it carries a few thousand mutants over a heavy suite, its full sweep runs
well past an hour, and **no score for it has been measured**. Rather than invent
a number, [`justdummies.json`](../../../../build/stryker/justdummies.json) sets
`break` to **0** — the score gate for this one library is off.

That is deliberate and it is temporary. The leg still runs, still fails on a
broken build or a failing suite, and still lists its surviving mutants in the run
summary; what it does not yet do is refuse a pull request over a score. **The
first weekly sweep publishes the library-wide figure** — that is the run this
threshold is waiting on. Read it, and set `break` from it exactly as the other
libraries' bars were set.

`JustDummies.Xunit` needs no such caveat: it is small enough that its bar came
from a full sweep like the rest, and it gates normally.

## Permissions & security

`contents: read` only. The workflow checks out, builds and runs tests; it stores
no secret and needs no write scope.

## When JustDummies moves to its own repository

Take, unchanged:

- this workflow file, renamed to `mutation.yml` there (and its `name:` with it);
- [`build/stryker/justdummies.json`](../../../../build/stryker/justdummies.json)
  and [`build/stryker/justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json);
- [`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) — the
  Stryker pin;
- this page, plus the shared sections of [`mutation`](mutation.en.md) folded into
  it, since the page it defers to will not exist over there.

Then change exactly one thing: the **`solution`** field in the two
configurations, which still names `FirstClassErrors.sln`. The `project` and
`test-projects` paths are already repository-relative and unchanged by the move.

On this side, delete this workflow, its configurations and this page, and drop
the `JustDummies mutation gate` entry from the branch protection.

## Handle with care

- **Keep this workflow and [`mutation`](mutation.en.md) in step.** They are
  duplicated on purpose — that is what makes the split a file move — so a fix to
  one is a fix to both until the split happens.
- Everything under
  [*Handle with care* on the `mutation` page](mutation.en.md#handle-with-care)
  applies here word for word: `fetch-depth: 0`, `--since` rejecting `HEAD`,
  `if: always()` on `gate`, the pinned engine, where the thresholds live.

## Running it locally

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/justdummies.json
```

That is the full sweep of the largest library and it takes a while. To reproduce
what the gate does on a branch:

```bash
dotnet stryker --config-file build/stryker/justdummies.json --since:$(git merge-base origin/main HEAD)
```

Reports land in `StrykerOutput/` (git-ignored); open `reports/mutation-report.html`.

## Related

- [`mutation`](mutation.en.md) — the same machine for the FirstClassErrors
  libraries, and where the mechanism is documented in full.
- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(no reference
  page yet)* — the other JustDummies-scoped workflow: it proves the packaged
  `netstandard2.0` and `net8.0` assets behave on their own runtimes.
- [ADR 0043 — Gate pull requests on the mutation score of what they
  changed](../adr/0043-gate-pull-requests-on-the-mutation-score-of-the-diff.md)
  — the decision both workflows implement.

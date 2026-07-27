# `mutation` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](mutation.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/mutation.yml`](../../../../.github/workflows/mutation.yml)

## What it is for

Coverage answers *"was this line executed by a test?"*. Mutation testing answers
the question that actually matters: *"would any test have noticed if this line
had been wrong?"*.

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) rewrites
the library one small change at a time — flip a comparison, drop a statement,
return the other constant, remove a block — rebuilds it, and re-runs the test
suite against each rewrite. A **mutant** the suite still passes on is a
**survivor**: a behaviour the code has and nothing asserts. A killed mutant is a
test doing its job.

This workflow makes that check automatic. On a pull request it mutates **only the
files the pull request changed**, which is what keeps it cheap enough to be a
required check; a weekly sweep measures everything else.

Its scope is the **three FirstClassErrors libraries the repository ships** —
`FirstClassErrors`, `FirstClassErrors.Testing` and
`FirstClassErrors.RequestBinder`. The `fce` tooling and the Roslyn analyzers are
deliberately out of scope; see *Handle with care* below.

**JustDummies is not measured here.** It has its own workflow with its own gate,
[`justdummies-mutation`](justdummies-mutation.en.md), because it is headed for a
repository of its own ([ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.md)).
The two are the same machine with different matrices; everything in this page
except the scope applies to both, and the JustDummies page links back here rather
than repeating it.

## When it runs

- On every **pull request targeting `main`** — diff-scoped. **This is the gate.**
- **Weekly** on a schedule (Monday, 03:23 UTC) — the full sweep, advisory.
- On demand via **`workflow_dispatch`** — the full sweep.

## How it runs

Each mutated library has its own Stryker configuration under
[`build/stryker/`](../../../../build/stryker/): the project to mutate, the test
projects that must kill its mutants, and the thresholds. Nothing about the run
policy lives only in the YAML, so `dotnet stryker --config-file
build/stryker/core.json` on a maintainer's machine gates exactly like CI does.

The engine itself is pinned in
[`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) and restored
with `dotnet tool restore`. That pin is load-bearing: a newer Stryker invents new
mutants, which moves every score on its own.

### `changed` — the diff, on every pull request

One matrix leg per shipped library. Each leg:

1. Checks out with **`fetch-depth: 0`** — Stryker's `--since` diffs against a
   commit, so the history has to be there.
2. Resolves the **fork point** (`git merge-base` of the pull request's base and
   `HEAD`), not the base branch tip: the tip may have moved on since the branch
   was cut, and every file changed on `main` in the meantime would otherwise be
   counted as "changed by this pull request".
3. Runs Stryker with `--since:<fork point>`, so only mutants **in files this pull
   request touched** are tested.
4. Renders the surviving mutants — status, file, line, kind of rewrite — into the
   run summary, so a failing gate can be diagnosed without leaving the run page.
5. Uploads the HTML and JSON reports as an artifact — `if: always()`, because the
   HTML view shows each survivor *in its source*, which the summary table cannot.

A leg whose library the pull request did not touch selects no mutant, reports
*"unable to calculate a mutation score"*, and exits 0. That is a pass.

### `gate` — the single required check

A matrix produces one check per leg, so marking the gate as required on `main`
would mean re-declaring the leg names in the branch protection every time the
matrix changes. `gate` collapses them into one stable check name — **`Mutation
gate`** — and that is the one to make required.

It runs with `if: always()`, which is load-bearing: without it, a failed leg
would leave `gate` *skipped*, and GitHub reports a skipped required check as a
success.

### `full` — the weekly sweep

The same three legs with the `--since` filter removed: every mutant of every
library in scope. It is **advisory by construction** — `--break-at 0` disables the
threshold — because its job is to publish a trend, not to turn `main` red on a
Monday morning over code nobody changed. Read it from the uploaded HTML report.

## Two settings that are not tuning knobs

`build/stryker/*.json` carries two settings that look like performance tuning and
are not. Both were established by measurement; changing either silently breaks
the gate rather than making it slower.

### `"test-runner": "mtp"` — mandatory, not a preference

Stryker's **default VSTest runner does not work on this test bed at all.** Every
test project here is xUnit v3, and an xUnit v3 test project *is* an executable
that the VSTest adapter launches as a child process — out of reach of the
in-process hooks Stryker uses both to capture coverage and, crucially, to
**activate** the mutant. The run completes, reports a plausible test count, and
scores **0 %**: every mutant comes back "survived", including mutants that
demonstrably break the suite when the same edit is applied by hand. Upstream:
[stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117).

The Microsoft Testing Platform runner launches the test executable itself, so the
mutant is activated and the score is real. Stryker flags it **preview** and says
so on every run; that warning is expected here, not a misconfiguration.

If a future Stryker upgrade makes every score collapse to zero, this is the first
thing to check.

### `"coverage-analysis": "off"` — accuracy, not speed

Stryker normally runs a coverage pass first so each mutant only re-runs the tests
that reach it. Under the MTP runner that selection is still incomplete
([stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629)):
mutants the suite *does* kill get classified as uncovered and counted against the
score. Measured on `Error.cs`, the same population scores 75 % with selection on
and 100 % with it off — and the 100 % is the true figure.

Turning it off costs almost nothing because these suites are fast: every mutant
runs the library's whole suite, and that is still a fraction of a second to a
couple of seconds per mutant.

## The cost model, and why the gate is diff-scoped

**One full run of the library's test suite per mutant**, plus roughly two minutes
of fixed cost per leg (solution analysis, build, initial test run, mutant
generation). A full sweep of one library is therefore minutes; a full sweep of
every library, on every push, is not something to sit in front of.

That is what makes the diff scope right for a required check. It also explains
two things that surprise people:

- **Selection is per changed *file*, not per changed *line*.** Stryker's `--since`
  has no line granularity. Adding one line to a large file selects **every**
  mutant in that file, so the gate reports the whole file's mutation score — not
  just the score of what was added. On the biggest files that is a longer job and
  a score that reflects pre-existing debt.
- **A pull request that only adds tests still selects mutants**, through the test
  files it changed.

## Where the thresholds come from

Each library carries its own `break` in `build/stryker/*.json`, and the values
differ from one to the next on purpose. They are **not** an opinion about how good a library
ought to be: each one was set from that library's measured full-sweep score at
the time the gate was introduced, rounded down, with a little room left for the
odd equivalent mutant.

That makes the gate a **ratchet**, not an aspiration. It says *do not go below
where this library already is* — a bar every library clears on day one, so the
gate never starts red, and one that only ever moves up. Raising a value after the
weekly sweep shows real headroom is the intended way to use it; lowering one
should feel like a decision.

The consequence to keep in mind: a library sitting well below 100 % has a low bar
today, and a pull request touching one of its weaker files can still fall under
it. That is the gate working, not misfiring — the report says which assertion is
missing.

One library escapes this rule for now: `JustDummies`, whose sweep is too long to
have been calibrated against, ships with its score gate off. See
[`justdummies-mutation`](justdummies-mutation.en.md#justdummies-has-no-score-threshold-yet).

## When the survivor is an equivalent mutant

Sometimes the honest answer is that the mutant cannot be killed: the rewrite does
not change observable behaviour, so no test could tell the difference. Writing a
test to chase it would be writing a test that asserts an implementation detail —
worse than the gap.

Stryker takes that answer in the source, next to the code, as a comment:

```csharp
// Stryker disable once Statement : the trace call has no observable effect
```

The form is `// Stryker disable [once] <mutator|all> [: reason]`, with
`// Stryker restore all` to end a non-`once` block. Prefer `once`, prefer naming
the mutator rather than `all`, and always give the reason — an undocumented
exclusion is indistinguishable from a missing test six months later. Reach for it
only after deciding the mutant really is equivalent; lowering a threshold to
silence one survivor hides every future survivor with it.

## Permissions & security

`contents: read` only. The workflow checks out, builds and runs tests; it stores
no secret and needs no write scope.

## Handle with care

- **`fetch-depth: 0` is required**, not a habit. A shallow clone leaves the fork
  point unreachable and `--since` cannot resolve it.
- **`--since` wants a branch, a tag or a real commit SHA — `HEAD` is rejected.**
  `--since:HEAD` fails the whole run with *"No branch or tag or commit found with
  given target"*, which is why the workflow resolves `git merge-base` to a SHA
  first rather than passing a rev expression through.
- **The CI warning ratchet does not need disabling here.** It is a fair worry —
  Stryker compiles *mutated* source, and a mutant routinely raises a warning the
  original never had — but measured, `GITHUB_ACTIONS=true` changes nothing:
  Stryker compiles the mutants through Roslyn with its own options and does not
  inherit `TreatWarningsAsErrors` from
  [`Directory.Build.props`](../../../../Directory.Build.props). The
  compile-error count is identical with the ratchet on and off. If a future
  Stryker started honouring it, mutants would silently turn into compile errors
  instead of being tested — the count in the run log is where that would show.
- **`if: always()` on `gate` is required.** Remove it and a red matrix leg turns
  the required check green.
- **The Stryker version is pinned in the tool manifest.** Bumping it is a
  deliberate act: expect the scores to move, and re-read the thresholds.
- **The thresholds live in `build/stryker/*.json`, not in the YAML.** That is what
  keeps a local run and CI in agreement. `break` is the value that fails the
  build; `high`/`low` only colour the report.
- **The tooling and the analyzers are out of scope on purpose.** Their tests
  drive Roslyn compilations and spawn processes, so the per-mutant cost is an
  order of magnitude above the libraries', and their behaviour is already pinned
  end to end by [`analyzers`](analyzers.en.md), `gendoc-docs` and the `floor` job
  of [`ci`](ci.en.md). Adding them is a cost decision, not an oversight.
- **A survivor is not automatically a bug**, and the answer to an equivalent one
  is a `// Stryker disable once` comment with a reason, never a lowered threshold
  — see *When the survivor is an equivalent mutant* above.

## Running it locally

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/core.json
```

That is the full sweep of one library and it takes a while. To reproduce what the
gate does on a branch:

```bash
dotnet stryker --config-file build/stryker/core.json --since:$(git merge-base origin/main HEAD)
```

Reports land in `StrykerOutput/` (git-ignored); open `reports/mutation-report.html`.

## Related

- [`ci`](ci.en.md) — the primary gate, and where the warning ratchet is enforced.
- [`sonar`](sonar.en.md) — line and branch coverage. Mutation testing is the
  complement, not the replacement: Sonar tells you what was *executed*, this
  workflow tells you what was *asserted*.
- [ADR 0043 — Gate pull requests on the mutation score of what they
  changed](../adr/0043-gate-pull-requests-on-the-mutation-score-of-the-diff.md)
  — the decision this workflow implements.

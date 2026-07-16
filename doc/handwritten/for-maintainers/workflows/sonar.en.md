# `sonar` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](sonar.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/sonar.yml`](../../../../.github/workflows/sonar.yml)

## What it is for

`sonar` runs the SonarQube Cloud analysis: it feeds the **Quality Gate** and the
**coverage** metric shown on the two SonarCloud badges in the README. It is the
static-analysis-plus-coverage view of the codebase, hosted off GitHub.

## When it runs

- On every **push to `main`**.
- On every **pull request targeting `main`** — **except PRs from forks** (see
  below).
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `analyze`, on Linux:

1. Checkout with **`fetch-depth: 0`** — full history, so Sonar can attribute
   issues via `git blame` and distinguish new code from old.
2. Set up .NET **and Java 17** — the SonarScanner for .NET runs on the JVM.
3. `dotnet-sonarscanner begin` → **build** → test with coverage →
   `dotnet-sonarscanner end`.

## Permissions & security

`contents: read` only. PR decoration (the inline Sonar comments) is delivered by
the **SonarQube Cloud GitHub App**, not by this workflow's token, so no
`pull-requests: write` is needed here. The analysis authenticates with the
`SONAR_TOKEN` secret.

## Handle with care

- **The build must sit *between* `begin` and `end`.** The scanner hooks MSBuild
  to observe the compilation; it cannot analyse a pre-built or `--no-build`
  output. Do not reorder these steps or add `--no-build` to the analysis build.
- **The analysis build disables the warning ratchet on purpose.** It passes
  `-p:TreatWarningsAsErrors=false -p:MSBuildTreatWarningsAsErrors=false`. The
  scanner needs the compilation to **complete** so it can collect the
  `SonarAnalyzer` diagnostics and upload them in `end`; a Sonar-rule warning
  promoted to an error would fail the build before results are reported. The
  ratchet stays enforced by [`ci`](ci.en.md) on both OS legs — that is the gate,
  this analysis leg is not.
- **The fork guard is required, not optional.** The
  `if: … head.repo.full_name == github.repository` condition skips the analysis
  on PRs from forks, because a fork PR cannot read `SONAR_TOKEN` and would fail
  on a missing secret rather than a real problem. Branches inside this repository
  (the normal contributor flow) run normally.
- **`fetch-depth: 0` matters.** A shallow checkout would break Sonar's new-code
  detection and blame attribution.

## Related

- [`ci`](ci.en.md) — produces the same OpenCover coverage shape via the shared
  `coverage.runsettings`, and is where the warning ratchet is actually enforced.

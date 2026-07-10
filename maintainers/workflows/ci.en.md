# `ci` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](ci.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)

## What it is for

`ci` is the primary gate: it builds the whole solution and runs the entire test
suite, on **both Linux and Windows**, and collects code coverage. If a change
breaks the build or a test on either platform, this is where it shows.

The cross-platform matrix is not ceremony. The documentation generator spawns
its worker as a **separate process** and manipulates file-system paths, so the
solution genuinely behaves differently on Windows and Linux — both legs are
exercised on purpose.

## When it runs

- On every **push to `main`**.
- On every **pull request targeting `main`**.
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `build-test`, on a `[ubuntu-latest, windows-latest]` matrix:

1. Checkout, then set up the .NET SDK.
2. `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release`.
3. Tests collect OpenCover coverage via `coverlet.collector`, configured by
   [`coverage.runsettings`](../../coverage.runsettings), into
   `artifacts/coverage/<guid>/coverage.opencover.xml`.
4. The coverage report is uploaded as a per-OS artifact.

## Permissions & security

`contents: read` only — the workflow just checks out and builds. It stores no
secrets and needs no write scope.

## Handle with care

- **`fail-fast: false` is deliberate.** It forces both matrix legs to run to
  completion so a platform-specific failure is always visible; do not remove it
  to "save minutes".
- **The coverage artifact name is per-OS** (`coverage-${{ matrix.os }}`).
  Uploading two matrix legs under the same artifact name would clash — keep the
  name parameterised.
- **`if-no-files-found: error`** is intentional: a silent "no coverage produced"
  would let a broken coverage setup pass unnoticed. It should fail.
- **This is the enforcement point for the warning ratchet.** The
  `TreatWarningsAsErrors` / `MSBuildTreatWarningsAsErrors` promotion (see
  [`Directory.Build.props`](../../Directory.Build.props)) is scoped to CI and is
  *enforced here*, on both OS legs. The `sonar` workflow deliberately disables it
  for its own analysis build — so `ci`, not `sonar`, is the gate that must stay
  green on warnings.

## Related

- [`sonar`](sonar.en.md) reuses the same `coverage.runsettings` so its coverage
  report matches this one.
- [`analyzers`](analyzers.en.md) covers the analyzer dogfood that `ci` does not.

# `ci` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](ci.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/ci.yml`](../../../../.github/workflows/ci.yml)

## What it is for

`ci` is the primary gate: it builds the whole solution and runs the entire test
suite, on **both Linux and Windows**, and collects code coverage. If a change
breaks the build or a test on either platform, this is where it shows.

The cross-platform matrix is not ceremony. The documentation generator spawns
its worker as a **separate process** and manipulates file-system paths, so the
solution genuinely behaves differently on Windows and Linux — both legs are
exercised on purpose.

A second job, `floor`, proves the *other* end of the supported range: that the
`fce` documentation tool and its worker — which ship targeting `net8.0`, the
tooling floor — actually **execute on the .NET 8 runtime**, not only on the
latest one CI builds with. See
[ADR 0002 — Floor the tooling runtime](../adr/0002-floor-the-tooling-runtime.md)
for the decision; this job is its runtime enforcement.

## When it runs

- On every **push to `main`**.
- On every **pull request targeting `main`**.
- On demand via **`workflow_dispatch`**.

## How it runs

### `build-test` — the whole solution on the latest .NET

On a `[ubuntu-latest, windows-latest]` matrix:

1. Checkout, then set up the .NET SDK.
2. `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release`.
3. Tests collect OpenCover coverage via `coverlet.collector`, configured by
   [`coverage.runsettings`](../../../../coverage.runsettings), into
   `artifacts/coverage/<guid>/coverage.opencover.xml`.
4. The coverage report is uploaded as a per-OS artifact.

### `floor` — the tooling on its minimum runtime

`build-test` runs on the latest .NET; `floor` runs the *shipped* tooling on the
oldest supported one. On `ubuntu-latest`:

1. Set up **two SDKs**: `10.0.x` (satisfies the repo `global.json`, so the build
   runs normally) and `8.0.x` (brings the **.NET 8 runtime**, the floor the
   tooling ships against).
2. Build the net8 `fce` tool with its worker, and a **net8 build of the `Usage`
   sample** as a real target to document. (`Usage` is multi-targeted
   `net8.0;net10.0` precisely so this job has a floor target.)
3. Run `fce generate` against that target with `DOTNET_ROLL_FORWARD=LatestPatch`
   in the environment, then assert the generated catalog actually contains a
   documented error — positive proof, not just exit 0.

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
  [`Directory.Build.props`](../../../../Directory.Build.props)) is scoped to CI and is
  *enforced here*, on both OS legs. The `sonar` workflow deliberately disables it
  for its own analysis build — so `ci`, not `sonar`, is the gate that must stay
  green on warnings.
- **`DOTNET_ROLL_FORWARD=LatestPatch` on the `floor` run is load-bearing.** It
  overrides the roll-forward baked into each runtimeconfig (`Major` on the CLI,
  `LatestMajor` on the worker) and stays within the requested major, so both
  processes bind the highest **.NET 8** patch present and can never roll onto the
  .NET 10 the runner also carries. Drop it and the tooling would silently run on
  .NET 10 — and the job would prove nothing about the floor.
- **The `floor` job greps the generated catalog for a documented error, not just
  exit 0.** A tool that started but loaded nothing would exit clean; requiring an
  extracted error proves the worker actually loaded the net8 target via
  `Assembly.LoadFrom`.
- **`Usage` must keep a `net8.0` target.** The job documents the net8 build of
  `Usage`; if `Usage` dropped net8 from its `<TargetFrameworks>`, the job would
  have no floor target to document.

## Related

- [`sonar`](sonar.en.md) reuses the same `coverage.runsettings` so its coverage
  report matches this one.
- [`analyzers`](analyzers.en.md) covers the analyzer dogfood that `ci` does not.
- [`canary.yml`](../../../../.github/workflows/canary.yml) *(no reference page yet)*
  runs the same net8 tooling on the next .NET **preview**, weekly, to catch a
  roll-forward regression onto a not-yet-released major before it ships — the one
  surface the `floor` job cannot cover (see ADR 0002).
- [ADR 0002 — Floor the tooling runtime](../adr/0002-floor-the-tooling-runtime.md)
  — the decision the `floor` job enforces.

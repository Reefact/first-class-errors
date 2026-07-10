# `analyzers` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](analyzers.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/analyzers.yml`](../../.github/workflows/analyzers.yml)

## What it is for

The library ships Roslyn analyzers (`FCExxx`) **bundled inside the NuGet
package**. This workflow proves two things that ordinary CI does not:

1. **Dogfood** — the analyzers actually run and flag nothing they shouldn't when
   built against the sample project that references them.
2. **Floor check** — the *shipped* analyzer, packed exactly as a consumer
   receives it, **loads and runs under the oldest supported compiler** (Roslyn
   4.8.0 == .NET 8 SDK / Visual Studio 2022 17.8).

The floor check exists because a bundled analyzer compiled against a newer
Roslyn fails to load (`CS8032`) on older SDKs/IDEs — silently degrading the
product for those users. See
[ADR 0001 — Lock the analyzer Roslyn floor](../adr/0001-lock-the-analyzer-roslyn-floor.md)
for the full rationale; this workflow is its CI enforcement.

## When it runs

- On every **push to `main`**, **pull request targeting `main`**, and on demand
  via **`workflow_dispatch`**.

## How it runs

Two jobs:

- **`dogfood`** — builds `FirstClassErrors.Usage` with the analyzers wired in
  (`OutputItemType=Analyzer`); fails on any `Error`-severity `FCExxx`
  diagnostic. (The analyzer *unit tests* run inside [`ci`](ci.en.md), which
  builds and tests the whole solution.)
- **`floor`** — the real contract test:
  1. Set up **two SDKs**: the release SDK (10.0.x, the one `release` packs with)
     and the floor SDK (8.0.100, Roslyn 4.8).
  2. **Pack** `FirstClassErrors` under the *release* SDK — the exact artifact a
     consumer restores.
  3. **Consume** that package from `tools/floor-check/`, whose nested
     `global.json` pins the build to the *floor* SDK.
  4. **Prove the analyzer loaded** by grepping the `ReportAnalyzer` log for a
     fully-qualified analyzer *type*.

## Permissions & security

`contents: read` only — it builds and packs locally, publishes nothing.

## Handle with care

This job is dense because each line closes a specific hole. Before editing it,
read the comments in the YAML — they are the source of truth. The traps:

- **The pack runs under the release SDK, the consume under the floor SDK.**
  Packing under the floor SDK would test an analyzer nobody ships and would pin
  the library to C# 12. The split is the whole point.
- **`FLOORCHECK_VERSION` carries a `run_number.run_attempt` suffix** so every run
  produces a version NuGet has never cached, forcing the consume step to restore
  *this run's* freshly packed `.nupkg` instead of a stale copy. `FloorCheck.csproj`
  pins that **exact** version (not a float), so it can never silently resolve a
  future stable `FirstClassErrors` from nuget.org.
- **SDK selection is CWD-based.** The consume step picks the floor SDK *because*
  it runs from `tools/floor-check/` with a nested `global.json` (`rollForward:
  disable`). Move the step out of that directory and it silently builds on the
  wrong SDK.
- **`ReportAnalyzer=true` + `-v detailed` + `--no-incremental` are all
  required** to make Roslyn's per-analyzer table reach the log. Drop any one and
  the "prove it loaded" grep has nothing to match.
- **The grep matches an analyzer *type* (`…SomethingAnalyzer`), not the assembly
  name.** The assembly name appears in ordinary build lines even when the
  analyzer never loaded; only the type appears in the `ReportAnalyzer` table.
  A never-loaded analyzer would otherwise leave the build green.

**To raise the floor deliberately**, follow the procedure in ADR 0001 — it is a
product decision, not a routine bump (which is why Dependabot is configured to
ignore `Microsoft.CodeAnalysis.*`).

## Related

- [ADR 0001 — Lock the analyzer Roslyn floor](../adr/0001-lock-the-analyzer-roslyn-floor.md)
- [`ci`](ci.en.md) — runs the analyzer unit-test suite as part of the full solution.

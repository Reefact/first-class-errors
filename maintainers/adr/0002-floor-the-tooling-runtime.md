# ADR-0002 | Floor the tooling runtime at the oldest supported LTS

**Status:** Accepted
**Date:** 2026-07-13
**Decision Makers:** Reefact

## Context

FirstClassErrors ships two very different kinds of artifact:

* the **library** (`FirstClassErrors`, `FirstClassErrors.Testing`) targets
  **`netstandard2.0`**. A netstandard library is consumed by *any* runtime that
  implements the standard — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5–10+,
  Mono/Unity — so the "runs almost everywhere" question is already answered,
  once, by the TFM, and needs nothing here.
* the **tooling** (`FirstClassErrors.Cli` — the `fce` .NET tool — plus
  `FirstClassErrors.GenDoc` and `FirstClassErrors.GenDoc.Worker`, which it
  loads in-process and spawns as a child) is a **runnable framework-dependent
  app**. Its TFM is a **hard minimum**: a framework-dependent app can never run
  on a runtime **older** than its TFM, and **roll-forward only ever goes up,
  never down**.

The tooling TFM therefore decides *which consumers can run `fce` at all*. It
was `net10.0`, which meant a shop whose newest installed runtime is .NET 8
could reference the library but **could not run the documentation generator** —
even though the library it documents is `netstandard2.0`.

There is a second, subtler constraint. The worker loads the **target** assembly
via `Assembly.LoadFrom` (see `FirstClassErrors.GenDoc.Worker/Program.cs`). That
target can be built for any runtime the consumer chose, so the worker
**process** must run on a runtime `>=` the target's. This is a *roll-forward*
problem, not a *TFM-count* problem, and the two are easy to conflate.

Roll-forward policies behave as follows: the default `Minor` policy never
crosses a major; `Major` rolls up to the next major only when the requested
major is *absent*; `LatestMajor` always binds the **highest installed** major.

.NET 8 is the oldest .NET still in Microsoft support (its EOL date is
2026-11-10), and it is the floor the analyzer already states:
[ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) pins Roslyn 4.8, the
compiler of the .NET 8.0.100 SDK.

CI builds on the latest released .NET SDK (currently .NET 10), and GitHub
runners carry several runtimes side by side.

## Decision

The tooling (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`,
`FirstClassErrors.GenDoc.Worker`) single-targets **`net8.0`** — the oldest
.NET still in Microsoft support — and covers every newer runtime with
roll-forward, not a target matrix.

## Rationale

The floor makes the support story one sentence: *FirstClassErrors supports
.NET 8 and up for its tooling and its analyzer; the library itself is
`netstandard2.0` and runs down to .NET Framework 4.6.1.* The tooling floor and
the analyzer floor (ADR-0001) state the **same** minimum, so the product
states **one** support number.

Roll-forward covers every newer runtime, tuned per process:

| Project | `RollForward` | Why |
|---|---|---|
| `FirstClassErrors.Cli` (`fce`) | `Major` | The front-end only needs to *run*. `Major` rolls the net8 build up to the next major when .NET 8 is absent, so a machine that has only .NET 10 runs it (rolls 8→10). Without it, the default `Minor` policy never crosses a major and `fce` would fail to start on the common "newer .NET, no .NET 8" machine. |
| `FirstClassErrors.GenDoc.Worker` | `LatestMajor` | The worker must **out-rank the target it loads**. `Major` only rolls up when the requested major is *absent*, so on a machine carrying **both** .NET 8 and .NET 10 a net8 worker would bind to 8 and fail to load a net10 target. `LatestMajor` always binds the **highest installed** major, so the worker can document a target built for any runtime present. |
| `FirstClassErrors.GenDoc` | — | Loaded in-process by `fce`; the runtime is chosen by the CLI's runtimeconfig, so a library sets no policy. |

`<LangVersion>latest</LangVersion>` stays on the three projects so the net8
floor bounds only the **BCL surface and target runtime**, not the C# the
source may use (the `netstandard2.0` projects already do exactly this).

This design also avoids a per-release treadmill: a new .NET release (net11,
net12, …) requires **no rebuild, no code change, no re-release** — roll-forward
runs the existing `net8.0` binaries on it — and a runtime *above* the floor
reaching EOL requires nothing, because we do not target it. Only the floor LTS
itself reaching EOL calls for a bump, one line per project, on a roughly
biennial cadence (see Follow-up Actions).

### Two guards: the TFM at build time, a CI job at run time

The floor is guarded on both axes it can regress on:

* **API surface, at build time — the TFM itself.** Because the projects
  *target* `net8.0`, every CI build (on the .NET 10 SDK) compiles them against
  the **net8 reference pack**, so a `net10`-only API cannot slip in silently —
  it breaks the ordinary build. No dedicated job is needed for this, unlike the
  analyzer's Roslyn floor, which is invisible on a modern CI and needs
  `tools/floor-check` (ADR-0001).
* **Runtime execution, at run time — the `floor` job in `ci.yml`.**
  *Documentation tooling on the .NET 8 floor* builds the net8 tooling and a
  net8 build of the `Usage` sample, then runs `fce generate` against it with
  `DOTNET_ROLL_FORWARD=LatestPatch` in the environment. That override wins over
  each runtimeconfig's baked-in policy (`Major` / `LatestMajor`) and stays
  within the requested major, so both the CLI and the worker bind the highest
  **.NET 8** patch present and can never roll onto the newer runtime the
  runner also carries. A green step therefore means the shipped net8 tooling
  genuinely executed on the .NET 8 runtime; were .NET 8 absent, the host would
  fail to start. This is the runtime counterpart of the analyzer floor-check,
  and it is why `Usage` is multi-targeted `net8.0;net10.0` — the net8 build
  gives the job a real target to document.

The one surface neither guard covers is roll-forward onto a
**not-yet-released** major. That is the **`canary.yml`** workflow: on a weekly
schedule (and on demand) it installs the next .NET **preview**, then runs the
net8 tooling on it with `DOTNET_ROLL_FORWARD=LatestMajor` and
`DOTNET_ROLL_FORWARD_TO_PRERELEASE=1` so both processes bind that prerelease
major. It is deliberately **not** a pull-request gate — a preview is often
unpublished or unstable, and that is not this repo's bug — so a missing
preview ends the run neutral, while a genuine roll-forward regression turns
the scheduled run red and notifies the maintainer before that major ships.

## Alternatives Considered

### Keep the tooling on `net10.0` (status quo)

Considered because it was the existing state: targeting the latest runtime is
the path of least resistance and needs no roll-forward tuning.

Rejected because the TFM is a hard minimum for a framework-dependent app: a
shop whose newest installed runtime is .NET 8 could reference the
`netstandard2.0` library but could not run the documentation generator that
documents it.

### Multi-target the tooling (`net8.0;net10.0`)

Considered as the conventional way to serve several runtimes at once.

Rejected because:

* roll-forward already lets a single `net8.0` build run on 8 / 9 / 10 / 11+,
  so a second TFM buys reach we already have;
* a documentation generator has no need for `net10`-only BCL APIs;
* a matrix puts the tooling on a per-release "add at the top, drop at the
  bottom" cadence, and **re-introduces the worker trap**: the low build in the
  matrix is exactly the one that cannot load a higher-TFM target.

One floor build + the two roll-forward settings is strictly simpler and has
the same reach.

## Consequences

### Positive

* Any consumer on **.NET 8 or newer** can run `fce`, not just those on the
  latest runtime.
* **One** shipped tooling artifact; no per-release TFM matrix to maintain.
* The tooling floor and the analyzer floor state the **same** minimum (.NET 8),
  so the support story is a single sentence.
* Verified end to end: a `net8.0` `fce` documents a **`net10`** target assembly
  on a machine that has **only** the .NET 10 runtime — `fce` rolls 8→10
  (`Major`) and the worker binds the highest major (`LatestMajor`) to load the
  net10 target.
* Guarded in CI across the whole range: `build-test` runs the suite on the
  latest released .NET (10); the `floor` job runs the shipped tooling on the
  .NET 8 runtime; and `canary.yml` runs it on the next .NET preview (see the
  two-guards section).
* No code churn from .NET version movement; at most a one-line TFM bump about
  once every two years.

### Negative

* `fce` cannot run on a machine whose newest runtime predates .NET 8 (e.g. an
  EOL .NET 6/7, or .NET-Framework-only). Accepted: those consumers still
  **use** the `netstandard2.0` library in their app; running a dev/CI *tool*
  on a currently-supported runtime is a reasonable prerequisite (a modern .NET
  SDK is already present wherever modern .NET is built).

### Risks

* `LatestMajor` on the worker will, on a box that has a **preview** of the
  next major installed, bind that preview. This is only a risk for machines
  that opt into previews, and `canary.yml` is exactly the early warning that
  this binding still works before that major ships.
* A roll-forward regression against a not-yet-released major is caught by the
  weekly canary, not by a pull-request gate — by design, since a preview may
  be unpublished or unstable.

## Follow-up Actions

* **When the floor LTS reaches EOL** (.NET 8 → 2026-11-10; hygiene rather than
  function — roll-forward keeps the net8 build working on newer runtimes after
  EOL — on a roughly biennial cadence):
  1. Change `<TargetFramework>` from `net8.0` to the new floor in the three
     tooling csprojs: `FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`,
     `FirstClassErrors.GenDoc.Worker`. Leave the `RollForward` settings as
     they are.
  2. Bump the new floor in the `Usage` sample's `<TargetFrameworks>` (so the
     CI floor job still has a target on the new floor) and in the `floor` job
     of `ci.yml` (`dotnet-version` `8.0.x` → the new floor's runtime).
  3. Update the runtime note in `FirstClassErrors.Cli/README.nuget.md`.
  4. Supersede this ADR (new floor, new minimum runtime).
  5. Optionally drop the `<LangVersion>latest</LangVersion>` overrides if the
     new floor's default C# is already the version you want.

  Keeping this in step with the analyzer floor (ADR-0001) keeps the product's
  single ".NET N and up" support statement true.
* **When the canary's preview major reaches GA:** `canary.yml` pins the
  preview major it targets (`dotnet-version: 11.0.x`, quality `preview`); bump
  it to the next one (`12.0.x`, …) so the canary keeps looking one release
  ahead. Nothing breaks if you forget: `build-test` picks up the newly
  released major as "latest", and the canary simply stops finding a
  newer-than-build-SDK preview and ends its runs neutral until bumped.

## References

* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — the analyzer's Roslyn
  floor, the build-time sibling of this run-time decision.
* [`ci` workflow reference](../workflows/ci.en.md) — the `floor` job,
  structurally.
* `FirstClassErrors.GenDoc.Worker/Program.cs` — the `Assembly.LoadFrom` call
  behind the worker's roll-forward constraint.

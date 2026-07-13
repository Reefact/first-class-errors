# 2. Floor the tooling runtime at the oldest supported LTS

- **Status:** Accepted
- **Deciders:** Reefact
- **Related:** ADR 0001 (the analyzer's Roslyn floor — the build-time sibling of this runtime-time decision)

## Context

FirstClassErrors ships two very different kinds of artifact:

- the **library** (`FirstClassErrors`, `FirstClassErrors.Testing`) targets
  **`netstandard2.0`**. A netstandard library is consumed by *any* runtime that
  implements the standard — .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5–10+,
  Mono/Unity — so the "runs almost everywhere" question is already answered, once,
  by the TFM, and needs nothing here.
- the **tooling** (`FirstClassErrors.Cli` — the `fce` .NET tool — plus
  `FirstClassErrors.GenDoc` and `FirstClassErrors.GenDoc.Worker`, which it loads
  in-process and spawns as a child) is a **runnable framework-dependent app**. Its
  TFM is a **hard minimum**: a framework-dependent app can never run on a runtime
  **older** than its TFM, and **roll-forward only ever goes up, never down**.

The tooling TFM therefore decides *which consumers can run `fce` at all*. It was
`net10.0`, which meant a shop whose newest installed runtime is .NET 8 could
reference the library but **could not run the documentation generator** — even
though the library it documents is `netstandard2.0`.

There is a second, subtler constraint. The worker loads the **target** assembly
via `Assembly.LoadFrom` (see `FirstClassErrors.GenDoc.Worker/Program.cs`). That
target can be built for any runtime the consumer chose, so the worker **process**
must run on a runtime `>=` the target's. This is a *roll-forward* problem, not a
*TFM-count* problem, and the two are easy to conflate.

## Decision

**Single-target the tooling at `net8.0`** — the oldest .NET still in Microsoft
support, and the **same floor as the analyzer** (ADR 0001 pins Roslyn 4.8 == the
.NET 8.0.100 SDK). The product now states **one** support number: *FirstClassErrors
supports .NET 8 and up for its tooling and its analyzer; the library itself is
`netstandard2.0` and runs down to .NET Framework 4.6.1.*

Cover every **newer** runtime with **roll-forward**, not a target matrix:

| Project | `RollForward` | Why |
|---|---|---|
| `FirstClassErrors.Cli` (`fce`) | `Major` | The front-end only needs to *run*. `Major` rolls the net8 build up to the next major when .NET 8 is absent, so a machine that has only .NET 10 runs it (rolls 8→10). Without it, the default `Minor` policy never crosses a major and `fce` would fail to start on the common "newer .NET, no .NET 8" machine. |
| `FirstClassErrors.GenDoc.Worker` | `LatestMajor` | The worker must **out-rank the target it loads**. `Major` only rolls up when the requested major is *absent*, so on a machine carrying **both** .NET 8 and .NET 10 a net8 worker would bind to 8 and fail to load a net10 target. `LatestMajor` always binds the **highest installed** major, so the worker can document a target built for any runtime present. |
| `FirstClassErrors.GenDoc` | — | Loaded in-process by `fce`; the runtime is chosen by the CLI's runtimeconfig, so a library sets no policy. |

Keep `<LangVersion>latest</LangVersion>` on the three projects so the net8 floor
bounds only the **BCL surface and target runtime**, not the C# the source may use
(the `netstandard2.0` projects already do exactly this).

### Why not multi-target (`net8.0;net10.0`)?

- Roll-forward already lets a single `net8.0` build run on 8 / 9 / 10 / 11+, so a
  second TFM buys reach we already have.
- A documentation generator has no need for `net10`-only BCL APIs.
- A matrix puts the tooling on a per-release "add at the top, drop at the bottom"
  cadence, and **re-introduces the worker trap**: the low build in the matrix is
  exactly the one that cannot load a higher-TFM target.

One floor build + the two roll-forward settings above is strictly simpler and has
the same reach.

### Two guards: the TFM at build time, a CI job at run time

The floor is guarded on both axes it can regress on:

- **API surface, at build time — the TFM itself.** Because the projects *target*
  `net8.0`, every CI build (on the .NET 10 SDK) compiles them against the **net8
  reference pack**, so a `net10`-only API cannot slip in silently — it breaks the
  ordinary build. No dedicated job is needed for this, unlike the analyzer's Roslyn
  floor, which is invisible on a modern CI and needs `tools/floor-check` (ADR 0001).
- **Runtime execution, at run time — the `floor` job in `ci.yml`.** *Documentation
  tooling on the .NET 8 floor* builds the net8 tooling and a net8 build of the
  `Usage` sample, then runs `fce generate` against it with
  `DOTNET_ROLL_FORWARD=LatestPatch` in the environment. That override wins over each
  runtimeconfig's baked-in policy (`Major` / `LatestMajor`) and stays within the
  requested major, so both the CLI and the worker bind the highest **.NET 8** patch
  present and can never roll onto the newer runtime the runner also carries. A green
  step therefore means the shipped net8 tooling genuinely executed on the .NET 8
  runtime; were .NET 8 absent, the host would fail to start. This is the runtime
  counterpart of the analyzer floor-check, and it is why `Usage` is multi-targeted
  `net8.0;net10.0` — the net8 build gives the job a real target to document.

The one surface neither guard covers is roll-forward onto a **not-yet-released**
major: a *preview-runtime canary* (run the tooling on an SDK-preview image) is the
optional hardening for that, left as a follow-up rather than standing infrastructure.

## Consequences

**Positive**

- Any consumer on **.NET 8 or newer** can run `fce`, not just those on the latest
  runtime.
- **One** shipped tooling artifact; no per-release TFM matrix to maintain.
- The tooling floor and the analyzer floor state the **same** minimum (.NET 8),
  so the support story is a single sentence.
- Verified end to end: a `net8.0` `fce` documents a **`net10`** target assembly on
  a machine that has **only** the .NET 10 runtime — `fce` rolls 8→10 (`Major`) and
  the worker binds the highest major (`LatestMajor`) to load the net10 target.
- Guarded in CI on **both** ends of the range: `build-test` runs the whole suite on
  the latest .NET (10); the `floor` job runs the shipped tooling on the .NET 8
  runtime (see the two-guards section above).

**Negative / accepted costs**

- `fce` cannot run on a machine whose newest runtime predates .NET 8 (e.g. an
  EOL .NET 6/7, or .NET-Framework-only). Accepted: those consumers still **use**
  the `netstandard2.0` library in their app; running a dev/CI *tool* on a
  currently-supported runtime is a reasonable prerequisite (a modern .NET SDK is
  already present wherever modern .NET is built).
- `LatestMajor` on the worker will, on a box that has a **preview** of the next
  major installed, bind that preview. This is only a risk for machines that opt
  into previews; the canary above is the mitigation if we want it.

## Do I have to revise code at each new .NET release, or at each EOL?

Almost never — this design is chosen precisely to avoid a per-release treadmill:

- **A new .NET release (net11, net12, …):** nothing to do. Roll-forward runs the
  existing `net8.0` binaries on it — **no rebuild, no code change, no re-release**.
- **A runtime *above* the floor reaching EOL:** nothing to do; we do not target it.
- **The floor LTS itself reaching EOL** (`.NET 8` → **2026-11-10**): bump the floor
  **one line per project** (`net8.0` → `net10.0`), **no logic change**. Even this is
  optional for *function* — roll-forward keeps the net8 build working on newer
  runtimes after EOL — it is **hygiene** (stop advertising an unpatched floor), on a
  roughly **biennial** cadence.

So: no code churn from .NET version movement; at most a one-line TFM bump about
once every two years.

## How to raise the floor (when .NET 8 goes EOL)

1. Change `<TargetFramework>` from `net8.0` to the new floor in the three tooling
   csprojs: `FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`,
   `FirstClassErrors.GenDoc.Worker`. Leave the `RollForward` settings as they are.
2. Bump the new floor in the `Usage` sample's `<TargetFrameworks>` (so the CI floor
   job still has a target on the new floor) and in the `floor` job of `ci.yml`
   (`dotnet-version` `8.0.x` → the new floor's runtime).
3. Update the runtime note in `FirstClassErrors.Cli/README.nuget.md`.
4. Update this ADR (new floor, new minimum runtime).
5. Optionally drop the `<LangVersion>latest</LangVersion>` overrides if the new
   floor's default C# is already the version you want.

Keeping this in step with the analyzer floor (ADR 0001) keeps the product's single
".NET N and up" support statement true.

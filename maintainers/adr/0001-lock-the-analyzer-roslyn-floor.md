# 1. Lock the analyzer's Roslyn floor

- **Status:** Accepted
- **Deciders:** Reefact
- **Shaped by:** #69 (initial lock), #74 / #75 / #77 (floor-check hardening)

## Context

`FirstClassErrors.Analyzers` is a Roslyn analyzer that ships **bundled inside the
`FirstClassErrors` NuGet package** (at `analyzers/dotnet/cs/`), so that consumers
who reference the package get the `FCExxx` diagnostics automatically, with no
extra install.

A bundled analyzer is loaded by **each consumer's host compiler** — the Roslyn
that comes with their .NET SDK or IDE. The `Microsoft.CodeAnalysis.*` version the
analyzer is *compiled against* therefore becomes the **minimum** Roslyn able to
load it:

- if the analyzer references a Roslyn **newer** than the host, the host refuses to
  load it and emits **`CS8032`** (and the analyzer silently does nothing);
- if the analyzer throws while loading, the host emits **`AD0001`**.

A routine dependency bump of `Microsoft.CodeAnalysis.*` is therefore **not routine
maintenance**: it silently raises the minimum SDK/IDE every consumer must have.
This exact regression happened once (the analyzer drifted to requiring Roslyn 5.6),
which is what prompted this decision.

The load contract is invisible on modern toolchains — CI on the latest SDK, and
the maintainer's own IDE, both satisfy any floor — so it can regress without any
red signal. It needs a guard that fails **loudly**, on the **oldest** host we
claim to support, against the **exact artifact we ship**.

## Decision

**Fix the analyzer's Roslyn floor at `4.8.0`** — the Roslyn that ships with the
**.NET 8.0.100 SDK / Visual Studio 2022 17.8**, the oldest host FirstClassErrors
supports — and protect that contract with **defense in depth**: one single source
of truth, and three independent guards, two of which fail loudly.

### Single source of truth

The floor is declared **once**, in `Directory.Build.props`:

```xml
<RoslynFloorVersion>4.8.0</RoslynFloorVersion>
```

Everything else references `$(RoslynFloorVersion)`, so the pin, the test and the
CI job can never disagree.

### Guard 1 — the pin (`FirstClassErrors.Analyzers.csproj`)

`Microsoft.CodeAnalysis.CSharp` is pinned to `$(RoslynFloorVersion)` with
`PrivateAssets="all"`, and the floor is surfaced through assembly metadata so the
test can read it back:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynFloorVersion)" PrivateAssets="all" />
<AssemblyMetadata Include="RoslynFloorVersion" Value="$(RoslynFloorVersion)" />
```

`Microsoft.CodeAnalysis.Analyzers` (5.6.0) is deliberately **not** part of the
floor: it is a build-time authoring analyzer (`PrivateAssets="all"`), not a
runtime reference of the shipped assembly, so it does not affect load.

### Guard 2 — the unit test (`RoslynFloorTests`, fails loudly)

`Analyzer_stays_on_the_supported_Roslyn_floor` reads the floor from the analyzer
assembly's `RoslynFloorVersion` metadata and asserts that **no** referenced
`Microsoft.CodeAnalysis*` assembly exceeds it. Key design points:

- it bounds the **whole `Microsoft.CodeAnalysis*` family** (via `StartsWith`), not
  a single assembly name, because the analyzers use only the language-agnostic
  `IOperation` API and the compiler records a reference to `Microsoft.CodeAnalysis`
  but not necessarily to `Microsoft.CodeAnalysis.CSharp`;
- it **fails if the family disappears** from the metadata (`Check.That(...).Not.IsEmpty()`),
  rather than passing vacuously;
- it compares on **major.minor.build** only, so a 4-part reference version
  (`4.8.0.0`) does not read as newer than the `4.8.0` floor.

This guard is fast, in-process, and runs in the normal `dotnet test`. It catches
the *reference* version drift — but not whether the analyzer actually **loads** on
an old host, nor whether it is actually **shipped** in the package.

### Guard 3 — the floor-check CI job (`analyzers.yml`, fails loudly)

The `Dogfood analyzers on the Roslyn floor` job proves, end to end, that the
**artifact as shipped** loads and runs on the **oldest supported compiler**. See
the dedicated section below; this is the guard that closes the gap the unit test
cannot.

### Guard 4 — Dependabot ignore (`.github/dependabot.yml`, silent by design)

`Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Common` are on
Dependabot's `ignore` list, so an automated PR never proposes raising the floor.
Bumping it is a conscious act (edit `RoslynFloorVersion`, accept the new minimum
SDK/IDE, update this ADR and the README's compiler-requirement note).

## The floor-check job design

The job is deliberately **not** part of `FirstClassErrors.sln`; `ci.yml` would
build it under the .NET 10 SDK, which proves nothing. It lives at
`tools/floor-check/` and is built solely by this job. Several subtleties took four
PRs to get right; each is recorded here so it is not "simplified" back into a bug.

### Two SDKs, split on purpose

The job installs **both** `10.0.x` and `8.0.100`:

1. **Pack under the release SDK.** `dotnet pack FirstClassErrors/…` is run **from
   the repo root**, so the root `global.json` selects .NET 10 — the same SDK
   `release.yml` packs with. This produces the **exact bytes a consumer receives**,
   with the analyzer bundled at `analyzers/dotnet/cs/`. Packing under the floor SDK
   instead would test an analyzer nobody ships, and would pin the whole library to
   C# 12 (`LangVersion=latest` under SDK 8).
2. **Consume under the floor SDK.** `dotnet build` is run **from
   `tools/floor-check/`**, whose nested `global.json` pins `8.0.100` with
   `rollForward: disable`. SDK resolution is **CWD-based**, so running from this
   directory is what selects the floor SDK. This is the real test: *the shipped
   analyzer, loaded by the oldest supported host (Roslyn 4.8).*

`FloorCheck.csproj` recompiles the real `FirstClassErrors.Usage` sources with the
analyzer wired in, and escalates `CS8032`/`AD0001` to errors — so a load failure
turns the build red.

### Proving the analyzer actually loaded

A never-loaded analyzer would leave the build green (CS8032 is emitted only when a
load is *attempted*). The job builds with `-p:ReportAnalyzer=true -v detailed`
(the per-analyzer table is silent at default verbosity) and `--no-incremental`
(to force a real compilation), then greps for a fully-qualified analyzer **type**
(`FirstClassErrors.Analyzers.<Name>Analyzer`) — which appears **only** in the
ReportAnalyzer table, i.e. only if the analyzer really ran. Matching the assembly
name alone would be a false positive (it appears in ordinary build lines).

### Consuming the package, not a ProjectReference

`FloorCheck` references the **packed `.nupkg`** from a local feed, not the analyzer
project. This makes one job validate two guarantees at once: the analyzer loads on
the floor **and** it actually ships where consumers expect it — a broken
`analyzers/dotnet/cs` path would leave the analyzer silently absent, and the grep
would fail. Getting this right required closing several NuGet traps:

- **Exact version pin, not a float.** The version is `$(FloorCheckVersion)`, passed
  identically to the pack (`-p:Version`) and the consume (`-p:FloorCheckVersion`).
  A floating `1.0.0-floorcheck-*` would, once a stable `FirstClassErrors 1.0.0` is
  published, resolve to that stable release (NuGet ranks a stable version above any
  prerelease sharing its root) and dogfood a published package instead of the one
  under test.
- **`packageSourceMapping`** routes the `FirstClassErrors` id **exclusively** to the
  local feed, so nuget.org can never substitute a published package for the one just
  packed. (nuget.org stays enabled for the net8.0 targeting packs, which otherwise
  fail restore with `NU1101`.)
- **Fresh per-run version.** `1.0.0-floorcheck.<run_number>.<run_attempt>` — a
  version NuGet has never cached, so restore always reads the freshly packed
  `.nupkg`. `run_attempt` covers re-runs; dot separators keep the numeric
  identifiers ordered numerically per SemVer.
- **`RestorePackagesPath=./packages`** keeps the throwaway package out of the
  machine-global `~/.nuget/packages` (which is keyed by `(id, version)` and never
  re-reads a feed for a version it already extracted — a stale-copy trap on the
  fixed local-dev version). The pack step wipes `local-feed/` and `packages/` so a
  reused workspace stays idempotent.
- **`DefaultItemExcludes;packages/**`** stops the SDK's default globs from compiling
  any `.cs` a restored package might carry (contentFiles, polyfills, source
  generators) now that `packages/` lives under the project directory.

## Consequences

**Positive**

- The load contract cannot silently regress: a too-new Roslyn reference fails the
  unit test (fast) **and** the floor-check job (authentic), and a broken packaging
  path fails the floor-check job.
- The floor-check job tests the *shipped artifact* on the *oldest supported host*,
  not a proxy.
- The floor is a one-line, self-documenting decision (`RoslynFloorVersion`).

**Negative / accepted costs**

- Two extra guards to keep green, and a non-solution `tools/floor-check/` project
  with intentionally intricate NuGet configuration (documented inline and here).
- The floor-check job downloads the 8.0.100 SDK on every run (~a few seconds).
- Raising the floor is a deliberate, multi-step act (by design).

## How to raise the floor (when we ever do)

1. Bump `<RoslynFloorVersion>` in `Directory.Build.props`.
2. Update the floor SDK in `analyzers.yml` (`dotnet-version` and the nested
   `tools/floor-check/global.json`) to the SDK whose Roslyn matches the new floor.
3. Update the README / `doc/README.fr.md` compiler-requirement note.
4. Update this ADR (new floor, new minimum SDK/IDE).

The pin, the unit test and the Dependabot ignore need no change — they all track
`$(RoslynFloorVersion)` or the package ids.

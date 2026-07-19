# Platform compatibility specification

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](platform-compatibility.fr.md)

This page is the mutable reference for the compatibility boundaries decided by
[ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.md),
[ADR-0002](../adr/0002-floor-the-tooling-runtime.md), and
[ADR-0022](../adr/0022-floor-the-library-on-net-framework-4-7-2.md).

## Supported boundaries

| Artifact | Build target / floor | Supported host boundary |
|---|---|---|
| `FirstClassErrors`, `FirstClassErrors.Testing`, `FirstClassErrors.RequestBinder` | `netstandard2.0` | .NET Framework 4.7.2 or later; compatible modern .NET implementations of .NET Standard 2.0 |
| `FirstClassErrors.Analyzers` | `netstandard2.0`, compiled against Roslyn 4.8.0 | .NET 8.0.100 SDK / Visual Studio 2022 17.8 or later compatible Roslyn hosts |
| `FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`, `FirstClassErrors.GenDoc.Worker` | `net8.0` | .NET 8 or later through roll-forward |

The .NET Standard specification names .NET Framework 4.6.1 as a theoretical
consumer, but this project supports **4.7.2+** because 4.7.2 is the first version
with the required facades in-box and is the lowest framework exercised by the
current xUnit v3 test stack.

## Analyzer floor implementation

The analyzer is shipped inside the main NuGet package under
`analyzers/dotnet/cs/`, so the consumer's compiler loads it. The implementation
must preserve all of these properties:

1. `RoslynFloorVersion` in `Directory.Build.props` is the single version source.
2. Analyzer project references compile against that floor.
3. `RoslynFloorTests` rejects any referenced `Microsoft.CodeAnalysis*` assembly
   above the floor.
4. The analyzer floor job packs the real NuGet artifact and loads the packaged
   analyzer under the floor SDK, proving both loadability and package placement.
5. Dependabot must not raise the Roslyn runtime references as a routine update.

The exact job wiring and NuGet isolation details live in the
[`analyzers` workflow reference](../workflows/analyzers.en.md).

## Tooling runtime implementation

The three tooling projects single-target `net8.0`. Their runtime behaviour is:

| Project | Roll-forward policy | Contract |
|---|---|---|
| `FirstClassErrors.Cli` | `Major` | Runs on the next installed major when .NET 8 is absent. |
| `FirstClassErrors.GenDoc.Worker` | `LatestMajor` | Runs on the highest installed major so it can load a target assembly built for that installed runtime. |
| `FirstClassErrors.GenDoc` | none | Loaded in-process; the CLI runtime configuration controls execution. |

The ordinary build guards the `net8.0` API surface. The `floor` job executes the
shipped tooling on .NET 8, while `canary.yml` exercises roll-forward on the next
preview runtime. Exact commands and runtime overrides live in the
[`ci` workflow reference](../workflows/ci.en.md).

## .NET Framework floor implementation

The `framework-floor` job runs the library test surface on Windows under
`net472`. Projects opt into that leg through `build/Net472TestFloor.props` and
`EnableNet472Floor`; the normal local and CI build remains on its ordinary target.
The job deliberately excludes tests whose fixtures require APIs unavailable on
.NET Framework, while still exercising each shipped library through an applicable
test project.

The support statement in package documentation must remain **.NET Framework
4.7.2+**. Branch protection should require the framework-floor status whenever
repository settings permit it.

## Changing a floor

A floor change is an architecture decision, not a routine specification edit.
The change must:

1. introduce a superseding ADR;
2. update the source-of-truth version or target framework;
3. update the matching floor and canary jobs;
4. update package and user documentation;
5. preserve a test that runs the shipped artifact on the newly claimed boundary.

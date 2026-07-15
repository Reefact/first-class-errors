# ADR-0001 | Lock the analyzer's Roslyn floor

**Status:** Accepted
**Date:** 2026-07-10
**Decision Makers:** Reefact

## Context

`FirstClassErrors.Analyzers` is a Roslyn analyzer that ships **bundled inside
the `FirstClassErrors` NuGet package** (at `analyzers/dotnet/cs/`), so that
consumers who reference the package get the `FCExxx` diagnostics automatically,
with no extra install.

A bundled analyzer is loaded by **each consumer's host compiler** — the Roslyn
that comes with their .NET SDK or IDE. The `Microsoft.CodeAnalysis.*` version
the analyzer is *compiled against* therefore becomes the **minimum** Roslyn
able to load it:

* if the analyzer references a Roslyn **newer** than the host, the host refuses
  to load it and emits **`CS8032`** (and the analyzer silently does nothing);
* if the analyzer throws while loading, the host emits **`AD0001`**.

A routine dependency bump of `Microsoft.CodeAnalysis.*` therefore silently
raises the minimum SDK/IDE every consumer must have. This exact regression
happened once: the analyzer drifted to requiring Roslyn 5.6. Dependabot
proposes such bumps automatically, like any other dependency update.

The load contract is invisible on modern toolchains — CI on the latest SDK,
and the maintainer's own IDE, both satisfy any floor — so it can regress
without any red signal.

The oldest host FirstClassErrors supports is the **.NET 8.0.100 SDK /
Visual Studio 2022 17.8**, whose compiler is **Roslyn 4.8**.

`CS8032` is emitted only when a load is *attempted*: an analyzer that is never
wired into a compilation at all leaves the build green. And the analyzer
reaches consumers through a packaging path (`analyzers/dotnet/cs/`) that can
break independently of the analyzer's own references.

`Microsoft.CodeAnalysis.Analyzers` (5.6.0) is a build-time authoring analyzer
(`PrivateAssets="all"`), not a runtime reference of the shipped assembly, so it
does not affect the load contract.

## Decision

The analyzer's Roslyn floor is fixed at **4.8.0** — the Roslyn of the oldest
supported host — declared once as `RoslynFloorVersion` in
`Directory.Build.props` and enforced by four independent guards.

## Rationale

The floor value is dictated by the context: 4.8.0 is exactly the Roslyn that
ships with the oldest host we claim to support (.NET 8.0.100 SDK / VS 2022
17.8). A lower floor buys nothing; a higher one silently drops supported
hosts.

The contract needs more than a version pin because it regresses without any
red signal on modern toolchains. A pin alone still *looks* like routine
maintenance, and a too-new reference can slip in three distinct ways that no
single check catches: the *reference version* can drift, the shipped analyzer
can fail to *load* on an old host, and it can fail to be *packaged* where
consumers look. The decision therefore layers **defense in depth** — one
source of truth and four guards, each closing a gap the others cannot:

* the floor is declared **once**, as `RoslynFloorVersion` in
  `Directory.Build.props`, so the pin, the test and the CI job track a single
  value and can never disagree;
* **the pin** compiles the analyzer against exactly that Roslyn, setting the
  floor at its source;
* **the unit test** (`RoslynFloorTests`) reads the floor back from assembly
  metadata and fails, fast and in-process, if any referenced
  `Microsoft.CodeAnalysis*` assembly exceeds it — catching *reference* drift;
* **the floor-check CI job** packs the library and rebuilds the sample against
  the packed analyzer under the floor SDK, proving the **shipped artifact**
  both **loads** and is **packaged** correctly on the **oldest supported
  compiler** — the two gaps the unit test cannot reach;
* **the Dependabot ignore** keeps an automated PR from ever proposing the bump,
  so raising the floor stays a conscious act rather than a rubber-stamped
  update.

Two of the guards fail **loudly** (the unit test and the CI job); the pin and
the Dependabot ignore work silently by construction. Together they satisfy the
requirement the context sets out: a guard that fails on the **oldest** host, on
the **exact artifact we ship**, before a consumer ever sees `CS8032`. The
trade-off accepted is the upkeep of a deliberately intricate CI job and a
non-solution `tools/floor-check/` project; the mechanics of that job — the
two-SDK split, the load proof, and the NuGet traps it closes — are documented
in the [`analyzers` workflow reference](../workflows/analyzers.en.md), and the
pin and metadata live in `FirstClassErrors.Analyzers.csproj`.

## Alternatives Considered

### Track the current Roslyn and let dependency bumps flow (status quo)

Considered because it is the default, zero-effort behavior: Roslyn bumps
arrive as routine dependency updates, and nothing in the toolchain objects.

Rejected because each bump silently raises the minimum SDK/IDE every consumer
must have — the drift to Roslyn 5.6 happened exactly this way, with no red
signal anywhere.

### Pin the Roslyn version, without further guards

Considered as the minimal fix: a one-line pin stops the drift.

Rejected because a bare pin still *looks* like routine maintenance — Dependabot
keeps proposing the bump, and accepting one fails nothing: no test reads the
floor back, and no build runs on the oldest host. The regression would return
with the next well-meaning update.

### Rely on the unit test alone (no floor-check CI job)

Considered because the test is fast, in-process, and runs in the ordinary
`dotnet test`.

Rejected because it only catches *reference* version drift. It cannot prove
that the shipped artifact actually **loads** on the oldest supported host, nor
that the analyzer is actually **packaged** at `analyzers/dotnet/cs/` — both of
which can break while every reference version stays at the floor.

## Consequences

### Positive

* The load contract cannot silently regress: a too-new Roslyn reference fails
  the unit test (fast) **and** the floor-check job (authentic), and a broken
  packaging path fails the floor-check job.
* The floor-check job tests the *shipped artifact* on the *oldest supported
  host*, not a proxy.
* The floor is a one-line, self-documenting decision (`RoslynFloorVersion`).

### Negative

* Two extra guards to keep green, and a non-solution `tools/floor-check/`
  project with intentionally intricate NuGet configuration (documented in the
  [`analyzers` workflow reference](../workflows/analyzers.en.md)).
* The floor-check job downloads the 8.0.100 SDK on every run (~a few seconds).
* Raising the floor is a deliberate, multi-step act (by design).

### Risks

* The floor-check's NuGet configuration looks over-engineered to a reader who
  does not know the traps it closes; a future "simplification" would
  reintroduce one of them as a silent bug. Mitigation: every subtlety is
  recorded in the [`analyzers` workflow reference](../workflows/analyzers.en.md)
  and in the workflow's own YAML comments.
* When the floor is raised, the floor SDK pinned in `analyzers.yml` and in
  `tools/floor-check/global.json` must be moved by hand; forgetting them
  leaves the job validating the old floor. Mitigation: the raise procedure
  below lists them explicitly.

## Follow-up Actions

* None immediate: the four guards (pin, `RoslynFloorTests`, the `analyzers.yml`
  floor-check job, the Dependabot ignore) shipped with this decision.
* When the floor is ever raised:
  1. Bump `<RoslynFloorVersion>` in `Directory.Build.props`.
  2. Update the floor SDK in `analyzers.yml` (`dotnet-version` and the nested
     `tools/floor-check/global.json`) to the SDK whose Roslyn matches the new
     floor.
  3. Update the README / `doc/README.fr.md` compiler-requirement note.
  4. Supersede this ADR (new floor, new minimum SDK/IDE).

  The pin, the unit test and the Dependabot ignore need no change — they all
  track `$(RoslynFloorVersion)` or the package ids.

## References

* Shaped by #69 (initial lock) and #74 / #75 / #77 (floor-check hardening).
* [ADR-0002](0002-floor-the-tooling-runtime.md) — the tooling runtime floor,
  the run-time sibling of this build-time decision.
* [`analyzers` workflow reference](../workflows/analyzers.en.md) — the
  floor-check job, structurally.

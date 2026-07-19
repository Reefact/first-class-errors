# ADR-0001 | Lock the analyzer's Roslyn floor

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0001-lock-the-analyzer-roslyn-floor.fr.md)

**Status:** Accepted
**Date:** 2026-07-10
**Decision Makers:** Reefact

## Context

`FirstClassErrors.Analyzers` is bundled inside the `FirstClassErrors` NuGet
package, so it is loaded by each consumer's compiler rather than by a runtime the
project controls. The Roslyn version referenced by the analyzer therefore becomes
the minimum compiler capable of loading it; a newer reference can make the host
emit `CS8032` and silently lose the diagnostics.

This compatibility boundary is easy to regress because modern CI and maintainer
IDEs satisfy a newer floor automatically. The project previously drifted to a
Roslyn version newer than the oldest supported host, and ordinary dependency
updates can recreate that failure. Packaging can also regress independently: a
correct analyzer assembly that is absent from `analyzers/dotnet/cs/` is never
loaded.

The oldest supported analyzer host is the .NET 8.0.100 SDK / Visual Studio 2022
17.8, whose compiler is Roslyn 4.8.

## Decision

The analyzer's Roslyn compatibility floor is fixed at **4.8.0**, matching the oldest supported compiler host, and is protected as an explicit product compatibility boundary.

## Rationale

A lower floor provides no additional supported host, while a higher floor drops a
host the product claims to support. The boundary must be defended on the distinct
ways it can fail: reference drift, host loading, package placement, and automated
dependency updates. Those guards share one source of truth and test the packaged
artifact on the floor host; their current implementation is maintained in the
[platform compatibility specification](../specifications/platform-compatibility.en.md)
and the [`analyzers` workflow reference](../workflows/analyzers.en.md).

Keeping the implementation details outside this ADR allows the guard structure to
be improved without rewriting the decision.

## Alternatives Considered

### Track the current Roslyn

Considered because it is the zero-maintenance default. Rejected because every
routine bump silently raises the minimum SDK or IDE, which is how the original
regression occurred.

### Pin the version without independent verification

Considered as the smallest fix. Rejected because the pin can still be changed as
routine maintenance and does not prove that the packaged analyzer loads on the
oldest host.

### Verify only referenced assembly versions

Considered because an in-process unit test is fast. Rejected because it cannot
prove package placement or authentic loading by the floor compiler.

## Consequences

### Positive

* The analyzer's minimum compiler is explicit and mechanically protected.
* The shipped package, not only the project references, is exercised on the floor.
* Raising the floor becomes a conscious compatibility decision.

### Negative

* Compatibility guards and a floor-host build must be maintained.
* Dependency updates to Roslyn require deliberate review rather than routine merge.

### Risks

* A maintainer could simplify a guard without understanding which failure mode it
  covers. Mitigation: the mutable specification and workflow reference document
  the current guard responsibilities.

## Follow-up Actions

* A future floor change must supersede this ADR and update the platform
  compatibility specification, package documentation, and floor validation.

## References

* [Platform compatibility specification](../specifications/platform-compatibility.en.md).
* [`analyzers` workflow reference](../workflows/analyzers.en.md).
* [ADR-0002](0002-floor-the-tooling-runtime.md) — the tooling runtime sibling.
* Issues #69, #74, #75 and #77 — initial lock and subsequent hardening.

# ADR-0001 | Lock the analyzer's Roslyn floor

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0001-lock-the-analyzer-roslyn-floor.fr.md)

**Status:** Accepted
**Date:** 2026-07-10
**Decision Makers:** Reefact

## Context

`FirstClassErrors.Analyzers` is bundled inside the `FirstClassErrors` NuGet package and is loaded by each consumer's compiler. The Roslyn version against which the analyzer is compiled therefore becomes the minimum compiler capable of loading it.

A routine Roslyn dependency update can silently raise that minimum. Modern CI and maintainer IDEs do not expose the regression because they already satisfy the newer floor. The repository previously experienced this drift when the analyzer came to require Roslyn 5.6.

The oldest supported analyzer host is the .NET 8.0.100 SDK / Visual Studio 2022 17.8, which carries Roslyn 4.8. A complete compatibility guarantee must cover both the analyzer's references and the packaged artifact consumers actually load.

## Decision

The analyzer's Roslyn floor is fixed at **4.8.0**, the Roslyn version of the oldest supported host, and is protected by independent build-time, test-time, packaging, and dependency-management guards.

## Rationale

The floor follows directly from the compatibility promise: a higher version would silently exclude a supported host, while a lower version would add no supported environment.

A version pin alone is insufficient because reference drift, analyzer loading, and package placement are distinct failure modes. Independent guards provide defense in depth and ensure that the real shipped artifact is exercised on the oldest supported compiler rather than only on a modern development environment.

The additional maintenance cost is justified because analyzer load failures are otherwise silent to maintainers and surface to consumers only after publication.

The current technical realization and floor-raising procedure are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#analyzer-compatibility-floor) and the [`analyzers` workflow reference](../workflows/analyzers.en.md).

## Alternatives Considered

### Track the current Roslyn version

Considered because it is the default dependency-maintenance path. Rejected because each update silently raises the minimum SDK and IDE required by consumers.

### Pin the version without additional guards

Considered as the smallest correction. Rejected because it neither proves that the packaged analyzer loads on the floor host nor prevents a well-intentioned future update from restoring the regression.

### Rely only on an assembly-reference test

Considered because it is fast and deterministic. Rejected because it cannot prove that the analyzer is packaged at the expected location or that the shipped artifact loads on the oldest compiler.

## Consequences

### Positive

* The analyzer's minimum compiler cannot drift through routine dependency maintenance.
* The compatibility claim is verified against the packaged artifact on the oldest supported host.
* Raising the floor remains an explicit architectural decision.

### Negative

* Several complementary guards must remain maintained.
* Raising the floor requires coordinated changes across build, test, packaging, and documentation.

### Risks

* A future maintainer could remove a guard whose purpose is not obvious. Mitigation: the mechanics are documented in the implementation and workflow references.
* A guard could continue validating an obsolete floor after an intentional change. Mitigation: a floor change must supersede this ADR and follow the documented maintenance procedure.

## Follow-up Actions

* None immediate.
* Supersede this ADR when the supported Roslyn floor changes.

## References

* [ADR implementation reference — Analyzer compatibility floor](../specifications/adr-implementation-reference.md#analyzer-compatibility-floor)
* [`analyzers` workflow reference](../workflows/analyzers.en.md)
* [ADR-0002](0002-floor-the-tooling-runtime.md) — the runtime counterpart of this compatibility decision.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

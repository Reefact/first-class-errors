# ADR-0002 | Floor the tooling runtime at the oldest supported LTS

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0002-floor-the-tooling-runtime.fr.md)

**Status:** Accepted
**Date:** 2026-07-13
**Decision Makers:** Reefact

## Context

FirstClassErrors ships both broadly consumable libraries and runnable tooling. The libraries target `netstandard2.0`; the command-line tool, documentation generator, and worker are framework-dependent applications whose target framework is a hard minimum runtime.

The tooling previously targeted the latest .NET runtime. That prevented consumers on an older supported LTS from running `fce`, even when their application could consume the libraries.

The worker also loads consumer assemblies. Its process must therefore be able to run on a runtime compatible with the target assembly it inspects. This is a runtime-selection concern rather than a reason to publish one binary per .NET release.

At the time of the decision, .NET 8 was the oldest supported LTS and matched the product's analyzer-host floor. The library's separate .NET Framework support floor is defined by [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md), which refines the incidental statement previously carried here.

## Decision

The tooling (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`, and `FirstClassErrors.GenDoc.Worker`) single-targets **`net8.0`**, the oldest supported .NET LTS at the time of this decision, and supports newer runtimes through roll-forward rather than a target-framework matrix.

## Rationale

A single floor build gives every consumer on the supported range access to the tooling without creating a per-release matrix. It keeps the compatibility statement aligned with the analyzer host while avoiding rebuilds that add no functional value.

Roll-forward is the correct mechanism because the tooling needs to execute on newer installed runtimes, and the worker must select a runtime capable of loading the target assembly. Publishing several target frameworks would not remove that worker constraint and would create ongoing release churn.

The floor remains enforceable on two independent axes: compilation prevents accidental use of APIs above the target framework, while dedicated runtime checks exercise the shipped tooling at the floor and on upcoming runtimes.

The exact runtime policies, CI jobs, project settings, and maintenance procedure are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#tooling-runtime-floor) and the [`ci` workflow reference](../workflows/ci.en.md).

## Alternatives Considered

### Keep the tooling on the latest runtime

Considered because it is the simplest project configuration. Rejected because the target framework is a hard minimum and would exclude consumers on an older supported LTS.

### Multi-target the tooling

Considered as the conventional compatibility strategy. Rejected because one floor build already reaches newer runtimes through roll-forward, while a matrix adds release maintenance and does not solve the worker's need to load higher-targeted assemblies.

## Consequences

### Positive

* Consumers on the oldest supported LTS or any newer runtime can run the tooling.
* The repository ships one tooling artifact rather than a per-release target matrix.
* Runtime compatibility is verified at the floor and monitored ahead of new .NET releases.

### Negative

* The tooling cannot run on runtimes older than the selected LTS floor.
* Runtime-selection policies and dedicated compatibility checks must remain maintained.

### Risks

* A future runtime could change roll-forward behavior or break the tooling. Mitigation: exercise the current floor in CI and the next runtime through the canary workflow.
* The support statement could become inconsistent when the floor LTS reaches end of support. Mitigation: supersede this ADR and update the analyzer and tooling support documentation together.

## Follow-up Actions

* Supersede this ADR when the tooling runtime floor changes.
* Keep the canary pointed at the next .NET release.

## References

* [ADR implementation reference — Tooling runtime floor](../specifications/adr-implementation-reference.md#tooling-runtime-floor)
* [`ci` workflow reference](../workflows/ci.en.md)
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — the analyzer-host counterpart.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) — refines the library's .NET Framework floor; it replaces the incidental 4.6.1 statement formerly present in this ADR.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

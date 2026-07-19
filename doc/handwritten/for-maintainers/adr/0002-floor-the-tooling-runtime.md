# ADR-0002 | Floor the tooling runtime at the oldest supported LTS

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0002-floor-the-tooling-runtime.fr.md)

**Status:** Accepted
**Date:** 2026-07-13
**Decision Makers:** Reefact

## Context

The shipped libraries are `netstandard2.0` assemblies, while `fce`, GenDoc and its
worker are runnable framework-dependent applications. A runnable application's
TFM is a hard minimum: roll-forward can move execution to a newer runtime but
cannot make a newer-targeted application run on an older one.

The tooling previously targeted the latest runtime, preventing a consumer on the
oldest supported LTS from running the documentation generator even though the
consumer could use the libraries. The worker also loads the target assembly in
its own process, so it must run on an installed runtime capable of loading that
target.

.NET 8 is the oldest supported LTS at the time of the decision and matches the
analyzer host floor established by ADR-0001. The libraries' practical .NET
Framework floor is separately defined as 4.7.2 by ADR-0022.

## Decision

The tooling (`FirstClassErrors.Cli`, `FirstClassErrors.GenDoc`, and `FirstClassErrors.GenDoc.Worker`) single-targets **`net8.0`**, the oldest supported LTS, and reaches newer runtimes through roll-forward rather than a target-framework matrix.

## Rationale

One floor artifact gives the widest supported reach without adding a target for
every .NET release. It keeps the product's tooling and analyzer support aligned
on .NET 8 while allowing the worker to select a runtime capable of loading the
consumer's target assembly.

The runtime policies, CI floor execution, preview canary, and maintenance steps
are implementation mechanics recorded in the
[platform compatibility specification](../specifications/platform-compatibility.en.md)
and the [`ci` workflow reference](../workflows/ci.en.md). They may evolve while
the single-floor and roll-forward decision remains unchanged.

## Alternatives Considered

### Keep the tooling on the latest runtime

Considered because it needs no roll-forward tuning. Rejected because it excludes
consumers whose newest installed runtime is the oldest supported LTS.

### Multi-target the tooling

Considered as the conventional compatibility matrix. Rejected because one floor
build already rolls forward to newer runtimes, while a matrix adds release churn
and does not solve the worker's need to load a higher-targeted assembly.

## Consequences

### Positive

* Consumers on .NET 8 or newer can run `fce`.
* The tooling ships one framework target instead of a per-release matrix.
* New .NET majors normally require no rebuild or release.
* The support boundary can be tested on the real floor runtime.

### Negative

* The tooling cannot run where the newest runtime predates .NET 8.
* Roll-forward behaviour must be deliberately configured and tested.

### Risks

* A future runtime can regress roll-forward or assembly loading. Mitigation: the
  floor and preview execution described by the platform specification.

## Follow-up Actions

* When the floor LTS reaches end of support, decide a successor floor in a new ADR
  and update the specification, runtime tests, and package documentation.

## References

* **Refined by [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md):**
  the library's .NET Framework support floor is 4.7.2; this ADR governs only the
  tooling runtime.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — analyzer host floor.
* [Platform compatibility specification](../specifications/platform-compatibility.en.md).
* [`ci` workflow reference](../workflows/ci.en.md).

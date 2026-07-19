# ADR-0022 | Floor the library's .NET Framework support at 4.7.2

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0022-floor-the-library-on-net-framework-4-7-2.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The shipped libraries target `netstandard2.0`, whose formal .NET Framework minimum is 4.6.1.

On .NET Framework versions before 4.7.2, `netstandard2.0` support relies on retrofitted facades, additional package assets, and consumer-side binding redirects. .NET Framework 4.7.2 is the first version that provides the relevant facades in-box and is the practical minimum recommended for reliable consumption.

The repository previously advertised .NET Framework 4.6.1 support without executing the libraries on that runtime. A compatibility promise that is not exercised cannot provide a trustworthy support boundary.

The current test stack can execute on .NET Framework 4.7.2 but not on earlier framework versions. The tooling runtime has a separate floor defined by ADR-0002.

## Decision

The supported .NET Framework floor for the shipped `netstandard2.0` libraries is **4.7.2**.

## Rationale

4.7.2 is the lowest version on which the libraries can be consumed without the fragile compatibility plumbing required by earlier framework versions.

It is also the lowest version the repository can exercise with its supported test stack. Aligning the documented floor with a continuously verified runtime turns an aspirational compatibility statement into an enforceable contract.

The decision intentionally chooses the practical and testable boundary rather than the theoretical `netstandard2.0` minimum. Lower versions would require a second test stack and environment-specific binding behavior for little continuing user value.

This ADR refines the incidental .NET Framework 4.6.1 statement that previously appeared in ADR-0002; it does not supersede ADR-0002 because that decision concerns runnable tooling rather than the libraries.

The exact Windows job, conditioned test targets, polyfills, project exclusions, and preview coverage are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#tooling-runtime-floor) and the CI workflow reference.

## Alternatives Considered

### Keep advertising .NET Framework 4.6.1

Considered because it is the formal `netstandard2.0` minimum. Rejected because the claim was unverified and depends on fragile consumer-side plumbing on largely obsolete runtime versions.

### Floor at .NET Framework 4.6.2

Considered because it remains serviced longer than 4.6.1. Rejected because it has the same facade and binding-redirect constraints and cannot be verified with the supported test stack.

### Test every modern .NET major as a blocking matrix

Considered for broad reassurance. Rejected because the valuable compatibility boundary is .NET Framework versus modern .NET, while the latest runtime and preview can cover the modern end without creating a per-release treadmill.

## Consequences

### Positive

* The .NET Framework support statement is continuously verified rather than merely asserted.
* The practical floor avoids consumer-side binding-redirect fragility.
* The library and tooling runtime boundaries are stated separately and precisely.
* The .NET Framework floor is stable because the platform is no longer adding new major versions.

### Negative

* Consumers on .NET Framework 4.6.1 through 4.7.1 are outside the supported range.
* Dedicated Windows compatibility coverage and test-target plumbing must remain maintained.

### Risks

* Some Request Binder scenarios use modern-only types and cannot run on the framework floor. Mitigation: cover the shipped binder assembly through compatible test suites and keep exclusions explicit in the implementation reference.
* A required floor job could be configured but not enforced by branch protection. Mitigation: maintain the job as a required status check when repository settings permit.

## Follow-up Actions

* Keep the user-facing support statement at .NET Framework 4.7.2 or later.
* Keep the framework-floor check required for merges.

## References

* [ADR implementation reference — Tooling runtime floor](../specifications/adr-implementation-reference.md#tooling-runtime-floor)
* [ADR-0002](0002-floor-the-tooling-runtime.md) — refined by this ADR for the library's .NET Framework floor.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md)
* `FirstClassErrors/README.nuget.md` and the CI workflow reference.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

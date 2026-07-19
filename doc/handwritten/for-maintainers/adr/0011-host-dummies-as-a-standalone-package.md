# ADR-0011 | Host Dummies as a standalone package in this repository

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0011-host-dummies-as-a-standalone-package.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The generic arbitrary-value engine anticipated by ADR-0006 serves domain-driven testing in general rather than error handling specifically.

A standalone library named `Dummies` now provides typed, constraint-carrying generators without knowledge of FirstClassErrors. Its intended audience extends beyond consumers of this repository's main package.

A package identity is costly to rename after adoption, while this repository already provides the CI, packaging, release, SBOM, SourceLink, and governance infrastructure needed to ship a package safely.

The library's API is expected to evolve quickly during its first iterations, and its earliest consumers are colocated in this repository.

## Decision

`Dummies` ships as an independent NuGet package named `Dummies`, hosted in this repository as a standalone project that must not reference any FirstClassErrors project.

## Rationale

The package name reflects the library's actual scope and avoids implying an error-handling dependency that does not exist.

Repository colocation reuses mature delivery infrastructure and keeps iteration inexpensive while the package boundary, namespace, and dependency rule preserve a distinct product identity.

The no-reference rule makes the independence enforceable and keeps a later repository extraction mechanical rather than architectural.

The current release-train and architecture-test mechanics are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#dummies-generation-contracts) and the repository's packaging documentation.

## Alternatives Considered

### Name it as part of FirstClassErrors.Testing

Considered because the engine originated near that package. Rejected because the name would narrow the audience, misdescribe the library, and imply a dependency the architecture forbids.

### Create a separate repository immediately

Considered because it gives the strongest organizational separation. Rejected because the package boundary already provides identity while a new repository would duplicate delivery infrastructure during the period of fastest API evolution.

### Extend the existing FirstClassErrors.Testing facade

Considered because it already ships. Rejected because it would couple a generic generation DSL to an error-specific package and prevent the intended independent audience.

## Consequences

### Positive

* Dummies has an independent package identity and audience from its first release.
* Delivery infrastructure is reused rather than duplicated.
* The dependency boundary is machine-checkable and future extraction remains inexpensive.

### Negative

* The repository maintains an additional package, release train, and documentation set.
* Contributors must understand that this project is deliberately outside the FirstClassErrors dependency graph.

### Risks

* The boundary could erode through a convenient project reference. Mitigation: enforce the rule with architecture tests.
* The package's release cadence could diverge from the repository. Mitigation: treat recurring cadence conflict, independent contributors, or a separate issue flow as extraction triggers.

## Follow-up Actions

* Revisit repository extraction when the package develops independent governance or release pressure.
* Decide separately whether FirstClassErrors.Testing should consume Dummies internally.

## References

* [ADR implementation reference — Dummies generation contracts](../specifications/adr-implementation-reference.md#dummies-generation-contracts)
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md)
* Architecture tests in `Dummies.UnitTests`.
* [ADR-0023](0023-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

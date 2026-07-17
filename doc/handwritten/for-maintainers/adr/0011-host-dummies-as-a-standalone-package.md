# ADR-0011 | Host Dummies as a standalone package in this repository

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0011-host-dummies-as-a-standalone-package.fr.md)

**Status:** Proposed
**Date:** 2026-07-17
**Decision Makers:** Reefact

## Context

`FirstClassErrors.Testing` supplies arbitrary test values through an error-aware
`Any` facade backed by a single seedable source (ADR-0006). That ADR listed, as a
follow-up, extracting the generic value engine into a standalone, error-agnostic
utility, and the engine was deliberately kept internally separable to that end.

A new library, `Dummies`, now provides a fluent DSL of typed, constraint-carrying
generators (`IAny<T>`) for arbitrary yet valid test values. Its constraints
express the invariants a value must satisfy — a value object's format, a contract
precondition — which targets domain-driven tests in general, not error handling:
the library carries no knowledge of FirstClassErrors, targets `netstandard2.0`,
and has zero dependencies. Its intended audience extends beyond FirstClassErrors
users.

Two facts constrain where and under what name it ships:

* A NuGet package ID is effectively permanent: renaming after adoption means
  publishing a new package and forcing a consumer migration.
* This repository already carries the shipping apparatus a published package
  needs — CI with a zero-warning ratchet, SBOM embedding, SourceLink, tag-driven
  release trains selected by an explicit project list, commit conventions, and
  this ADR base. A separate repository would have to duplicate all of it.

The library's API is expected to evolve fastest in its first iterations, while
its most likely early consumers (this repository's own test projects, and
possibly `FirstClassErrors.Testing` later) live here.

## Decision

The `Dummies` library ships as its own NuGet package, named `Dummies`, hosted in
this repository as a standalone project that references no FirstClassErrors
project — a boundary guarded by an architecture test.

## Rationale

* **The name must not narrow the audience.** The library is a generic
  test-value generator; a `FirstClassErrors.Testing.*` name would describe it as
  error-handling tooling, cap its audience to FirstClassErrors users, and imply
  a dependency that does not exist. Because a package ID is permanent, this had
  to be decided before first publication, not after.
* **The identity lives in the package boundary, not the repository boundary.**
  A standalone package ID, its own namespace, and a zero-reference rule deliver
  the independent identity; hosting the sources here reuses the existing
  shipping apparatus and keeps iteration friction low precisely while the API
  churns most.
* **The boundary is enforced, not hoped for.** An architecture test fails any
  build in which `Dummies` gains a FirstClassErrors reference, so the standalone
  promise cannot erode silently, and a later extraction to its own repository
  stays a mechanical operation.
* **It realizes ADR-0006's follow-up as intended.** The standalone,
  error-agnostic utility that ADR anticipated now exists as a first-class
  package rather than an internal engine.

## Alternatives Considered

### Name it `FirstClassErrors.Testing.Dummies`

Considered because the library was conceived while splitting the generic value
engine out of `FirstClassErrors.Testing`, and a family name inherits that
package's audience. Rejected because the name misdescribes the content (the
library is not about errors), caps the audience the library is built for, and
suggests a coupling the code deliberately forbids.

### Create a separate repository now

Considered because a standalone product in its own repository is the cleanest
long-term identity. Rejected for now because it duplicates the entire shipping
apparatus for no identity gain the package boundary does not already deliver,
and it adds cross-repository friction at the moment the API evolves fastest.
The extraction stays cheap as long as the no-reference boundary holds; the
triggers for revisiting are listed as follow-ups.

### Extend the `Any` facade of `FirstClassErrors.Testing` in place

Considered because that facade exists and is shipped. Rejected because it welds
the generic engine to the error-specific surface — the opposite of the
standalone ambition — and because growing a full constraint DSL inside a
test-support package for errors would misplace its center of gravity.
`FirstClassErrors.Testing` keeps its own facade unchanged.

## Consequences

### Positive

* The library carries an identity and an audience of its own, independent of
  FirstClassErrors, from its first release.
* No shipping infrastructure is duplicated; the package benefits from the
  repository's existing CI, packaging hardening, and conventions.
* The no-reference boundary is machine-checked, and extraction to a dedicated
  repository remains a low-cost, mechanical option.

### Negative

* One more published package to maintain from this repository: its own release
  train, documentation, and versioning cadence.
* The repository's name does not advertise the package; discoverability rests
  on the package itself and its documentation.
* The commit-scope list grows by one (`dummies`), and contributors must know
  that one project in this repository is deliberately not part of the
  FirstClassErrors dependency graph.

### Risks

* **Boundary erosion** — a convenient shortcut adds a FirstClassErrors
  reference. Mitigated by the architecture test and by this ADR recording the
  rule.
* **Cadence conflict** — Dummies' release rhythm may start fighting the
  repository's release trains. That pressure is an extraction trigger, not a
  reason to couple the package tighter.

## Follow-up Actions

* Give `Dummies` its own release train in the packaging tooling before its
  first publication; until then, no release publishes it.
* Extract to a dedicated repository (keeping the package ID) when a trigger
  fires: external contributors arrive, the release cadence diverges, or the
  package develops an issue flow of its own.
* Write the user documentation (English and French) once the V1 surface
  stabilizes.
* Decide separately whether `FirstClassErrors.Testing` later re-bases its
  internal value engine on `Dummies`; nothing in this decision requires it.

## References

* ADR-0006 — Supply arbitrary test values from a single seedable source (the
  follow-up this decision realizes).
* The architecture test guarding the boundary, in `Dummies.UnitTests`.

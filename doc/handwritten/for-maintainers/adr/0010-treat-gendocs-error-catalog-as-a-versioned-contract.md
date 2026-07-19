# ADR-0010 | Treat GenDoc's error catalog as a versioned contract

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0010-treat-gendocs-error-catalog-as-a-versioned-contract.fr.md)

**Status:** Accepted
**Date:** 2026-07-17
**Decision Makers:** Reefact

## Context

GenDoc exposes stable first-class error codes and typed context that external consumers can match in CI, integrations, and support tooling.

GenDoc ships as part of the `fce` command-line package rather than on an independent release train. Removing or changing one of its documented codes or context contracts is therefore a compatibility change in the `cli` package.

The repository already knows how to snapshot and classify catalog changes, but the tool's own error catalog was not tied to the semantic version published by the release process.

## Decision

A breaking change to GenDoc's own error catalog requires a major version bump of the `cli` release train and is enforced when that release is published.

## Rationale

The catalog is a published contract because consumers depend on stable error identities and context. A breaking catalog change must therefore be signalled by the same semantic-versioning promise as any other externally observable break.

Release time is the correct enforcement point. A breaking change can be legitimate during development; the failure is shipping it under a version that promises compatibility.

The comparison must remain anchored to the last shipped catalog rather than a moving development snapshot so that the version number answers what changed since the previous release.

Reusing the existing catalog-diff classification avoids creating a second, competing definition of compatibility.

The exact baseline location, release workflow, update commands, and recovery procedure are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#gendoc-catalog-compatibility), the workflow reference, and the catalog versioning documentation.

## Alternatives Considered

### Rely on Conventional Commits and review discipline

Considered because the repository already records intended breaking changes. Rejected because an accidental catalog break can occur without the commit author recognizing it, while the generated catalog provides a mechanical measurement.

### Gate every pull request

Considered because it would surface breaks earlier. Rejected because a breaking catalog change is valid during development as long as the eventual release carries the correct major version.

### Publish GenDoc on an independent release train

Considered because it would give the catalog its own version. Rejected because GenDoc has no standalone consumer and is intentionally shipped inside `fce`; the additional release machinery would provide no corresponding user benefit.

## Consequences

### Positive

* A breaking GenDoc catalog change cannot ship under a compatible-looking `cli` version.
* Reviewers can see pending catalog compatibility impact before release.
* The generated catalog becomes an explicit release contract rather than a best-effort snapshot.

### Negative

* The `cli` release now depends on a valid catalog baseline and compatibility check.
* Maintainers must understand that accepting a catalog break requires a major version bump or reversal of the change.

### Risks

* A stale or incorrectly advanced baseline could misclassify a release. Mitigation: keep baseline updates inside the controlled release procedure and document recovery when publication and baseline advancement diverge.

## Follow-up Actions

* Maintain the release procedure and recovery path in the workflow reference rather than this ADR.

## References

* [ADR implementation reference — GenDoc catalog compatibility](../specifications/adr-implementation-reference.md#gendoc-catalog-compatibility)
* [Catalog Versioning Reference](../../for-users/CatalogVersioningReference.en.md)
* [ADR-0009](0009-report-the-toolings-failures-as-first-class-errors.md)
* [ADR-0002](0002-floor-the-tooling-runtime.md)
* Issue #167.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

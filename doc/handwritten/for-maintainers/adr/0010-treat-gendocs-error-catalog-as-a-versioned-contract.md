# ADR-0010 | Treat GenDoc's error catalog as a versioned contract

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0010-treat-gendocs-error-catalog-as-a-versioned-contract.fr.md)

**Status:** Accepted
**Date:** 2026-07-17
**Decision Makers:** Reefact

## Context

ADR-0009 made GenDoc's own failures first-class errors with stable `GENDOC_`
codes and typed context. Those identities are emitted by `fce` and can be matched
by CI, integrations and support tooling.

GenDoc is bundled in the `cli` release train rather than published independently.
A removed code or a removed or retyped context key is therefore a compatibility
change to what that train emits. The repository already knows how to snapshot and
diff an error catalog, but GenDoc's own catalog was not previously tied to the
version that publishes it.

## Decision

A breaking change to GenDoc's own error catalog, measured against the last released baseline, requires a major version bump of the `cli` release train and is enforced at release time.

## Rationale

Stable error identities are a published contract and must obey the same semantic
versioning promise as the tool that emits them. The correct enforcement point is
the release: a breaking change is legitimate during development, but publishing
it under a compatible-looking version is not.

The comparison must remain anchored to the last successful release rather than a
moving development snapshot. The existing catalog diff classification remains
the single definition of a breaking catalog change. Baseline lifecycle, workflow
ordering and recovery mechanics are maintained in the
[GenDoc catalog contract specification](../specifications/gendoc-catalog-contract.en.md).

## Alternatives Considered

### Rely on Conventional Commits and review

Considered because breaking-change markers already exist. Rejected because a
catalog break can be an unintended effect that no author marked manually.

### Gate every pull request

Considered for earlier feedback. Rejected because an intentional break is valid
before release and should not force a premature version decision.

### Give GenDoc its own package and release train

Considered for independent versioning. Rejected because GenDoc has no standalone
consumer and already ships as an internal part of `fce`.

## Consequences

### Positive

* A catalog break cannot ship under a non-major `cli` version.
* Reviewers can see the pending compatibility diff before release.
* Living documentation is anchored to an explicit released contract.

### Negative

* The release depends on a committed baseline and a catalog diff.
* Advancing the baseline after publication requires an automated write to `main`.

### Risks

* A failed baseline update after a successful publish leaves a stale baseline.
  Mitigation: the recovery procedure is explicit in the specification and must not
  republish the already-released package.

## Follow-up Actions

* Keep workflow wiring and operational recovery aligned with the catalog contract
  specification.

## References

* [GenDoc catalog contract specification](../specifications/gendoc-catalog-contract.en.md).
* [Catalog Versioning Reference](../../for-users/CatalogVersioningReference.en.md).
* ADR-0009 — first-class GenDoc failures.
* ADR-0002 — bundled tooling release model.
* Issue #167.

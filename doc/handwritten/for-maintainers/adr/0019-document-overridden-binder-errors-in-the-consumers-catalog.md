# ADR-0019 | Document overridden binder errors in the consumer's own catalog

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0019-document-overridden-binder-errors-in-the-consumers-catalog.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

ADR-0018 made the Request Binder's structural errors configurable as consumer-owned definitions containing their effective codes and messages.

The documentation generator discovers documented errors from opted-in consumer projects, not from runtime configuration inside referenced packages. A package binary can expose only its defaults, while an overriding consumer owns the values it actually emits.

The meaning of the binder's structural failures is stable and package-owned, but their effective identities and messages may be consumer-owned.

The repository's catalog model documents errors where they are defined and avoids untyped string links between an error and its documentation.

## Decision

A consumer that overrides Request Binder structural-error definitions documents those effective errors in its own generated catalog through compile-safe binder-provided documentation seams rather than through automatic discovery of referenced-package catalogs.

## Rationale

The consumer owns the effective codes and messages, so its own catalog is the only place that can faithfully describe what the application emits at runtime.

Binder-provided seams allow the consumer to reuse the stable prose and create representative errors through the same package-owned behavior without copying descriptions or manually reconstructing error shapes.

Keeping the catalog entry in the consumer's ordinary `[ProvidesErrorsFor]` / `[DocumentedBy]` flow avoids a special cross-package discovery mechanism, package-closure analysis, collision policy, and the risk of documenting defaults that the application no longer uses.

Compile-safe code links preserve the repository's stance against magic strings and remain refactorable by normal tooling.

The exact public members, analyzer suppressions, and consumer examples are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#documentation-only-public-surfaces) and the Request Binder documentation.

## Alternatives Considered

### Auto-discover documented codes from referenced packages

Considered because it removes consumer glue for package defaults. Rejected because referenced binaries cannot reveal consumer overrides and could therefore document codes the application does not emit.

### Link documentation through build configuration

Considered to keep the wiring outside code. Rejected because member names would become unchecked strings with poor navigation and refactoring safety.

## Consequences

### Positive

* A consumer documents exactly the binder errors it emits.
* Stable prose and representative error construction are reused instead of duplicated.
* The generator and its discovery model remain unchanged.
* Documentation links stay compile-safe.

### Negative

* Consumers add a small amount of explicit catalog glue.
* The binder exposes a limited public surface whose main purpose is documentation support.

### Risks

* Documentation-oriented public members could expand the runtime API unnecessarily. Mitigation: keep the surface minimal, stable, and tied to the catalog contract; reconsider metadata or generator-side alternatives before adding more.
* A consumer could call a sample seam outside documentation. Mitigation: samples are inert error values and do not alter binder behavior.

## Follow-up Actions

* Review future documentation-only public members against the minimization rule in the implementation reference.

## References

* [ADR implementation reference — Documentation-only public surfaces](../specifications/adr-implementation-reference.md#documentation-only-public-surfaces)
* [ADR-0018](0018-bundle-the-binders-structural-error-code-and-messages.md)
* [ADR-0016](0016-make-the-binders-structural-error-codes-configurable.md) — superseded origin of the deferred question.
* Issue #140 and analyzer FCE009.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

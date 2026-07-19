# ADR-0017 | Provide a configurable application-wide default for the binder options

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

ADR-0012 fixed a binder's options before binding begins and retained an explicit configured entry point.

Applications without a dependency-injection container can still need one application-wide naming and structural-error policy while using the bare binding entry. Repeating or manually threading a configured entry through every call site is possible but less convenient.

A freely mutable process-wide default would introduce runtime drift and parallel-test interference. `RequestBinderOptions` itself is immutable, so the remaining hazard is reassignment of the shared reference after use.

## Decision

`RequestBinderOptions.Default`, used by the bare binding entry, is configurable once during application composition and becomes immutable after its first binding use.

## Rationale

A configurable default gives hosts without dependency injection a host-agnostic way to establish one application policy while preserving the explicit `Bind.WithOptions` path for injected or per-call configuration.

Freezing the reference on first use constrains the global state to application startup and prevents the runtime drift that ADR-0012 rejected. The shared object is itself immutable, so consumers cannot mutate an in-use settings instance.

A scoped test-only override preserves parallel test isolation without making the production default freely resettable.

This decision deliberately revisits one alternative rejected by ADR-0012 with stronger constraints; it does not change ADR-0012's decision that every individual binder receives fixed options before binding begins.

The exact freeze semantics, exception behavior, test seam, and entry-point interaction are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts) and the Request Binder documentation.

## Alternatives Considered

### Keep only explicit configured entries

Considered because it avoids all process-global state. Rejected because container-free hosts would need to thread configuration through every binding call even when the policy is application-wide.

### Use a freely resettable global default

Considered because it is the simplest global configuration model. Rejected because it can change while requests are being processed and makes test isolation unsafe.

### Require dependency injection

Considered because it is test-safe and idiomatic where a container exists. Rejected as the only mechanism because the library is host-agnostic and supports CLIs, workers, and small tools without DI.

## Consequences

### Positive

* Hosts with or without dependency injection can configure one application-wide binder policy.
* The default cannot drift after binding starts.
* Explicit configured entries remain available and can override the default.

### Negative

* The library accepts one process-global configuration reference.
* Reading the default too early can freeze it before intended application configuration.
* Tests require a dedicated scoped override rather than resetting production state.

### Risks

* Hidden initialization order can make configuration fail if another component binds first. Mitigation: document startup ordering and fail loudly on late assignment.
* Consumers may use the global default where explicit configuration would be clearer. Mitigation: keep `Bind.WithOptions` prominent and recommend it for libraries, tests, and composition roots with DI.

## Follow-up Actions

* Expose a consumer testing seam only if demand justifies adding it to a dedicated testing package.
* Reconsider the global default before the stable release if real usage shows that explicit reusable configured entries are sufficient.

## References

* [ADR implementation reference — Request Binder implementation contracts](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts)
* [ADR-0012](0012-fix-the-binder-options-before-binding-begins.md) — this ADR revisits one rejected alternative while preserving fixed options per binder.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md)
* Issue #181.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

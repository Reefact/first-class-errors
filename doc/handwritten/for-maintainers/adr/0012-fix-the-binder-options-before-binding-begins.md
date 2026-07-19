# ADR-0012 | Fix the binder options before binding begins

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0012-fix-the-binder-options-before-binding-begins.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

The Request Binder resolves argument paths through an `IArgumentNameProvider` carried by `RequestBinderOptions`.

The previous fluent surface allowed options to change after some properties had already been bound. Because each binding read the options active at that moment, one failure envelope could contain paths produced by different naming policies.

A consumer relies on a single naming policy to map every error path back to the input it sent. Documentation alone could not prevent the invalid call order.

`RequestBinderOptions` carries no per-request state and can therefore be configured once and reused across requests.

## Decision

A binder's options are fixed at its entry point before any source is bound, and the public API does not permit them to change after binding begins.

## Rationale

Fixing the options before the binder exists makes mixed-policy envelopes unrepresentable rather than merely detecting them later.

Keeping options outside the request-specific binder also reflects their actual scope: a naming and structural-error policy is application configuration, not request data, and a configured entry can be reused safely.

The decision concerns when options become fixed, not whether the application may provide a default. [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) later revisits the process-wide-default alternative with additional freezing and test-isolation safeguards while preserving this entry-point immutability.

The exact configured-entry type, fluent call shape, inheritance by nested binders, and examples are documented in the [ADR implementation reference](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts) and the Request Binder guide.

## Alternatives Considered

### Lock options at runtime on the first binding

Considered because it preserves more of the old surface. Rejected because invalid ordering would still compile and fail only at runtime.

### Provide only a process-wide ambient default

Considered because it removes configuration from call sites. Rejected at the time because an unrestricted ambient default could drift during execution and leak across parallel tests. ADR-0017 later adopts a constrained, freeze-on-first-use form without changing this ADR's decision.

### Keep the mutable instance setter and strengthen documentation

Considered because it requires the smallest code change. Rejected because documentation cannot make an inconsistent envelope impossible.

## Consequences

### Positive

* Every failure envelope uses one naming and structural-error policy.
* Invalid late configuration becomes impossible through the public shape.
* Configured entries can be reused across requests.

### Negative

* Configuration has a distinct entry path that consumers must learn.
* Existing code using a late options setter must move configuration before binding.

### Risks

* A future need for intentionally different nested options would not fit this model. Mitigation: require an explicit new decision rather than introducing a late mutation path.

## Follow-up Actions

* Keep dependency-injection and application-default guidance in integration and user documentation.

## References

* [ADR implementation reference — Request Binder implementation contracts](../specifications/adr-implementation-reference.md#request-binder-implementation-contracts)
* [ADR-0017](0017-provide-a-configurable-application-wide-default-for-the-binder-options.md) — revisits the process-wide default alternative while preserving fixed options per binder.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md)
* [ADR-0007](0007-name-the-binder-terminals-new-and-create.md)
* Issue #145 and pull request #126.
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

# ADR-0012 | Fix the binder options before binding begins

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0012-fix-the-binder-options-before-binding-begins.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* The binder resolves each bound property's argument path — the key reported in error
  paths, such as `GuestEmail` or `Stay.CheckIn` — through the `IArgumentNameProvider`
  carried by `RequestBinderOptions`.
* Before this decision, options were set through an instance method `WithOptions` on
  `RequestBinder<TRequest>`, callable at any point in the fluent chain, including after
  some properties had already been bound.
* Each property binding reads the options in effect at the moment it is bound, so
  changing the provider between two bindings produces argument paths under two different
  naming policies within one failure envelope — for example `GuestEmail` beside
  `guest_email`.
* The binder collects every failure into a single envelope; a client reads the argument
  paths to map them back to the keys it sent, and relies on one consistent naming policy
  across that envelope.
* The binder already draws a hard line between a client error (recorded, surfaced once)
  and a programming error (thrown), and treats a misuse of its API as a programming
  error surfaced loudly rather than a silent inconsistency.
* The instance `WithOptions` was documented "call before binding any property", but
  nothing enforced it: the ordering was a prose convention, not a compile-time or
  runtime guarantee.
* The library exposes no ambient mutable state elsewhere: the clock, instance-id and
  arbitrary-value seams are all an immutable default plus an `AsyncLocal`, scoped,
  test-only override (ADR-0006), and the core deliberately exposes zero global mutable
  state.
* `RequestBinderOptions` carries no per-request state: the provider maps a
  `PropertyInfo` to a name and depends on nothing about the request instance.
* The library is pre-release, unpublished on NuGet with no external consumers, so moving
  where options are set carries no downstream migration cost.

## Decision

The request binder's options are fixed once, at the entry point —
`Bind.WithOptions(options).PropertiesOf(request)` — before any property is bound, and
the ability to change them after binding has started is removed.

## Rationale

* Fixing the options before the binder exists makes an inconsistent envelope impossible
  to write rather than merely discouraged: once binding has begun there is no point in
  the fluent chain at which the naming policy can be swapped, so the two-policy envelope
  the Context describes cannot arise. This closes the defect at the API shape, in the
  spirit of the binder's existing programming-error channel but one step stronger — the
  mistake is uncompilable, not thrown.
* Placing the options before `PropertiesOf` rather than between `PropertiesOf` and
  `FailWith` keeps them independent of the request type: a naming policy is about how a
  property is named, not which request is bound, so it need not — and now does not —
  depend on `TRequest`, which is also what lets the configured entry point be reused
  across requests.
* Keeping options an explicit argument passed at the entry point, rather than an ambient
  default the binder reads, is consistent with the library's settled stance that a
  genuine production dependency is passed explicitly and the core exposes no global
  mutable state (ADR-0006): a naming policy is a real production choice with legitimate
  variation, so it is an explicit dependency, not ambient configuration.
* Because the options carry no per-request state, fixing them at a reusable entry point
  lets an application configure the policy once — for example at startup — and reuse it
  for every request without threading it through each binding: the ergonomic the removed
  instance setter reached for, now without the ordering hazard.
* The pre-release status means the API shape is settled now, when there are no consumers
  to migrate.

## Alternatives Considered

### Lock the options at runtime on the first binding

Considered because it keeps the existing instance `WithOptions` and only adds a guard: a
late call, once a property is bound, throws — consistent with the binder's
programming-error channel.

Rejected because it detects the misuse instead of preventing it: the inconsistent-envelope
code still compiles and only fails at runtime, and the options still needlessly depend on
the request type. Moving the setter before `PropertiesOf` makes the same mistake
unrepresentable at no extra cost.

### A process-wide ambient default configured once (a static `Configure`)

Considered because it would let `Bind.PropertiesOf(request)` pick up an application-wide
policy with nothing threaded through call sites at all.

Rejected because it would introduce the first piece of global mutable state in the
library, contradicting the settled no-ambient-mutable-state stance (ADR-0006): it would
leak across tests running in parallel and reintroduce a "configured at the wrong time"
hazard of its own. The application-wide configuration ergonomic belongs in the future
ASP.NET Core integration, through dependency injection, where it is test-safe by
construction.

### Keep the instance setter and document the ordering more firmly

Considered because it is the smallest change.

Rejected because a prose "call before binding" is exactly the unenforced convention that
produced the defect; documentation cannot make the inconsistent envelope unrepresentable.

## Consequences

### Positive

* A single failure envelope always reports argument paths under one naming policy; the
  two-policy inconsistency is impossible to write.
* Options no longer depend on the request type, and the configured entry point is
  reusable across requests — one policy configured once, reused per request.
* The binder keeps the library's no-global-mutable-state property intact.

### Negative

* The fluent chain gains a distinct entry point (`Bind.WithOptions(...).PropertiesOf(...)`)
  beside the default `Bind.PropertiesOf(...)`, and a new public `ConfiguredBind` type to
  document.
* A consumer who set options after `PropertiesOf` / `FailWith` must move the call before
  `PropertiesOf` — a source change, mitigated by the pre-release status (no external
  consumers).

### Risks

* A future need to vary options per nested binder would not fit the "fixed at the entry
  point" model; mitigated by nested binders inheriting the parent options by design, and
  by this being outside the current requirements.

## Follow-up Actions

* Document the application-level configuration ergonomic (dependency injection) when the
  ASP.NET Core integration is built, rather than adding ambient configuration to the core.

## References

* ADR-0006 — supply arbitrary test values from a single seedable source; the
  no-ambient-mutable-state stance this decision keeps.
* ADR-0007 — name the binder terminals New and Create; a sibling public-API decision on
  the same binder.
* Issue #145 — the finding this decision resolves.
* Pull request #126 — the request binder feature these options belong to.

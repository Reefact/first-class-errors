# ADR-0017 | Provide a configurable application-wide default for the binder options

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0017-provide-a-configurable-application-wide-default-for-the-binder-options.fr.md)

**Status:** Accepted
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

* `Bind.PropertiesOf(request)` binds with `RequestBinderOptions.Default`. Binding with
  custom options otherwise requires `Bind.WithOptions(options).PropertiesOf(...)` —
  threading the configured entry point through each call, or resolving it from a DI
  container.
* A host without a DI container — a CLI, a worker, a small tool — has no host-agnostic
  way to set an application-wide default that the bare `Bind.PropertiesOf` picks up.
* ADR-0012 fixed a binder's options at its entry point (no change once binding has
  begun) and, among its rejected alternatives, rejected "a process-wide ambient default
  configured once (a static `Configure`)" on the grounds that it would introduce global
  mutable state, leak across tests running in parallel, and could be configured at the
  wrong time.
* `RequestBinderOptions` is an immutable value: a shared instance carries no mutable
  settings state, so the classic hazard — mutating a shared settings object while it is
  in use — does not apply to it.
* The .NET convention is split: `Newtonsoft.Json`'s `JsonConvert.DefaultSettings` is a
  freely re-settable global; `System.Text.Json`'s `JsonSerializerOptions` becomes
  immutable on first use (frozen) and is configured per-instance or through DI.
* The library exposes no other ambient mutable state: the clock, instance-id and
  arbitrary-value seams are immutable defaults with an `AsyncLocal`, scoped, test-only
  override (ADR-0006).
* The library is pre-release, unpublished on NuGet with no external consumers.

## Decision

`RequestBinderOptions.Default` — the options `Bind.PropertiesOf` binds with — is a
settable application default, configured once at application startup and frozen on the
first bind that reads it.

## Rationale

* A settable process default is the only host-agnostic way for the bare
  `Bind.PropertiesOf` to pick up an application-wide policy without a DI container, which
  a CLI or worker needs; the DI-friendly entry point (`Bind.WithOptions`) stays available
  where a container exists.
* The hazard ADR-0012 guarded against — an ambient default that drifts at runtime — is
  removed by freezing on first use: once the first bind reads it, reassignment throws, so
  it is a composition-time choice that cannot change while requests are flowing. This is
  the `System.Text.Json` discipline (immutable-once-used), not the freely-mutable
  `JsonConvert.DefaultSettings` one.
* Because `RequestBinderOptions` is immutable, a shared default carries no mutable
  settings state, so the classic global-settings footgun does not apply; the only global
  state is which immutable options the default points at, fixed once.
* Keeping the configuration on `RequestBinderOptions.Default` rather than on a method of
  `Bind` leaves the binding entry point free of configuration surface: a developer binding
  requests sees only binding verbs.
* The parallel-test-isolation concern ADR-0012 raised is confined to the library's own
  tests, and is met by a scoped, test-only override (an `AsyncLocal`) — the same seam
  pattern the clock uses (ADR-0006) — which never touches or freezes the production
  default.
* The pre-release status means the surface is settled now, when there are no consumers to
  migrate.

## Alternatives Considered

### Keep options entry-point-only (the status quo of ADR-0012)

Considered because it is already shipped and has no global state at all.

Rejected because it offers no host-agnostic application-wide default: every call site must
thread the configured entry point, or a DI container must supply it — unavailable to a CLI
or worker that wants to configure the binder once.

### A freely re-settable global default (the JsonConvert.DefaultSettings model)

Considered because it is the simplest settable global and a widely-used convention.

Rejected because a default that can be reassigned while requests are flowing can drift,
reintroducing the runtime-configuration hazard ADR-0012 warned about. Freezing on first
use keeps the ergonomic while removing the drift.

### Dependency injection only (the System.Text.Json / ASP.NET model)

Considered because it is the modern idiom where a container exists, and is fully
test-safe.

Rejected as the sole mechanism because it is not host-agnostic: a CLI, a worker, or any
host without a DI container cannot use it to make the bare `Bind.PropertiesOf` pick up an
application default. The injected entry point stays available; this decision adds the
container-free path.

## Consequences

### Positive

* Any host — with or without DI — configures the binder's naming policy and structural
  codes once at startup, and the bare `Bind.PropertiesOf` uses them.
* Freezing on first use prevents runtime drift; the only mutable global is an
  immutable-options reference set once.
* `Bind`'s surface stays free of configuration; a per-call `Bind.WithOptions` still
  overrides the default.

### Negative

* The library gains one piece of process-global state (the settable default) — the first
  such state in the library, accepted deliberately for the host-agnostic ergonomic.
* The library's own tests need a scoped, test-only override seam to stay parallel-safe;
  the production default is not directly settable within a parallel suite.

### Risks

* A consumer that reads `RequestBinderOptions.Default` before configuring it freezes it
  and can then no longer configure it; mitigated by the throwing setter's diagnostic
  ("configure at startup, before the first bind") and by documentation.

## Follow-up Actions

* Surface the test-override seam to consumers through a dedicated testing package if
  consumer demand appears (it is currently internal, for the library's own tests).

## References

* ADR-0012 — fix the binder options before binding begins; this decision revisits the
  process-wide ambient default that ADR-0012 weighed and rejected as an alternative,
  adopting it with mitigations. ADR-0012's own decision — a binder's options are fixed at
  its entry point — is unchanged, so ADR-0012 is not superseded.
* ADR-0006 — supply arbitrary test values from a single seedable source; the `AsyncLocal`
  test-seam pattern this decision reuses for its own tests.
* Issue #181 — the request this decision resolves.
* `JsonConvert.DefaultSettings` (Newtonsoft.Json) and `JsonSerializerOptions`
  (System.Text.Json) — the two conventions weighed.

# ADR-0023 | Keep expression-tree selectors for the v1 binder API

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0023-keep-expression-tree-selectors-for-the-v1-binder-api.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The request binder selects DTO properties through expression-tree selectors
(`r => r.GuestEmail`), from which it derives both the value and the argument
name behind every error path. Issue #151 (review finding 15/19) measured a
happy-path cost of roughly 2.2–2.4 µs and ~700 B allocated per bound property
and gated the remedy on a decision to take **before the v1 API freeze**.

Re-measurement on the current code (BenchmarkDotNet harness in
`FirstClassErrors.RequestBinder.Benchmarks`, whose README carries the full
tables) decomposed that cost:

* **~70–75 % of it is the expression tree itself**, allocated by the C#
  compiler *at the caller's call site* on every execution — ~488 B and
  ~416 ns per property. Roslyn caches delegate lambdas but never
  expression-tree lambdas, and no change inside the library can remove an
  allocation that happens before the library is entered.
* The remainder was library-internal — uncached reflection reads, boxing of
  nullable value types, eager path strings for list elements and nested
  prefixes — and has been eliminated by caching compiled getters and
  deferring path construction to the failure path (issue #151's
  implementation). What is left inside the library is the fluent API's own
  object-per-property shape.
* The same benchmark shows the selector cost is already avoidable **with the
  current API**: selectors hoisted into static fields bind 5 properties in
  ~342 ns / 880 B instead of ~2 805 ns / 3 600 B, and a compiler-cached
  delegate selector would cost ~1 ns / 0 B.
* A binder call site binds a DTO once per request at the primary-adapter
  boundary; the surrounding work (deserialization, I/O, the domain call)
  is measured in tens to hundreds of µs.

Two API-level remedies exist: overloads taking a name plus a plain delegate
(`SimpleProperty("GuestEmail", d => d.GuestEmail)`), which are additive and
non-breaking, or replacing the expression API outright, which is breaking.
Every selector overload exists six times (scalar and list, reference and
value-type, complex), so a parallel delegate family roughly doubles the
selector surface and creates two idiomatic ways to write every binding.

## Decision

The v1 binder selects DTO properties exclusively through expression-tree
selectors; the per-call expression cost is accepted for v1, and any
delegate-based fast path is deferred to a post-v1, additive decision.

## Rationale

The measured absolute cost — a few µs per request — is negligible against
the tens to hundreds of µs a request spends at the boundary it binds for;
the per-property cost matters only in outlier shapes (very wide DTOs on very
hot endpoints), and those callers already have a zero-breaking-change escape
hatch: hoisting selectors into static fields, which removes ~85–90 % of the
time and ~75 % of the allocation with the API exactly as it is.

A single selector idiom is worth more to v1 than the residual nanoseconds: a
duplicated delegate+name family would double a six-overload surface, split
call-site style across codebases, and re-introduce the stringly-typed
property naming the expression API exists to prevent — its single entry
point is what lets the binder derive value, name, and mis-declaration guard
from one construct. Because a delegate family is purely additive, deferring
it loses nothing: it can ship in any post-v1 minor if profiling of real
consumers ever shows the call-site tree to matter, whereas shipping it now
is a surface commitment v1 would carry forever.

## Alternatives Considered

### Add delegate+name overloads now

Considered because it is the only way to reach the ~0 B selector floor
(compiler-cached delegates) without breaking anyone. Rejected for v1: it
doubles the selector surface, institutionalizes two ways to bind before the
first stable release, and optimizes a cost that hoisting already lets hot
callers remove today.

### Replace the expression API with delegate+name

Considered for the same floor with a single idiom. Rejected: it is a
breaking redesign of the binder's central construct, reverts argument
naming to strings, and contradicts the compile-checked selector contract the
analyzers and guards build on.

### Generate selectors at compile time (source generator)

Considered as the long-term way to get expression-level ergonomics at
delegate-level cost. Rejected for v1 as a scope, not on merit: it is a new
component with its own compatibility surface; the ADR base records it as
the natural successor if the residual cost ever matters.

## Consequences

### Positive

* One selector idiom in v1; the six-overload surface stays as it is.
* The internal optimizations of issue #151 stand on their own: paths and
  reflection no longer cost anything on an all-valid bind, whichever way
  the selector decision evolves later.
* Hot callers have a documented, measured mitigation (hoisted selectors)
  that requires no library change.

### Negative

* The default call-site style keeps paying ~488 B / ~416 ns per bound
  property for the expression tree.

### Risks

* If v1 adoption surfaces workloads where the selector cost dominates in
  practice, the remedy is additive (delegate overloads or a source
  generator) — the risk is bounded to carrying this ADR's successor, not to
  a breaking change.

## Follow-up Actions

* Document the hoisted-selector mitigation in the binder's user guide when
  the topic first comes up with consumers.
* Re-run `FirstClassErrors.RequestBinder.Benchmarks` when the binder's
  selector surface next changes, and revisit this ADR with numbers.

## References

* Issue #151 — Binder: per-property binding uses uncached reflection and
  allocates a path string on the happy path (review finding 15/19).
* `FirstClassErrors.RequestBinder.Benchmarks/README.md` — measurement
  harness, full before/after tables, and cost decomposition.
* [ADR-0008](0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md) —
  the struct-constrained overload family the selector surface is built on.
* [ADR-0021](0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md) —
  the out-of-DTO entry, whose name-based path is the binder's existing
  non-expression shape.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) — the .NET
  Framework 4.7.2 floor the compiled-getter cache was verified against.

# ADR-0044 | Ship first-party JustDummies analyzers, and guard the reproducible async surface with them

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0044-ship-justdummies-analyzers.fr.md)

**Status:** Proposed
**Date:** 2026-07-27
**Decision Makers:** Reefact

## Context

* `JustDummies` is a test-support library: its whole value is that a broken arrangement *fails*. `Any.Reproducibly`
  runs a test body under a pinned seed and reports it on failure. It overloaded a synchronous `Action` and an
  asynchronous `Func<Task>` on one name.
* That overload set had a silent-failure footgun. An `async` lambda is a better conversion to `Func<Task>` than to
  `Action`, so `Any.Reproducibly(async () => { ... })` bound to the async overload, which returned a `Task`. A test
  method is usually a synchronous `void`, so the returned task was discarded; the body's assertions ran on a
  continuation after the method had already returned, and the failure surfaced — if at all — as a later
  `UnobservedTaskException`. **The test passed green.** The compiler's own `CS4014` does not fire in a synchronous
  method, so nothing warned.
* Renaming the async overload to a TAP-conventional `ReproduciblyAsync` fixes the naming, but on its own it *reopens*
  the hazard from the other side: with only `Action` overloads left on `Reproducibly`, an `async` lambda binds to
  `Action` as **`async void`**, whose post-await exception escapes the reproducible scope's `try/catch` entirely.
* C# offers no non-droppable `Task`, and no way to forbid an `async`-lambda→`Action` conversion. The two residual
  mistakes — passing an async body to `Reproducibly`, and discarding a `ReproduciblyAsync` task — are therefore not
  expressible in the type system.
* `FirstClassErrors` already ships Roslyn analyzers (`FCE001`…`FCE022`) inside its own NuGet package. `JustDummies`
  shipped none, and is a **standalone, error-agnostic** library (ADR guards it: it must never depend on
  FirstClassErrors). A JustDummies-specific rule cannot live in `FirstClassErrors.Analyzers` — that assembly ships in
  the FirstClassErrors package and carries the FCE identity — so a JustDummies consumer would never receive it.
* Alternative guards were considered and rejected in the design discussion: an `[Obsolete(error: true)]` "poison"
  overload (a member deprecated in a brand-new 1.0 is a contradiction that clutters the shipped surface), and an
  async overload that blocks with `GetAwaiter().GetResult()` (sync-over-async risks deadlock under a captured
  `SynchronizationContext` — the anti-pattern async exists to avoid).

## Decision

`JustDummies` ships its own first-party Roslyn analyzers, in a new `JustDummies.Analyzers` project packaged inside the
`JustDummies` NuGet package (`analyzers/dotnet/cs`), error-agnostic and independent of `FirstClassErrors`, under a
JustDummies-owned diagnostic-id scheme (`JDxxx`, mirroring `FCExxx`).

The first application makes the reproducible async surface un-misusable: the asynchronous entry point is
`Any.ReproduciblyAsync(Func<Task>)` (TAP-named, returns an awaited `Task`), the synchronous one stays
`Any.Reproducibly(Action)`, and two error-severity analyzers close the mistakes the types cannot — **JD001**, an
`async` lambda passed to `Any.Reproducibly`, and **JD002**, a discarded `Any.ReproduciblyAsync` task.

## Rationale

* The defect is invisible where it matters most — a passing build over a failing test — so a build-time error is the
  only enforcement strong enough. A warning, or documentation, leaves the green build green.
* The choice of enforcement follows what each mechanism can carry (the same grain as ADR-0035). The type system
  *cannot* express "this `Task` must be awaited" or "this async lambda must not bind here", so a run-time or
  compile-time analyzer is the legitimate tool — not a fallback, the only mechanism available. Where the language
  *can* carry the rule, it is preferred; here it cannot.
* A first-party analyzer is not exotic for this repository — it already ships and tests analyzers, with a floor-pinned
  Roslyn load contract and release-tracked rules. Extending that discipline to JustDummies reuses a proven pattern
  rather than inventing one, and keeps the JustDummies rules in the JustDummies package where their audience is.
* The rejected alternatives each trade the silent-green for a worse or uglier failure: the poison overload ships a
  deprecated member on day one; the blocking overload trades a silent green for a possible deadlock. The analyzer
  leaves the public surface clean (two honest methods) and the failure mode loud (a compile error).
* Splitting `Reproducibly`/`ReproduciblyAsync` by name — rather than keeping one overloaded name — is what lets JD001
  and JD002 be precise: each rule targets one method, so neither over-reports on the other's correct use.

## Alternatives Considered

### Keep the overloaded `Reproducibly(Func<Task>)` and add only a "don't discard" analyzer

Considered because it is the smallest change and needs a single rule. Rejected because it leaves a `Task`-returning
method without the `Async` suffix (a TAP violation and a lasting naming smell frozen at 1.0), and because the overload
that returns a droppable `Task` is exactly the shape the footgun exploits — the naming choice and the safety choice
are better made together.

### Poison overload — `[Obsolete("Use ReproduciblyAsync", error: true)] Reproducibly(Func<Task>)`

Considered because it closes the `async void` hazard purely in the language, with no analyzer. Rejected because
`[Obsolete]` means "deprecated since an earlier version", of which a fresh 1.0 has none; it ships a permanently
un-callable member in the very first public surface, which reads as an error rather than a design.

### Blocking async overload — `void Reproducibly(Func<Task>)` that runs the body with `GetAwaiter().GetResult()`

Considered because it exposes no `Task` to drop and needs no analyzer. Rejected because it forces sync-over-async on
every asynchronous test body, which can deadlock under a captured `SynchronizationContext`; trading a silent green for
an intermittent hang is not an improvement for a test tool.

### Put the JustDummies rule in `FirstClassErrors.Analyzers`

Considered because the analyzer project already exists. Rejected because that assembly ships inside the
FirstClassErrors package and carries the FCE identity: a JustDummies-only consumer would never receive the rule, and
routing a JustDummies rule through the error library breaks the standalone boundary the architecture test guards.

## Consequences

### Positive

* The silent-green footgun becomes a compile error: `Any.Reproducibly(async …)` (JD001) and a discarded
  `Any.ReproduciblyAsync(…)` (JD002) both fail the build, with a message pointing at the fix.
* The async entry point is TAP-named (`ReproduciblyAsync`), so it reads correctly and `CS4014` covers the
  await-in-async-method case for free.
* JustDummies gains a first-party analyzer story it can extend to future rules, in its own package, without coupling
  to FirstClassErrors.

### Negative

* The rename is a breaking change to the (pre-release, unshipped) public surface: `Reproducibly(Func<Task>)` becomes
  `ReproduciblyAsync`. Acceptable only in the pre-1.0 window, at no migration cost since there are no consumers.
* A second analyzer project, package-embedding target, and diagnostic-id scheme enlarge the repository and the
  JustDummies package's build.

### Risks

* The `JustDummies.Analyzers` load contract must stay pinned to the Roslyn floor, like `FirstClassErrors.Analyzers`,
  or the analyzer silently fails to load (CS8032) on older SDKs; mitigated by pinning `Microsoft.CodeAnalysis.CSharp`
  to `$(RoslynFloorVersion)`.
* JD001/JD002 detect the invocation by the `JustDummies.Any` metadata name and the method name; a future rename of
  those members would silently disable the rules, so their names are now part of the diagnostic contract.

## Follow-up Actions

* None required for the reproducible surface. Apply the same first-party-analyzer pattern when a future JustDummies
  mistake is expressible only at compile time.
* `AnyEnum` / `AnyGuid` conflict-message provenance (issue #314) is unrelated and unaffected.

## References

* ADR-0035 — enforce structural Any conflicts at compile time, value-dependent ones at run time; the "types where
  they can carry the rule, checks where they cannot" grain this decision follows.
* ADR-0031 — name Any's factories after their CLR type; precedent for "make the rule un-break-able rather than merely
  checked", and for TAP-style naming discipline on the surface.
* ADR-0042 — serialize draws on a random source; the sibling reproducibility fix (#310 / #311).
* Issue #317 — the silent-green footgun this ADR resolves.

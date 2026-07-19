# ADR-0022 | Floor the library's .NET Framework support at 4.7.2

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0022-floor-the-library-on-net-framework-4-7-2.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

The shipped libraries — `FirstClassErrors`, `FirstClassErrors.Testing` and
`FirstClassErrors.RequestBinder` — target **`netstandard2.0`**. A `netstandard2.0`
assembly is *consumable* by any runtime that implements the standard, and the
standard names **.NET Framework 4.6.1** as its minimum on that platform.

That 4.6.1 minimum is retrofitted. `netstandard2.0` shipped after .NET Framework
4.6.1, and support for it was added back: on 4.6.1 through 4.7.1 the `netstandard.dll`
facade and a set of `System.*` shims are delivered as NuGet assets and require
consumer-side binding redirects to load. **.NET Framework 4.7.2 is the first version
that ships those facades in-box**, and Microsoft's own `netstandard2.0` support
guidance recommends 4.7.2 or later.

Servicing reinforces the same line. .NET Framework 4.6, 4.6.1 (and 4.5.2) reached
end of support in April 2022; 4.6.2 is the oldest still serviced; and **4.8.1 is the
last .NET Framework** — there will be no 4.9.

Until now the product advertised, in `FirstClassErrors/README.nuget.md` and as an
incidental line in the Rationale of [ADR-0002](0002-floor-the-tooling-runtime.md),
that the library "runs on .NET Framework 4.6.1+". Nothing in CI ever loaded the
assemblies on a .NET Framework runtime: `build-test` runs the suite on .NET 10 and
the `floor` job runs only the *tooling* on the .NET 8 runtime. The compatibility
claim was therefore never verified.

The test stack is **xUnit v3**, whose lowest supported .NET Framework target is
**`net472`**; there is no supported way to run these test projects on an earlier
.NET Framework. CI already guards the two other runtime bounds of the product: the
tooling's .NET 8 floor ([ADR-0002](0002-floor-the-tooling-runtime.md)) and, ahead of
release, the next .NET preview (`canary.yml`).

## Decision

The supported .NET Framework floor for the `netstandard2.0` libraries is **4.7.2**.

## Rationale

4.7.2 is the lowest .NET Framework version on which the library runs *without
consumer-side plumbing*: it is the first to ship the `netstandard2.0` facades in-box,
so the "runs almost everywhere" promise becomes true rather than conditional on
binding redirects. It is therefore the honest floor, where 4.6.1 was the theoretical
one.

A support claim is only worth what verifies it. The 4.6.1 line was never exercised,
which is a liability for a library whose entire purpose is production diagnosability.
Flooring at 4.7.2 makes the claim *checkable on every pull request*, because 4.7.2 is
also the lowest framework the test stack itself can run on — the same number closes
both the support question and the verification question.

4.6.x is the wrong place to anchor the guard. 4.6 and 4.6.1 are end-of-life, and the
4.6.1–4.7.1 facade-and-binding-redirect fragility would make a red signal ambiguous —
the library's fault, or the platform's? A floor exists to give an unambiguous signal,
and only 4.7.2 provides one with the current stack.

Flooring the library at 4.7.2 mirrors the tooling floor at .NET 8: each supported
runtime bound is proven by its own dedicated job, so the product states its supported
runtimes precisely instead of by assertion. This decision **refines** the incidental
"4.6.1" claim in [ADR-0002](0002-floor-the-tooling-runtime.md) without superseding it:
that ADR's decision is the *tooling's* net8 floor, not the library's .NET Framework
floor, which had no ADR of its own until now.

## Alternatives Considered

### Keep advertising .NET Framework 4.6.1+ (status quo)

Considered because it is the `netstandard2.0` minimum on paper and demands no change.

Rejected because it was never verified and cannot be verified cheaply: 4.6.1's
`netstandard2.0` support is retrofitted and fragile, the versions concerned are
largely end-of-life, and xUnit v3 cannot target below `net472`, so proving the claim
would require a second test stack and consumer binding redirects for a runtime almost
nobody should still deploy.

### Floor at 4.6.2 (the oldest serviced 4.6.x)

Considered because 4.6.2, unlike 4.6/4.6.1, is still serviced, so it would keep the
lowest still-supported number.

Rejected because 4.6.2 predates the in-box facades: it carries the same
binding-redirect fragility as 4.6.1, and it is still below the xUnit v3 floor, so it
remains unverifiable with the current stack — the guard's signal would stay
ambiguous.

### Floor the libraries across the whole modern .NET matrix (net6/net8/… as blocking legs)

Considered as the conventional "test on everything" answer for broad reassurance.

Rejected because the library is a single `netstandard2.0` assembly and the modern
runtimes are one CoreCLR family: the behavioural delta across majors, for
zero-dependency value objects, is negligible; end-of-life majors should not be
floored at all; and a per-major matrix re-introduces exactly the per-release treadmill
that [ADR-0002](0002-floor-the-tooling-runtime.md) rejected. The single valuable
boundary is .NET Framework versus modern .NET, which `net472` covers, while the latest
runtime (`build-test`) and the next preview (`canary.yml`) already cover the modern
end.

## Consequences

### Positive

* The advertised .NET Framework support is now **verified on every pull request** by a
  dedicated Windows job, not merely claimed.
* The floor is **frozen**: 4.8.1 is the last .NET Framework, so this guard never
  chases a moving target and needs no per-release upkeep.
* The product's supported-runtime story is symmetric and precise: a library
  .NET Framework floor (4.7.2) and a tooling floor (.NET 8), each proven by its own
  job, with the next preview watched ahead of release.

### Negative

* Consumers pinned to .NET Framework 4.6.1–4.7.1 lose a claim of support they never
  actually had verified; they must be on 4.7.2 or later. Accepted: 4.7.2 is the
  practical `netstandard2.0` floor and the lower versions are largely end-of-life.
* A small, test-only `IsExternalInit` polyfill and a `net472`-conditioned build path
  are added to the affected test projects. The **shipped libraries are untouched** —
  they use neither `init` nor records — so nothing in the product depends on the
  polyfill.

### Risks

* The `net472` leg runs on Windows only, so a .NET-Framework-specific regression is
  invisible on the Linux legs until the Windows job runs. Mitigated by running the job
  on every pull request.
* `FirstClassErrors.RequestBinder.UnitTests` cannot join the floor because its
  fixtures bind `DateOnly`, a .NET 6+ type absent from .NET Framework; RequestBinder is
  floored through its property tests instead. Accepted: the excluded scenarios exercise
  a type that cannot exist on `net472` in the first place.

## Follow-up Actions

* State 4.7.2+ in `FirstClassErrors/README.nuget.md` (done in this change).
* Add the `framework-floor` job to `ci.yml` and the shared `build/Net472TestFloor.props`
  that carries the gated `net472` leg (done in this change).
* Make `framework-floor` a **required status check** in branch protection so it blocks
  merges, matching the intent that the floor is enforced, not advisory.
* Should a maintainer wish to reconcile the incidental "4.6.1" line in
  [ADR-0002](0002-floor-the-tooling-runtime.md), add an erratum note there pointing to
  this ADR; this ADR is the authority on the library's .NET Framework floor.

## References

* [ADR-0002](0002-floor-the-tooling-runtime.md) — the tooling runtime floor; the
  sibling runtime-bound decision this ADR refines.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — the analyzer's Roslyn floor,
  the third supported-tooling bound.
* `FirstClassErrors/README.nuget.md` — the user-facing support statement.
* `build/Net472TestFloor.props`, the `framework-floor` job in
  `.github/workflows/ci.yml`, and the library preview run in
  `.github/workflows/canary.yml` — where this decision is enforced.

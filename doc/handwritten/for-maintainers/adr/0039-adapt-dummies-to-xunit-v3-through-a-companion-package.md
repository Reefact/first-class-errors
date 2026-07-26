# ADR-0039 | Adapt Dummies to xUnit v3 through a companion package

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0039-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md)

**Status:** Accepted
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

A test that draws arbitrary values is reproducible only if a seed is pinned and
reported. `Dummies` supplies that through a runner that pins a seed for the
duration of a delegate and reports it when the delegate throws, so every
value-sensitive test must wrap its body in that delegate. The ceremony is
re-derived by hand in every consumer.

An adapter that removes it was anticipated and then lost. ADR-0006's follow-ups
called for an optional test-framework adapter "so the seed is surfaced
automatically, without wrapping each body"; ADR-0026 rebased the value engine
onto `Dummies` and did not carry that follow-up forward. The capability is
therefore anticipated by one accepted ADR and replaced by nothing. The
2026-07-20 `Dummies` architecture and design audit asks for an explicit yes or no
on it rather than continued silence, and places the decision in the first stable
cycle.

The capability an adapter must provide is narrow: pin a seed for the duration of
a test, and surface that seed to the developer **only when the test fails**. A
seed reported on every run is noise; a seed never reported leaves a failure
unreproducible.

`Dummies`' identity is that it depends on nothing beyond the standard library
(ADR-0011), a boundary an architecture test asserts over its own assembly. It
therefore cannot reference a test framework, so any adapter is a separate,
companion package — the arrangement `FirstClassErrors.Testing` already
establishes as a precedent in this repository.

The frameworks differ in what their supported extensibility exposes, and the
difference is decisive for the failure-only condition:

* **xUnit v3.** Its before/after test hook receives the test itself, and the
  ambient test context exposes the finished test's outcome — pass or fail, its
  failure cause and its exception detail — together with the test's output sink.
  The failure-only condition is therefore expressible in documented
  extensibility, with no involvement in test discovery or execution. The same
  hook is collected from the method, the class and the assembly, so a single
  attribute serves one test, a whole class or an entire suite, and it runs once
  per theory case rather than once per theory method.
* **xUnit v2.** Its equivalent hook receives only the method under test, and its
  assembly carries no test context at all. A v2 attribute cannot observe whether
  the test passed or failed. Expressing the failure-only condition there requires
  replacing the test-case discovery and execution chain.

The two versions ship distinct assemblies and namespaces, so one assembly cannot
reference both; supporting each would mean a separate package either way.

This repository's own test projects already run on xUnit v3, so a v3 adapter is
exercised by its author as well as documented.

The delegate runner keeps working on every framework and is unaffected by this
decision, so users of any other framework lose no capability — they keep the
form that exists today.

ADR-0038 opens the ambient seed scope as a public handle, so an adapter needs no
privileged access to `Dummies` and adding an adapter for another framework later
requires no change to it.

`Dummies` is published on the `dum` release train, which currently carries a
single package. The library is expected to move to its own repository in time.

## Decision

`Dummies` gains a companion package that pins and reports the seed automatically
for xUnit v3 tests, and targets no other test framework.

## Rationale

* **The failure-only report is the whole capability, and only v3 can express
  it.** Pinning a seed is easy everywhere; deciding whether to surface it is what
  separates a useful adapter from noise. xUnit v3 exposes the finished test's
  outcome in its documented extensibility, so the adapter is a small amount of
  code over a supported contract. In v2 the same condition is unreachable from
  the corresponding hook and requires owning discovery and execution over
  semi-internal surface — a permanent, fragile cost, taken on for a package
  explicitly not expected to be revisited.
* **One hook covers the whole surface.** Because the framework collects the hook
  from method, class and assembly and runs it per theory case, a single attribute
  serves a test, a class, a whole suite, and every case of a theory. That is the
  full surface the capability needs, without a second type per kind of test and
  without touching how tests are discovered.
* **Nothing is taken away from anyone else.** The delegate runner remains the
  portable form and keeps working on every framework, so choosing one framework
  for the adapter withholds no capability from users of the others; it withholds
  only the convenience.
* **A companion package is forced by the library's identity.** The zero-dependency
  boundary makes a test-framework reference impossible inside `Dummies`, and the
  repository already ships a companion package for exactly this reason.
* **The choice is dogfooded.** The repository's own suites run on xUnit v3, so
  the adapter is used where it is maintained rather than shipped untried.
* **Narrowness is deliberate and cheap to revisit.** Because ADR-0038 makes the
  seed scope publicly reachable, an adapter for another framework is an additive
  decision requiring no change here — so declining the others now forecloses
  nothing.

## Alternatives Considered

### Ship nothing and keep the delegate runner

Considered because it costs nothing, works on every framework, and is already
what consumers do. Rejected because it is what silence has already produced once:
the follow-up ADR-0006 recorded was dropped in the rebase and replaced by
nothing, and the audit asks for the question to be answered rather than left
open. The ceremony it preserves is re-derived by hand in every consumer, which is
the cost the adapter exists to remove.

### Support xUnit v2 as well, in a second package

Considered because v2 remains a large installed base, and "easily adopted" argues
for meeting consumers where they are. Rejected because the failure-only condition
is not expressible in v2's before/after hook: delivering it means replacing the
test-case discovery and execution chain and maintaining that against
semi-internal surface indefinitely. That is a disproportionate, permanent cost
for a convenience whose absence leaves v2 users exactly where they are today,
with the portable delegate runner.

### Derive from the framework's fact and theory attributes

Considered because it yields a single self-describing attribute per kind of test,
matching the name ADR-0006's follow-up had sketched. Rejected because it costs one
type per kind of test, does not compose with third-party fact attributes, cannot
be applied to a class or an assembly, and buys exposure to the discovery
internals in exchange for no capability the before/after hook lacks.

### Build one framework-agnostic adapter

Considered because it would serve every framework at once and make the choice
moot. Rejected because there is no cross-framework hook to build it on: the
capability is defined by what each framework exposes about a finished test, and
those surfaces have neither a common shape nor a common vocabulary.

## Consequences

### Positive

* Reproducibility becomes declarative and opt-in at the granularity the author
  chooses — a test, a class, or an entire suite — instead of a delegate wrapped
  around every value-sensitive body.
* A failing run names its seed without the author having anticipated the failure,
  which is the case the delegate runner serves only when it was applied in
  advance.
* The adapter is exercised by this repository's own suites.

### Negative

* A new published package, with its own documentation in English and French, its
  own public-API baseline, and its own place in the build and release pipeline.
* The convenience reaches xUnit v3 users only; every other framework keeps the
  delegate runner.
* The package's supported-framework floor is the test framework's, which is above
  the floor `Dummies` itself keeps — so the two cannot share one target list.

### Risks

* The adapter depends on the framework's before/after contract and on its
  exposure of a finished test's outcome. A future major version could change
  either. The exposure is bounded: the adapter uses documented extensibility, not
  internals, so a change would surface as a compilation or behavioural failure in
  the adapter's own suite rather than silently.
* A seed scope opened before a test must be closed even when the test throws, or
  the pinned seed leaks into whatever runs next in the same execution context.

## Follow-up Actions

* Publish the package on the existing `dum` train initially, so it versions with
  `Dummies`. **When `Dummies` moves to its own repository, revisit whether the
  companion package needs a train of its own** — a shared train is only
  appropriate while the two ship from the same place and cadence.
* Document the adapter in the user guide and the package readme, in English and
  French in lockstep, presenting the delegate runner as the portable form and the
  adapter as the xUnit v3 convenience.
* Revisit an adapter for another framework only on demonstrated demand; ADR-0038
  keeps each one additive.

## References

* ADR-0038 — Open the ambient seed scope to test-framework adapters: the public
  handle this package is the first consumer of.
* ADR-0006 — Supply arbitrary test values from a single seedable source: the
  follow-up that anticipated this adapter.
* ADR-0011 — Host Dummies as a standalone package: the zero-dependency boundary
  that forces a companion package.
* ADR-0026 — Rebase the testing package's arbitrary values on Dummies: the rebase
  in which the anticipated follow-up was dropped.
* `doc/handwritten/for-maintainers/audit/2026-07-20-dummies-architecture-and-design-audit.md`
  — the audit asking for an explicit decision.
* Issue #226 — the Dummies nice-to-have backlog where the adapter is tracked.

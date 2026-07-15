# ADR-0006 | Supply arbitrary test values from a single seedable source

**Status:** Proposed
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

The companion package `FirstClassErrors.Testing` exists so that tests about
errors and outcomes read like tests about values. It already ships two
test seams — a freezable clock and freezable instance ids — that share one
contract: overrides are scoped with `using`, disposable, and context-local
(backed by `AsyncLocal`), so they never leak across tests running in parallel.

A test almost always needs inputs it does **not** assert on: an error code, a
diagnostic or short message, an occurrence instant, an instance id. Two facts
follow from how those inputs are supplied today.

* **Legibility.** A hand-picked literal (`ErrorCode.Create("PAYMENT_DECLINED")`)
  reads as significant — "this value matters to the test" — even when the test
  never checks it. The reader cannot tell an asserted value from an incidental
  one.
* **Overfitting.** A fixed constant reused across a suite can let a test pass for
  the wrong reason (it happens to match that one constant). A value that varies
  between runs surfaces such coupling.

Established libraries solve this with an "anonymous / arbitrary value" facility
(AutoFixture, Bogus, the GOOS `Any` helper, FsCheck generators). Adopting one
here meets two hard constraints of this package:

* **Zero third-party runtime dependencies.** The package's stated promise is
  that it "adds nothing to your production dependencies". A shipped test-support
  package taking a dependency on AutoFixture/Bogus/FsCheck would impose that
  dependency, transitively, on every consumer.
* **Parallel-test safety on .NET Standard 2.0.** Test runners execute test
  classes concurrently; a single shared, mutable `System.Random` is not
  thread-safe and would produce cross-test interference and non-reproducible
  values. `System.Random.Shared` does not exist on the netstandard2.0 target.

Reproducing a failure that used an arbitrary value requires a **seed**: without
one, a failing run cannot be replayed.

## Decision

`FirstClassErrors.Testing` supplies arbitrary test values through a single,
dependency-free, context-local pseudo-random source whose determinism is opt-in
via a seed scope, and the clock and instance-id seams gain `UseAny` variants
layered on that same source.

## Rationale

* **Keeps the zero-dependency promise.** A first-party source built on
  `System.Random` adds no package reference, so the promise that the package
  adds nothing to a consumer's dependencies holds — the constraint that rules
  out AutoFixture/Bogus/FsCheck.
* **Parallel-safe by reusing the package's own idiom.** Storing the source in an
  `AsyncLocal` gives each execution context its own generator, which is exactly
  the "never leaks across parallel tests" contract the clock and instance-id
  seams already keep. The new surface is the same shape as the surface it sits
  next to, rather than a second, unrelated mechanism.
* **Arbitrary by default, reproducible on demand.** An unseeded default makes
  values differ between runs, which is what exposes overfitting; an opt-in seed
  scope makes a chosen test or run replayable. This directly serves the
  legibility and overfitting facts, and the reproducibility requirement, without
  forcing every test to manage a seed.
* **The name carries the intent.** Reaching for an explicitly *arbitrary* value
  reads as "this input is incidental", which is the distinction a hand-picked
  literal cannot make. The `UseAny` variants extend the existing `UseFixed` /
  `UseSequential` family on the same seams, so the clock and ids gain the same
  "value is irrelevant here" expression the value factory offers.

## Alternatives Considered

### Depend on AutoFixture, Bogus, or FsCheck

They are the mature, well-understood tools for arbitrary test data. Rejected
because each is a third-party runtime dependency that a shipped test-support
package would push onto every consumer, breaking the package's zero-dependency
promise; their broader object-graph and generator machinery also exceeds what a
small, focused "give me a value I don't assert on" helper needs.

### A shared static source without context-locality

The simplest implementation — one static `Random` behind the facade. Rejected
because it is not safe under parallel test execution: concurrent draws interfere,
values are not reproducible, and the coupling between unrelated tests is exactly
what the package's existing seams were designed to avoid.

### An instance-based generator (`new …(seed)`) as the primary surface

The AutoFixture-style shape, where a test constructs a generator and calls it.
Rejected as the primary surface because it threads generator state through each
test and diverges from the package's established `Use*` / disposable-scope idiom;
it remains available as a possible future addition for callers who want an
explicit object, but it is not the shape the package leads with.

### Add test factories to the value objects themselves

Put "make an arbitrary one" on `ErrorCode`, `Error`, and friends in the core
library. Rejected because it mixes a test concern into production types and would
ship test-only surface in the main package.

## Consequences

### Positive

* An incidental input is legible as incidental at the call site, and a failing
  run is reproducible once a seed is set.
* No new dependency reaches consumers, and the new surface reuses the package's
  existing parallel-safe, disposable-scope contract rather than inventing a
  second one.

### Negative

* A new public surface on a shipped package that must be maintained and kept
  documented in English and French.
* The generic enum helper can still return a sentinel value (such as `Unknown`);
  callers that need a *meaningful* value must use the dedicated per-enum helpers
  that exclude it.
* `ErrorContextKey` is deliberately left out of the first surface: keys live in a
  process-wide registry with no public reset, so minting arbitrary keys would
  accumulate global state across a run. Tests needing an arbitrary key are
  unserved until that is designed.

### Risks

* The unseeded default draws from a per-context generator; if two contexts
  coincidentally start from the same seed, their "arbitrary" values coincide.
  This is harmless (the values are not asserted on) and is mitigated by deriving
  the default seed from a fresh identifier per context.
* Reproducing a failure still depends on the author having set a seed; an
  unseeded failing test is not replayable from its output alone. Surfacing the
  auto-chosen seed for replay is a possible later refinement.
* Until enforced by tooling, the "arbitrary value ⇒ use the source, not a
  literal" habit holds only through review and documentation.

## Follow-up Actions

* Document the surface in the testing guide, in English and French, in lockstep.
* Consider a design for arbitrary `ErrorContextKey` values that respects the
  process-wide registry.
* Consider surfacing the auto-chosen seed so an unseeded failure can be replayed.
* Consider an instance-based generator if callers ask for an explicit object.
* Consider extracting the generic value engine into a standalone, error-agnostic
  utility if a second consumer appears; it is kept internally separable from the
  error-specific surface to that end.

## References

* `doc/ArbitraryTestValues.en.md` — the guide where the new surface is documented.
* ADR-0005 — prior naming decision in the same spirit (a name should announce
  what a call does); context only, not a precedent for this choice.

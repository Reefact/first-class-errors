# ADR-0042 | Serialize draws on a random source, and scope reproducibility to the draw sequence

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0042-serialize-draws-on-a-random-source.fr.md)

**Status:** Accepted
**Date:** 2026-07-27
**Decision Makers:** Reefact

## Context

`JustDummies` draws every arbitrary value from a `RandomSource`, which owns one `System.Random`. Two sources exist: the ambient one behind the static `Any` entry points, whose state lives in an `AsyncLocal`, and the fixed one owned by an `AnyContext` from `Any.WithSeed`.

`System.Random` is not thread-safe. Its seeded implementation mutates an array and two indices on every draw with no synchronisation; under contention the two indices can converge, after which the generator returns zero permanently. Nothing resets it. Because the value layer maps a zero draw onto the bottom of whatever range was declared, every generator then settles on the minimum of its own domain — `0`, `""`, `Guid.Empty`, `int.MinValue` — for the remaining life of that source, and no exception is raised.

A source reaches several threads by two ordinary routes, neither of which is a misuse.

* An `AsyncLocal` **flows into** the tasks and threads its owner starts. Once a seed scope is installed — which `Any.Reproducibly` and `Any.UseSeed` always do — a `Parallel.For` or a `Task.WhenAll` inside the test hands the same source to every worker. Outside a seed scope the ambient state is created lazily, so each worker writes its own slot and gets its own generator: the unseeded path is unaffected, and pinning a seed is what creates the sharing.
* An `AnyContext` is an object; whoever holds it can share it.

ADR-0006 recorded the original decision and named this hazard exactly — *"a single shared, mutable `System.Random` is not thread-safe and would produce cross-test interference and non-reproducible values"* — then chose `AsyncLocal` context-locality as the remedy. That remedy addresses the **cross-test** axis: two tests running in parallel never see each other's seed, which holds and is separately guarded. It does not address the **intra-test** axis, which the ADR does not consider; and the mechanism it selects is what propagates the shared instance. ADR-0006 is superseded by ADR-0026, which rebases `FirstClassErrors.Testing` onto `JustDummies` and does not revisit the question.

Two properties of the library bear on the remedy. Draws sit on error and arrangement paths, never in hot loops. And `Any.UseSeed` is public since ADR-0038 — opened for test-framework adapters, but usable by any caller, including inside a parallel loop body, where each worker's own `AsyncLocal` slot makes the scope private to that iteration.

The package's stated promises are that values are arbitrary yet valid, and that a run is reproducible from a reported seed. The user documentation states that the source is *"safe under parallel tests"* and explains the `AsyncLocal`; `AnyContext`'s remarks state it is *"not thread-safe"* without anything enforcing it. Neither source is protected, and the two say different things.

`JustDummies` is pre-1.0 and unpublished (ADR-0011), so the contract can still be set rather than corrected.

## Decision

Every draw on a random source is serialized on that source's own lock, and the reproducibility promise is scoped to a sequence of draws taken one at a time — a parallel run replays only when each unit of work opens its own seed scope.

## Rationale

* **The defect is a mutation hazard, not a scoping one, so the remedy belongs where the mutation is.** `AsyncLocal` answers *which* source is in effect; it was never able to answer *how* that source is touched. Adding a lock leaves the scoping decision of ADR-0006 intact and supplies the property it was mistakenly believed to provide. Removing the `AsyncLocal` would break the cross-test isolation it does provide, and replacing it with thread-affine storage would lose the seed at the first `await`.
* **Serializing costs nothing that matters, and preserves every existing run.** An uncontended lock does not change the order in which a single thread consumes the stream, so a pinned seed replays bit-identically — the property that lets this ship without invalidating any test, in this repository or in a consumer's. On paths that are arrangement rather than computation, the cost of the lock is immaterial.
* **The generator must be the only door, so that bypassing it cannot compile.** Handing out the underlying `Random` behind a synchronized façade would leave every member the façade does not override silently unprotected, and the next draw added would decide by accident whether it is safe. Keeping the instance private turns that into a compile error. This is the same reasoning ADR-0031 and ADR-0035 apply elsewhere: make the rule un-break-able rather than merely checked.
* **The promise has to shrink to what serialization actually delivers.** The lock is taken per primitive draw, and a single generated value may consume many — a string draws once per character — so two threads interleave *inside* one generation. Neither the sequence nor the multiset of generated values is therefore stable under parallelism, and a promise of parallel reproducibility would be false. Stating the narrower guarantee is what keeps the seed report trustworthy, which is the same standard the library already applies when it withholds a full-replay claim for a foreign generator.
* **The narrower promise costs the user nothing, because the wider one is already reachable.** A scope opened inside a parallel loop body is private to its worker, so deriving one seed per unit of work from the run's seed makes the whole run replay. That mechanism is already public, so the decision adds no surface: it documents a capability rather than building one.
* **One rule for both sources removes a contradiction.** Locking the shared choke point protects the ambient source and `AnyContext` alike, which lets the latter's unenforced "not thread-safe" remark be replaced by the contract that now holds for both.

## Alternatives Considered

### Give each thread its own generator

Removes the corruption without a lock, by deriving a per-thread generator from the run's seed. Rejected because it destroys the property the library exists to provide: the mapping from thread to sub-stream is set by the scheduler, so the same seed yields different values from one run to the next. In a seeded library the number of generators and their ownership *is* the reproducibility contract, not an implementation detail — the very reason this cannot be treated as a local choice about thread safety.

### Throw when a source is drawn from concurrently

The "forbid it explicitly" branch, and the one most aligned with the library's habit of failing fast on a contradiction. Rejected on two counts. A concurrent draw is not a contradiction: a test that parallelises without needing a per-call replay is legitimate, and under a lock it works. And the detection is unsound in the shape that matters — an `async` test legitimately resumes on another thread with no concurrency at all, so any thread-affinity check would reject correct code, while a true overlap detector costs what a lock costs and delivers less.

### Use a thread-safe generator from the platform

`Random.Shared` is thread-safe and lock-free. Rejected because it cannot be seeded, which forecloses the entire reproducibility surface, and because it does not exist on the `netstandard2.0` target the library floors on (ADR-0022).

### Leave it and document the limitation

Rejected because the failure is silent and its result is indistinguishable from a legitimate value: a dummy that becomes `0`, `""` or `Guid.Empty` is exactly the value most likely to make an assertion pass for the wrong reason. A limitation a user can neither observe nor detect is not one documentation can discharge.

## Consequences

### Positive

* Concurrent draws can no longer degrade a source, on either the ambient or the context path, and a source stays usable for the sequential draws taken after a parallel section.
* Existing seeded runs are unaffected: single-threaded sequences are bit-identical.
* The two sources state one contract instead of two contradictory ones.
* Reproducible parallel generation becomes an expressible, documented recipe rather than an unstated impossibility.

### Negative

* Every draw goes through a lock, including the overwhelming majority that are single-threaded and cannot contend.
* The reproducibility promise is now explicitly conditional, which is a weaker sentence to write in the documentation than the one a reader might have assumed.
* Callers of the internal source now go through its methods rather than a `Random`, so a future draw primitive must be added to that type before it can be used.

### Risks

* A user who parallelises and expects the seed alone to replay the run will find it does not. Mitigation: the condition is stated in the XML documentation of the draw and of both seeding entry points, and the per-work-item recipe is documented in the user guide.
* Serialization makes a *pathologically* parallel arrangement slower than it would otherwise be. Accepted: dummy generation is not a hot path, and the alternative is silent corruption.

## Follow-up Actions

* Revisit only if a measured workload shows the lock to be material, which would mean reopening the per-work-item sub-stream idea as a *public* seam rather than as an internal substitution.

## References

* Issue [#310](https://github.com/Reefact/first-class-errors/issues/310) — the defect and its measurements.
* [ADR-0006](0006-supply-arbitrary-test-values-from-a-seedable-source.md) — the original seedable-source decision, which named the hazard and addressed the cross-test axis only.
* [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) — supersedes ADR-0006 without revisiting the question.
* [ADR-0038](0038-open-the-ambient-seed-scope-to-adapters.md) — makes the ambient seed scope public, which is what puts the per-work-item recipe within reach.
* [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) — the `netstandard2.0` floor that rules out `Random.Shared`.
* [ADR-0031](0031-name-any-factories-after-their-clr-type.md), [ADR-0035](0035-enforce-structural-any-conflicts-at-compile-time.md) — the "make the rule un-break-able rather than merely checked" precedent.

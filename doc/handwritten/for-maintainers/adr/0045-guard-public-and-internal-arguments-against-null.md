# ADR-0045 | Guard public and internal arguments against null, enforced by a reflection convention

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0045-guard-public-and-internal-arguments-against-null.fr.md)

**Status:** Proposed
**Date:** 2026-07-27
**Decision Makers:** Reefact

## Context

* `JustDummies` treats invariants as the point of the library: a broken arrangement must *fail*, close to its cause.
  Its value objects and results are validating classes (the repository's class-not-struct rule), whose whole guarantee
  is that no instance exists without having passed a validating entry point.
* Nullable reference types are a **compile-time** annotation only. A caller with nullable analysis disabled, a
  `null!`, reflection, or `default` can still route a `null` through a parameter typed as non-nullable at run time. The
  library already reasons from this fact — it is the stated reason value objects are classes, not structs.
* Before this change, many members did not validate their reference arguments. The gap was widest at the **internal
  boundary**: the `Create(RandomSource)` factories and internal constructors, where a class's dependencies (the random
  source, the interval/string/URI specs) first enter it. The public API can never route a `null` into those members,
  so a `null` reaching them could only come from an internal wiring mistake — and would surface later as a
  `NullReferenceException` far from its cause.
* The contract suite was strictly **black-box**: no `InternalsVisibleTo` existed, so every test exercised the public
  surface only. The 2026-07-20 architecture audit (§9.3) recorded this as a deliberate choice — it proves the public
  API is sufficient to specify the library, and makes engine refactors test-transparent.
* Constructing an exception happens on the error-handling and logging path. `System.Exception` tolerates a `null`
  message and inner exception.
* The library floors on **.NET Standard 2.0** (so `ArgumentNullException.ThrowIfNull`, a .NET 6+ API, is unavailable),
  and the contract suites additionally run on the .NET Framework 4.7.2 support floor. Reflection nullability metadata
  (`NullabilityInfoContext`) is a .NET 6+ API.

## Decision

Every `public` or `internal` member of `JustDummies` — constructor or method — rejects a `null` non-nullable
reference-type argument with an `ArgumentNullException` naming the parameter, exception-type constructors excepted; a
reflection-driven convention test enforces this across the whole surface, and the library's internals are opened to the
contract suite so it can.

## Rationale

* **The class, not the assembly, is the trust boundary.** A member cannot assume its callers are correct, and "caller"
  includes another class of the same assembly. Validating what crosses the boundary — and only there, trusting what a
  validating member has already accepted — is what keeps a `null` from travelling far from the mistake that produced
  it, without redundant re-checking inside the class.
* **Nullable annotations are not enforcement.** Because they vanish at run time, the only mechanism that actually
  rejects a `null` is a runtime guard. This is the same reasoning the repository already accepts for making value
  objects classes rather than structs; applying it to argument validation is consistent, not new.
* **The internal boundary is the guard most worth having and the hardest to test.** It is where dependencies first
  enter a class, so it is where an internal wiring bug is caught — yet the public API can never drive a `null` there, so
  a public-only convention would leave exactly that guard unverified. Verifying it is what makes opening the internals
  worth its cost.
* **Only reflection makes the convention self-maintaining.** The convention must hold for every member that exists and
  every one added later; a test that discovers members by reflection holds a new generator, factory, or fluent method
  to it automatically, with nothing to add. Hand-written per-parameter tests forget exactly the new member the
  convention exists to catch.
* **Relaxing black-box is the price of verifying the internal boundary, and it is bounded.** Since a `null` cannot
  reach the internal members through the public API, verifying their guards requires internal access. The behavioural
  suites keep their black-box posture and its benefits; the single test that needs internals names no member — it is
  reflection-generic — so it stays refactor-transparent. What is given up is only the property that *all* tests touch
  the public surface alone.
* **Exceptions are exempt because a guard there would defeat its own purpose.** Their constructors run while an error
  is being handled or logged; throwing an `ArgumentNullException` over a `null` message would mask the original
  failure, and the base type already tolerates it.

## Alternatives Considered

### Keep the black-box posture: enforce the convention over the public surface only

Considered because it preserves the deliberate posture the audit records without opening any internals. Rejected
because it leaves the internal boundary's guards — the `Create` factories and internal constructors the public API can
never route a `null` through — unverified, which is the coverage the convention most needs; and it does not match the
decision's own scope, which is *public or internal*.

### Exercise internals by reflection without `InternalsVisibleTo`

Considered because it keeps the literal "no `InternalsVisibleTo`" fact true. Rejected because it still exercises
internals — so it relaxes the very same posture — while forcing the test to reach, through reflection alone, types it
is not allowed to name; if the posture is being relaxed, doing it explicitly is clearer and no more of a departure.

### Hand-written per-parameter tests

Considered as the most black-box-friendly option. Rejected because it does not self-maintain: every new member needs a
new test, and a forgotten guard on a new member — the exact defect the convention exists to prevent — is exactly what a
hand-written suite also forgets.

### Rely on nullable reference annotations, or an analyzer, instead of runtime guards

Considered because annotations document intent at compile time and an analyzer could flag missing guards. Rejected
because neither rejects a `null` at run time, which is the guarantee sought; downstream consumers may compile with
nullable analysis disabled, and the library's own class-not-struct rule already rests on run-time enforcement being the
only real one.

## Consequences

### Positive

* A `null` argument fails fast at the boundary, as an `ArgumentNullException` naming the parameter, instead of later as
  a `NullReferenceException` far from the cause.
* The convention is self-enforcing: a new `public`/`internal` member is held to it automatically, with no test to write.
* The internal boundary — previously unreachable by any test — is now verified.

### Negative

* The deliberate black-box test posture is relaxed: the library's internals are visible to the contract suite.
* A small, permanent volume of guard code is spread across the public and internal surface.
* The convention test uses .NET 6+ reflection nullability metadata, so it runs on the modern leg only and is excluded
  from the net472 support-floor build; the guards it enforces are themselves netstandard2.0.

### Risks

* The convention test can only verify a member it can construct valid arguments for. Mitigation: a member it cannot
  exercise is reported as *uncovered* and fails the test (fail-loud), never silently skipped — a coverage gap shows up
  as a red test, to be closed by a sample or an explicit test.
* Open internals could tempt future tests into white-box coupling. Mitigation: the convention test names no member, and
  the behavioural suites stay black-box.

## Follow-up Actions

* None required for the convention to hold: future members are covered automatically. Keep the convention test green.
* The 2026-07-20 audit's §9.3 observation that "no `InternalsVisibleTo` exists" is, from this decision on, deliberately
  no longer true.

## References

* [ADR-0011](0011-host-dummies-as-a-standalone-package.md) — JustDummies is a standalone, error-agnostic package.
* [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) — the Testing package rebases its arbitrary values on JustDummies.
* 2026-07-20 JustDummies architecture and design audit, §9.3 (testing strategy — the black-box posture).
* `CLAUDE.md` — the value-object class-not-struct rule (run-time enforcement of invariants).

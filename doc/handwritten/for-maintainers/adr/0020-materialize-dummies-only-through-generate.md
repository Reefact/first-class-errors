# ADR-0020 | Materialize dummies only through Generate()

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0020-materialize-dummies-only-through-generate.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

`Dummies` is a fluent DSL of typed, constraint-carrying generators. Every
generator implements `IAny<T>`, whose single member `Generate()` draws one value
satisfying the declared constraints; the composition seams `As` and `Combine`
build larger generators and materialize their parts by calling `Generate()`. The
library's stated model is that a generator is an **immutable recipe, not a
value**: randomness is drawn only when `Generate()` runs, and the same recipe can
be generated from several times, yielding a fresh value each time.

Until now, each concrete generator also defined an **implicit conversion** to its
generated type — 28 operators in total, one per simple type and one per
collection type (`List<T>`, `T[]`, `HashSet<T>`, `Dictionary<TKey,TValue>`). That
conversion let an explicitly-typed assignment read tersely, for example a
`string` local assigned directly from a string generator.

Several facts about that conversion were surfaced during a focused review of the
library (issue #190):

* The conversion has **side effects**: it draws randomness, so it is not a
  widening; it can **throw** (`AnyGenerationException`,
  `ConflictingAnyConstraintException`) at a site that reads like a plain
  assignment; and it is **not idempotent** — each conversion draws a fresh value,
  so reading the "same" variable twice yields two values.
* The conversion fires in only **one** syntactic shape — an explicitly-typed
  local or parameter. In the adjacent shapes it silently does something else:
  `var` binds the generator, `object` and `params object[]` box the generator,
  generic inference passes the generator, and competing overloads can resolve to
  the generator rather than the value. The test suite already had to use
  explicitly-typed locals around a `params object[]` API for this reason.
* `Generate()` already works uniformly in every one of those contexts, is the
  member generic inference flows through, and is the operation the composition
  seams use. It is the dominant idiom across the test suite.

Two constraints bound the timing. `Dummies` is a pre-1.0, standalone package
whose API is expected to churn most in its early iterations, and it is referenced
only by its own test project (ADR-0011); removing a public operator surface is
therefore cheap now and a breaking change once a stable `1.0` is published. Issue
#190 also requires the contract of these conversions to be decided and recorded
before that `1.0`.

## Decision

Concrete `Dummies` generators expose no implicit conversion to their generated
type: a value is materialized only by `Generate()`, called directly or by the
`As` and `Combine` composition seams that call it internally.

## Rationale

* **An implicit conversion should be cheap, total, and referentially
  transparent; this one is none of those.** Because it draws randomness, can
  throw, and returns a different value on each run, it is an effectful method
  call disguised behind an assignment. That directly contradicts the model the
  library teaches — a generator is a recipe, and the value is drawn only at
  `Generate()` — by providing the one path that lets a caller forget the draw is
  happening at all.
* **The convenience is a partial, surprising abstraction.** It behaves as
  advertised in a single syntactic shape and silently misbehaves in the shapes
  next to it. Keeping it and documenting the hazard would describe an accidental
  complexity rather than remove it; the complexity is accidental precisely
  because `Generate()` already covers every context uniformly.
* **Removal costs no capability.** `Generate()` is already the canonical path —
  interface-level, the target of generic inference, the operation the
  composition seams call, and the dominant idiom in the suite. What is lost is a
  shorthand that saved one call in one context, not any expressiveness.
* **This is the cheapest moment to decide.** The package is pre-1.0, standalone,
  and self-consumed (ADR-0011), so the change touches only its own tests today;
  the same removal after a stable `1.0` would break every consumer that assigned
  a generator to a typed local. Issue #190 requires the decision to be recorded
  before that release.

## Alternatives Considered

### Keep the conversions and document the contract

The direction issue #190 leads with. Considered because it preserves the terse
headline call site and states, in documentation, where the conversion does and
does not run. Rejected because it documents a hazard instead of removing one, and
keeps an effectful, non-idempotent, throwing conversion that contradicts the
recipe-versus-value model at the center of the library.

### Keep the conversions and add an analyzer for the misleading contexts

Considered because an analyzer flagging `var`, `object`, and generic-inference
uses could preserve the ergonomics while catching the traps. Rejected because it
is a large, permanent surface — 28 operators plus an analyzer and its tests — to
preserve a one-call shorthand, and a "converts, except where the analyzer says it
does not" contract is itself split-brained. Removing the operators makes the
analyzer moot, which is why issue #190 lists it as optional.

### Remove the conversions only from some types

Considered as a compromise — for example keeping them on immutable simple types
and dropping them only on collections. Rejected because a per-type rule is harder
to explain than either uniform choice and still leaves the effectful-assignment
surprise on the types that keep it.

## Consequences

### Positive

* There is one obvious, uniform way to materialize a value, and the
  recipe-versus-value distinction the library teaches is no longer contradicted
  by a feature that hides the draw.
* A generator never silently stands in for its value under `var`, `object`,
  `params object[]`, generic inference, or overload resolution; those sites now
  fail to compile instead of misbehaving.

### Negative

* The headline call site is more verbose: an explicitly-typed assignment gains a
  `.Generate()`.
* Twenty-eight operators, along with their tests and documentation examples, are
  removed; the pre-1.0 public surface changes — acceptable now, and the reason
  the decision is taken before `1.0`.

### Risks

* A user carrying an implicit-conversion mental model may at first omit
  `.Generate()`. The risk is bounded: the omission is a compile-time error with
  an actionable message (assign through `IAny<T>` or call `Generate()`), never a
  silent wrong value.

## Follow-up Actions

* Update the documentation so `.Generate()` is presented as the sole
  materialization — the package README and the XML docs — done in the same
  change as this decision.
* Do not pursue the optional analyzer suggested in issue #190; the removal makes
  it unnecessary.
* Revisit only if a future, non-test consumer demonstrates an ergonomic need the
  `Generate()` form cannot meet.

## References

* Issue #190 — Define and document the contract of implicit generator
  conversions.
* ADR-0011 — Host Dummies as a standalone package (pre-1.0 churn, self-consumed).
* ADR-0006 — Supply arbitrary test values from a single seedable source.
* `Dummies/IAny.cs` — the `Generate()` contract these generators flow through.

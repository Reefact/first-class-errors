# ADR-0015 | Cap Any.Combine at arity eight

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0015-cap-any-combine-at-arity-eight.fr.md)

**Status:** Proposed
**Date:** 2026-07-18
**Decision Makers:** Reefact

## Context

`Dummies` composes constrained generators into larger objects through
`Any.Combine(parts..., compose)`: the parts are generated, then handed to a
caller-supplied constructor lambda, so a value object or aggregate is assembled
without reflection and the domain's own constructor stays the single gatekeeper.
Building large objects easily is an explicit goal of the library.

C# has no heterogeneous variadic generics: passing *N* differently-typed parts in
one call requires a distinct overload per arity, each with *N*+1 generic type
parameters and *N*+1 value parameters. Until now `Combine` existed only for two
and three parts; composing more forced nesting (`Combine(Combine(a, b, …), c, …)`)
or routing through tuples, both of which surface positional `Item1..ItemN` access
or nested lambdas at the call site.

The repository runs a SonarCloud analysis whose rule S107 flags a method with more
than seven parameters. A `Combine` of seven parts has eight parameters (seven
generators plus the composer) and of eight parts, nine — so the two largest
overloads cross that threshold. The repository's standing practice is to keep the
analysis clean, suppressing a rule inline with a justification where a deliberate
exception is made.

A constructor that needs many parts is itself often a design signal — a missing
intermediate value object.

## Decision

`Any.Combine` offers overloads from two up to eight parts and stops there,
accepting the parameter- and generic-count code smell of the two largest overloads
as a deliberate trade-off for a flat, reflection-free composition call site.

## Rationale

* **It serves the "build large objects" goal directly.** A flat
  `Combine(a, b, c, d, e, (…) => new Thing(…))` with caller-named lambda parameters
  reads far better than nested `Combine` calls or tuple `Item1..ItemN` access, and
  keeps the composition reflection-free — the whole point of `Combine`.
* **Eight is where the ceiling belongs.** Eight parts cover essentially every
  hand-written DDD constructor; beyond that, the object is complex enough that
  intermediate value objects are the healthier design, so a ceiling at eight nudges
  toward that structure instead of smoothing over arbitrarily wide constructors.
* **The smell is inherent, not accidental.** There is no lower-smell way to pass
  *N* differently-typed parts in a single call; the parameter and generic counts
  are the irreducible cost of heterogeneous composition. Suppressing S107 with a
  justification on the two largest overloads records that trade-off at the code,
  and this ADR records why it is acceptable.
* **Stopping short of sixteen keeps the surface bounded.** Matching `Func`'s
  sixteen-argument ceiling would add hand-maintained, fully-documented overloads
  whose marginal value is low and whose boilerplate cost is real; the demand past
  eight does not justify it.

## Alternatives Considered

### Keep only arity two and three; compose more by nesting

Considered because it adds no new surface. Rejected because nesting forces
positional tuple access (`Item1..ItemN`) or nested lambdas at the call site —
unreadable precisely where the library promises easy large-object construction.

### A fluent tuple-accumulating builder (`Combine(a).And(b).And(c)…`)

Considered as a way to avoid one overload per arity. Rejected because `.And` would
itself need an overload per source arity, and the accumulated tuple exposes
positional access again — it trades one form of boilerplate for a worse call site.

### Extend all the way to arity sixteen

Considered for completeness, matching `Func`. Rejected because nine-to-sixteen-part
constructors are a design smell the library should not smooth over, and each
overload is fully-documented, hand-maintained surface with negligible real demand.

### A `params` array of same-typed generators

Considered for the homogeneous case. Rejected because it only works when every part
shares one type and it loses per-part typing — it does not serve the
heterogeneous-constructor case `Combine` exists for. It remains an orthogonal option
that could be added later without touching this decision.

## Consequences

### Positive

* Large value objects and aggregates compose in one flat, readable, reflection-free
  call, with caller-named parameters.
* The ceiling gently steers very wide constructors toward intermediate value
  objects.

### Negative

* Five more hand-maintained, fully-documented overloads on the facade.
* The two largest overloads carry an inline S107 suppression — a documented,
  localized exception to the parameter-count guideline.

### Risks

* **Ceiling pressure** — a genuine nine-or-more-part need could recur. Mitigated
  because that is itself a design signal; the ceiling is revisited only if the need
  proves common, and adding higher arities later stays non-breaking.

## Follow-up Actions

* If a homogeneous, same-type composition need appears, consider a `params`-based
  `Combine` separately — it is orthogonal to this decision.
* Reflect the arity ceiling in the user documentation once the surface stabilizes.

## References

* ADR-0011 — Host Dummies as a standalone package in this repository.
* The S107 suppressions on the arity-seven and arity-eight `Combine` overloads.

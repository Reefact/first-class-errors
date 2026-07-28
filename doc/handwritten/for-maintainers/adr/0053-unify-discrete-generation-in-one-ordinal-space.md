# ADR-0053 | Unify discrete generation in one ordinal space, with a dedicated engine only where the arithmetic substrate forces one

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0053-unify-discrete-generation-in-one-ordinal-space.fr.md)

**Status:** Accepted
**Date:** 2026-07-28
**Decision Makers:** Reefact

## Context

JustDummies exposes the same interval-shaped constraint algebra over a wide set of value types: the
eight fixed-width integers, `char`, `TimeSpan`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
the three binary floating-point types, `decimal`, and the two 128-bit integers. Across all of them a
test may declare bounds, an allow-list, exclusions, and — where the type has a natural stride — a
lattice such as a multiple, a temporal granularity, or a decimal scale.

Two library-wide promises constrain how that algebra may be implemented. Values are **built to
satisfy** the declared constraints rather than drawn and filtered, so a generator that exists must
produce a value in one draw with no retry loop. And constraints that contradict each other must fail
at declaration time with a message naming **both** sides, which requires each bound to carry the
constraint that set it rather than just a number.

The types divide by arithmetic substrate, not by kind:

* Every discrete type whose domain fits 64 bits — the integers, the ticks-based time types, day and
  time-of-day numbers, `char` — admits an **order-preserving** mapping onto the unsigned 64-bit
  range. Bounds, exclusions, strides, cardinality and sampling are then the same problem for all of
  them, stated once over ordinals.
* `Int128` and `UInt128` have domains that exceed 64 bits, so no such mapping into a 64-bit ordinal
  exists.
* IEEE binary floating point is continuous. Its bit patterns are monotonic and could be mapped, but a
  uniform draw over bit patterns is not a uniform draw over values — roughly half of all `double`
  values lie in `[-1, 1]`. Excluding a point from a continuum also differs in kind from excluding one
  from a finite set: the collision has measure zero, yet the constraint must still hold exactly.
* `decimal` is a 96-bit mantissa with a scale, and it has no next-representable-value ladder, so an
  exclusive bound cannot be expressed by stepping to the adjacent value the way it can for integers
  and for floats.

The floor target is netstandard2.0 (ADR-0022 fixes the .NET Framework floor the library must keep
loading on). It offers no generic math abstraction over numeric types, and no 128-bit integers at
all, so arithmetic cannot be written once against a numeric type parameter in code that must compile
on the floor. C# additionally forbids the self-referential generic base class pattern for the public
sealed builders this API exposes.

The resulting duplication is real and was measured by the 2026-07-20 architecture audit: the fourteen
numeric builders are near-identical clones modulo type substitution — roughly 2 450 lines — and the
five temporal builders follow the same pattern for some 800 more. A scripted scan of those clone
families found no behavioural copy-paste slip, and issue #214 has since added reflection-driven parity
guards over both the mirrored entry points and each family's constraint method set.

This arrangement is the decision that most shapes the library's internals, and it constrains how every
future discrete or numeric builder is added. Its reasoning lives only in internal XML documentation,
while smaller decisions — the `Any.Combine` arity cap (ADR-0015) — carry records.

## Decision

Every discrete value type whose domain fits 64 bits is generated through one shared engine over a
common unsigned 64-bit ordinal space, and a separate engine exists only where the arithmetic substrate
cannot be represented there: 128-bit integers, IEEE binary floating point, and `decimal`.

## Rationale

* **The ordinal space is what lets the hard promises be stated once.** Eager satisfiability, exact
  one-draw exclusion, cardinality counting and conflict detection are the parts of the algebra that
  are easy to get subtly wrong and expensive to get wrong in more than one place. Over ordinals they
  are one problem with one implementation, and every 64-bit discrete type inherits the same guarantees
  by construction rather than by review. A `DateTime` bound and an `Int64` bound are then the same
  object, so the promise to name both sides of a conflict does not have to be re-earned per type.
* **The split follows the substrate, which makes it falsifiable rather than a matter of taste.** Each
  dedicated engine exists because a stated property of its arithmetic — width beyond 64 bits,
  continuity, absence of a representable-value ladder — makes the shared engine's formulation
  inapplicable, not because its type felt different. The rule reads as a test a future maintainer can
  apply: a new type gets a mapping if its domain fits the ordinal space, and an engine only if it can
  be shown not to.
* **A uniform draw must be uniform over values, not over representations.** This is why the monotonic
  bit patterns of floating point are not pressed into the ordinal space even though the mapping
  exists. Ordinal uniformity is exactly right where consecutive ordinals mean consecutive values, and
  exactly wrong where they do not; keeping the continuous types on their own engine preserves the
  meaning of "arbitrary" for both groups.
* **The floor target removes the generic alternative, so the choice is between one shared engine plus
  three exceptions, or none at all.** Without generic math on netstandard2.0, sharing arithmetic across
  numeric types requires either an ordinal indirection or per-type code. The ordinal space buys sharing
  for the largest group — thirteen types — using only integer arithmetic the floor provides, and pays
  for it in the three places where it genuinely cannot apply.
* **The accepted duplication is bounded and now guarded.** The cost of this decision is that the parts
  of the algebra which cannot be shared are stated up to four times. That cost was accepted knowingly:
  the audit's scan found no behavioural drift, and the parity guards from issue #214 turn "the clones
  agree" from a discipline into a failing test. A decision whose main drawback is watched by a test is
  in a different position than one whose drawback is watched by attention.

## Alternatives Considered

### One engine over a widened ordinal space

Considered as the version of this design with no exceptions: map everything into a 128-bit ordinal, or
an arbitrary-precision one, and keep a single engine for the whole numeric and discrete surface.

Rejected on the floor target first — netstandard2.0 has no 128-bit integer type, so the shared engine
could not compile where the library must load, and an arbitrary-precision substitute would put an
allocating numeric type on every draw for the thirteen types that need none. It also would not achieve
what it promises: widening the ordinal addresses only the width problem, leaving floating-point
uniformity and `decimal`'s missing ladder exactly as they were. The unified engine would still need
per-substrate branches, having lost the property that made unification worth it.

### Per-type engines with no sharing

Considered for its simplicity: each builder owns its own bounds, exclusions and sampling, with no
indirection to understand and no ordinal concept to learn.

Rejected because it multiplies the algebra's difficult parts — eager satisfiability, exclusion
mapping, conflict provenance — by the number of types rather than by the number of substrates. The
clone families the audit measured show what that costs even where the code is generated by discipline:
the duplication that remains under this decision is the part that cannot be shared, and per-type
engines would make the shareable part duplicated too.

### A generic numeric base class over a self-referential type parameter

Considered as the language-level way to abstract the arithmetic without an ordinal indirection, which
would give one implementation and preserve each type's native arithmetic.

Rejected as unavailable rather than undesirable: C# forbids the pattern for the public sealed builder
types this API exposes, and netstandard2.0 provides no generic math constraint through which the
arithmetic could be expressed, so the base class would have nothing to abstract over. Issue #214
recorded reflection-driven parity tests as the mitigation for the duplication this alternative was
meant to remove.

### Ordinal-map floating point through its bit patterns

Considered because IEEE binary formats order monotonically as integers, which makes the mapping
available and would fold three more types into the shared engine.

Rejected because it silently changes what an arbitrary value means. Uniformity over ordinals becomes
uniformity over representations, which for floating point concentrates the draw near zero; and point
exclusion over a continuum, which must be honoured exactly on a set of measure zero, is a different
problem from exclusion over a finite ordinal set. The mapping is possible, the semantics are not
equivalent, and the equivalence is the only reason to share an engine.

### Ordinal-map `decimal` through its mantissa and scale

Considered for the same reason: `decimal` is discrete, so an injection into an ordinal space seems
natural, and it would leave only two dedicated engines.

Rejected because `decimal`'s discreteness is not uniform. The same value has several representations at
different scales, and the distance between adjacent representable values depends on the scale, so an
order-preserving mapping onto a contiguous ordinal range does not exist without first fixing a scale —
which is a constraint a caller may or may not declare. Bounds and strides for `decimal` therefore have
to be expressed in its own arithmetic.

## Consequences

### Positive

* The difficult part of the discrete algebra — eager satisfiability, exact exclusion, cardinality,
  conflict provenance — has one implementation for thirteen types, so a fix or a new constraint lands
  once for all of them.
* Adding a discrete type whose domain fits 64 bits is a mapping and a display name, not a new engine.
* The engine boundary is stated as a property of the substrate, so a future maintainer can decide where
  a new type belongs without re-litigating the architecture.
* A discrete draw stays uniform over its type's values, and no representation-space mapping distorts a
  continuous one. Which magnitude an unconstrained continuous draw favours is a separate decision,
  recorded in ADR-0052; this one settles only that the distortion never comes from the mapping.

### Negative

* Four engines mean the shareable-looking parts of the algebra are written up to four times, and a
  change to the algebra may have to be applied in each. The 128-bit engine is deliberately a verbatim
  sibling of the ordinal one, which is the clearest case of this cost.
* A reader must learn the ordinal indirection before following how a `DateTime` bound becomes a draw;
  the shared engine is domain-agnostic by design, so nothing in it names the types it serves.
* The decision fixes a 64-bit boundary as the sharing threshold. It is the widest integer arithmetic
  the floor target provides, not a derived optimum.

### Risks

* The clone families can drift behaviourally the next time one is edited. Mitigated by the parity
  guards from issue #214, which fail on a renamed or missing constraint, and bounded by the fact that
  the drift the audit found was in documentation rather than in behaviour.
* A future type may fit the ordinal space in principle while its semantics make ordinal uniformity
  wrong, as floating point does. The decision's test is stated in terms of representability, so such a
  case has to be recognized on its own merits rather than by applying the rule literally.

## Follow-up Actions

* Record the determinism and ambient-source contract separately (issue #216); it is the other
  cross-cutting decision the audit found unrecorded, and it is not settled by this one.
* When the 128-bit engine and the ordinal engine next diverge, state whether the divergence is
  intentional or a drift — this decision expects them to stay verbatim siblings.

## References

* ADR-0011 — Host JustDummies as a standalone package in this repository: the packaging decision this
  internal architecture sits inside.
* ADR-0013 — Gate distinct collections by cardinality, otherwise by a bounded draw: the cardinality
  reasoning that the shared engine's counting serves.
* ADR-0015 — Cap `Any.Combine` at arity eight: the smaller decision whose existing record made this
  one's absence conspicuous.
* ADR-0022 — Floor the library's .NET Framework support at 4.7.2: the floor target that removes the
  generic-math alternative.
* ADR-0025 — Generate matching strings from a home-grown regular subset: the neighbouring decision to
  build values constructively rather than by filtering.
* ADR-0052 — Draw arbitrary numbers within an ordinary magnitude: the neighbouring decision governing
  what an unconstrained continuous draw favours, on the two engines this decision keeps separate.
* Issue #217 — the audit item that asked for this record.
* Issue #214 — the parity guards this decision relies on to keep its accepted duplication safe.
* [2026-07-20 JustDummies architecture & design audit](../audit/2026-07-20-dummies-architecture-and-design-audit.md),
  §5 — where the missing record was reported, with the measured size of the clone families.

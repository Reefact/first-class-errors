# ADR-0041 | Draw flag-enum combinations behind an opt-in

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0041-draw-flag-enum-combinations-behind-an-opt-in.fr.md)

**Status:** Proposed
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

An enum marked `[Flags]` declares bits meant to be combined: its members are not alternatives but the parts of a set. Its **valid** values are therefore the combinations, while the values it **declares** are only the parts — `Read | Write` is a value the type is designed to hold and never names. The BCL agrees on both counts: `Enum.GetValues` returns the declared members only, and `Enum.IsDefined` answers `false` for a combination.

`AnyEnum<TEnum>` draws uniformly from the declared members, a contract its own remarks state. For a flags enum this means a dummy carries at most one bit, so a branch reading two — the ordinary shape of flag-consuming code — is never exercised by a JustDummies value. That is the inverse of what the library exists for: the constraint surface is meant to surface hidden assumptions, and here the generator silently installs one of its own ("this value has zero or one bit"). It is the same shape as a reachability defect, except reached by design.

The generator is bound by three of the library's standing rules. It builds values constructively in one draw and never generates-then-filters. It detects contradictory constraints eagerly, at the fluent call that caused them, naming both sides. And it advertises a distinct cardinality through `ICardinalityHint<TEnum>`, which is what lets a distinct collection over an enum fail at declaration rather than at generation — so the size of the draw domain is part of the public contract, not an implementation detail.

Two properties of real flags enums matter for the domain's shape. A flags enum need not declare a zero member, and one that does not has no "no flags" value to yield. And a flags enum may declare **composites** — `ReadWrite = Read | Write`, `All = 7` — which are combinations already, so several subsets of the declared members collapse onto the same value.

JustDummies has never been released, so the meaning of the unconstrained draw is still free to be fixed. The audit of 2026-07-20 recorded flag combinations as a demand-driven addition (issue #226).

## Decision

`AnyEnum<TEnum>` keeps drawing from the declared members by default and gains `AllowingCombinations()`, an explicit constraint widening the draw to the OR-closure of the declared members — plus the zero value when the enum declares one — refused on an enum that is not `[Flags]` and on one with more non-zero members than can be enumerated.

## Rationale

**The default cannot depend on the attribute.** Making `Any.Enum<T>()` behave differently because the type carries `[Flags]` would make the draw a function of a type's metadata rather than of what the test wrote, which is the class of implicit, action-at-a-distance behaviour ADR-0020 removed from this library when it deleted the implicit conversions. Declared-members-only is also the sole default that is *valid* for both enum families: a declared member is always a legitimate value, whereas a combination is legitimate only for a flags enum. Keeping it costs the flags user one call and costs everyone else nothing.

**Making it a constraint, not a second factory,** puts the choice where the reader already looks for the shape of a value. `Any.Enum<Permissions>().AllowingCombinations()` reads as a widening of the same generator, composes with `OneOf`/`Except`/`DifferentFrom` through the existing pool, and needs no mirror on `AnyContext` — the factory is unchanged, so the hand-mirrored surface does not grow.

**The universe is the OR-closure of the declared members, not of the individual bits.** Taking the declared members as the generating set absorbs a declared composite without having to decide which members "are" bits: `ReadWrite = Read | Write` contributes nothing new, and an enum whose members are not all powers of two needs no special case. Adding the zero value only when a zero member is declared keeps the promise that every drawn value is one the type defines: an enum declaring `Left` and `Right` alone has no name for the empty set, and inventing it would be exactly the undeclared value the declared-members default refuses.

**Exclusions keep comparing by equality.** `Except(Read)` forbids the value `Read` and leaves `Read | Write` drawable. Reading the same call as a bit mask under the opt-in would make one method mean two things depending on another constraint — the same implicitness the default rejects — and would silently delete most of the universe. The library already distinguishes near-synonyms by name when the intent differs, so a bit-level exclusion, if it is ever wanted, is a separate named constraint rather than a mutation of this one.

**Enumerating the universe is what keeps the two standing guarantees.** A per-member coin flip would be cheaper and unbounded, but it is uniform over *subsets*, not over *values*: with a declared composite, several subsets collapse onto one value, and that value is then drawn far more often than the others — a biased dummy is a worse failure than a refused constraint, because nothing reveals it. Materializing the closure also keeps `ICardinalityHint` exact, which is what preserves the eager conflict on a distinct collection asking for more values than exist. The cost is that the closure is exponential in the number of members, so it needs a ceiling.

**Beyond the ceiling the constraint is refused, not degraded.** A silent fallback would split the generator into two regimes — one uniform and eagerly-checked, one neither — distinguishable only by counting an enum's members. Refusing by name, and pointing at the explicit allow-list that serves the case, is the answer ADR-0025 already gave for constructs outside the supported subset: a clear error beats a value whose properties the caller cannot predict. A flags enum wide enough to hit the ceiling is far outside the shapes real code declares.

## Alternatives Considered

### Make combinations the default for `[Flags]` enums

Considered because it needs no new API and gives the flags user the right domain without asking: arguably "arbitrary yet valid" already means the combinations for a type designed to hold them.

Rejected because the draw would then depend on the type's metadata rather than on the test's text, so adding `[Flags]` to an existing enum would silently change every dummy drawn from it — and, before that, change every seeded sequence. It also widens the domain for the many flags enums whose consumers only ever pass single members, where a two-bit dummy is a surprise rather than a revelation. The explicit call costs one line and makes the widening legible at the call site.

### Draw each member with an independent coin flip

Considered because it is a few lines, has no ceiling, and needs no enumeration at all: OR a random subset of the members and the result is a valid combination by construction.

Rejected because it is uniform over subsets rather than over values, so any declared composite skews the distribution heavily towards the collapsed value, and because it cannot report a distinct cardinality — which would silently drop distinct collections over an enum from an eager conflict to a bounded draw, a regression against today's behaviour.

### Expose the combinations through a separate factory

Considered because a distinct entry point would state the intent even more loudly and could carry its own constraint surface.

Rejected because it duplicates the whole enum constraint algebra for one widening, and because it would have to be mirrored on `AnyContext`, growing the hand-mirrored surface the parity guards exist to police. A constraint on the existing builder composes with everything already there.

### Read `Except` as a bit mask under the opt-in

Considered because "no value carrying the Read bit" is a plausible thing a test wants, and reusing `Except` would need no new name.

Rejected because it makes one method mean two different things depending on whether another constraint was declared, and because it is the more destructive of the two readings: excluding one bit would remove half the universe, which a caller writing `Except` on an enum has no reason to expect. A distinct name remains available for that need.

## Consequences

### Positive

* A flags dummy can carry the combinations the type exists to hold, so a branch reading two bits is exercised.
* The default is unchanged, so no existing draw, seeded sequence, or documented behaviour moves.
* The widened domain flows through the existing cardinality hint, so a distinct collection over combinations keeps failing eagerly rather than at generation.
* The refusals — not `[Flags]`, too many members — are declaration-time and name their cause, consistent with the rest of the constraint surface.

### Negative

* The flags user must know the call exists; a generator drawing single members remains the default they meet first.
* The universe is materialized, so an enum near the ceiling costs memory and one-off computation proportional to its combination count.
* The opt-in is order-sensitive with respect to an allow-list naming combinations: applied after `OneOf`, it widens a universe the allow-list has already pinned, so it changes nothing.

### Risks

* An enum wide enough to be refused is a supported type whose combinations cannot be drawn at all. Mitigation: the message names the ceiling and points at the explicit allow-list; the shape is far outside what real code declares, and the ceiling can be raised by a later decision on evidence.
* The order-sensitivity with `OneOf` could read as a silent no-op. Mitigation: documented on both members, and the reverse order — an allow-list naming a combination before the opt-in — fails with a message naming the missing constraint rather than accepting it.

## Follow-up Actions

* Should a bit-level exclusion be requested, introduce it under its own name rather than by widening `Except`.
* Revisit the enumeration ceiling if a real flags enum is ever reported against it.

## References

* [ADR-0020](0020-materialize-dummies-only-through-generate.md) — the removal of implicit, metadata-driven behaviour whose reasoning the default follows, and the pre-1.0 timing argument reused here.
* [ADR-0025](0025-generate-strings-from-a-home-grown-regular-subset.md) — the "a clear refusal beats an unpredictable value" rule the ceiling applies.
* [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the cardinality-hint contract the materialized universe keeps exact.
* [ADR-0032](0032-draw-arbitrary-values-from-an-explicit-top-level-pool.md) — the explicit-pool draw the ceiling's message points at.
* [ADR-0037](0037-vary-the-datetimeoffset-offset-dimension.md) — the precedent for an opt-in extra dimension whose default is left untouched, including the same `OneOf` terminal-enumeration interaction.
* Issue [#226](https://github.com/Reefact/first-class-errors/issues/226) — the backlog entry this resolves.

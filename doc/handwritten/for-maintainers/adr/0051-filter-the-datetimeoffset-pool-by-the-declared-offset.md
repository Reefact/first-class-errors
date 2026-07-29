# ADR-0051 | Filter the DateTimeOffset pool by the declared offset

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0051-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-28
**Accepted:** 2026-07-28
**Decision Makers:** Reefact

Supersedes [ADR-0037](0037-vary-the-datetimeoffset-offset-dimension.md).

## Context

ADR-0037 gave `AnyDateTimeOffset` an offset dimension and recorded, under *Risks*, that combining it with `OneOf`
would leave the offset unapplied: "`WithOffset` combined with `OneOf` does not replace a `OneOf` value's own offset.
Mitigation: documented, and consistent with `OneOf`'s terminal enumeration semantics."

The mitigation did not hold, and the risk is larger than a surprise.

`WithOffset`'s public XML documentation states that it "pins the offset dimension — **every generated value carries
exactly that offset**" and declares `ConflictingAnyConstraintException` "when the constraint contradicts a constraint
already declared". Combined with `OneOf`, it did neither: the constraint was dropped in both declaration orders, no
exception was raised, and the values came out with their own offsets. The published contract said the opposite of
what the code did, and the JustDummies readme does not mention the interaction at all.

The library answers this shape consistently everywhere else. `Any.Int32().OneOf(1, 2, 3).GreaterThan(10)` and
`Any.DateTime().OneOf(d1, d2).After(2022)` both raise `ConflictingAnyConstraintException`; `OneOf(1, 2, 3)
.GreaterThan(1)` narrows and draws. `DateTimeOffset` was the one family where a constraint declared after a pool was
neither applied nor refused.

The repository's governing rule for a fluent constraint is that a method the DSL offers must be honoured when its
arguments permit and must fail when they do not. Silently discarding it is neither.

## Decision

A declared offset **filters** the `OneOf` pool to the values whose offset it admits, in either declaration order, and
contradicts when it admits none.

## Rationale

* **It restores the published contract.** `WithOffset` promises that every generated value carries that offset, and
  now every generated value does — including a pooled one, because a pooled value carrying a different offset is
  simply not drawn.
* **It keeps the half of ADR-0037 that was right.** A pooled value is still returned verbatim, offset included:
  rebuilding it from the instant would normalize the offset to UTC, which is exactly what ADR-0037 set out to avoid.
  What changes is *which* pooled values may be drawn, not how a drawn one is rendered.
* **It makes the two orders agree.** Declaring the pool first or the offset first now reaches the same verdict, which
  is the property the library already guarantees for every other constraint pair and which a caller has no way to
  reason about otherwise.
* **A contradiction is reported rather than swallowed.** An offset no pooled value carries is a specification the
  generator cannot satisfy; failing at declaration is what the eager check exists for, and it is what the
  documentation already told the caller to expect.

## Alternatives Considered

### Keep the behaviour and fix the documentation instead

Considered because it is the cheapest resolution and because ADR-0037 had already reasoned its way to it. Rejected
because the documentation would then have to describe a rule that holds for one generator family and no other, and
because the caller writing `WithOffset` after a pool is asking for something the library can decide: either a pooled
value carries that offset or none does. Documenting a silent no-op does not make it a good answer, and the divergence
would be discovered at the point where a test passes for the wrong reason.

### Rewrite the pooled value's offset to the declared one

Considered because it honours `WithOffset` literally in every case, with no contradiction to report. Rejected because
it changes the value the caller supplied: `OneOf` enumerates exact values, and returning one that was never in the
pool is a worse surprise than the one being removed. It also destroys the instant, since moving the offset while
keeping the local time yields a different point in time.

### Make `OneOf` terminal on `AnyDateTimeOffset`

Considered because a terminal type would make the combination unwritable, which is the strongest possible guarantee.
Rejected because it removes combinations that are legitimate and useful — `OneOf(...).Except(...)`, and an offset that
some pooled value does carry — and because the wider question of terminal pools is being settled on its own terms for
the string and object pools rather than family by family.

## Consequences

### Positive

* `WithOffset` and `WithOffsetBetween` mean the same thing whatever else the generator carries.
* An impossible pool/offset combination is reported at declaration, with a message naming what was asked and what the
  pool admits.
* `AnyDateTimeOffset` stops being the one family where a declared constraint can vanish.

### Negative

* A caller who relied on the old silence — writing `WithOffset` after a pool and expecting the pool to win — now gets
  either a filtered pool or a conflict. That behaviour was contradicted by the method's own documentation, so the
  change corrects the code rather than the expectation, but it is a behaviour change in a shipped generator.

### Risks

* **A pool of one value with a mismatched offset now fails where it used to generate.** That is the intended
  correction, and the message names both sides so the fix is obvious — drop the offset constraint, or pool a value
  that carries it. Mitigation: the message states what the offset dimension admits.

## Follow-up Actions

* Flip ADR-0037's status to *Superseded* with a link here.
* Keep the JustDummies readme's offset section in step: it documents `WithOffset` and `WithOffsetBetween` without
  mentioning `OneOf`, which was correct only under the old behaviour.

## References

* ADR-0037 — Vary the DateTimeOffset offset dimension: the decision this supersedes, and the *Risks* entry it closes.
* ADR-0030 — Draw arbitrary strings from an explicit terminal set: the terminal-pool semantics ADR-0037 leaned on.
* `AnyDateTimeOffset` in the `JustDummies` project.

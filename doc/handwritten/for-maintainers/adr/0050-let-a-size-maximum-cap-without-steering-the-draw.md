# ADR-0050 | Let a size maximum cap without steering the draw, and ceiling an explicitly demanded size

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0050-let-a-size-maximum-cap-without-steering-the-draw.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-28
**Accepted:** 2026-07-28
**Decision Makers:** Reefact

## Context

JustDummies lets a test declare a size through two families: `WithLength`, `WithMinLength`,
`WithMaxLength` and `WithLengthBetween` on strings; `WithCount`, `WithMinCount`, `WithMaxCount` and
`WithCountBetween` on collections.

Unconstrained, a dummy is deliberately small: a string draws between 0 and 16 characters, a collection
between 0 and 8 elements. ADR-0025 refers to this as the "0 to a handful" default the string and
collection generators already share, and reuses it for unbounded regex quantifiers.

A declared maximum, however, does not compose with that default — it **replaces** it. The draw becomes
uniform over the whole declared interval, so the upper bound doubles as a size hint:
`WithMaxLength(100000)` yields strings of roughly 60 000 characters, while the same generator left
unconstrained yields 0 to 16. Two different size policies apply to what a reader sees as one thing.

The only argument validation on these methods is non-negativity; nothing bounds the top. Pushed to
`int.MaxValue`, the four entry points were measured to behave in four different ways:

| declaration                  | measured behaviour                                              |
| ---------------------------- | --------------------------------------------------------------- |
| `WithLength(int.MaxValue)`    | `ArgumentOutOfRangeException` naming an internal parameter, from an arithmetic overflow inside the draw |
| `WithMaxLength(int.MaxValue)` | returns a string of about 130 MB                                 |
| `WithMaxCount(int.MaxValue)`  | runs for minutes                                                 |
| `WithCount(int.MaxValue)`     | fails immediately                                                |

The divergence is not designed. Two of the four follow directly from the maximum steering the draw. The
other two share one code path and differ only in where the requested number falls relative to the
allocator's limits: one capacity request is refused outright, the other is granted and then filled one
element at a time. None of the four failures is raised by the library: two are BCL exceptions naming
parameters the caller never wrote, one is an unbounded wait, one is a silently enormous value.

The library already has an exception taxonomy. A caller mistake on a single argument surfaces as a BCL
argument exception — `UnsupportedRegexException` documents this for a malformed pattern, and ADR-0045
fixed it for `null` across the whole surface, enforced by a reflection-driven convention test. A
contradiction *between* declared constraints raises `ConflictingAnyConstraintException` at declaration
time. A generation that fails despite accepted constraints raises `AnyGenerationException`.

Large sizes do have legitimate uses: tests that exercise a business limit ("rejects a label longer than
255 characters", "the batch splits past 1 000 items"). Those sizes are calibrated on the limit under
test — hundreds, thousands, tens of thousands — two orders of magnitude below the values that produce
the behaviours above.

JustDummies has never been released, so the meaning of a declared bound is still free to be fixed
(the same standing ADR-0041 relied on).

## Decision

A declared size maximum only ever narrows a draw and never widens it beyond the default spread, and a
size the generator must actually produce — an exact or minimum length or count — is refused above
1 000 000 with an `ArgumentOutOfRangeException` at declaration time.

## Rationale

* **The steering maximum is the cause, not a fourth symptom.** Once a maximum only caps, a loose bound
  no longer inflates the draw, and two of the four measured behaviours stop existing: an enormous
  `WithMaxLength` yields an ordinary small string, an enormous `WithMaxCount` an ordinary small
  collection. Removing a cause is worth more than guarding four effects, and it is what makes the
  remaining guard small enough to specify in one sentence.
* **A bound is a permission, not a request.** "At most N" states what the value must not exceed; it says
  nothing about what size is wanted. Reading it as a request is what makes the unconstrained default and
  the bounded default disagree for what a reader sees as the same generator. Under this decision one
  policy governs size everywhere: a dummy is small unless something explicitly asks for more, and only a
  minimum, an exact size or a required fragment can ask.
* **Ceilinging only what must be produced removes the guard's false positives.** A maximum costs
  nothing to honour, so capping a string at a column width of four million stays legal and keeps
  yielding small dummies. Only a size the library would have to materialize is refused — which is
  exactly the set that decides how much memory and work a draw costs.
* **The ceiling belongs to the argument-validation category, not to the library's own exceptions.** A
  size too large is a single argument unusable on its own, exactly like the negative size already
  rejected there; it is not a contradiction between two constraints, and it is not a generation that
  failed. Following the taxonomy keeps the count of exception types and documented categories unchanged,
  and replaces a message naming an internal parameter with one naming the parameter the caller wrote.
* **1 000 000 sits in the gap between legitimate and absurd.** It is five orders of magnitude above the
  default spread, so ordinary use cannot approach it; it is two orders of magnitude above the largest
  business limit a boundary test plausibly exercises, so such a test is never refused; and a value of
  that size still materializes in milliseconds, so the ceiling never turns a slow test into a fast one —
  it turns a hang or an allocation failure into a diagnosable one. A lower ceiling would start refusing
  the boundary test that legitimately checks a 64 KB input.
* **A convention test is what keeps the rule true.** The overflow behind the first measured behaviour
  exists because the same arithmetic was already made overflow-safe one line away and not here; a rule
  applied by hand to each size-taking method will be forgotten by the next builder in exactly the same
  way. ADR-0045 established reflection-driven enforcement as this repository's answer to a rule that must
  hold across a whole surface, including the members not yet written.

## Alternatives Considered

### Ceiling every size argument, maxima included

Considered for the uniformity of a single rule with no exception to remember. Rejected: a maximum is
free to honour once it no longer steers the draw, so refusing a large one buys no protection while
refusing a legitimate declaration — a cap mirroring a storage limit larger than the ceiling. The rule
stays one sentence either way, and this version has no false positives.

### Raise the ceiling breach as a library exception

Considered: a `ConflictingAnyConstraintException`, or a new member of the library's own hierarchy, so
that the whole failure surface is catchable in one clause. Rejected because it contradicts the taxonomy
recorded elsewhere in the library: an argument that is unusable on its own is a caller mistake, not a
constraint interaction, and mapping it to a conflict would make the word "conflict" mean two different
things. Reserving the library's hierarchy for what the library itself decides is what keeps that
hierarchy meaningful.

### Offer a per-call escape hatch for very large sizes

Considered, so that no legitimate use is ever blocked. Rejected on demand grounds: no such use is
recorded, the escape hatch invites the misuse the ceiling exists to prevent, and adding one later is a
non-breaking addition whereas removing one would be breaking. A genuine need is met by revisiting the
ceiling — a decision — rather than by a per-call bypass.

### Treat `Between` as an explicit request for its range

Considered because `WithLengthBetween(0, 100000)` reads as a request for values spread across that
range, and under this decision it yields the small default instead. Rejected because it would break the
identity between `WithLengthBetween(a, b)` and the same generator declared with a minimum and a maximum:
two spellings of one constraint would draw differently, and the uniform constraint algebra is a
deliberate property of this API. In practice a range starting at zero is written to express a limit, and
a test that wants large values raises the minimum — which reads as what it is.

### Fix only the arithmetic overflow

Considered as the minimal change, since it is the one behaviour that produces a confusing message.
Rejected: it addresses the least harmful of the four. A silent 130 MB string and a run of several
minutes cost far more to diagnose than an exception with a poor message, and neither is touched by
overflow-safe arithmetic.

## Consequences

### Positive

* One size policy across the API: a dummy is small unless something explicitly asks for more.
* Two of the four measured behaviours disappear as a consequence of the policy, with no guard involved.
* An absurd size is reported at declaration time, against the parameter the caller wrote, instead of
  surfacing as a hang, an allocation failure, or a BCL message about internal arithmetic.
* The arithmetic overflow becomes unreachable, since a produced size can no longer approach the range
  where it occurs.
* No new exception type and no new documented category.

### Negative

* A declaration that used to yield large values now yields small ones. `WithMaxLength(100000)` returning
  0-to-16-character strings is the intended new behaviour, and it will surprise anyone who read the
  bound as a size hint — the documentation has to state the rule explicitly rather than let it be
  inferred.
* `WithLengthBetween(0, N)` yields the default spread rather than values across the range, which is the
  accepted price of keeping `Between` decomposable.
* The ceiling is a constant with no derivation. It is defensible, not provable, and the argument for it
  rests on the gap between legitimate and absurd rather than on a measurement of the runtime.

### Risks

* A consumer legitimately needing more than the ceiling is blocked until it is revisited. Mitigated by
  the size of the gap: the ceiling is far above any boundary test calibrated on a business limit.
* The convention test must recognize a size-carrying parameter to hold a future builder to the rule; a
  size parameter named outside the convention would escape it silently. This is the same exposure
  ADR-0045 accepted for its own reflection-driven rule.

## Follow-up Actions

* Settle, during implementation, whether the ceiling applies to a size the caller states directly or to
  the effective minimum after required fragments are counted — the two differ only for a declaration
  whose prefix, suffix or contained values are themselves near the ceiling.
* Keep the draw's arithmetic overflow-safe independently of the ceiling: unreachability is a property of
  the current rule, not a guarantee, and the safe form costs nothing.
* State the rule in the user documentation, English and French, where the size constraints are
  described.

## References

* ADR-0025 — Generate strings from a home-grown regular subset: names the "0 to a handful" default this
  decision restores where a maximum used to bypass it.
* ADR-0041 — Draw flag-enum combinations behind an opt-in: the standing that an unreleased library may
  still fix the meaning of an unconstrained draw.
* ADR-0045 — Guard public and internal arguments against null: the argument-validation posture and the
  reflection-driven convention test this decision reuses.
* Issue #226 — the JustDummies backlog the audit's demand-driven items were filed under.

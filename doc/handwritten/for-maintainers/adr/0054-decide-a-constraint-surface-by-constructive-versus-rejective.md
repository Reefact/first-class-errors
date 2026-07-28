# ADR-0054 | Decide a generator's constraint surface by constructive versus rejective, not by terminality

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0054-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md)

**Status:** Accepted
**Date:** 2026-07-28
**Decision Makers:** Reefact

Supersedes [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md).

## Context

Every `JustDummies` generator is a fluent recipe: each constraint narrows what may be drawn, contradictory constraints
fail at declaration with a `ConflictingAnyConstraintException` naming both sides, and the value is built to satisfy the
whole specification rather than generated and filtered. Which constraints a given generator exposes has, until now,
been decided generator by generator.

`OneOf` is the clearest case of that drift. Measured on `main` by reflection over the returned type's public instance
methods other than `Generate()`:

| call | returns | chainable constraints |
|---|---|---|
| `Any.Int32().OneOf(1, 2)` | `AnyInt32` | 13 — composable |
| `Any.DateTime().OneOf(d)` | `AnyDateTime` | 9 — composable |
| `Any.DateTimeOffset().OneOf(x)` | `AnyDateTimeOffset` | 11 — composable |
| `Any.Guid().OneOf(g)` | `AnyGuid` | 5 — composable |
| `Any.String().OneOf("a", "b")` | `AnyStringOneOf` | 0 — terminal |
| `Any.OneOf(x, y)` | `AnyOneOf<T>` | 0 — terminal |

Four families return their own composable builder; two return a distinct dead-end type. Nothing at the call site tells
the two apart.

Further facts framing the choice:

* ADR-0030 made `Any.String().OneOf(...)` terminal, on the ground that reconciling an explicit value set with the
  prefix, suffix, contained values, character family, casing and length of a string would multiply contradictory
  combinations and their conflict messages, for a combination nobody needed. It listed as a *Risk* that a caller may
  expect the scalar `OneOf`'s composability and be surprised. ADR-0025 made `Any.StringMatching(...)` terminal on the
  same reasoning, and ADR-0030 aligned with it as a precedent.
* The gap is not theoretical. `Any.ElementOf(existingOrders).DifferentFrom(theOneAlreadyUsed)` — drawing another
  element of a fixture — does not exist, and `Any.String().OneOf("abc", "de").WithLength(3)` does not either, though
  `"abc"` satisfies both. The LINQ workaround for the first, `pool.Where(x => x != used).ToArray()`, works but reports
  an emptied domain as `ArgumentException: At least one value is required`, blaming the caller for an empty array
  instead of naming the two constraints in play. The numeric families name both.
* The two costs ADR-0030 avoided are not symmetric. Length, prefix, suffix, contained values, character family and
  casing *shape* a string the generator builds. `Except`/`DifferentFrom` do not shape anything: they remove values.
* Strings have no ordinal mapping to build an exclusion into, so on a shaped string an exclusion is already met by a
  **bounded redraw** — a documented exception to "built, never filtered" that the package readme states, and the only
  failure `AnyString` defers to generation.
* `AnyPattern` already runs a bounded build-verify-redraw loop on every draw: since ADR-0048 each built value is
  checked against the real .NET engine and redrawn on a miss, so that "a generated value matches its pattern" holds by
  construction.
* Distinct collections gate at declaration on the element generator's advertised cardinality and membership through
  the internal `ICardinalityHint<T>` (ADR-0013). `AnyStringOneOf` and `AnyOneOf<T>` both advertise it; `AnyString`
  does not.
* Issue #337 established that a generation failure may assert only what the search actually established: a spent
  budget is reported as a spent budget, never as a proof of impossibility.
* Nothing has been published. `PublicAPI.Shipped.txt` contains only `#nullable enable`, no `dum-v*` tag exists and the
  version is `0.1.0-dev`, so changing a return type and removing a public type costs nothing today and would be a
  major version after the first release.

## Decision

What constraints a generator exposes is decided by whether each constraint is **constructive** — it describes a value
the generator must build, and is offered only where the generator can build one satisfying it — or **rejective** — it
removes values from a domain, and is offered everywhere — rather than by declaring a generator terminal.

## Rationale

* **"Terminal" described the returned type, not the domain, so it could not be reasoned about.** ADR-0030 and ADR-0025
  each reached a sound refusal, but recorded it as a property of the generator: *this one exposes nothing further*. A
  caller cannot predict that, and neither can a maintainer adding the next generator — the measured table above is what
  a rule nobody can apply looks like after four families went one way and two the other. Constructive versus rejective
  is a property of the constraint, so the same test answers the question for every generator, including ones not yet
  written.
* **A caller-supplied value set is a domain, not a layout, so the combinatorial cost ADR-0030 refused never arises.**
  ADR-0030's argument was that an explicit set would have to be reconciled with each shaping constraint, each
  reconciliation needing its own conflict analysis. That is true while the generator *builds* a string. Once the values
  are supplied there is nothing to build: every other constraint becomes a test each value passes or fails, the domain
  is the values that pass, and satisfiability is the single question of whether any remain. One question replaces the
  matrix — and it is answered eagerly, so the promise that a generator which exists can generate is kept.
* **The refusal on a pattern survives the reframing, and gains a reason it did not have.** A shape constraint on
  `Any.StringMatching(...)` would require building a value in the intersection of two regular languages, machinery the
  library does not have and would not add for this. That is now a statement about the constraint rather than about the
  type: the refusal stands on why it cannot be built, not on a label, and it explains why the exclusion pair is
  admitted alongside it rather than looking like an inconsistency.
* **A rejective constraint needs no new machinery and creates no new exception to "built, never filtered".** On a
  shaped string, exclusions are already met by a bounded redraw, and that exception is already documented. On a
  pattern, the loop that would carry the exclusion is the one ADR-0048 already turns on every draw; the exclusion is
  one more predicate inside it. On a value set the question does not even arise: the domain is finite and enumerable,
  so the excluded values are removed at declaration and the draw stays a single uniform pick.
* **Symmetry is what makes the surface learnable; naming both sides is what keeps it honest.** A caller who has met
  `Except`/`DifferentFrom` on one generator can expect them on the next, and an emptied domain reports the two
  constraints that emptied it instead of an argument error about an array the caller never wrote. That is the same
  "an impossible Arrange is a test defect" contract the library applies everywhere else, extended to the two places
  that fell outside it.
* **Where a bounded search backs the exclusion, the failure keeps the claim it can support.** A pattern generator
  builds values from its pattern; it never enumerates the language, so an exhausted budget is evidence and not proof,
  and the message says so — the standard issue #337 set, applied to the one new failure mode this decision creates.
* **The window is open now and closes at the first release.** Making the string `OneOf` composable changes a return
  type and deletes a public type. With nothing published that is free; after `dum-v1` it is a major version, and the
  asymmetry would have to be lived with or paid for.

## Alternatives Considered

### Keep the terminal types and give them the shaping constraints

Considered because it preserves ADR-0030's and ADR-0025's decisions intact while closing the capability gap: a
composable `AnyStringOneOf` would answer `OneOf("abc", "de").WithLength(3)` without changing what `Any.String().OneOf`
returns.

Rejected because it closes the gap by duplicating the surface rather than removing the asymmetry: the constraint set
would exist twice, on two types, with two sets of conflict messages to keep in step, and the caller would still have to
know which type they hold. The asymmetry the measured table shows is the defect; a second composable type leaves it in
place.

### Make only `Any.String().OneOf` composable, and leave the pool and the pattern terminal

Considered because it fixes the case with the most obvious cost — the string value set — for the smallest change, and
leaves two decisions untouched.

Rejected because it fixes the instance and not the rule. `Any.ElementOf(orders).DifferentFrom(used)` is the idiom this
work exists for and would still be missing, and the next generator would face the same undocumented judgement call.
Recording the distinction is what makes the surface predictable; applying it to one of the three places it covers would
record nothing.

### Open the pattern to shape constraints as well, by generating and filtering

Considered for full symmetry: with a redraw loop already in place, a length or prefix constraint could be met by
drawing until a value satisfies it, making every string generator carry the same constraints.

Rejected because it would meet a *constructive* constraint by rejection, which is the one thing the library refuses to
do. The expected number of draws is unbounded and pattern-dependent — a length constraint the pattern rarely produces
turns a declaration into a silent lottery — so the failure would depend on luck rather than on the specification, and
the eager-conflict promise would be quietly abandoned for a whole class of constraints. Building in the intersection of
two regular languages is the only honest way to offer them, and it is out of scope.

### Intersect the pattern with the shape constraints through an automaton product

Considered as the honest form of the previous alternative: compiling both the pattern and the shape constraints to
automata and generating from the product would meet constructive constraints by construction, with no filtering.

Rejected on cost and identity. It would add an automata engine to a package whose whole regular subset is deliberately
home-grown and small (ADR-0025), for a combination no reported use case asks for — the caller who wants a shaped value
writes the shape into the pattern. The decision refuses the constraint, and the refusal is now stated as a limit of the
machinery rather than a property of the type, so the door is left open should a real need appear.

### Leave the pool case to the caller's LINQ

Considered because `pool.Where(x => x != used).ToArray()` already works, needs no API, and keeps `AnyOneOf<T>`
minimal.

Rejected because it degrades exactly what the library exists to protect: the diagnostic. Filtering to nothing raises
`ArgumentException: At least one value is required (Parameter 'values')`, which blames the caller for an empty array
instead of naming the pool and the exclusion that emptied it — while the numeric families report
`Cannot apply DifferentFrom(42) because it forbids every value OneOf(42) allows`. It also moves a domain decision out
of the specification and into the arrange code, where a distinct collection can no longer see it.

## Consequences

### Positive

* One test — is this constraint constructive or rejective? — answers what any generator, present or future, may
  expose. The measured asymmetry becomes a rule rather than a table of precedents.
* `Any.String().OneOf(...)` composes with every string constraint, and an emptied set names the two constraints in
  play — the same verdict whichever of the two was declared first, each order phrasing it from the side the second
  declaration arrives on.
* `Any.ElementOf(orders).DifferentFrom(used)` and `Any.StringMatching(p).DifferentFrom(existing)` exist, with the
  conflict and failure diagnostics the rest of the library gives.
* `AnyString` advertises the cardinality of its surviving value set, so a distinct collection over a pooled string
  generator still gates eagerly — the guarantee ADR-0030 secured through `AnyStringOneOf` is kept by the type that
  replaces it.
* One public type disappears and none is added.

### Negative

* A string value set is no longer a single-purpose type whose emptiness is impossible by construction; it is a filter
  whose satisfiability must be validated on every subsequent constraint, and that validation is code that can be wrong.
* With a value set in force, `Containing(...)` is answered by testing the supplied value rather than by the
  side-by-side layout the constructive path uses, so a pooled `"aba"` satisfies `Containing("ab").Containing("ba")`
  while a built string never could. The two paths give the same answer wherever the constructive one can build at all,
  but the pooled path is strictly more permissive, and that difference has to be documented.
* That permissiveness is reachable only where the constructive path had not already refused. A combination it rejects
  on its own terms is refused the moment it is declared — the generator cannot know a value set is coming, and
  deferring the refusal would cost every shaped string its eager conflict — so those constraints with `OneOf` last
  still conflict, while `OneOf` first accepts them. Order is otherwise immaterial; here it is not, and the surface has
  to say so rather than promise a symmetry it does not have.
* `AnyPattern` is no longer describable as exposing nothing: ADR-0025's "terminal generator" framing now needs the
  constructive/rejective qualification to stay accurate.

### Risks

* A caller may read the pattern's exclusion pair as an invitation to expect shape constraints there too, and read
  their absence as an oversight. Mitigated by stating the refusal and its ground — no machinery to build in the
  intersection of two regular languages — in the type's own documentation rather than only here.
* An exclusion on a pattern can empty a small language, and the redraw that discovers it costs its whole budget before
  failing. Mitigated by keeping the budget separate from the match budget, so neither failure borrows the other's
  evidence, and by a message that claims the spent budget and explicitly not impossibility (issue #337).
* Validating a value set against every constraint is O(values × constraints) at declaration. Mitigated by the domain:
  these are hand-written sets in test arrange code, evaluated once per generator, never per draw.

## Follow-up Actions

* Flip ADR-0030's status to *Superseded* with a link here, once this record is accepted.
* Decide whether ADR-0025 needs a successor: its decision — generate from a home-grown regular subset — stands
  untouched, but its description of the generator as *terminal* is narrowed by this record. Flagged rather than acted
  on: this ADR does not revisit how patterns are generated.

## References

* ADR-0030 — Draw arbitrary strings from an explicit, terminal value set: the decision this supersedes, and the *Risk*
  it recorded about callers expecting composability.
* ADR-0025 — Generate matching strings from a home-grown regular subset: the terminal-generator precedent ADR-0030
  aligned with, and the reason a constructive constraint on a pattern stays refused.
* ADR-0048 — Guarantee a generated regex value matches its pattern, by bounded redraw: the loop an exclusion joins.
* ADR-0013 — Gate distinct collections by cardinality, otherwise by a bounded draw: the `ICardinalityHint` contract a
  value set must keep answering.
* ADR-0045 — Guard public and internal arguments against null: the convention every new member follows.
* Issue #352 — the audit item that asked for this record.
* Issue #337 — the claim-truthfulness standard for an exhausted budget.
* `AnyString`, `StringSpec`, `AnyOneOf<T>` and `AnyPattern` in the `JustDummies` project.

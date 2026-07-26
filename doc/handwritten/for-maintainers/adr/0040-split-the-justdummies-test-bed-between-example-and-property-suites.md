# ADR-0040 | Split the JustDummies test bed between an example suite and a property suite

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0040-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md)

**Status:** Proposed
**Date:** 2026-07-26
**Decision Makers:** Reefact

## Context

`JustDummies` builds arbitrary values that satisfy declared constraints. Its
correctness claim is therefore universally quantified: *every* value a generator
produces satisfies *every* constraint declared on it, for *every* legal
combination of constraint arguments.

Until now that claim was proven by a single suite, `JustDummies.UnitTests`, which
establishes it by sampling. A test pins a constraint — `Between(10, 20)`,
`WithLength(12)`, `WithCount(4)` — then draws a few hundred values and asserts the
invariant on each. The drawn values vary; the **constraint arguments do not**. The
suite therefore instantiates the universal claim at a handful of hand-picked
points and proves nothing about the rest of the constraint space.

Defects have already been found in that unproven space. Issue #206 was a decimal
interval generator whose draws never crossed the midpoint of the requested range:
every candidate fell in the lower half. It was found by hand and pinned as a
seeded regression over one interval, `[0, 100]`. The bug lived in the relation
between arbitrary bounds and the produced value — precisely the dimension a fixed
argument cannot vary.

The suite also draws its own sampled values from `JustDummies`, so the component
under test participates in deciding what it is tested with.

The repository already operates property-based suites. `FirstClassErrors.PropertyTests`
and `FirstClassErrors.RequestBinder.PropertyTests` run FsCheck, carry the .NET
Framework 4.7.2 floor leg, and are what OpenSSF Scorecard's Fuzzing check reads to
credit the project. Neither was introduced by an ADR: a sibling `*.PropertyTests`
project is established practice here, not a new architectural move.

Not every contract of the library is universally quantified. A conflict must raise
`ConflictingAnyConstraintException` with a message naming *both* offending
constraints; a null argument must raise `ArgumentNullException`; the mirror between
`Any` and `AnyContext`, the factory naming convention, and the library's standalone
assembly boundary are structural facts checked by reflection. These are specific,
named cases, and their wording is deliberately direction-aware — a property that
quantified over them would assert less, less readably.

The July 2026 architecture audit recorded that ADR-0025 cites "a property test"
against the real regular-expression engine, whereas what exists is a fixed-seed,
fixed-corpus oracle test in the unit-test project, and asked that the text say what
the safety net actually is.

## Decision

`JustDummies` is tested by two sibling suites under one boundary: `JustDummies.PropertyTests`
owns every invariant that can be quantified over **generated constraint arguments**,
and `JustDummies.UnitTests` owns every contract whose subject is a specific, named
case — message content, argument validation, structural conventions, and dated
regressions.

## Rationale

The library's claim is a universal quantification, so the test that matches its
shape is one that quantifies. Generating the constraint arguments — the bounds,
lengths, counts, pools and seeds a caller declares — turns each test from an
instance of the claim into the claim itself, and moves the search into the space
where #206 actually lived. Sampling more values behind a fixed `Between(10, 20)`
explores none of it.

A property framework also reports failures differently. Shrinking reduces a
counter-example to its minimal form, so a defect arrives as the smallest interval
and value that break it rather than as one opaque draw out of a few hundred. For a
component whose failures are arithmetic edge cases, that is the difference between
a diagnosis and a starting point.

Drawing the constraints from an independent generator breaks the circularity noted
in Context: the suite no longer uses `JustDummies` to decide what to test
`JustDummies` with. FsCheck's own bias toward small values is compensated by
explicitly mixing in domain edges, since an off-by-one at `int.MaxValue` is
otherwise almost never drawn.

The boundary is drawn where each style is genuinely stronger, not by kind of code.
Message content, null handling and reflection-driven conventions are not universal
claims; expressing them as properties would add quantification over inputs that do
not vary, obscure what is being asserted, and make the exact-wording assertions
harder to read. Keeping them in the example suite leaves each suite saying the kind
of thing it says best, and lets a failure's location already indicate what class of
contract broke.

Dated regressions stay with the examples for the same reason. A regression pins a
defect that actually occurred, at the coordinates where it occurred; that specificity
is its value, and a property covering the same ground does not retire it.

Two sibling projects rather than one mixed project follows the convention the
repository already applies twice, keeps the FsCheck dependency out of the suite that
does not need it, and lets each project state its own framework-floor story.

## Alternatives Considered

### Widen the sampled loops in the existing suite

Raising the sample count and adding more hand-picked intervals is the cheapest
change and needs no new project.

Rejected: it multiplies draws inside the same fixed constraint arguments. The
dimension left unexplored — the relation between an arbitrary bound and the produced
value — stays unexplored, so the class of defect that #206 belonged to remains
invisible. It buys runtime, not information.

### Convert the whole test bed to properties

A single suite is simpler to explain, and the invariant-shaped tests would all gain
from quantification.

Rejected: the contracts described in Context are not universally quantified. A
property asserting that a conflict message names both constraints is a worse example
test — it quantifies over nothing while making the assertion harder to read — and the
reflection-driven convention guards have no input space at all.

### Host the properties inside `JustDummies.UnitTests`

One project is one thing to build, run and configure.

Rejected: it puts two assertion styles and two dependency sets in one assembly, and
loses the signal that a failing project name already carries. It would also depart
from the sibling-project convention the repository has applied to `FirstClassErrors`
and `FirstClassErrors.RequestBinder`.

### Generate the constraint arguments with `JustDummies` itself

The library is a value generator, so it could supply its own test inputs and avoid a
dependency.

Rejected: it deepens the circularity rather than breaking it. A generator defect
would then be free to bias the very inputs meant to expose it, and no failure could
be attributed with confidence.

## Consequences

### Positive

* The library's universal claim is proven over a constraint space rather than at a
  handful of points, and defects of the #206 class become reachable by the suite.
* A failing property arrives shrunk to a minimal counter-example.
* Each suite states one kind of contract, so a failure's location already classifies it.
* The property suite carries the .NET Framework 4.7.2 floor leg like its siblings, so
  the invariants are proven against the `netstandard2.0` asset consumers actually load.
* The regular-expression round-trip is proven by an actual property, which lets ADR-0025's
  claim be restated accurately — the audit finding that prompted this is closed by
  construction rather than by editing the text.

### Negative

* Two suites must be kept in mind when a generator changes, and the boundary has to be
  applied deliberately rather than by habit.
* An invariant already proven by a property may still be re-asserted by an example that
  looks redundant when read in isolation; the redundancy is intended where the example
  is a dated regression, and unintended otherwise.
* FsCheck's default generators need explicit edge-biasing to be useful here, which is
  additional test-support code to maintain.

### Risks

* A property whose generated arguments straddle a value-dependent legality boundary
  (ADR-0035) can be written so that it fails intermittently rather than deterministically.
  A property must decide the expected outcome from the generated value, not from the call
  shape.
* Statistical properties — that a range is reached, that both branches of a coin flip are
  observed — are probabilistic, not universal. Written carelessly they flake; they belong
  under a pinned seed and must be labelled as statistical guards.
* Pruning the example suite as properties land can silently reduce coverage if an example
  is removed whose invariant the property does not in fact subsume.

## Follow-up Actions

* Revisit ADR-0025's "property test" wording, which the audit flagged as inaccurate, now that
  a round-trip property exists. Only `@reefact` may amend or supersede an accepted ADR.

## References

* [Writing JustDummies tests](../WritingJustDummiesTests.en.md) — how this boundary is applied when adding a test
* [ADR-0025](0025-generate-strings-from-a-home-grown-regular-subset.md) — the regular subset whose round-trip this suite proves
* [ADR-0035](0035-enforce-structural-any-conflicts-at-compile-time.md) — structural versus value-dependent conflicts, which decides how a property must branch
* [ADR-0036](0036-draw-lattice-constrained-scalars-on-the-grid.md) — lattice constraints, whose grid invariant is quantified by the property suite
* [JustDummies architecture and design audit, 2026-07-20](../audit/2026-07-20-dummies-architecture-and-design-audit.md) — the "not property-based" finding on ADR-0025
* Issue #206 — the decimal interval defect that motivated quantifying over bounds

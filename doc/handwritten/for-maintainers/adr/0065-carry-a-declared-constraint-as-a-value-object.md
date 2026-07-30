# ADR-0065 | Carry a declared constraint as a value object, not as its rendered text

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0065-carry-a-declared-constraint-as-a-value-object.fr.md)

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

## Context

A contradiction between two constraints fails at declaration, with a message naming both sides —
`Cannot apply Between(0, 100) because GreaterThan(200) is already defined.` Naming the constraint the
caller wrote, in the spelling they wrote it, is part of the library's contract: a contradiction in a
test's `Arrange` is a defect of the test and must read as one. ADR-0063 routes those throws through
factories named after the failure.

Until this decision, a constraint reached those messages as a string assembled at the site that
declared it. There were around 290 such sites across some thirty files: the generators' fluent
methods, the four interval engines, the string, collection, count and URI specifications. Three
shapes recurred — a name alone, a name with its arguments rendered, and a name whose arguments the
library must not render because the pooled type is opaque and its `ToString` belongs to the caller.
Each site wrote its own parentheses.

Three properties follow from that arrangement, and they are facts about the code as it stood:

* The spelling was not tied to the method it named. Renaming a public method left its diagnostics
  behind, and a misspelled name was a string literal that compiled.
* A specification does not only render a constraint; it **compares** them. Around twenty comparisons
  decide whether a second declaration is a harmless redeclaration — the same call with the same
  arguments, which returns the specification untouched — or a genuine conflict. Some were written as
  ordinal string comparison, some with `==`.
* Several engine entry points take a constraint next to other strings — a type name, a rendered
  bound, an exhaustion clause — with nothing distinguishing them but their position.

Two further facts bear on the shape of the solution. Building an exception must never throw, which
ADR-0064 records and which the failure-reporting path is exempted from argument guards for.
And ADR-0063's `ConstraintClaim` pairs a blamed subject with what it claims: that subject is usually
a constraint the caller wrote, but not always — a part of a shape can be blamed too, and those are
phrases the library composes.

The repository has been here before with rules that only a reader enforces: ADR-0056 records an
explicit-type rule that drifted to 203 violations while it lived in a settings file nothing could
act on.

## Decision

A declared constraint is carried through the library as a value object that renders itself, never as
the text it renders to.

## Rationale

The punctuation that makes a constraint read as a call belongs in one place. Written at 290 sites it
is 290 chances to diverge, and divergence in a diagnostic is invisible until someone reads the
message that got it wrong.

Tying the name to the method through `nameof` converts two classes of defect into build failures. A
rename now carries its diagnostics along instead of silently leaving them stale, and a misspelling
stops being a literal that compiles. This is the same move ADR-0056 argues for: a rule the compiler
can express should be expressed there rather than trusted to attention, because attention is exactly
what was shown to fail.

Equality has to belong to the type rather than to each comparison site, because the comparison
carries behaviour: it is what separates a redeclaration that must be a no-op from one that must
conflict. Defining `==` is part of the decision rather than a convenience — those comparisons are
written with it, and a reference type without it compares identities in silence, turning every
legitimate redeclaration into a conflict with nothing in the compiler or the type system to catch
it. That is the one failure mode in this area that no other guard would have found.

Rendering when the constraint is declared, rather than when a message is composed, is what makes the
type safe on the path ADR-0064 protects. A constraint is quoted while an exception is being built;
if quoting it could compose anything, it could fail there. Reading back text produced on the path
that succeeded cannot.

Typing the applied constraint alone would not have been enough. The comparisons are between the
constraint being applied and the one a specification recorded, so both sides must be the same type
or the comparison degrades to something weaker without saying so. The stored pins therefore carry
the type too, and the exception factories that quote them accept it — which is what closes the
surface: once every parameter that means "a constraint" has the type, a constraint can no longer be
written as a literal anywhere in the library.

`ConstraintClaim`'s subject slot stays able to hold a phrase, because a blamed subject genuinely is
not always a constraint. Naming that case rather than letting a phrase pass through the constraint
slot keeps the slot's meaning intact, and a phrase carries no constraint — which is what makes it
never compare equal to the one being applied, the comparison the blame choice turns on.

The cost accepted is a wide, mechanical change: every engine's stored state and every fluent method
moved at once, because a shared engine's signature cannot change for one caller. It was taken in
tranches, each compiling and passing on its own.

## Alternatives Considered

### Keep the strings and add a naming convention

Considered because it costs nothing to adopt and leaves every call site as it is.

Rejected because it is the arrangement that already existed, and the properties it lacks are the
ones that matter: a convention cannot make a rename carry, cannot make a misspelling fail the build,
and cannot give a comparison its meaning. ADR-0056 records what happens to a rule of this kind in
this repository when nothing can act on it.

### Add an analyzer that checks the literals' shape

Considered because the repository already ships first-party analyzers (ADR-0044) and reaches for one
where the type system cannot (ADR-0059).

Rejected because the type system *can* reach this. An analyzer would verify that a literal looks
like a call while leaving it a literal — it could not tie the spelling to the method, and it could
not give the redeclaration comparison its semantics. ADR-0059 reaches for an analyzer where no type
expresses the rule; here one does.

### Make it a struct

Considered for the allocation on a path that runs once per declared constraint.

Rejected under the repository's standing rule that a value enforcing an invariant is a class: a
struct exposes a parameterless constructor yielding an instance that bypassed every factory. The
same reasoning `ConstraintClaim` states for itself.

### Render lazily, composing the text when a message asks for it

Considered because a constraint that never reaches a conflict would then never be rendered, and most
do not.

Rejected because the moment a constraint *is* rendered is the moment an exception is being built,
which is the one place the library must not do work that can fail (ADR-0064). Trading a guarantee on
the failure path for an allocation on the success path is the wrong direction.

### Type only the constraint being applied, leaving the stored ones as strings

Considered as a smaller change reaching most of the benefit.

Rejected because the two are compared against each other. Leaving one side a string either forces a
rendering at every comparison — reintroducing the text the decision removes — or lets the comparison
mean something weaker than it did.

## Consequences

### Positive

* A constraint's spelling follows the method it names; renaming carries the diagnostics along.
* A misspelled or invented constraint name is a build failure rather than a message no one reads
  until it is wrong.
* Redeclaration-versus-conflict is a property of the type, decided once instead of at twenty sites.
* The parentheses exist once.
* Adjacent parameters that used to be interchangeable strings are now distinguishable by type.
* Quoting a constraint into a message cannot fail, by construction rather than by inspection.
* A constraint literal cannot be written anywhere in the library; the compiler refuses it.

### Negative

* A wide change: every generator, every engine's stored state, and the exception factories moved.
* A second small value type lives beside `ConstraintClaim` in the same area, and a reader must tell
  the two apart — a constraint, versus a subject paired with what it claims.
* Equality, its operators and their coverage are now something the type owns and must keep.

### Risks

* A blamed subject is not always a constraint, so a phrase form remains. A contributor could route a
  real constraint through it and lose the typing for that message. Mitigated by naming the phrase
  factory for the case it exists for, rather than leaving a stringly-typed slot that accepts both.
* The generators still render their own arguments through per-type helpers, so the *arguments* of a
  constraint remain strings assembled locally. That is a smaller surface than before and is
  type-specific by nature, but it is where a rendering inconsistency could still appear.

## Follow-up Actions

* Consider deduplicating the per-generator argument renderers, which are near-identical across the
  scalar generators and diverge only where a type genuinely renders differently.
* Revisit whether `ConstraintClaim`'s subject and this type should merge once the phrase cases are
  better understood.

## References

* [ADR-0040](0040-split-the-justdummies-test-bed-between-example-and-property-suites.md) — which
  suite owns a message's wording.
* [ADR-0044](0044-ship-justdummies-analyzers.md) — first-party analyzers.
* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.md) — a rule nothing can act
  on drifts.
* [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.md) — analyzers where the
  type system cannot reach.
* [ADR-0063](0063-throw-the-library-s-own-exceptions-through-named-factories.md) — named throw
  factories, and `ConstraintClaim`.
* [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md) —
  building a failure report must not fail.

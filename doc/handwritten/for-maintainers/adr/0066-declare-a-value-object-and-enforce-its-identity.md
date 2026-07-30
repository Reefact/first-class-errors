# ADR-0066 | Declare a value object with an attribute, and enforce its identity by convention

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0066-declare-a-value-object-and-enforce-its-identity.fr.md)

**Status:** Proposed
**Proposed:** 2026-07-30
**Decision Makers:** Reefact

## Context

The library holds three values built to be compared or carried by their content rather than by which instance one
holds: a declared constraint (ADR-0065), the pair of a blamed subject and what it claims, and what a failed draw
needs in order to be replayed. Two of the three describe themselves in their own remarks as values like every other
in this repository, and are immutable with private constructors reached through factories.

Only one of the three carried a value identity. The other two answered "is this the same one?" by reference, and did
so silently: a reference type compares by identity when nobody writes another answer, which raises no compiler
warning, fails no test, and reads as nothing at all to a reviewer. The one that had its identity had it because code
happened to compare it with `==`, which forced the question; nothing forced it for the other two, and the gap
shipped.

The `==` operator is the half of this that degrades quietest. A type missing `Equals` is at least visibly missing it
to anyone reading the type; a type missing the operators still compiles at every `a == b` and compares references
there.

Immutability does not identify a value here. The generators and the specifications are immutable too — they are
rebuilt rather than mutated on every constraint — but two identically constrained generators are two recipes, not
one value; comparing them by content would answer a question that has no meaning for them.

The repository already meets a rule of this shape with a marker plus a reflection convention: ADR-0045's null-guard
convention discovers members rather than naming them, and ADR-0064 declares its exemption with
`[BuiltOnTheFailurePath]` rather than inferring it. ADR-0056 records what becomes of a rule in this repository when
nothing can act on it: an explicit-type rule drifted to 203 violations while it lived where only a reader could
enforce it.

## Decision

A type whose instances are values declares itself with `[ValueObject]`, and a reflection convention holds every
marked type to a full value identity.

## Rationale

The gap this closes is invisible by construction, which is what makes a convention the right instrument rather than
attention or review. Nothing about a value missing its equality looks wrong: the type is immutable, its factories
are named, its remarks say it is a value. Only asking the question reveals the answer, and two of three values
shipped without anyone asking.

The marker earns its place because the rule cannot be derived. Detecting values by immutability would sweep in the
generators and the specifications and demand of them an equality that would misstate what they are. Deriving them
from a naming pattern would be worse: it would depend on a convention no less fragile than the one being enforced.
Declaring is a decision a human makes once per type, and a decision is exactly what an attribute records — the same
reasoning ADR-0064 applied to its own exemption rather than inferring it from a type's shape.

Enforcing the operator pair is the part that most repays the cost. It is the only member of the set whose absence
changes behaviour without changing whether the code compiles, so it is the one a reviewer is least able to catch and
a convention is most able to.

The convention checks structure, and stops there deliberately. Whether two equal instances hash alike, and whether
the fields chosen for equality are the right ones, are questions about a specific type's meaning that no reflection
over its shape can answer; they belong to that type's own tests. What reflection can settle — sealed, immutable, and
the full member set present — is precisely the half that goes missing when nobody is looking, and it cannot be
satisfied by accident.

Sealedness is required rather than encouraged because an unsealed value cannot keep its equality symmetric: a
subclass carrying an extra field compares equal to its base in one direction and unequal in the other, which breaks
the contract every collection type relies on. Rejecting a marked struct restates, where it can be enforced, the
standing rule that a value guarding an invariant is a class: a struct exposes a parameterless constructor yielding
an instance that bypassed every factory.

## Alternatives Considered

### Require the identity of every immutable type, with no marker

Considered because it needs nothing declared and cannot be forgotten on a new type.

Rejected because it is not true of every immutable type here. The generators and the specifications are immutable
and are not values, so the rule would either force a meaningless equality on them or need an exclusion list — which
is a marker, inverted, and one that grows silently as the library does.

### Infer values from a naming or namespace convention

Considered because it would need no attribute and no list.

Rejected because it would rest the enforcement on a convention exactly as unenforced as the one it replaces. A type
renamed out of the pattern would leave the convention silently, which is the failure this decision exists to
prevent.

### Rely on an analyzer instead of a test

Considered because the repository ships first-party analyzers (ADR-0044) and reaches for one where the type system
cannot express a rule (ADR-0059).

Rejected because the rule is about the library's own types rather than about how a consumer writes code. An analyzer
is the right instrument when the diagnostic must reach a consumer's build; here the audience is this repository, and
its own suite already enforces conventions of this shape by reflection.

### Use records for the values

Considered because a record generates the whole identity set, so the gap could not occur.

Rejected because the generated equality is over all members, which is not always the right answer — one of these
values compares a constraint alongside the text that renders it, precisely so that a phrase reading like a
constraint is not mistaken for it. A record would also make the primary constructor a public entry point, where
these types deliberately route construction through named factories.

## Consequences

### Positive

* A value that forgets its identity fails a test instead of shipping.
* The operator pair — the member whose absence is silent — is enforced like the rest.
* What a type is, is declared where the type is, and a reader learns it from the type itself.
* Marked types are enumerable, so the set of values in the library is now a question with an answer.

### Negative

* A new value must be marked to be covered; forgetting the marker leaves it unchecked, and only a reviewer catches
  that.
* The convention constrains its marked types beyond equality — sealed, immutable, class — so a legitimate future
  value that needed to be otherwise would have to argue the point rather than simply differ.

### Risks

* Structural checking can read as sufficient. A type can carry the whole member set and still compare on the wrong
  fields; the convention says nothing about that, and its own documentation says so rather than leaving the reader
  to assume otherwise.
* The marker can be applied to something that is not a value, which would demand an equality that misstates it.
  Mitigated only by review — the attribute is a claim, and a wrong claim is a wrong decision, not a broken rule.

## Follow-up Actions

* Consider whether the values in `FirstClassErrors` — which carry their identities already — should declare
  themselves the same way; the two assemblies cannot share the attribute, since JustDummies is standalone by
  ADR-0011.

## References

* [ADR-0011](0011-host-dummies-as-a-standalone-package.md) — JustDummies depends on nothing in this repository.
* [ADR-0044](0044-ship-justdummies-analyzers.md) — first-party analyzers.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md) — a convention that discovers members rather
  than naming them.
* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.md) — a rule nothing can act on drifts.
* [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.md) — when an analyzer is the instrument.
* [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md) — a marker declaring a
  decision rather than inferring it.
* [ADR-0065](0065-carry-a-declared-constraint-as-a-value-object.md) — the value whose equality forced the question.

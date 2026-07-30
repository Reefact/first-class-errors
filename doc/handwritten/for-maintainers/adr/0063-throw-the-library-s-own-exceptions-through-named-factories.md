# ADR-0063 | Throw the library's own exceptions through named factories

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0063-throw-the-library-s-own-exceptions-through-named-factories.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-07-30
**Decision Makers:** Reefact

## Context

JustDummies refuses contradictions at declaration time, and says why. That promise is kept by a
message: `Cannot apply WithLength(3) because StartingWith("ORD-") already requires at least 4
characters.` The messages are good, and they were assembled where they were thrown — in the middle
of the code that decides.

The result is that a method about constraints spends four lines on prose. In the interval specs a
draw loop read like this:

```csharp
throw new AnyGenerationException(
    $"Generation failed: no {_typeName} value near the drawn candidate satisfies the exclusions. {source.ReplayGuidance(random.Seed)}",
    random.Seed,
    new InvalidOperationException($"Every representable value within {NudgeBudget.ToString(CultureInfo.InvariantCulture)} steps of the drawn candidate, in both directions, is excluded or out of bounds. Values further away were not examined, so this is an exhausted local search rather than an empty range."));
```

Four lines the reader must step over to follow the algorithm, none of which is about drawing a
value. And when the same failure is reported from several places the wording is retyped: the
sentence `Cannot apply X because Y.` was written out at **84 throw sites** across the library.

Duplication is the visible symptom, and it is not the reason for this decision. A message assembled
once is still prose in the middle of logic.

## Decision

**Every throw of one of this library's own exceptions goes through a static factory on that
exception, named after the failure it reports.**

* The factory names a **case**, not a shape of sentence: `NoValueSatisfies`, `NoValueRemains`,
  `AlreadyDefined`, `GridNudgeExhausted`. A method that names the *grammar* of a message
  (`Because(applying, reason)`, taking a free-form reason) is not a factory and does not qualify —
  it is the constructor with a prefix, and its call sites say nothing.
* This holds **whether or not the message repeats**. A failure reported from one place, once, and
  for ever still gets a name.
* Factories are `internal` unless a consumer needs to construct the exception. The exception type
  keeps its public constructors; nothing about the public surface changes.
* Where several factories share a sentence, a **private** helper may own it, so the shape exists in
  one place. Private because it names grammar: every caller must be a named case.
* **Nothing on the construction path guards its arguments.** ADR-0045 already exempts exception
  types and the reflection convention skips them, for the reason given there: building an exception
  must never throw, or the failure being reported is replaced by a failure about reporting it.
  Non-nullable parameters make the contract the compiler's instead.
* When naming the case would need more loose arguments than a reader can keep in order, the
  arguments that belong together become a **value object** — a class, immutable, and on this path
  unvalidated for the reason above. `ConstraintClaim` (a constraint and what it claims) is the first.

**This applies only to the exceptions this library declares** — the `DummyException` hierarchy:
`ConflictingAnyConstraintException`, `AnyGenerationException`, `UnsupportedRegexException`. It does
**not** apply to `System` exceptions or to any type the library does not own. `ArgumentNullException`,
`ArgumentException` and `ArgumentOutOfRangeException` keep their guard-clause form
(`if (x is null) { throw new ArgumentNullException(nameof(x)); }`), which ADR-0045 requires and which
no factory could improve — and which in any case cannot be added to a type the library does not
declare.

## Rationale

**The business code stays business code.** `WithMinimum` is about tightening a bound. That a
contradiction produces a particular English sentence is plumbing, and plumbing belongs with the
mechanism. An exception type *is* a mechanism more than anything else, which makes it the right
home: the call site states which failure occurred, and the exception knows how to say it.

**A name is worth more than a message at the call site.** `throw AnyGenerationException.GridNudgeExhausted(...)`
tells a reader what happened in three words. The message it produces tells the *user* what happened,
which is a different audience and a different moment. Separating them lets both be good.

**The rule is cheap to follow and cheap to check.** "Does this file contain `throw new`, for one of
our own exceptions?" is a question with a yes/no answer, which is what makes a convention hold. A
rule qualified by "when the message repeats" would need judgement at every site and would drift, as
the 84 hand-written sentences show.

**Uniformity is the point, not economy.** Ten factories for ten call sites used once each is not
waste; it is ten call sites that read as statements of fact rather than as string assembly.

## Alternatives Considered

### Factories only where a message repeats

The first version of this work applied that criterion, and it is wrong in both directions. It
leaves single-use failures assembling prose inline — the very case the interval-spec draw loop
showed at its worst — and it makes the rule un-checkable, since "repeats" is a property of the
whole library, not of the site being written.

### A general-purpose factory taking a free-form reason

Tried, in the shape `Because(applying, reason)`. It centralises the sentence and nothing else: the
caller still composes the reason, so the call site still says nothing about the failure. Worse, it
is an escape hatch — with it available, no future case needs a name. Rejected, and the four sites
that used it turned out to be one nameable case.

### Guarding the factories' arguments

Considered and implemented briefly, then removed. It contradicts ADR-0045, whose reflection
convention excludes exception types before accessibility is ever considered — so the guards were
never exercised, and a green test suite said nothing about them.

### Adopting the FirstClassErrors error model

The obvious neighbour: FirstClassErrors already models errors as first-class values with codes,
context and generated documentation. Rejected on the boundary ADR-0011 records — JustDummies must
not reference any FirstClassErrors project, and is deliberately *error-agnostic*, because it is
referenced by its consumers' test projects and must not impose an error model on them. What crosses
that boundary is the discipline, not the types. Error codes are declined with it: these failures are
read once by a developer fixing a test, and a stable, documented code would carry a documentation
cost no reader of theirs would ever collect.

## Consequences

* A new failure requires a factory before it can be thrown. That is the intended friction: naming
  the case is the design step, and it happens before the message is written.
* The exception types grow. `ConflictingAnyConstraintException` holds the sentence shape for every
  conflict in the library, which makes it the file to read when a message must change — and the
  only one.
* The messages are observable behaviour and the unit suites assert their content, so the conversion
  is verifiable: a green suite is the byte-for-byte guard that no wording moved.
* Converting the existing sites touches most of the library. It is done in **functional slices** —
  the interval specs, the size specs, the `Any*` generators, the collection and URI specs, the
  pattern engine — chosen so each pull request is a unit a reviewer can name in one sentence.
* Mutation testing selects per file — the cost of a slice follows the files it touches, not the
  lines it changed — so a wide slice may exceed the advisory per-PR budget and report no score.
  That is a consequence of the slicing, never a constraint on it: functional coherence decides the
  boundaries, and the weekly full sweep is the enforced bar (ADR-0046).

## References

* [ADR-0011](0011-host-dummies-as-a-standalone-package.md) — JustDummies is standalone and
  error-agnostic; it must not reference any FirstClassErrors project.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md) — argument guards, and the
  exemption of exception types on which this decision rests.
* [ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md) — the per-PR mutation check
  is advisory.
* [ADR-0049](0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md) —
  records that Stryker's `--since` selects per file, and drops the JustDummies generator from the
  per-pull-request matrix because of what that costs.

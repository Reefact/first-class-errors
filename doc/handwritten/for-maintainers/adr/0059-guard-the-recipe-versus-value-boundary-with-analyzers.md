# ADR-0059 | Guard the recipe-versus-value boundary with analyzers where the type system cannot reach it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0059-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-29
**Accepted:** 2026-07-31
**Decision Makers:** Reefact

## Context

* ADR-0020 removed the 28 implicit conversions from JustDummies generators to their generated types, making
  `Generate()` the sole materialization. Its *Risks* section assessed the residual danger as bounded: a user who
  omits `.Generate()` hits "a compile-time error with an actionable message ..., **never a silent wrong value**".
  Its *Follow-up Actions* concluded: "Do not pursue the optional analyzer suggested in issue #190; the removal
  makes it unnecessary."
* That assessment holds wherever the target position is typed by the **generated value**. `int x = Any.Int32()` is
  `CS0029`, `Any.Int32() == 5` is `CS0019`, and `Assert.Equal(Any.Int32(), value)` is `CS0411`. There, removing the
  conversion did turn a silent substitution into a compile error.
* It does not hold wherever the target position accepts the generator's **own static type**. Generators are
  reference types, so no conversion is needed and none was there to remove: `object`, `params object[]`, `dynamic`,
  an `object[]` or `List<object>` element, an interpolation hole, an operand of a `string` concatenation, and the
  inherited `object.ToString()` / `object.Equals` all accept a generator as it stands.
* No JustDummies generator overrides `ToString()`. Rendering one as text therefore yields the builder's CLR type
  name — `$"{Any.String()}"` produces the literal string `"JustDummies.AnyString"`. Verified by compilation: every
  shape above builds with zero diagnostics of any kind.
* The resulting value is non-empty, plausible, and identical on every run. It reaches the code under test as if it
  were an arbitrary value, so the test passes green while exercising a constant — the precise outcome `Any` exists
  to prevent, and the one ADR-0020 recorded as impossible.
* A second, adjacent shape is silent for the same structural reason. Generators are immutable recipes, so a
  constraint returns a new generator; a call whose result is discarded (`numbers.NonEmpty();`) reads as a mutation
  and drops the declared invariant. Verified: no compiler, CA or IDE diagnostic fires, even at
  `AnalysisLevel=latest-all`, because an invocation is a legal expression statement.
* ADR-0044 established first-party JustDummies analyzers as the repository's answer to a mistake the type system
  cannot express, and its own follow-up invites applying that pattern to future such mistakes. ADR-0035 draws the
  dividing line the other way round for constraint conflicts: the type system carries what is structural, the
  analyzer carries what it cannot.
* JustDummies is pre-1.0, so no consumer has yet been taught either behaviour.

## Decision

The recipe-versus-value boundary is guarded by first-party JustDummies analyzers in every position that accepts a
generator's own static type, which the removal of the implicit conversions did not close.

## Rationale

* The decision ADR-0020 took is untouched and remains right: `Generate()` stays the sole materialization, and no
  implicit conversion returns. What this ADR revises is one **prediction** ADR-0020 made about the world after that
  removal — that no silent wrong value could survive it — and the follow-up action that rested on the prediction.
  A record whose reasoning is sound but whose factual claim is now known to be false is corrected by a new record,
  not by leaving the claim to be read as still true.
* The analyzer ADR-0020 declined and the analyzers decided here are not the same instrument. The rejected one was
  the price of *keeping* the conversions — a permanent 28-operator surface plus a rule to police its traps, to
  preserve a shorthand. These are the opposite: nothing is preserved and no surface is added, they close what the
  removal left open. ADR-0020's argument against the first does not reach the second.
* The enforcement point follows what each mechanism can know, the same grain as ADR-0035 and ADR-0044. C# cannot
  refuse a reference type in a position typed `object`, and it cannot make an expression statement illegal; the
  type system therefore *cannot* carry these two rules, which makes the analyzer the only available mechanism
  rather than a weaker substitute for one.
* Severity follows the failure mode rather than the family. A generator rendered as text is a silent green — the
  build succeeds, the test passes, the assertion is meaningless — which is the case ADR-0044 already judged worth
  failing the build for. A discarded constraint is a *probabilistic* green, red only on the run that draws outside
  the intended domain, so it warns rather than fails.
* The cost is bounded by what the rules decline to report. A diagnostic on the recipe-versus-value boundary is
  cheap to be wrong about, because a legitimate use of a generator in an `object` position is rare and a
  suppression is one line; the rules are nevertheless scoped so that an explicitly discarded result and a
  conflict-asserting negative test stay silent, which is what keeps them usable in a suite that tests the library's
  own failure behaviour.

## Alternatives Considered

### Leave it to documentation, as ADR-0020's follow-up prescribed

Considered because it is the standing decision, costs nothing, and the library's documentation already teaches the
recipe-versus-value model at length.

Rejected because documentation cannot reach the failure. The defect produces a passing build and a passing test:
there is no moment at which a reader is prompted to consult the documentation, and no artifact that says anything
is wrong. Every other mechanism in the library that guards this model — the removed conversions, the eager
constraint conflicts — fails loudly; leaving this one case to prose is the only place where the model is taught but
not enforced.

### Restore a narrow implicit conversion so the compiler can refuse the ambiguous positions

Considered because a conversion to the generated type would make an `object` position bind the value rather than
the recipe, closing the hole in the language rather than beside it.

Rejected because it reintroduces exactly what ADR-0020 removed, and for a worse reason: the conversion is
effectful, non-idempotent and throwing, and the `object` position is the one place where its behaviour would be
least predictable. It would trade a diagnosable mistake for an undiagnosable one.

### Make the generators sealed against text rendering by overriding `ToString()`

Considered because an override returning the drawn value, or a deliberately alarming string, would make
`$"{Any.String()}"` harmless or obviously wrong at a glance, with no analyzer at all.

Rejected on both readings. Returning a drawn value makes `ToString()` an effectful, non-idempotent draw — the
implicit conversion again, under another name. Returning an alarm string improves the symptom without preventing
it: the test still passes, still asserts on a constant, and the alarm surfaces only if a human reads the value.

## Consequences

### Positive

* The two silent shapes become build-time diagnostics: a generator rendered as text fails the build, and a
  discarded constraint warns, with a message that teaches the model rather than merely naming the rule.
* ADR-0020's factual claim is corrected in the record rather than left to be discovered by whoever hits it, and the
  reason its follow-up action no longer applies is stated where a future maintainer will look.
* The `JustDummies.Usage` category gives the recipe-versus-value rules a home, so a consumer can tune them
  independently of the reproducibility rules.

### Negative

* The rule set grows, and with it the documentation surface: each rule carries an English and a French page, an
  index entry and a release-tracking row.
* Two rules fire on shapes that a suite testing JustDummies' own failure behaviour legitimately writes, so both
  carry a documented exclusion that a reader must know to reason about what the rules do not catch.

### Risks

* The `object`-position family is wider than the two rules decided here — an `object`-typed parameter, a
  `params object[]` element, `dynamic` — and covering it carries a genuine false positive: a test helper that
  deliberately accepts `object` and materializes it itself. Mitigated by leaving that rule out of this decision and
  deciding it on dogfooding evidence rather than in advance.
* A rule keyed on the absence of a `ToString()` override would silently stop applying if a generator ever gained
  one. Mitigated by resolving the inherited `object.ToString()` specifically, so a real override is excluded by
  construction rather than by assumption.

## Follow-up Actions

* Supersede nothing: ADR-0020's decision stands unchanged, and its status is the maintainer's to revisit if the
  corrected claim is judged to warrant it.
* Decide the remaining `object`-position rule on dogfooding evidence gathered from this repository's own suites,
  not before.

## References

* ADR-0020 — materialize dummies only through `Generate()`; the decision this one leaves standing and whose
  residual-risk claim it corrects.
* ADR-0044 — ship first-party JustDummies analyzers; the pattern this decision applies, and the source of the
  severity grain ("a silent green is worth failing the build").
* ADR-0035 — enforce structural `Any` conflicts at compile time, value-dependent ones at run time; the same
  "enforcement follows what the mechanism can know" reasoning, applied to the constraint surface.
* Issue #190 — define and document the contract of implicit generator conversions; the origin of the analyzer
  ADR-0020 declined.

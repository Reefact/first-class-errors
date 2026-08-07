# ADR-0061 | Run the JustDummies analyzers on the repository's own code

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0061-run-the-justdummies-analyzers-on-the-repository-s-own-code.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-29
**Accepted:** 2026-07-29
**Decision Makers:** Reefact

## Context

This repository ships two analyzer packages. `FirstClassErrors.Analyzers` carries
`FCE001`–`FCE022`; `JustDummies.Analyzers`, created under
[just-dummies ADR-0023](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0023-ship-justdummies-analyzers.md), carries `JD001`–`JD028`.

The two are verified very differently.

The FirstClassErrors analyzers are loaded by `FirstClassErrors.Usage`, which the
`analyzers` workflow builds on every pull request. The rules therefore run, on every
change, over code written to use the library rather than to exercise the rules.

The JustDummies analyzers are loaded by no project in this repository. Their only
standing verification is their own unit suite — 246 tests, each compiling a snippet
written by the rule's author. Beyond that, an agent injects the built analyzer into the
repository's suites by hand, through an MSBuild property, and reads the warnings.

That hand sweep is not a formality. It has contradicted the author's model at least once
in each of the last four rule waves, and each contradiction was a rule that would have
shipped wrong:

* `JD015` modelled letter casing as a character pool, which would have condemned the
  legal `UpperCase().StartingWith("ORD-")`.
* `JD016` counted an enum's declared members while `AllowingCombinations()` widens the
  universe to their OR-closure.
* `JD023` treated `LessThanOrEqualTo(long.MinValue)` as unsatisfiable, a chain the
  library's own suite asserts is legal.
* `JD028` assumed every draw is a fresh instance, which is false for a pool: `OneOf`
  returns the very references it was given.
* `JD027`'s stand-down for a throwing composer did not fire, because an
  expression-bodied `=> throw` is a return *carrying* the throw, under a conversion.

None of these were found by the unit suite, and none could have been: the author writes
both the rule and the snippet it is tested against, so a shared misconception passes
both. All were found on the library's own code, which was written for other reasons and
therefore does not share the misconception.

The rules span four severities. `JD001`–`JD005` are `Error`; most are `Warning`;
`JD020`, `JD022` and `JD024` are `Info`; `JD011` and `JD019` ship disabled, opt-in.
Roslyn does not surface `Info` diagnostics at default build verbosity — an early sweep
looked clean for exactly that reason, and reported nothing while two rules were live and
silent.

The library's own test suites deliberately write the shapes the rules report. That is
not incidental: a test for a behaviour has to exercise it. Seven such sites exist today.
Five carry a `SuppressMessage` naming their rule and stating why the shape is deliberate;
the two duplicate-collapsing tests do not.

Loading the analyzers into `JustDummies.UnitTests` was measured before this record was
written. The build succeeds. Exactly two diagnostics are reported — the two
duplicate-collapsing tests. The five existing suppressions silence their rules, so the
mechanism works on a real test. Adding two more attributes brings the surface to zero.
The other JustDummies-consuming projects report nothing at all. A cold, non-incremental
build of that project moved from roughly six and a half seconds to roughly nine.

## Decision

Load `JustDummies.Analyzers` into every project in this repository that consumes
JustDummies, so the rules run at build and in the IDE, and record each deliberate
violation as a suppression naming the rule it answers.

## Rationale

A rule in this catalogue is a claim about how JustDummies behaves. Its unit suite proves
something weaker: that the rule fires on a snippet its author wrote for it. When the
author's model of the library is wrong, both sides of that test are wrong together and it
passes. Every model error listed in the Context passed its unit suite.

What caught them was realistic code — the library's own suites, written to exercise
JustDummies rather than to exercise the rules. That body of code is the only such corpus
the repository controls, and running the rules over it is the only verification that can
fail for a reason the rule's author did not think of. Making it continuous rather than
manual is the whole decision.

The current arrangement has the signal without the guarantee. It depends on whoever is
working remembering to run a sweep that nothing in the repository asks for, and the
record shows what that is worth: the sweep was not run at all before the fourth wave, and
the rules that shipped before it were the ones that needed correcting after. A check that
has caught five wrong rules and rests on memory is a check that will eventually not be
run.

The measured suppression surface — two sites, on a mechanism already proven to work on
five others — is small enough that the decision is adoptable today rather than after a
cleanup. More importantly, those suppressions are not a cost being tolerated. A test that
writes a flagged shape is the test *for* that shape, and the attribute is where a future
reader learns that the shape is the subject rather than a mistake. The repository was
already moving that way: the five existing suppressions were written before this record,
precisely because the annotation reads better than the bare shape.

That `Error`-severity rules will break the build if they ever fire on the repository's
own code is the correct behaviour, not a drawback to be worked around. An `Error` rule
firing on realistic code means either a real defect or a wrong rule, and both have to be
settled before the change merges.

The build cost is known, bounded, and paid on a test project rather than on anything
shipped.

## Alternatives Considered

### Keep dogfooding by hand

It is what found every model error, so it is not ineffective — but its effectiveness is
not the question. Nothing in the repository states that the sweep exists, when it must be
run, or what a clean result means, so the check survives only as long as whoever is
working remembers it. The record already shows it lapsing: three waves of rules shipped
before the sweep became routine, and those are the waves whose rules needed correcting.

Rejected because a verification that depends on memory is not a verification.

### Add a dedicated JustDummies sample, mirroring FirstClassErrors.Usage

Symmetrical with the arrangement that already works for the FirstClassErrors analyzers,
and it would carry no suppression pressure at all, since a sample written to demonstrate
the library has no reason to write a flagged shape on purpose.

Rejected as the answer, because a sample only exercises what someone thought to put in
it. None of the model errors came from a sample; they came from suites written well
before the rules existed, for reasons unrelated to them, which is exactly why they did
not share the author's misconception. A sample would have found none of them.

It remains a reasonable addition on its own merits — as documentation that compiles — and
this record does not argue against one.

### Run the sweep as an advisory CI job

The mechanics already exist; formalizing the injection as a workflow job would put the
result on every pull request without touching any project file, and the repository has
precedent for an advisory check in the per-pull-request mutation score
([ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md)).

Rejected on two grounds. It keeps the result out of the IDE, where the author is at the
moment the mistake is made and where a rule about a silent mistake is worth most; and an
advisory signal is one nobody is obliged to act on, which reproduces the failure mode
this record exists to close. The mutation precedent does not transfer: a mutation score
is a continuous measure whose threshold is a judgement call, while a diagnostic is a
binary claim that something specific is wrong.

## Consequences

### Positive

* The rules are verified continuously against code that was not written to please them,
  which is the only verification that can surface a wrong model.
* The five model errors this practice has already caught become impossible to reintroduce
  silently.
* The `SuppressMessage` attributes become live: a rule that stops firing on a site that
  claims to exercise it is a signal the rule or the library moved.
* A new rule's false-positive rate is measured while it is being written, in the IDE,
  instead of at the end of a wave.

### Negative

* Every project that consumes JustDummies pays the analyzer cost on each build.
* A new rule may require new suppressions in the library's suites before it can merge.
* Whoever writes a JustDummies test now meets the rules, and has to know why a suppression
  is the right answer rather than a workaround.

### Risks

* **The `Info` rules stay invisible.** `JD020`, `JD022` and `JD024` do not surface at
  default verbosity, so this decision does not verify them — it verifies the rules that
  are loud. A clean build will read as full coverage while three rules are not being
  exercised, which is precisely the trap that made an early sweep look clean.
* **The opt-in rules stay off.** `JD011` and `JD019` ship disabled and would not run,
  leaving two rules with no standing verification at all.
* **An `Error` rule can break the build on a deliberate shape.** The answer is a
  suppression, but for a rule introduced in the same change the failure arrives at merge
  rather than at authoring.
* The analyzer's own test project must not load the analyzer it tests.

## Follow-up Actions

* Decide whether the `Info` rules should be escalated in the repository's own
  configuration. Without that, the three rules whose entire value is that the run time
  says nothing are the three this decision does not exercise.
* Decide whether `JD011` and `JD019` should be exercised anywhere, given they ship
  disabled by design.
* Reconsider whether the FirstClassErrors analyzers, verified today only through
  `FirstClassErrors.Usage`, should reach that library's own suites on the same argument.

## References

* [just-dummies ADR-0023](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0023-ship-justdummies-analyzers.md) — the decision to ship
  first-party JustDummies analyzers.
* [ADR-0046](0046-make-the-per-pull-request-mutation-gate-advisory.md) — the advisory
  per-pull-request check this record declines to imitate.
* [just-dummies ADR-0038](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the
  recipe-versus-value rules, whose dogfooding produced part of the evidence above.
* [The JustDummies analyzer rules](../../for-users/analyzers/README.md).

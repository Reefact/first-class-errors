# ADR-0052 | Draw arbitrary numbers within an ordinary magnitude

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0052-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md)

**Status:** Proposed
**Date:** 2026-07-28
**Decision Makers:** Reefact

## Context

The floating-point and decimal generators sample uniformly between their bounds. Unconstrained, those
bounds are the type's whole domain — for `double`, a range spanning some 616 decades.

A uniform draw over such a range is uniform *by value*, not by magnitude, and there is as much room
between 1e307 and 1e308 as between 0 and 1e307. Essentially all the probability mass therefore sits within
a few decades of the type's maximum. Measured on 5 000 draws:

| measurement                                         | result       |
| --------------------------------------------------- | ------------ |
| `Any.Double()` — `\|v\| < 1e6`                        | 0 / 5000     |
| `Any.Single()` — `\|v\| < 1e34`                       | 0 / 5000     |
| `Any.Decimal()` — `\|v\| < 1e24`                      | 0 / 5000     |
| `Any.Double().Positive()` × 1.2 → `Infinity`         | 16.1 %       |
| `Any.Decimal()` × 1.2m → `OverflowException`          | 17.1 %       |
| `x + 1 == x` on a `Positive()` draw                  | true         |

At those magnitudes a floating-point type stops behaving like arithmetic: a further multiplication
overflows — to `Infinity` for the binary types, which is contagious and yields `NaN` downstream, and to a
thrown `OverflowException` for `decimal`. Precision is exhausted, so `x + 1 == x`. A scale constraint has
no fractional digits left to act on: `Any.Decimal().WithScale(2)` was satisfied by 5 000 draws out of
5 000, every one of them a 29-digit integer — true and empty at once.

The magnitudes where ordinary code runs, and where rounding, comparison and formatting defects live, are
never visited.

The integer generators share the same distribution — `Any.Int32()` draws below 1e6 in 0.06 % of cases,
`Any.Int64()` in 0 of 5 000 — but not the same consequence: a large integer is an ordinary integer, C#
integer arithmetic wraps silently rather than saturating or throwing, `x + 1 != x` always holds, and an
integer overflow in the code under test is frequently a genuine defect. The integer builders also ride the
shared ordinal engine, which four builder families depend on.

`Half` stops at 65 504, so its entire domain already lies within ordinary magnitudes.

ADR-0050 recorded the counterpart rule for *sizes*: a dummy is small unless something explicitly asks for
more, a maximum being a permission rather than a request. It deliberately scoped itself to sizes and left
values open. JustDummies has never been released, so the meaning of the unconstrained draw is still free
to be fixed — the standing ADR-0041 relied on.

## Decision

An arbitrary floating-point or decimal value is drawn from within an ordinary magnitude of one million,
that window clipping the declared interval and stepping aside only where it would leave that interval
empty, while the integer generators keep the full range of their type.

## Rationale

* **A dummy that breaks the test it decorates has failed at its one job.** The library's purpose is to
  supply a value whose content the test does not care about. A value that makes an unrelated
  multiplication overflow one time in six is not that: it turns the *fixture* into the cause of the
  failure, and the diagnosis costs far more than the value saved anybody. This is the whole argument, and
  the measurements above are its evidence.
* **Clipping, rather than replacing, is what keeps the rule honest.** A caller who names a magnitude —
  an interval lying beyond the window — gets exactly it, because the window steps aside when it would
  leave nothing. A caller who merely *permits* a magnitude keeps drawing ordinary values, because
  permitting is not requesting. The window therefore never breaks a declared bound; it only declines to
  target one. That is ADR-0050's rule for sizes, transplanted unchanged to values, so the library states
  one principle rather than two.
* **It restores meaning to the constraints built on top.** A scale constraint that every draw satisfies
  and none exercises is worse than an absent one: it reads as coverage in a test that has none. Ordinary
  magnitudes give a decimal its fractional digits back, so `WithScale` constrains again.
* **One million sits where a dummy is unremarkable.** It is large enough to look like a real quantity and
  to exercise multi-digit formatting, small enough that any plausible further arithmetic stays hundreds of
  decades from overflow, and it leaves a `double` around nine significant digits below the decimal point.
  A type already inside it is untouched, which is why `Half` needs no special case: a rule that narrows
  the extravagant and is silent elsewhere is a rule, not a list of exceptions.
* **The integer generators are excluded on evidence, not on convenience.** Their distribution is the same,
  their consequence is not: nothing in the measurements shows integer arithmetic breaking down, and the
  overflow a large integer may provoke downstream is often the defect a test should surface rather than
  noise it should avoid. Extending the rule there would also reach the shared ordinal engine that four
  builder families depend on, for a harm not demonstrated.

## Alternatives Considered

### Sample log-uniformly over the full domain

Considered as the option that keeps every magnitude reachable while making the extremes rare, which is
closer to "any value of the type" than a bounded window. Rejected on its numbers: it would fix the
overflow (the top decade shrinks to about 0.01 % of draws) but barely touch the second defect — the
ordinary window of 1 to 1e6 is 6 decades out of 616, so it would still be visited about 1 % of the time —
while introducing a third, since half of all draws would fall below 1e0 with a long tail toward 1e-200.
Values that small break a different class of code — divisions, epsilon comparisons, accumulations that
absorb the term — so the trade is one pathology for two.

### Mix ordinary values with notable ones

Considered because drawing mostly ordinary values with an occasional 0, ±1 or domain extreme would give
edge-case coverage for free. Rejected because it makes the dummy *remarkable*: one draw in ten would turn
the fixture into the subject of the test, and a suite that fails once in ten runs for a reason the test
never named is the exact failure mode this decision exists to remove. A test that wants an extreme should
name it.

### Extend the rule to the integer generators

Considered for consistency, since leaving them out gives the library two default policies for numbers.
Rejected for this decision on the evidence above — same distribution, materially milder consequence — and
on blast radius, the ordinal engine being shared by four builder families. The asymmetry is accepted
knowingly and recorded here rather than left to be discovered, and it is a legitimate candidate for a
later ADR should the integer case turn out to bite.

### Add an explicit opt-in for extreme values

Considered because the current behaviour stress-tests overflow by accident, and bounding the default
stops that. Rejected as unnecessary API: the capability already exists and reads better than an opt-in
would — an interval naming the magnitude is honoured exactly. Coverage that fires 16 % of the time inside
tests about something else is a cost rather than a benefit, and making that test explicit is a gain in
what the suite says about itself.

## Consequences

### Positive

* Ordinary arithmetic on an unconstrained draw stays finite and does not throw, on every continuous type.
* Generated values finally occupy the magnitudes where rounding, comparison and formatting defects live.
* Constraints layered on the value — scale above all — constrain something again.
* The library states one principle across sizes and values: a dummy is unremarkable unless something
  explicitly asks otherwise.

### Negative

* A caller who declared a wide interval no longer receives values spread across it: `Between(0,
  double.MaxValue)` yields ordinary values. This is the intended reading — the bound is honoured, not
  targeted — but it will surprise anyone who read a wide bound as a request, and the documentation has to
  say so rather than let it be inferred.
* The accidental overflow coverage the old default provided is gone. It has to be asked for explicitly,
  which is an improvement in intent and a loss for anyone who was relying on it without knowing.
* The library carries two default policies for numbers: continuous types bounded, integers full-range.

### Risks

* One million is a defensible constant, not a derived one. The argument rests on the gap between the
  magnitudes ordinary code uses and those where the types misbehave, not on a measurement of any
  particular consumer.
* A consumer whose domain legitimately lives above the window — astronomical or cryptographic
  quantities — must name its interval. Mitigated by the window stepping aside for exactly that case.

## Follow-up Actions

* State the rule in the package documentation, where the constraint surface is described.
* Revisit the integer exclusion if a consumer reports the same class of harm there.

## References

* ADR-0050 — Let a size maximum cap without steering the draw: the same principle applied to sizes, whose
  vocabulary ("a bound is a permission, not a request") this decision reuses deliberately.
* ADR-0041 — Draw flag-enum combinations behind an opt-in: the standing that an unreleased library may
  still fix the meaning of an unconstrained draw.
* ADR-0040 — Split the JustDummies test bed between example and property suites: why the window's rule is
  quantified as properties while the measured extremes stay as examples.

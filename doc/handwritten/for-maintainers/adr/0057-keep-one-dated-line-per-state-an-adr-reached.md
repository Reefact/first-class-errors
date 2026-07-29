# ADR-0057 | Keep one dated line per state an ADR reached, and never overwrite one

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0057-keep-one-dated-line-per-state-an-adr-reached.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-29
**Accepted:** 2026-07-29
**Decision Makers:** Reefact

## Context

An ADR header carries a single `Date:` line. The convention attached to it, as most recently
written, is that the date is the day the decision reached its **current** status — the day it
was proposed while *Proposed*, the day it was accepted once *Accepted* — with supersession as
the one exception that leaves the date alone.

That convention makes acceptance destructive. Flipping an ADR from *Proposed* to *Accepted*
overwrites the proposal date with the acceptance date, and the earlier one is gone from the
record. This base describes itself as "dated records of significant decisions" and "a historical
log"; the two dates answer different questions, and only one survives.

Both dates are facts about the decision, not about the file. When a decision was drafted and
when it was ratified are separately meaningful: the interval between them says whether the
decision was debated or waved through, and whether a maintainer sat on it. Nothing else in the
repository records that interval.

Of the 56 ADRs in the base, 20 have passed through a *Proposed* state in this repository. The
other 36 were entered already *Accepted* — ADR-0001 through ADR-0005, for example, were all
committed on 2026-07-21 in a single import, carrying hand-written dates from 2026-07-10 to
2026-07-19 that predate the file's existence in git. For those, no proposal date exists to be
recovered: they were never proposed here, and git records only when the record was imported.

Git holds the history of the files but not the history of the decisions. The commit that added
a file dates the writing, not the proposal; the commit that flipped a status dates the edit, not
the ratification. For records maintained by hand, and for a base created retroactively, the two
diverge.

The `.md` and `.fr.md` variants of every ADR carry the same header, so any change to its shape
is made twice per record.

## Decision

An ADR header carries one dated line per state the decision actually reached in this repository,
and no date is ever overwritten.

## Rationale

The single date is not merely incomplete, it is lossy in the one direction a log cannot afford:
it discards a fact that was recorded, at the moment a decision is ratified, and nothing else
holds it. Adding a line at acceptance rather than rewriting one costs nothing and keeps both
facts, which is the whole purpose of a dated record.

Naming the lines after the states removes the rule the previous convention needed. `Date:`
meant something different depending on the status, so it could not be read without knowing the
convention, and it could not be updated without applying it. `Proposed:` and `Accepted:` say
what they are. Supersession then needs no exception carved out for it: a supersession is not a
state this decision reached, so it adds no line, which is the same outcome the previous rule
reached by declaring an exception.

Records predating the format are converted rather than left behind, and where the base holds
only one date that date is written to both lines. This is not the fabrication it first looks
like, and the distinction is what makes the conversion admissible: writing the same date twice
asserts nothing that was not already asserted. It says one date is known and stands for both
states — which is exactly what a single `Date:` line meant — whereas inventing a *different*
proposal date, from the import commit or from anywhere else, would assert an interval that was
never observed. The repetition is the statement of the gap, not a cover for it.

Converting them is worth the edit because the alternative is a base with two header shapes for
good. Half the records answering "when was this proposed?" and half unable to would put the
burden on every future reader, to save a mechanical pass over files whose dates are not in
dispute.

## Alternatives Considered

### Keep the single date and accept the loss

Considered because it is the status quo, was reaffirmed days ago, and requires nothing.

Rejected because the loss is silent and permanent. The proposal date disappears at the moment
of acceptance, with no trace in the record that a date was ever replaced — the reader of an
accepted ADR cannot tell whether it was ratified on sight or after a month, nor that the
question is even askable.

### Keep the single date, and recover proposal dates from git when needed

Considered because git does hold the file history, so nothing looks truly lost, and it keeps
the header at one line.

Rejected because git dates the file, not the decision. For the 36 records imported already
accepted, the first commit dates the import; for records maintained by hand, a status flip
dates the edit. Neither is the proposal date, and a reader would have no way to know which
of the two they were looking at.

### Change the format but leave pre-existing records on the single-date form

Considered, and initially preferred, because 36 of the 56 records have no proposal date to
recover: they were entered already accepted. Converting them looked like it would require
inventing one, which is precisely what this decision exists to prevent.

Rejected once the conversion rule was settled: where only one date is known, it is written to
both lines, which invents nothing — it repeats a date the base already held, and the repetition
is itself the statement that no interval was recorded. That removed the objection, and left only
the cost of a mechanical pass against the permanent cost of a base where half the records answer
"when was this proposed?" and half cannot.

## Consequences

### Positive

* Accepting an ADR stops destroying a recorded fact.
* The interval between proposal and acceptance becomes readable, for the first time.
* The header is self-describing: each line says which state it dates, so neither reading nor
  updating it requires knowing a convention.
* Supersession no longer needs an exception; it simply adds no line.

### Negative

* Every record in the base was edited to carry the new header, in both language variants.
* For the 36 records that never held a *Proposed* state here, the two lines carry the same date
  and say nothing the single line did not.
* Each ADR grows a line at acceptance, in both language variants.
* The template and the format section had to change, so anything generated from them earlier is
  now out of date.

### Risks

* A reader may take two identical dates as an interval of zero — a decision ratified the day it
  was drafted — rather than as "only one date is known". The README distinguishes the two, but
  only for those who reach it.
* An agent or maintainer used to the previous rule may overwrite `Proposed:` at acceptance out
  of habit, silently restoring the loss this decision removes. Nothing checks it.

## Follow-up Actions

* Watch the first few acceptances under the new format for the overwrite habit, and consider a
  check if it recurs.
* Should a pre-existing record ever be superseded, leave its single `Date:` alone: the successor
  carries the new dates.

## References

* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — the bounded
  exception to not editing accepted ADRs in place, and the precedent for touching the whole base
  only under a stated, traceable rule.
* [ADR-0004](0004-check-every-pull-request-against-the-adr-base.md) — why this base is checked
  on every pull request, and so why its records are read rather than merely written.

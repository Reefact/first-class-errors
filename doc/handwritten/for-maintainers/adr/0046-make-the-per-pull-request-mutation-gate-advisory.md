# ADR-0046 | Make the per-pull-request mutation gate advisory

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0046-make-the-per-pull-request-mutation-gate-advisory.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-27
**Accepted:** 2026-07-31
**Decision Makers:** Reefact

## Context

ADR-0043 made mutation testing a **required, diff-scoped check on every pull request**, backed by a
weekly full sweep. Two properties of that check, both deliberate and documented, combine into a cost
that is unbounded by anything a pull request controls:

* **One full run of the library's test suite per mutant.** `"coverage-analysis"` is `off` on purpose —
  under the MTP runner Stryker's per-test selection is incomplete (stryker-net#3629) and reports a
  *false, understated* score, so the accurate figure requires running the whole suite against every
  mutant.
* **Selection is per changed *file*, not per changed *line*.** Stryker's `--since` has no line
  granularity, so touching a single line of a large file puts **every** mutant in that file on the
  gate.

The consequence surfaced on a one-line-region fix to `JustDummies/Any.cs` (~1000 lines, the largest
file in the repository): the `justdummies` leg selected the whole file's mutants and ran **~40 minutes**,
blocking the merge, while every other required check finished in ~2–3 minutes. The cost follows the size
of the *file the change lands in*, which no author can keep small on a central type.

Two further facts bear on the decision:

* The gate carries **`break: 0`** — it enforces **no score threshold**. Its only pull-request-time
  assertion is "the legs ran to completion." The enforced quality signal is the **weekly full sweep**
  (which re-measures everything) plus the per-PR **report** of surviving mutants.
* The `gate` job fails (`exit 1`) whenever its legs report `cancelled`. The workflow's `concurrency`
  group cancels an in-flight run whenever a newer commit lands on the branch — an ordinary event
  (`Update branch`, a dependabot merge into `main`). Each such supersession therefore posted a
  **spurious "mutation gate failed"** check on a pull request that was perfectly healthy.

## Decision

On **pull requests**, the mutation gate is **advisory**. The per-PR legs still run and report the
diff's mutation score, but the `gate` job **never fails the pull request**: a genuine leg failure is
surfaced as a warning to investigate, and a run cancelled by a superseding push is treated as noise, not
a failure. The **enforced bar is the weekly full sweep**. The gate's job and check name stay stable, so
no branch-protection entry has to change.

## Rationale

* **The blocking cost cannot be held to a sane feedback budget without giving up accuracy.** The only
  lever that would make full-suite-per-mutant fast — per-test coverage selection — is exactly the one
  ADR-0043 turned off because it lies under MTP (stryker-net#3629). Blocking a merge on a check whose
  honest form is minutes-to-tens-of-minutes, scaled by the size of whatever file the change touches, is
  not a reasonable required-check contract.
* **Making it advisory removes almost no real enforcement.** With `break: 0` the gate never enforced a
  score; it asserted only that the legs completed. A genuine build failure that would break the legs
  also breaks `Build & test`, which stays required. What is given up is a per-PR *threshold that never
  existed*.
* **The weekly sweep is the real signal, and it is unchanged.** It re-measures every mutant of the whole
  library, threshold-free, precisely so `main` is not turned red over unedited code. Advisory pull-request
  legs keep the fast per-diff signal as a *report* without promoting it to a blocker.
* **The concurrency-cancellation failures were never intended.** A superseded run reporting "gate
  failed" is a false negative; advisory reporting removes that class of noise as a side effect.

## Alternatives Considered

### Keep it blocking, make it fast with per-test coverage selection

Rejected: under the MTP runner that selection miscounts killed mutants as uncovered and reports a false
score (stryker-net#3629). Accuracy is the reason `"coverage-analysis"` is `off`; trading it for speed
would make the gate lie — the one thing this repository refuses of a diagnostic.

### Keep it blocking, split every large file so per-file selection stays small

A real improvement worth doing on its own merits — `Any.cs` is a god-file, and splitting it into
partial-class files by concern would keep any single diff's mutant set small. But it is a large, careful
refactor, it offers no guarantee against the *next* file growing, and it is not a precondition for
merging a correct change today. Left as a follow-up (see below), not a blocker on unblocking.

### Drop the per-PR legs entirely and rely only on the weekly sweep

Rejected: it discards the fast per-diff signal a contributor uses while the change is fresh. Advisory
keeps that signal — as a report — without the block.

### Remove the gate from branch protection instead of changing the job

Equivalent in effect, but it leaves the `gate` job still emitting `exit 1` on cancellation (so the
spurious red persists on the run's own page), and it depends on a branch-protection edit rather than
being self-contained in the workflow. Changing the job fixes both the block and the noise in one place.

## Consequences

### Positive

* Pull-request merge feedback returns to the ~2–3 minutes of the other required checks; the mutation
  legs no longer sit on the critical path.
* The spurious "mutation gate failed" checks from concurrency-cancelled runs disappear.
* The per-PR mutation **report** (surviving mutants, file and line) is unchanged and still surfaced.

### Negative

* A genuine mutation regression introduced by a pull request no longer blocks it; it is caught by the
  weekly sweep and the warning-level per-PR report, on a delay of up to a week.

### Risks

* The weekly full sweep becomes the **sole enforcement**. If its output is not read, real coverage can
  drift between Mondays. Mitigation: the sweep already publishes the survivor list per library; keeping a
  habit of reading it is the counterpart to this decision.

## Follow-up Actions

* **Speed the advisory run itself.** A `concurrency` bump is applied in `justdummies.json`. The larger
  run-time levers — dropping the FsCheck property suite from the mutation *oracle* (its
  hundred-cases-per-property dominates per-mutant time, and its non-determinism is the very reason
  `coverage-analysis` is off), and/or splitting `JustDummies/Any.cs` so per-file `--since` selection
  stays small — are separate decisions, recorded here so they are not lost.
* **Branch protection — the gate must be *removed* from the required checks to actually stop the wait.**
  Advisory removes the *false red*, not the *wait*: the `gate` job runs `needs: changed`, so it does not
  report until the diff legs finish, and a **required check that is still pending blocks the merge** even
  though it can no longer fail. So a required-and-always-green gate still holds a pull request for the
  whole ~40-minute leg. Removing `JustDummies mutation gate` and `Mutation gate` from the required status
  checks is what returns merge feedback to the other checks' few minutes; the legs keep running (advisory)
  for the report. (An earlier draft of this ADR wrongly said keeping it required was equivalent — it is
  not: pending blocks.)
* Revisit re-enabling per-test coverage selection if stryker-net#3629 is fixed upstream — it would make
  a blocking, accurate, fast gate possible again and could supersede this decision.

## References

* ADR-0043 — Gate pull requests on the mutation score of the diff: the decision this amends. This ADR
  narrows its pull-request half from *required* to *advisory*; the weekly full sweep it established is
  unchanged.
* ADR-0011 — Host JustDummies as a standalone package: why `justdummies-mutation.yml` is a separate
  workflow with its own gate.
* stryker-net#3629 — the per-test coverage-selection defect under the MTP runner that keeps
  `"coverage-analysis": "off"`.
* `doc/handwritten/for-maintainers/workflows/mutation.en.md` and `justdummies-mutation.en.md` — the cost
  model and the accuracy-not-speed reasoning quoted above.

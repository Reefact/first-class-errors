# ADR-0043 | Gate pull requests on the mutation score of what they changed

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0043-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md)

**Status:** Proposed
**Date:** 2026-07-27
**Decision Makers:** Reefact

## Context

FirstClassErrors ships five libraries — `FirstClassErrors`,
`FirstClassErrors.Testing`, `FirstClassErrors.RequestBinder`, `JustDummies` and
`JustDummies.Xunit` — whose product *is* their semantics: which error a factory
returns, which branch an `Outcome` takes, which value a constraint admits. A
defect there is not a crash a consumer can see coming; it is a wrong answer
delivered confidently.

The repository already enforces two automated quality signals on every pull
request: the full test suite on two platforms (`ci`), and SonarQube Cloud's
quality gate including line and branch coverage (`sonar`). Coverage records that
a line was **executed** by a test. It cannot record whether any test would have
**noticed** that line being wrong: a suite that calls a method and asserts
nothing about the result scores the same coverage as one that pins every case.

Mutation testing measures exactly that difference. The tool rewrites the library
one small change at a time, re-runs the suite against each rewrite, and reports
the rewrites the suite still passes on. On .NET the tool is Stryker.NET; there is
no maintained alternative.

Four facts about running it on this repository were established by measurement
before this decision was taken:

* **Stryker's default VSTest runner does not work on this test bed.** Every test
  project here is xUnit v3, and an xUnit v3 test project is an executable the
  VSTest adapter launches as a child process — out of reach of the in-process
  hooks Stryker uses both to capture coverage and to *activate* a mutant. The
  run completes, reports a plausible-looking number of tests, and scores **0 %**:
  every mutant is reported as survived, including mutants the suite demonstrably
  kills when the same edit is applied by hand. A gate built on that runner would
  be permanently red and would prove nothing.
* **Stryker's Microsoft Testing Platform runner does work**, because it launches
  the test executable itself. It is flagged *preview* by its authors.
* **Its coverage-based mutant selection is not trustworthy yet.** With selection
  enabled, mutants the suite does kill are classified as uncovered and counted
  against the score; with it disabled — every mutant tested against the whole
  suite — the same population scores materially higher and matches what running
  the mutations by hand shows. Disabling it also costs almost nothing here,
  because the suites are fast.
* **The cost is roughly one test-suite run per mutant.** That is about a second
  per mutant for these libraries, which makes a full sweep of one library a
  matter of minutes and a full sweep of all five too long to sit in front of on
  every push. Selecting only the mutants a change touches brings the common case
  down to the fixed cost of analysis and build.

Stryker's diff mode selects mutants per changed **file**, not per changed line;
there is no line granularity. A one-line edit to a large file therefore puts that
whole file's mutants on the gate.

Not every survivor is a defect: some mutants are *equivalent*, changing the code
without changing observable behaviour, and no test can kill them. A threshold of
100 % is therefore not reachable in principle.

Two of the five libraries — `JustDummies` and its xUnit v3 adapter — are already
kept free of any reference to the other three
([ADR-0011](0011-host-dummies-as-a-standalone-package.md)) and are intended to
move to a repository of their own.

Runs happen on GitHub-hosted runners: four vCPU, a six-hour cap per job. A check
can only be made mandatory on `main` through branch protection, which names
checks individually — a matrix contributes one check name per leg.

## Decision

Every pull request targeting `main` must clear a mutation-score threshold,
measured by Stryker.NET over the mutants of the files it changed, for each of the
five libraries the repository ships — enforced by two independent gates, split
along the prospective repository boundary.

## Rationale

The gate closes the specific hole the existing signals leave open. `ci` proves
the suite passes; `sonar` proves the code was executed. Neither can distinguish a
test that pins behaviour from a test that merely visits it, and that distinction
is the whole quality of a library whose product is its semantics. Mutation
testing is the only automated signal that measures it.

Making it **mandatory rather than advisory** is the point of the decision, not an
aggravating detail. An advisory report on a repository maintained by one person
is a report nobody reads; the practice it is meant to install — write the
assertion, not just the call — only survives if merging depends on it. The
repository already treats its other invariants this way: the warning ratchet, the
commit convention, the support floors are all enforced, not suggested.

Scoping the gate to **what the pull request changed** is what makes mandatory
affordable. The cost model is linear in the number of mutants, and the number of
mutants is proportional to the code under measurement; measuring the diff keeps a
typical pull request at the fixed cost of analysis and build, while measuring
everything, every time, would put tens of minutes on every push for code the
author did not touch. The trade-off accepted is that the file-level granularity
of the diff mode makes a small edit to a large file report that whole file's
score, so a pull request can be asked to answer for pre-existing gaps in a file
it merely touched. That is a real cost, and it is the acceptable direction of
error: it pushes coverage of the weakest files up on contact, and the maintainer
can always waive a leg.

Restricting the scope to the **five shipped libraries** follows the same cost
argument in the other direction. The `fce` tooling and the Roslyn analyzers are
not linked into anyone's application; their tests drive Roslyn compilations and
spawn processes, so their per-mutant cost is an order of magnitude higher, and
their behaviour is already pinned end to end by dedicated workflows. Including
them would multiply the gate's cost for the part of the codebase where a defect
is cheapest to notice.

Enforcing it through **two gates rather than one** costs nothing today and buys
the move. The JustDummies packages are already isolated from the rest by design,
and they are leaving; a single shared matrix would have to be edited, and its
required-check entry renegotiated, at exactly the moment when the least
interesting part of a repository split should be its CI. Two gates make that step
a file move. They also let the two bars move independently — which they must,
since the libraries sit at visibly different levels of test maturity, and one bar
would have to be set at the weaker of the two.

Pinning the mutation engine matters for the same reason the analyzer's Roslyn
floor is pinned ([ADR-0001](0001-lock-the-analyzer-roslyn-floor.md)): a newer
engine invents new mutants, and the score would move without a line of code
changing. A threshold is only meaningful against a fixed generator.

The **preview** status of the runner the gate depends on is the decision's main
weakness, and it is accepted knowingly: the alternative is no mutation signal at
all, because the supported runner does not merely under-report on this test bed —
it reports zero. The mitigation is that the failure mode is loud rather than
silent: a runner regression that stopped activating mutants would take every
score to zero and fail the gate, not quietly pass it.

Finally, the threshold is set **below 100 %** because equivalent mutants make
100 % unreachable, and it gates a *score*, not the absence of survivors: the
report, not the exit code, is what a maintainer reads to decide whether a
survivor is a missing assertion or an equivalent mutant.

Each library gets its **own** threshold, derived from its own measured score
rather than from a target chosen in the abstract. A single number across five
libraries would have to be either low enough for the weakest — and therefore
toothless for the ones already at the top — or high enough for the strongest, and
therefore red on day one for the rest. Deriving it per library makes the gate a
ratchet: it forbids regression from where each library already stands, clears on
introduction, and only ever moves up. The cost of that choice is that the bar is
low where the test bed is weak, which is exactly where the gate would be most
useful; raising those thresholds as the weekly sweep shows headroom is the way
the ratchet is meant to be used, and it is deliberate that doing so is a
maintainer's decision rather than an automatic one.

## Alternatives Considered

### Report the mutation score without failing the build

Considered because it carries none of the risk: no pull request is ever blocked
by a preview-status tool, and the number is still published.

Rejected because it changes nothing. The repository's existing quality
invariants are all enforced rather than suggested, precisely because a signal
that costs nothing to ignore is ignored. The decision worth recording here is
that a pull request must answer for the assertions it did not write; an advisory
report does not make that demand.

### Mutate every library in full on every pull request

Considered because it is the honest measurement: a score over the whole library
is comparable between runs, and it cannot be gamed by touching a file's edges.

Rejected on two counts. The first is cost: the per-mutant cost is a full run of
the library's suite, so a complete sweep of the five libraries is a matter of
tens of minutes at best — paid on every push, mostly to re-measure code nobody
changed. The second is decisive on its own: **a whole-library score is far too
insensitive to gate on**. The largest library carries several hundred mutants, so
one newly added, unasserted behaviour moves its score by a fraction of a
percent — well below any threshold that is not itself noise. The diff-scoped
score is sensitive precisely because its denominator is small: a handful of new
mutants, one of which survives, is a visible drop. The weekly sweep recovers the
whole-library number where it belongs — as a trend, not as a gate.

### Raise the SonarQube Cloud coverage requirement instead

Considered because the quality gate already exists, is already mandatory, and
already reports on new code only — the mechanism this decision needs.

Rejected because it measures the wrong thing. Coverage cannot fall below 100 %
for a line that a test executes and asserts nothing about; raising the required
percentage buys more executed lines, not more pinned behaviour. The two signals
are complements, and this one has no substitute.

### Keep Stryker's default VSTest runner

Considered because it is the supported, non-preview configuration, and preferring
a preview component in a required check is not a decision to take lightly.

Rejected because it does not work on this test bed at all. Verified by
measurement: it reports every mutant as survived, including mutants that
demonstrably break the suite when applied by hand. Choosing it would mean either
a permanently red gate or a threshold low enough to be meaningless.

### Extend the gate to the tooling and the analyzers

Considered for uniformity — a single rule covering the whole repository is easier
to defend than a list of five projects.

Rejected because the cost is concentrated exactly where the value is lowest.
Analyzer and generator tests compile code and spawn processes; their suites are
the slow ones, so their mutants are the expensive ones. Their externally visible
behaviour is already pinned by the `analyzers`, `gendoc-docs` and `ci` floor
jobs, and a defect there surfaces as a broken build or a wrong document, not as a
silently wrong answer inside a consumer's application.

## Consequences

### Positive

* A pull request that adds behaviour without adding the assertion that pins it is
  refused automatically, on the code it changed, before review.
* The weakest-tested files improve on contact: touching one puts its whole
  mutation score on the gate.
* The weekly sweep gives the untouched parts of the libraries a trend line that
  nothing else in the pipeline produces.
* The diagnosis is concrete. A failing gate names the surviving mutant, its file
  and its line — it says which assertion is missing, not merely that a number is
  too low.

### Negative

* A pull request touching a large, weakly covered file pays for that file's
  pre-existing gaps, not only for its own change.
* Two more required checks on the critical path of every merge, and one more
  moving part to keep working — the tool pin, the runner mode and the thresholds
  all have to be maintained deliberately.
* The two workflows are near-identical files. Until the split happens, a fix to
  one is a fix to both, and nothing enforces that.
* Equivalent mutants make part of the remaining distance to 100 % unreachable, so
  the threshold is a judgement call rather than a derived value.

### Risks

* **The Microsoft Testing Platform runner is preview.** A regression in it could
  change scores between engine versions. Mitigated by pinning the engine in the
  tool manifest, so an upgrade is a deliberate act, and by the failure mode being
  loud: a runner that stops activating mutants takes every score to zero.
* **Its coverage-based selection is disabled**, so the cost is one full suite run
  per mutant. That is affordable today because these suites are fast; a
  substantially slower suite would make the sweep, and eventually the gate,
  expensive.
* **The thresholds are calibrated against today's scores.** A library added later
  with a weaker test bed would fail the gate on its first contact rather than on
  its introduction.
* **`JustDummies` ships without a score threshold.** Its sweep is too long to
  calibrate against interactively, so that library is gated on everything except
  a score until the first weekly sweep supplies one.
* **Test-only pull requests select mutants too**, through the test files they
  change, so a pull request that only adds tests can still be gated.

## Follow-up Actions

* Mark both aggregated checks — `Mutation gate` and `JustDummies mutation gate` —
  as required on `main` in the branch protection; a workflow cannot make itself
  mandatory.
* When JustDummies moves to its own repository, take its workflow, its two
  configurations and the tool manifest across unchanged and repoint the
  `solution` field; the workflow reference page carries the checklist.
* Revisit the runner choice when Stryker's Microsoft Testing Platform support
  leaves preview, and re-enable coverage-based selection when it classifies
  covered mutants correctly.
* Re-read the thresholds after each engine upgrade, and after any library is
  added to the scope.
* Set the `JustDummies` threshold from the first weekly sweep. Its full sweep is
  too long to run interactively, so no score was measured for it and its bar is
  currently disabled — the one library the gate does not yet hold to a score.

## References

* [`mutation` workflow reference](../workflows/mutation.en.md) — how the decision
  is implemented, and the knobs it exposes.
* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — the precedent for pinning
  a tool version that would otherwise move a measured result on its own.
* [ADR-0040](0040-split-the-justdummies-test-bed-between-example-and-property-suites.md)
  — the test-bed split whose two suites both feed this gate.
* [stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117)
  — the upstream report of Stryker's VSTest runner mishandling xUnit v3.
* [stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629)
  — the upstream limitation of coverage analysis under the Microsoft Testing
  Platform runner.

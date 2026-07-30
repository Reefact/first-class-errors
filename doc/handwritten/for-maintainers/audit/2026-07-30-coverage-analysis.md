# FirstClassErrors — Code Coverage Analysis

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./2026-07-30-coverage-analysis.fr.md)

**Date:** 2026-07-30
**Analysed revision:** `da6e7ee` (tip of `main` at analysis time; `main` has advanced since)
**Source:** SonarCloud project `reefact_first-class-errors`, analysis of 2026-07-30 07:51 UTC
**Scope:** every component SonarCloud reports for the solution — 614 components, 20,668 ncloc, of which
182 files carry a coverage gap.
**Status:** advisory. Per the repository's own convention (ADR-0004), this analysis produces
recommendations, never blockers; the two candidate ADRs it names are drafts for `@reefact` to accept
or reject.

**Method.** Per-file measures were pulled from SonarCloud's `api/measures/component_tree` (both pages,
614 components), and per-line hit counts and branch counts from `api/sources/lines` for all 182 files
carrying a gap. The reconstructed totals match Sonar's published figures **exactly** — 840 uncovered
lines and 1,303 uncovered conditions — so the classification below covers the whole population rather
than a sample. Each uncovered line was attributed to its enclosing member by parsing the local sources;
each missing branch was classified by the shape of its own expression.

**Not verified locally.** `dotnet build` and `dotnet test` were **not** run for this analysis. Every
figure comes from the SonarCloud analysis of 2026-07-30, which is itself produced by
`dotnet test … --settings coverage.runsettings` in [`sonar.yml`](../workflows/sonar.en.md).

> **Terminology.** Throughout this document a *branch* is a conditional path in the code — each way an
> `if`, `&&`, `||`, `??`, `case` or ternary can go. SonarCloud calls these *conditions*. A line
> `if (x is null) { throw new ArgumentNullException(nameof(x)); }` counts as one line and two branches;
> a test that never passes `null` covers the line without covering the branch. Nothing here refers to
> Git branches.

---

## 1. Executive summary

The solution sits at **86.6% overall coverage** — 91.1% line, 80.2% branch — with **2,143 uncovered
units**: 840 lines and 1,303 branches. Those 2,143 units are not 2,143 missing tests. They fall into
five disjoint kinds, and only one of them is answered by writing more tests.

Three findings drive every recommendation below.

1. **The deficit is a branch deficit, and it is not where the percentage suggests.** Line coverage is
   already at 91.1%; branches are 61% of all uncovered units. **JustDummies and the two analyzer
   projects hold 1,084 of the 2,143 units**, of which 859 are branches. Meanwhile the two projects with
   the worst percentages — `FirstClassErrors.Cli` at 53.9% and `FirstClassErrors.GenDoc.Worker` at 0% —
   are largely code that CI already exercises for real, just not under the coverage instrument.
   Prioritising by percentage would start in exactly the wrong place.

2. **`JustDummies.Xunit` is not in the denominator at all.** A shipping NuGet package is classified by
   Sonar as test code and its coverage never counts, in either direction. Any "100% of the solution"
   target silently excludes it today. See [§5](#5-a-measurement-blind-spot-justdummiesxunit).

3. **The cheapest available move is already written in this repository.** JustDummies has a reflective
   `NullArgumentGuardConventionTests` that invokes every member with `null` and asserts the resulting
   `ArgumentNullException`. No other project has an equivalent — which is why the core library's 49
   branch gaps are 30 null guards, 24 of them in `OutcomeTaskExtensions.cs` alone.

The quality gate is **green** and measures *new* code only (88.3% against an 80% bar). Nothing is
failing. Every number in this document is therefore a choice, not a requirement.

## 2. Headline metrics

| Metric | Value |
|---|---|
| Overall coverage | **86.6%** |
| Line coverage | 91.1% — 840 uncovered of 9,449 lines to cover |
| Branch coverage | 80.2% — 1,303 uncovered of 6,572 conditions to cover |
| Total gap units | **2,143** (840 lines + 1,303 branches) |
| Quality gate | Pass — `new_coverage` 88.3% vs an 80% threshold |
| Analysed size | 20,668 ncloc across 614 components; 182 files carry a gap |

Coverage over the reporting period, from SonarCloud's metric history:

| Date | 07-09 | 07-12 | 07-19 | 07-26 | 07-27 | 07-28 | 07-29 | 07-30 |
|---|---|---|---|---|---|---|---|---|
| Coverage | 78.1% | 79.3% | 79.3% | 82.8% | 84.2% | 86.1% | 86.6% | 86.6% |

## 3. Where the gap sits

Per project, ordered by gap size. `LTC` is lines to cover, `CTC` conditions to cover.

| Project | Coverage | LTC | CTC | Unc. lines | Unc. branches | Gap units |
|---|---:|---:|---:|---:|---:|---:|
| `JustDummies` | 90.4% | 3,229 | 2,745 | 167 | 404 | **571** |
| `JustDummies.Analyzers` | 88.0% | 1,451 | 1,671 | 50 | 324 | **374** |
| `FirstClassErrors.Cli` | 53.9% | 502 | 248 | 227 | 119 | **346** |
| `FirstClassErrors.GenDoc` | 84.2% | 1,414 | 542 | 161 | 148 | **309** |
| `FirstClassErrors.Usage` | 67.1% | 351 | 72 | 81 | 58 | 139 |
| `FirstClassErrors.Analyzers` | 89.7% | 858 | 494 | 8 | 131 | 139 |
| `FirstClassErrors.RequestBinder.Usage` | 75.1% | 352 | 98 | 55 | 57 | 112 |
| `FirstClassErrors` | 94.7% | 701 | 482 | 14 | 49 | 63 |
| `FirstClassErrors.RequestBinder` | 93.2% | 483 | 176 | 33 | 12 | 45 |
| `FirstClassErrors.GenDoc.Worker` | 0.0% | 43 | 0 | 43 | 0 | 43 |
| `FirstClassErrors.Testing` | 98.2% | 65 | 44 | 1 | 1 | 2 |

`FirstClassErrors.RequestBinder.Benchmarks` is absent because it is already excluded from coverage by
`sonar.coverage.exclusions` in [`sonar.yml`](../workflows/sonar.en.md) — a measurement harness that is
never shipped and never unit-tested. Its code still gets the SonarAnalyzer pass.

The twelve files carrying the largest gaps:

| # | File | Unc. lines | Unc. branches | Gap | Coverage |
|---:|---|---:|---:|---:|---:|
| 1 | `FirstClassErrors.GenDoc/SolutionErrorDocumentationGenerator.cs` | 143 | 83 | 226 | 50.0% |
| 2 | `JustDummies/Any.Combine.cs` | 0 | 75 | 75 | 76.6% |
| 3 | `JustDummies/WideIntervalSpec.cs` | 16 | 46 | 62 | 80.9% |
| 4 | `JustDummies/DecimalIntervalSpec.cs` | 19 | 42 | 61 | 83.1% |
| 5 | `JustDummies/RegexParser.cs` | 17 | 41 | 58 | 90.8% |
| 6 | `FirstClassErrors.Usage/Model/Temperature.cs` | 35 | 22 | 57 | 0.0% |
| 7 | `JustDummies.Analyzers/ScalarConstraintState.cs` | 1 | 56 | 57 | 74.3% |
| 8 | `FirstClassErrors.Cli/RendererLoader.cs` | 27 | 21 | 48 | 7.7% |
| 9 | `FirstClassErrors.GenDoc.Worker/Program.cs` | 43 | 0 | 43 | 0.0% |
| 10 | `FirstClassErrors.Usage/Utils/DocumentationFormatter.cs` | 21 | 19 | 40 | 46.7% |
| 11 | `JustDummies.Analyzers/RejectedConstantArgumentAnalyzer.cs` | 14 | 26 | 40 | 86.5% |
| 12 | `FirstClassErrors.Cli/CatalogSnapshotSource.cs` | 26 | 12 | 38 | 0.0% |

## 4. The five kinds of gap

Every one of the 2,143 units was classified from its own source line. The buckets are disjoint and sum
to the total.

| Kind | Unc. lines | Unc. branches | Units | Share |
|---|---:|---:|---:|---:|
| **V1** — exercised by CI, invisible to the instrument | 186 | 83 | 269 | 12.6% |
| **V2** — sample and demo code | 136 | 115 | 251 | 11.7% |
| **V3** — a test closes it today | 340 | 993 | **1,333** | **62.2%** |
| **V4** — needs a seam before a test can reach it | 176 | 88 | 264 | 12.3% |
| **V5** — practically unreachable | 2 | 24 | 26 | 1.2% |
| **Total** | 840 | 1,303 | 2,143 | 100% |

### V1 — exercised by CI, invisible to the instrument (269 units)

`SolutionErrorDocumentationGenerator`'s MSBuild shell-out (226 units) and `GenDoc.Worker`'s entry point
(43 units). The uncovered regions are precisely the process-spawning paths: `DotNetBuild`,
`DotNetGetProperty`, `RunProcess`'s timeout and kill branches, and `RunWorker`'s subprocess invocation.

These are not untested. [`canary.yml`](../../../../.github/workflows/canary.yml) runs the real `fce.dll
generate` against a real project, captures the worker's diagnostics, and *asserts* two things: that the
emitted catalog contains error codes, and that the worker's own banner reports the newest runtime
(`Documenting … on .NET <n>.`). [`gendoc-docs.yml`](../../../../.github/workflows/gendoc-docs.yml) runs
the same binary to regenerate the committed catalog. That is a *better* test of the MSBuild and
roll-forward behaviour than any mock could be. These paths are uncovered because `dotnet test` never
spawns them — a property of the instrument, not of the test bed.

One caveat: the canary only runs when a .NET preview is available and skips otherwise, so it is not a
per-commit guarantee. `gendoc-docs` has no such condition.

### V2 — sample and demo code (251 units)

`FirstClassErrors.Usage` and `FirstClassErrors.RequestBinder.Usage`. The intent is already on record in
the code: `Usage/Model/Amount.cs` carries a `SuppressMessage` justifying that the comparison operators
are out of scope because they "would add untested surface to a type the tests only exercise
indirectly". The coverage scope has simply never been made to match that stated intent.

### V3 — a test closes it today (1,333 units)

No refactor, no seam, no policy decision — only tests that do not exist yet. 993 of these are branches.
This is the only bucket where writing tests is the answer, and it is broken open in [§6](#6-what-the-1333-actionable-units-are-made-of).

### V4 — needs a seam before a test can reach it (264 units)

The CLI's `renderer` and `config` command branch writes straight to `Console.Out` and calls
`Assembly.LoadFrom`, while the `generate` and `catalog` commands go through `IOutputSink`,
`IErrorDocumentationGenerator` and `ICatalogSnapshotSource` and sit between 80% and 97%.

The untested commands are exactly the ones that never adopted the seam the project already has. The
list is `RendererLoader`, `RendererListCommand`, `RendererAddCommand`, `RendererRemoveCommand`,
`ConfigShowCommand`, `ConsoleGenerationLogger`, `CatalogSnapshotSource`, `CatalogSourceResolver`,
`RendererCatalog`, `SolutionErrorDocumentationGeneratorAdapter` — the last of which is the production
side of the very seam the tests drive with doubles — plus `Cli/Program.cs`, which is Spectre wiring.

### V5 — practically unreachable (26 units)

Defensive guards against states the compiler cannot produce: Roslyn `is not <shape>` checks on operation
and symbol types, and `default:` arms of exhaustive switches. **This count is a floor, not a total** —
it is only what could be proved from syntax alone; the true number of unreachable defensive branches is
higher. Chasing these costs correctness, because the only way to "cover" such a guard is to delete a
guard that is there on purpose.

## 5. A measurement blind spot: `JustDummies.Xunit`

`JustDummies.Xunit/ReproducibleAttribute.cs` — 60 code lines of a **shipping NuGet package** — is filed
by SonarCloud under qualifier `UTS` (unit-test source), not main code. The SonarScanner for .NET
classifies a project as a test project when it references a test framework, and this package references
`xunit.v3.extensibility.core` because that is exactly what it is: the xUnit adapter.

The consequence is *not* that it is untested — `JustDummies.Xunit.UnitTests` exists and exercises it,
including through an `InternalsVisibleTo` seam deliberately added so the "report only on failure" rule
can be proved without a test that has to fail for real. The consequence is that its coverage **never
counts**, in either direction: a regression that dropped it to zero would move the reported number by
0.0 points, and the work already done to test it earns nothing.

Any "100% of the solution" target has this package silently outside it. Correcting it means forcing the
classification — `sonar.test.exclusions`, or an explicit `SonarQubeTestProject=false` on that one
project.

## 6. What the 1,333 actionable units are made of

This is bucket **V3** broken open — the units a test can close today.

| Pattern | Unc. lines | Unc. branches | Units | Shape of the fix |
|---|---:|---:|---:|---|
| Analyzer guard chains and case dispatch | 56 | 431 | **487** | Negative-case snippets through the existing `AnalyzerTestHarness` |
| JustDummies spec engines (interval, string, regex) | 73 | 205 | **278** | Boundary and exhaustion cases; `DescribeExhaustion` and `Cardinality` are never reached |
| Rest of the `Any<T>` surface | 34 | 68 | 102 | Constraint builders dead on specific scalar types — `MultipleOf` on `AnySByte`, `LessThan` on `AnyUInt16`, … |
| GenDoc renderers and versioning | 18 | 65 | 83 | Renderer edge cases and catalog-diff labels; the seam already exists and is tested |
| CLI, the part that is already seamed | 51 | 31 | 82 | More cases through the doubles `GenerateCommand` and the catalog commands already use |
| `Any<T>` introspection interfaces never called | 55 | 9 | 64 | One reflective theory over every `Any<T>` — 26 files closed at once |
| Range and domain guards never violated | 4 | 57 | 61 | A convention test that passes each guard its illegal value |
| Null-argument guards never given a `null` | 4 | 52 | 56 | Port JustDummies' `NullArgumentGuardConventionTests` to the other projects |
| `Any.Combine` `??` chains (operand-position matrix) | 1 | 51 | 52 | A theory varying which operand carries the `RandomSource` |
| `FirstClassErrors` core library | 14 | 19 | 33 | Loader-failure and null-name paths in `AssemblyErrorDocumentationReader` |
| `FirstClassErrors.RequestBinder` | 29 | 4 | 33 | `BindingScope.Get` and the simple-property converter path |
| Other (`FirstClassErrors.Testing`) | 1 | 1 | 2 | — |
| **Total** | **340** | **993** | **1,333** | |

### The four replicated patterns

Several entries above are one pattern repeated across many files, which is what makes them worth
attacking: a single harness closes dozens of units at once.

**The `Any<T>` introspection matrix — 64 units across 26 files.** `IHasRandomSource.Source`,
`ICardinalityHint<T>.DistinctCardinality` and `ICardinalityHint<T>.Contains` are explicit interface
implementations, and for most scalar types nothing in the suite ever routes through them. One
reflection-driven theory over every `Any<T>` closes all 26 files at once — and the repository already
has that harness shape in `SurfaceParityTests`, `FactoryNamingConventionTests` and
`NullArgumentGuardConventionTests`.

**Guards that exist but are never violated — 117 units.** Split by exception type and project:

| Project | `ArgumentNullException` | `ArgumentOutOfRangeException` | `ArgumentException` | Total |
|---|---:|---:|---:|---:|
| `JustDummies` | 14 | 10 | 47 | 71 |
| `FirstClassErrors` | 30 | 0 | 0 | 30 |
| `FirstClassErrors.RequestBinder` | 8 | 0 | 0 | 8 |
| `FirstClassErrors.Usage` | 0 | 0 | 2 | 2 |
| **Total** | **52** | **10** | **49** | **111** |

JustDummies' `NullArgumentGuardConventionTests` reflectively invokes every member with a `null` and
asserts the `ArgumentNullException` — which is why its null column is the smallest despite being the
largest project. **`FirstClassErrors` has no equivalent**, and its 30 uncovered null guards are the
direct result; 24 of them sit in `OutcomeTaskExtensions.cs`, one per `is null` guard on `next`,
`fallback`, `onSuccess` and `onFailure`. Porting that one convention test is the single cheapest move
in this document. The same gap exists for **range and domain guards**, which no convention test covers
in any project.

**`Any.Combine`'s operand-position matrix — 52 units in one file.** Each arity overload chains
`SourceOf(first) ?? SourceOf(second) ?? …`, so an *N*-operand overload emits 2*N* branches, and the
tests only ever put the source in the first position. `Any.Combine.cs` has 0 uncovered *lines* and 75
uncovered *branches* — every line runs, half the paths never do. A theory that varies which operand
carries the source walks the whole chain.

**Analyzer guard chains — 487 units, the largest single bucket.** By expression shape, across both
analyzer projects (455 branch units classified):

| Shape | Units | Share |
|---|---:|---:|
| null / null-conditional guard on a Roslyn symbol | 150 | 33.0% |
| simple `if` covered on one side only | 109 | 24.0% |
| other | 71 | 15.6% |
| `switch` / `case` dispatch | 48 | 10.5% |
| `&&` / `\|\|` short-circuit | 35 | 7.7% |
| `is not <Roslyn shape>` guard (bucket V5) | 24 | 5.3% |
| loop with no zero-iteration path | 12 | 2.6% |
| `??` coalesce | 6 | 1.3% |

Most of these are genuine analyzer paths — malformed or unusual syntax the analyzer must survive —
reachable through the existing `AnalyzerTestHarness` with negative-case source snippets. Note the
contrast with mutation testing, which this repository already gates on (ADR-0043, ADR-0046): many of
these branches are *executed* but never *asserted*, so they are likely surviving mutants too.

## 7. What each decision buys

Two different levers move the number and should not be confused. **Exclusions** change the denominator
and cost nothing but a documented decision. **Tests** change the numerator and cost work. Figures are
cumulative, computed with Sonar's own formula `((LTC − unL) + (CTC − unC)) / (LTC + CTC)` against the
per-file measures.

| Step | Lever | Units | Coverage |
|---|---|---:|---:|
| Today | — | — | 86.62% |
| Exclude the two `Usage` sample projects | denominator | −251 | 87.51% |
| … and `GenDoc.Worker`, the worker entry point | denominator | −43 | 87.76% |
| … and `Cli/Program.cs`, the Spectre wiring | denominator | −17 | 87.86% |
| … and `SolutionErrorDocumentationGenerator.cs`, the MSBuild shell-out | denominator | −226 | 89.03% |
| … then cover the ten un-seamed CLI files | numerator (after a seam) | −247 | **90.71%** |

After all of it, **1,359 units remain and 90.7% is the ceiling of the cheap moves** — 1,084 of them in
JustDummies and the analyzers, overwhelmingly branches. There is no shortcut past that bucket: it is
the actual work, and it is also the code where correctness matters most.

## 8. Recommendation

1. **Fix the blind spot first — it is a measurement bug, not a coverage gap.** Force
   `JustDummies.Xunit` to be analysed as main code. Until then no coverage target actually covers the
   solution, and the number cannot be trusted to move when that package regresses.

2. **Decide the scope explicitly, once, in an ADR.** The samples, the process entry points and the
   MSBuild shell-out are 520 units — a quarter of the total — that no unit test should ever be written
   for. `Benchmarks` is already excluded for exactly this reason and the reasoning is already written
   down in `sonar.yml`; this extends the same rule to the same kind of code. That is a lasting decision
   a future maintainer would question, so it wants an ADR rather than a comment.

3. **Make the CI exercise count, instead of writing unit tests to imitate it.** `canary` and
   `gendoc-docs` already run the real `fce generate`, spawn the real worker and assert on the result.
   Either collect coverage from those runs, or exclude the path and say why — but do not write a fake
   `IProcessRunner` to make a number move. If the path is excluded, note that the canary is
   preview-conditional, so `gendoc-docs` is the exercise that actually runs on every relevant push.

4. **Port the null-guard convention test out of JustDummies.** One harness, already written and proven
   in this repository, applied to `FirstClassErrors`, `RequestBinder` and `GenDoc`. Closes 56 units,
   removes a whole category permanently, and every future guard is covered on the day it is written.
   Then extend the same harness to range guards (+61).

5. **Then the two reflection matrices in JustDummies.** The `Any<T>` introspection interfaces (64) and
   `Any.Combine`'s operand positions (52). Both are single theories over an existing type list. Per
   [ADR-0040](../adr/0040-split-the-justdummies-test-bed-between-example-and-property-suites.md) these
   are invariants that hold for every legal argument, so they belong in `JustDummies.PropertyTests`,
   not the unit suite — see [Writing JustDummies tests](../WritingJustDummiesTests.en.md).

6. **Only then the analyzers — and drive them from mutation, not coverage.** 487 units, mostly branches
   that are already *executed* but not *asserted*. Coverage will report them closed as soon as a snippet
   reaches them; only the mutation sweep will tell you whether the test actually pinned the behaviour.
   The repository already runs that sweep — let it pick the targets here rather than the coverage
   percentage.

### Candidate ADRs

Two decisions above are lasting ones a future maintainer would question, and are offered as drafts:

- **The coverage scope policy** — which categories of code are deliberately outside the coverage
  denominator (samples, process entry points, process shell-outs) and why. Recommendation 2.
- **Where the process-level paths are verified** — that the `canary` and `gendoc-docs` exercises are the
  accepted verification for the MSBuild and worker paths, rather than mocked unit tests.
  Recommendation 3.

Neither is drafted here. Per the repository convention, an agent proposes and never accepts.

## 9. What this analysis does not claim

**That 100% is the right target.** The quality gate is on new code and it is green at 88.3%. Nothing
here is failing. The reachable ceiling after every reasonable move is roughly 96–97%, because bucket V5
is real and its 26 units are only the ones provable from syntax.

**That coverage measures test quality.** This repository already knows that: it gates on mutation score
precisely because a test can execute a line without asserting anything about it (ADR-0043, ADR-0046).
Several buckets above would go green under coverage while staying red under Stryker. Where the two
disagree, the mutation sweep is the one telling the truth.

**That the figures are current.** They describe `da6e7ee`. `main` has advanced since, including
refactors inside `JustDummies`, so per-file numbers will have shifted. The structure of the analysis —
the five kinds, the replicated patterns, the two levers — is what is meant to outlive the snapshot.

## 10. Reproducing these figures

All data is public; the SonarCloud project is readable without a token.

```sh
# Headline metrics
curl -s "https://sonarcloud.io/api/measures/component?component=reefact_first-class-errors\
&metricKeys=coverage,line_coverage,branch_coverage,uncovered_lines,uncovered_conditions,\
lines_to_cover,conditions_to_cover,ncloc"

# Per-file measures (paginate: ps=500, p=1 then p=2)
curl -s "https://sonarcloud.io/api/measures/component_tree?component=reefact_first-class-errors\
&metricKeys=coverage,uncovered_lines,uncovered_conditions,lines_to_cover,conditions_to_cover\
&strategy=leaves&ps=500&p=1&s=metric&metricSort=uncovered_lines&asc=false"

# Per-line hits and branch counts for one file
curl -s "https://sonarcloud.io/api/sources/lines?key=reefact_first-class-errors%3A<url-encoded-path>"
```

A line is uncovered when `lineHits == 0`; a line has missing branches when `coveredConditions <
conditions`. Summing `uncovered_lines` and `(conditions − coveredConditions)` over all files must
reproduce the project totals — that reconciliation is what makes the classification exhaustive rather
than indicative.

## Related

- [`sonar` workflow](../workflows/sonar.en.md) — how the analysis and its coverage report are produced.
- [`sonar-gate` workflow](../workflows/sonar-gate.en.md) — how the quality gate is read back.
- [`ci` workflow](../workflows/ci.en.md) — produces the same OpenCover shape via `coverage.runsettings`.
- [`mutation`](../workflows/mutation.en.md) and
  [`justdummies-mutation`](../workflows/justdummies-mutation.en.md) — the checks that measure whether a
  covered line is actually asserted.
- [Writing JustDummies tests](../WritingJustDummiesTests.en.md) — which suite a new JustDummies test
  belongs to.

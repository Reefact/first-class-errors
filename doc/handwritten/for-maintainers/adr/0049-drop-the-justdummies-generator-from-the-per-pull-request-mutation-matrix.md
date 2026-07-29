# ADR-0049 | Drop the JustDummies generator from the per-pull-request mutation matrix

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-28
**Accepted:** 2026-07-28
**Decision Makers:** Reefact

## Context

ADR-0043 gates every pull request on the mutation score of what it changed. `justdummies-mutation.yml`
runs that gate as a three-leg matrix — the generator (`justdummies`), its xUnit v3 adapter
(`justdummies-xunit`) and its analyzers (`justdummies-analyzers`) — each scoped to the diff with
`--since`.

Two of those three legs finish in about ninety seconds. The generator's does not finish at all.

On pull request #337 — a four-commit bug-fix diff of **99 changed production lines** — the
`Mutate the diff (justdummies)` leg selected **844 mutants** and was still running after **sixty
minutes**, producing no score, before it was cancelled by hand. It was not an outlier: the leg's cost is
set by the size of the *files* the diff touches, not by the size of the diff.

Three constraints, each recorded and measured, together make that cost structural rather than incidental:

* **`--since` is file-scoped, not line-scoped.** Stryker mutates every mutant in a changed file. Those
  99 lines pulled in whole files — `StringSpec.cs` (246 mutants), `Any.Combine.cs` (205),
  `ContinuousIntervalSpec.cs` (204), `CollectionState.cs` (109) — so roughly nine tenths of the work
  landed on code the pull request never touched.
* **`"coverage-analysis": "off"` is mandatory, not tuning.** Under the MTP runner Stryker's test
  selection misclassifies killed mutants as uncovered, so every mutant re-runs the whole oracle. That is
  an accuracy decision (ADR-0043, and `mutation.en.md`, "Two settings that are not tuning knobs"), not a
  lever available here.
* **JustDummies is the largest library in the repository** — a few thousand mutants — which is why its
  *full* sweep already carries `timeout-minutes: 350`.

The observed rate on `ubuntu-latest` is about **fourteen mutants per minute**. A two-to-three minute leg
therefore admits at most **forty-five mutants**.

Every lever Stryker 4.16 exposes was measured against the #337 diff:

| Lever | Mutants | Reduction |
|---|---|---|
| Baseline (`--since`) | 844 | — |
| `mutation-level: Basic` | 648 | −23 % |
| `ignore-mutations: [string]` | 766 | −9 % |
| `ignore-mutations: [string, block, statement]` | 542 | −36 % |

Stryker.NET 4.16 offers **no mutant cap and no sampling**: the only filters are which mutators run, which
mutator categories run, and which *files* are mutated. Line-scoped `mutate` patterns — the one lever that
would match the diff to the work — are **silently inert**: `**/RegexNode.cs` selects that file's 34
mutants, while `**/RegexNode.cs{153..165}`, `**/RegexNode.cs{153-165}` and the project-relative forms
each select **zero**, in the configuration file as well as on the command line. A gate configured that
way would go green having tested nothing.

Sharding the leg over the changed files was considered and measured too. Multiple `--mutate` patterns do
compose as a union (34 + 13 = 47 mutants, verified), so sharding is implementable — but a shard cannot be
smaller than one file, and eight of the library's central files exceed the forty-five-mutant budget on
their own: `RegexParser.cs` (507), `DecimalIntervalSpec.cs` (388), `OrdinalIntervalSpec.cs` (387),
`WideIntervalSpec.cs` (382), `StringSpec.cs` (357), `UriSpec.cs` (304), `ContinuousIntervalSpec.cs` (284),
`CollectionState.cs` (188).

The best achievable combination is roughly −50 %, against a requirement of −95 %.

## Decision

The `justdummies` leg is **removed from the per-pull-request matrix** in `justdummies-mutation.yml`.
`justdummies-xunit` and `justdummies-analyzers` keep theirs. The generator's mutation score continues to
be measured by the **weekly full sweep**, unchanged.

The `gate` job and its check name are unchanged, so no branch-protection entry moves.

## Rationale

* **The leg produces nothing today.** It is not slow, it is unfinished: sixty minutes of runner time for
  no score. Removing a check that never reports loses no signal — it stops paying for the absence of one.
* **The measurements close the alternatives.** Every in-tool lever tops out at −36 %, sharding is floored
  by the largest changed file, and line-scoping does not exist in this Stryker version. This is not
  "we did not tune it hard enough".
* **ADR-0046 already removed its authority.** The per-PR gate is advisory; the enforced bar is the weekly
  sweep. The leg was reporting into a channel that cannot fail a pull request.
* **The narrow removal keeps what works.** The adapter and analyzer legs are small, finish in ninety
  seconds, and keep per-PR mutation feedback where it is affordable. Dropping all three would discard a
  working signal to fix an unrelated one.

## Alternatives Considered

### Shard the leg over the changed files

Considered because it needs no ADR — it is an implementation detail of ADR-0043's "gate the diff", and
multiple `--mutate` patterns were verified to compose. Rejected because a shard's floor is one whole
file: on the #337 diff the `StringSpec.cs` shard alone is 246 mutants, about eighteen minutes, and eight
central files are individually over budget. It would turn "never finishes" into "fifteen to twenty
minutes whenever the pull request touches anything interesting" — real machinery, for a target it still
misses.

### Cap the work with `mutation-level` and `ignore-mutations`

Rejected on the numbers above: −36 % at best, an order of magnitude short. It also costs signal in the
wrong place — dropping `statement` and `block` mutations stops testing the removal of argument guards,
which is among the defects mutation testing catches best (ADR-0045 is the decision those guards
implement).

### Sample a bounded subset of mutants per pull request

Considered because a partial signal on an advisory leg is defensible. Rejected because Stryker exposes no
sampling: mutants are generated deterministically and exhaustively from the syntax tree, so the only way
to bound the count is to bound the *files*. Selecting changed files up to a mutant budget biases the
sample systematically toward the small ones, so `RegexParser`, `StringSpec`, `UriSpec` and the interval
engines — where mutation testing is worth most — would never be covered per pull request. Rotating the
selection instead surfaces survivors in files the pull request did not touch, which does not help the
reviewer decide anything.

### Run a nightly diff-scoped leg instead

Rejected because it does not exist as described: diff-scoping needs a pull request's fork point, and
after a merge there is no diff to scope to. A nightly can only be the *full* sweep — the repository's
longest job — seven times a week instead of once, which costs more, not less.

### Raise `timeout-minutes` above sixty

Rejected: the leg would report an hour or more after the pull request is ready, on a check that cannot
block it. That is the cost without the benefit.

## Consequences

### Positive

* An hour of runner time per pull request touching the generator, spent on no result, is recovered.
* The pull request check list settles in about ninety seconds instead of hanging on a leg that never
  reports.
* Per-PR mutation feedback survives where it works — the adapter and the analyzers.

### Negative

* A mutation regression in the generator is now seen on Monday rather than on the pull request that
  introduced it. With `break: 0` on this library, nothing was being enforced on the pull request either
  way; what is lost is the survivor list in the run summary, not a gate.
* `justdummies-mutation.yml` and `mutation.yml` no longer run an identical matrix. That parity is stated
  in `justdummies-mutation.en.md`, which this decision requires updating.

### Risks

* **The threshold this defers.** `justdummies.json` carries `break: 0` because no score has been
  measured; the first full sweep is meant to publish the figure that sets it. Once it does, the per-PR
  leg becomes worth having again — and this decision will need revisiting rather than assuming it is
  settled. Mitigation: the follow-up below.

## Follow-up Actions

* Update `justdummies-mutation.en.md` and its French translation: the matrix is two legs on pull
  requests, three on the full sweep, and why.
* Re-open this decision if Stryker gains working line-scoped `mutate` patterns, or if the MTP coverage
  selection (stryker-net#3629) is fixed so `"coverage-analysis"` can be turned on — either one changes the
  cost model that decides this.
* Revisit when the first weekly sweep publishes the JustDummies figure and `break` stops being 0.

## References

* ADR-0043 — Gate pull requests on the mutation score of the diff: the decision this narrows.
* ADR-0046 — Make the per-pull-request mutation gate advisory: why the leg has no authority to lose.
* ADR-0047 — Measure JustDummies mutation against the unit suite only: the previous attempt to make this
  leg affordable.
* ADR-0045 — Guard public and internal arguments against null: the guards `ignore-mutations` would stop
  testing.
* `.github/workflows/justdummies-mutation.yml`, `build/stryker/justdummies.json`.
* Pull request #337 — the run whose cancellation after sixty minutes prompted this.
* [stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629) — the MTP coverage
  selection defect behind `"coverage-analysis": "off"`.

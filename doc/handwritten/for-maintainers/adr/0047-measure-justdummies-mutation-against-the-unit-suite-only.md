# ADR-0047 | Measure JustDummies mutation against the deterministic unit suite only

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0047-measure-justdummies-mutation-against-the-unit-suite-only.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-27
**Accepted:** 2026-07-31
**Decision Makers:** Reefact

## Context

ADR-0043 configured the JustDummies mutation run to use **both** test suites as the oracle that must
kill each mutant: `JustDummies.UnitTests` **and** `JustDummies.PropertyTests`
(`build/stryker/justdummies.json`, `test-projects`).

The property suite is FsCheck-based. Each property draws ~100 random cases per run, from a random seed.
That makes it two things at once:

* **The expensive half of the per-mutant cost.** Every mutant re-runs the whole oracle
  (`"coverage-analysis": "off"`, ADR-0043), and a hundred cases per property dominates that time —
  which, on a large changed file like `Any.cs`, is the difference between minutes and tens of minutes.
* **A non-deterministic oracle.** A mutation verdict answers "does *any* test in the oracle fail on this
  mutant?" With a randomized oracle, that answer depends on the FsCheck seed: a mutant can be **killed on
  one run's draws and survive on another's**. The mutation score then reflects the seed as much as the
  code and the assertions — the opposite of the reproducible, true figure `"coverage-analysis": "off"`
  exists to protect (ADR-0043).

The non-determinism is not hypothetical. On 2026-07-27 (issue #335) the property suite itself flaked in
CI: a latent `IgnoreCase` regex bug surfaced only after ~89 FsCheck cases and failed the `Build & test`
leg of an **unrelated** pull request. The same randomness that flakes a real test flakes a mutation
verdict built on it.

## Decision

The JustDummies mutation oracle is the **deterministic unit suite only**: `test-projects` in
`build/stryker/justdummies.json` lists `JustDummies.UnitTests` alone. The FsCheck property suite is
removed from the oracle. It still runs in `Build & test` as a real assurance — it simply no longer judges
mutants.

## Rationale

* **A reproducible score.** Mutation now measures whether the **example** (unit) tests pin behaviour — a
  property of the code and those tests only. The same commit yields the same score, run after run, which
  is the whole point of measuring it.
* **Faster, on the paths that hurt.** The property suite's hundred-cases-per-property is the per-mutant
  bottleneck; removing it shortens every mutation run — the per-PR diff leg and the weekly sweep alike.
* **Property tests still protect the library.** They run in `Build & test` and catch regressions; they
  are only removed from the mutation *judge*. Mutation testing asks "do your assertions pin this
  behaviour?", and an example test is the natural, deterministic oracle for that question. A property
  that re-randomises every run is not — it answers a different question (does the invariant hold across
  many inputs?), which the suite still asks where it belongs (ADR-0040).

## Alternatives Considered

### Keep the property suite in the oracle

Rejected: it makes the mutation score non-reproducible (seed-dependent) and is the slowest half of the
run. Both are the exact costs this decision removes.

### Seed the property suite to a fixed seed for the mutation run

Considered because it would make the oracle deterministic without dropping it. Rejected: it is still the
slow half (a hundred cases per property), and it pins the score to one arbitrary seed rather than
removing the dependence on a seed at all — a hidden lever that moves every score when touched. Whether to
seed the property suite in **`Build & test`** to end the intermittent-red landmine (#335) is a separate
question, decided on its own terms.

## Consequences

### Positive

* The JustDummies mutation score is reproducible: it depends on the code and the unit tests, not on a
  random seed.
* Every JustDummies mutation run is faster — the per-PR diff leg and the weekly full sweep.

### Negative

* A behaviour pinned **only** by a property test, with no unit test asserting it, now shows as a mutation
  **survivor**. That is a true signal, not a false one: it says "no example test pins this." Where the
  coverage genuinely matters, the fix is to add a unit test — ADR-0040 already governs which suite owns
  which case.
* The score baseline shifts. With `break: 0` this fails nothing; the next weekly sweep publishes the new
  figure.

## References

* ADR-0043 — Gate pull requests on the mutation score of the diff: the run this narrows the oracle of.
* ADR-0040 — Split the JustDummies test bed between an example suite and a property suite: why the two
  suites answer different questions, which is why one is the mutation oracle and the other is not.
* ADR-0046 — Make the per-pull-request mutation gate advisory: the sibling speed/blocking decision.
* Issue #335 — the `IgnoreCase` property flake that made the non-determinism concrete.

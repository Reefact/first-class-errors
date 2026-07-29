# ADR-0060 | Let stated intent outrank generic analyzer advice

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0060-let-stated-intent-outrank-generic-analyzer-advice.fr.md)

**Status:** Proposed
**Proposed:** 2026-07-29
**Decision Makers:** Reefact

## Context

The SonarQube Cloud report for this project carries 255 open findings. Four of
its rules flag code that is not defective but deliberate, and together they
account for 65 of those findings. They fall into two families, distinguished by
where the finding is produced.

Two arrive under the `external_roslyn` namespace, meaning they are not
SonarQube's own analysis: they are diagnostics the .NET compiler and the BCL
analyzers emit during the build, which the scanner observes through MSBuild and
republishes. A rule configured to `none` is never emitted, so the report loses
it at the source. The other two are SonarQube's own shell analysis, which no
build setting can reach — nothing the compiler does produces or suppresses them.

The four rules, and what the flagged code does today:

* **`CA1859` — 22 findings.** Asks that non-public members typed
  `IReadOnlyList<T>` or `IEnumerable<T>` be retyped to the concrete collection
  they are observed to return, so callers make a direct call instead of an
  interface dispatch. The rule fires only on non-public members. In the flagged
  code the interface expresses a contract — a helper building an error message
  returns `IReadOnlyList<string>` so its callers cannot mutate the result, and
  the test helpers take `IAny<T>` precisely because the public abstraction is
  what is under test.
* **`CA1861` — 22 findings, every one of them in a test project.** Asks that a
  constant array passed as an argument be hoisted into a static readonly field,
  so it is allocated once rather than per call. The flagged arguments are the
  expected values of assertions and the case lists of property generators,
  written inline next to the check that reads them.
* **`S7682` — 12 findings**, in the repository's shell tooling and Claude hooks.
  Asks for an explicit `return` at the end of a shell function. Every function
  it flags ends with the command whose exit status is the function's intended
  result — a `cat` heredoc, an `awk` invocation, a `printf` — and one of them
  ends with `exit`, after which a `return` is unreachable.
* **`S7679` — 9 findings**, in the same scripts. Asks that a positional
  parameter be assigned to a local variable. Every script in the repository
  declares `#!/bin/sh`, and `local` is not part of POSIX; `tools/trains.sh`
  already shows what obeying costs without it, since the one helper there
  needing named parameters carries `_tf_`-prefixed globals instead. The
  remaining flagged functions are one- and two-line helpers whose `$1` sits a
  line below the function's own name.

The code these rules flag is on error-construction paths, documentation
tooling, test suites and repository scripts. None of it is a measured hot path,
and no performance requirement is recorded against any of it.

The repository already holds the two precedents this decision sits between.
ADR-0055 established that a style rule the compiler can express is restated in
`.editorconfig` and enforced at build time, with the DotSettings authoritative
for everything Roslyn cannot express. ADR-0058 declined `CA1510` and chose a
per-project suppression over a repository-wide one, on the express ground that
projects able to honour a rule should keep it. Separately, the repository's
coding rules already resolve one performance-versus-invariant trade in favour
of the invariant: value objects and results stay validating classes rather than
becoming structs, because correctness outranks allocation on error paths.

## Decision

Generic analyzer advice is declined — in writing, next to the reason, and at
the narrowest scope that covers the finding — wherever the code it flags is a
deliberate expression of intent, with readability and stated contracts
outranking micro-performance unless a measured need is recorded.

## Rationale

* **The rules are generic; the code is specific.** Each of the three is sound
  where it was written for, and wrong here for a reason the analyzer cannot
  see. `CA1859` cannot tell an incidental abstraction from a contract, so it
  reads `IReadOnlyList<string>` as an oversight when it is the whole point:
  honouring it would hand `.Add()` to every caller in exchange for nanoseconds
  on a path that runs once per validation conflict. `CA1861` cannot tell a hot
  loop from an assertion, so it would move the expected values of a test away
  from the check that reads them to save an allocation occurring a few hundred
  times in a suite. Suppressing them is not evading the advice; it is answering
  it.
* **Declining in the configuration beats declining in the report.** Wherever a
  finding originates in the build, a severity of `none` stops it being produced
  at all; where it does not — the two shell rules — the scanner's own
  configuration carries the refusal. Either way the decision lands in a file
  that lives in the repository and carries its reason inline, where marking the
  findings "won't fix" on the SonarQube server would put the reasoning
  somewhere the code never shows it.
* **Where the refusal is written follows where the finding is produced.** The
  Roslyn rules are declined in `.editorconfig`, which the compiler reads, so
  the build stops emitting them and every contributor meets the reason at the
  same place the rule would have fired. The shell rules cannot be reached that
  way and are declined in the scanner invocation instead. Splitting them is not
  an inconsistency but the only arrangement in which each refusal sits where
  its rule lives.
* **Scope follows the reason, not convenience.** `CA1861`'s justification is
  about tests, and every one of its findings is in a test project, so it is
  declined for test projects and left live for shipping code, where a hot path
  can genuinely want it. `CA1859` and the two shell rules are declined across
  the code they reach, because their justifications hold everywhere they fire. This keeps ADR-0058's
  principle — a project that can honour a rule keeps it — while recognising
  that here the reason to decline is uniform rather than a platform accident.
* **The performance limb is a trade this repository has already made.** Both
  performance rules ask for the same currency: legibility spent on speed that
  nothing has asked for. The value-objects-as-classes rule settled the same
  trade the same way. Deciding it once, generally, stops it being re-argued at
  each finding.
* **Declining is for advice the code contradicts, not for advice that costs.**
  The same report carried a fifth candidate, `IDE0028`, with 147 findings —
  by far the largest group and the cheapest to make disappear. It is being
  applied instead, because its findings marked a genuine drift (the codebase
  spelled collection initializers both ways, 85 sites against 147) rather than
  a deliberate choice. Volume is not an argument for declining a rule, and this
  ADR does not want to be read as one.

## Alternatives Considered

### Apply all four rules

Clears 65 findings by complying, and leaves no suppression to explain.

Rejected because it inverts the point of the exercise. Every one of the four
would degrade the code it touches: widening a read-only contract into a mutable
one, separating test data from the assertion that reads it, adding a `return`
that either masks a failure or restates the default, and introducing a
non-POSIX `local` into scripts that declare `#!/bin/sh`.

### Suppress per site with `[SuppressMessage]` and a justification

The finest possible scope, and each suppression carries its reason at the exact
line that raised it.

Rejected on volume, on message, and on reach. Sixty-five attributes would add
more lines than the fixes they replace, repeating one argument once per site
states it many times without ever stating it once, and the shell rules have no
such mechanism available at all. The reason here is a policy, not a local
exception, and a policy belongs in one place.

### Mark the findings "won't fix" in SonarQube Cloud

Costs nothing in the repository and clears the report immediately.

Rejected because it puts the decision outside the code. The build would keep
emitting the diagnostics, every new occurrence would have to be dismissed by
hand, and a contributor reading the source would find no trace of the reasoning
— exactly the failure ADR-0056 recorded when a rule lived only where the code's
readers could not see it.

### Decline `CA1861` repository-wide as well

Simpler and symmetrical with the other two.

Rejected because the justification does not reach that far. The argument is
that a literal beside its assertion is clearer than a hoisted field; in
shipping code inside a loop, the rule's own argument wins instead. Declining it
where it is not justified would trade a precise decision for a tidy one, and
would remove the nudge in the only place it could matter.

## Consequences

### Positive

* 65 of 255 findings clear, and every future occurrence clears with them
  rather than accumulating.
* The reasoning lives beside the effect — in `.editorconfig` for the rules the
  build produces, in the scanner invocation for the two it does not — readable
  by anyone, human or agent, editing the repository.
* The two limbs of the policy are stated once and can be cited, so the same
  argument is not re-run at each new analyzer finding.
* `CA1861` remains live where it could genuinely pay, so the decision keeps its
  own escape hatch.

### Negative

* No analyzer will nudge a genuinely hot shipping path toward a concrete return
  type any more, since `CA1859` is off everywhere. That judgement now rests
  entirely with the author and the reviewer.
* Four declined rules is a list that can grow, and it lives in two files. Each
  addition needs the same justification, and nothing but review enforces that.

### Risks

* Reading the count alone overstates the change: no code improved. The value
  here is a recorded policy and a report that shows only what is worth acting
  on.
* A contributor may read the declined-rules sections as licence to switch off
  any inconvenient analyzer. They are scoped to four named rule ids, each
  carrying its reason, precisely so that reading is hard to sustain.
* If a performance requirement is ever recorded against a path these rules
  cover, the decision has to be revisited there rather than assumed still
  valid.

## Follow-up Actions

* Re-examine the `CA1859` decision for any code path that acquires a measured
  performance requirement.

## References

* ADR-0055 — restating the compiler-expressible style rules in `.editorconfig`
  and enforcing them at build time.
* ADR-0056 — stating the coding rules where an agent can act on them, the
  reason a decision recorded out of the code's reach does not hold.
* ADR-0058 — declining `CA1510`, and the scoping principle this ADR follows.
* `.editorconfig` — where the three declined Roslyn rules live, each with its
  reason.
* `.github/workflows/sonar.yml` — where the two declined shell rules live, for
  the same reason and in the only place that can carry it.
* `CONTRIBUTING.md`, `CLAUDE.md` — the coding rules, including the
  value-objects-as-classes trade cited in the Rationale.

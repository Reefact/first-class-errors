# ADR-0062 | Derive the build's Sonar rule set from the quality profile

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0062-derive-the-build-rule-set-from-the-quality-profile.fr.md)

**Status:** Proposed
**Proposed:** 2026-07-29
**Decision Makers:** Reefact

## Context

SonarQube Cloud scores this project against a **server-side quality profile** — "Sonar
way", carrying 375 active C# rules. Nothing in the repository knew what those rules were.
They ran only inside the scanner-hooked compilation in the `sonar` workflow, so a
contributor — human or agent — met them *after* the merge, in a report, and never while
writing the code.

The `sonar` workflow does not close that gap and was never built to. `dotnet-sonarscanner
end` uploads the analysis and exits; it neither waits for the Quality Gate nor reads its
verdict. The job is green when the *upload* succeeds. No GitHub check carries the gate's
verdict either: of the 28 checks on a recent pull request, the only Sonar one was the
repository's own analysis job. The gate is therefore enforced by nothing, while the job
*is* a required check that calls the service — so an outage blocks merging, and a red gate
does not.

The obvious remedy does not work on its own. Adding `SonarAnalyzer.CSharp` as a plain
`PackageReference` does **not** reproduce the profile: measured on this codebase, the
package's default set fires 29 rules across 107 sites, and leaves `S3776` (cognitive
complexity) and `S1192` (duplicated literals) **disabled** although the profile activates
both — the two rules that accounted for most of the report's C# findings. The package is a
different rule set, narrower in exactly the places that mattered.

Three further facts were measured rather than assumed:

* `.editorconfig` **can** enable a rule the package ships disabled; `S3776` then fires
  across 13 files.
* Enabling the whole profile at `warning` produces **135 warning sites across 33 rules** —
  a finite, knowable number.
* All 375 rules at `suggestion` produce **zero** warnings. But a `suggestion` diagnostic also
  prints **nothing** in `dotnet build`, at `quiet` or `normal` verbosity: it reaches an IDE and
  the SARIF log (as `level: note`) and no console. So `suggestion` is not "visible and inert" —
  it is inert and invisible.
* Of the 375 rules, only **33 fire at all**. The other **342 have zero violations** in the tree,
  so nothing stands between them and being enforced.
* `.editorconfig` takes precedence over a global AnalyzerConfig **in both directions**: a
  promotion to `warning` and a decline to `none` both win.

The SonarCloud API answers **unauthenticated** for this public project; the profile and its
active rules are two paginated calls. The profile is SonarSource's **built-in** "Sonar way":
`isBuiltIn` is true and `userUpdatedAt` is null, so nobody in this organization has ever edited
it, and drift can only arrive with an analyzer release. The profile object also reports 378 active
rules where the rules endpoint enumerates 375; the endpoint is self-consistent across every filter
and its per-type totals sum exactly to 375, so three rules cannot be read.

SonarLint was considered and measured too. It is an IDE extension: it does not run in
`dotnet build`, in CI, or for an agent editing the repository from a command line. Its
connected mode emits a `SonarLint.xml`, but committing that file as an `AdditionalFiles`
item did **not** activate `S3776` — the file carries rule parameters, not activation.

The repository has already recorded what happens to a rule that lives where the code's
readers cannot see it. ADR-0055 and ADR-0056 exist because the explicit-type rule, held
only in the ReSharper DotSettings, drifted to 203 violations. ADR-0060 records the
complementary rule: a refusal is written next to what it refuses.

## Decision

The build's C# rule set is derived from the SonarCloud quality profile and **enforced by
default** — membership generated into a committed global AnalyzerConfig at `warning`, every
exception written by hand in `.editorconfig` with its reason or its outstanding count, and a weekly
job failing when the two have drifted apart.

## Rationale

* **The report cannot be the enforcement point, and hardening it would make things worse.**
  Nothing reads the Quality Gate, so it enforces nothing; meanwhile the job that uploads it
  is required and calls a third-party service, so it blocks merging exactly when it has
  nothing to say. Making the gate blocking would deepen that coupling — a SaaS outage would
  then stop all merges — instead of removing it. Enforcement belongs on our own runners.
* **The profile has to be read, because the package is not the profile.** This is the fact
  that shapes everything else: a rule set that omits the two rules the report complained
  about most is not an alignment, it is a different opinion. Reading the profile is the only
  arrangement in which the build and the report are talking about the same rules.
* **Generating membership and writing fates by hand puts each half where it belongs.** What
  the server currently asks for is a fact, changes without anyone deciding, and would rot if
  hand-maintained — exactly the drift ADR-0055 and ADR-0056 were written about. Whether a
  rule *blocks* is a decision, needs a reason, and must be legible next to the rule, which
  is ADR-0060's rule. Two files, two owners, and a reader asking "why does this block?"
  never lands in generated output.
* **Enforcing by default is the only setting that delivers anything today.** A generated list
  at `suggestion` would have printed nothing in a build, so it would have been a list nobody
  reads — the very failure being fixed, reproduced with more files. Enforcing by default turns the
  342 rules that are already clean into a working gate at no cost, and reduces the question to a
  named list of exceptions.
* **The exceptions carry counts, so the backlog is a diff and not a mood.** 33 rules are parked at
  `suggestion` with the number of sites each still has. Clearing a rule means deleting its line;
  the generated file then enforces it with nothing further to write. A list that shrinks by
  deletion is one whose progress is visible in review.
* **Reporting rather than repairing keeps a scheduled job out of the merge-governing file.**
  A scheduled job holding write access to the file that decides which rules block a merge is the
  shape a workflow-security audit flagged twice on this repository. The cost is that a human
  must run one command; the benefit is that no schedule can quietly widen what blocks a
  merge.
* **An IDE cannot be the mechanism.** SonarLint would show the profile faithfully to whoever
  has Rider open, and to nobody else — the precise failure ADR-0056 recorded. Its
  configuration artefact does not close the gap either; that was measured, not assumed.

## Alternatives Considered

### Adopt the analyzer package's default rule set

The smallest possible change: add the package, accept what it enables.

Rejected because it is not the profile. It omits `S3776` and `S1192`, so the build would
stay silent about the two rules the report complained about most, while its 107 existing
violations would turn CI red immediately — paying the full price of enforcement for an
alignment it does not deliver.

### Make the Quality Gate blocking (`sonar.qualitygate.wait=true`)

One scanner argument, and the existing required check would finally mean something.

Rejected as posed, because it bundles two separate decisions: whether the `sonar` check should
be *required*, and whether the gate's verdict should be *read*. As things stand the check is
required and already calls SonarCloud, so an outage already blocks merging while a red gate does
not; adding the wait extends that dependency instead of removing it. The combination worth
considering — **not required, and reading the gate** — is informative without ever blocking on a
third party. It is left open as a follow-up rather than decided here.

### SonarLint, in connected mode

It binds to the server and shows exactly the profile's rules, solving the divergence at its
root.

Rejected because it is an IDE extension. It does not run in `dotnet build`, in CI, or for an
agent working from a command line, so it cannot enforce anything — and a rule enforced only
while a human has an IDE open is the failure ADR-0056 recorded about the DotSettings.

### Commit the connected-mode `SonarLint.xml` and feed it to the analyzer

The appealing hybrid: the server's own profile artefact, read by the analyzer at build time.

Rejected because it does not work. Wired as an `AdditionalFiles` item, it did not activate
`S3776`; the file carries rule *parameters*, while activation is a Roslyn concern settled by
default severity and AnalyzerConfig.

### Generate the whole profile at `suggestion` and promote by hand

This was built first: membership generated at `suggestion`, nothing blocking, each rule promoted
in `.editorconfig` as its sites were cleared. It has the appeal of never being able to redden a
build by regenerating.

Rejected after measuring what `suggestion` actually does: a Sonar diagnostic at that severity
prints nothing in `dotnet build` at any verbosity. The list would have been invisible to the
contributor and to any agent, enforcing nothing on the day it landed, with all of its value owed
to promotion work that nothing forces anyone to do. It also left 342 already-clean rules
unenforced for no reason — the free half of the job, declined by accident.

### Enable the whole profile at `warning`, including the rules with violations

The end state in one step, with no parked list to be ignored.

Rejected on sequencing. It lands 135 blocking violations, so it cannot merge until all of them are
resolved — the mechanism would ship last instead of first, and every promotion decision would be
taken under the pressure of a red build.

### Have the scheduled job open a pull request, or commit the regenerated file

Closer to "it updates itself", and removes the one manual command.

Rejected for now. It gives a schedule write access to the file that governs which rules block a
merge, and a push made with the default token does not re-trigger CI, so the resulting pull request
would arrive unverified. It matters less than it looks: the profile is SonarSource's built-in one,
so drift arrives a handful of times a year, and one command a few times a year is not the cost that
justifies the risk. Listed as a follow-up rather than refused outright.

### Hand-maintain the rule list

No script, no scheduled job, no generated file — just the rules someone chose, with their
reasons.

Rejected because it is the exact arrangement this repository has already watched fail. A
list nothing regenerates diverges from the server silently, and ADR-0055 and ADR-0056 were
written after the explicit-type rule drifted to 203 violations under the same conditions.

## Consequences

### Positive

* **342 Sonar rules are enforced from the day this lands**, at no cost, because they had zero
  violations. A new violation of any of them appears as a warning locally and an error in CI —
  verified end to end by introducing one.
* The build and the report talk about the same rules, and the list saying so is in the repository
  rather than inferred.
* The backlog is a named list of 33 rules and 135 sites, carrying counts, that shrinks by deletion
  — so its progress, or its absence, is visible in a diff.
* Every rule that does *not* block says so on a hand-written line, with a reason or a number.
* No new secret and no third-party action: the API is public and the script uses `curl` and `jq`.

### Negative

* 135 violations stay outstanding across 33 parked rules, and nothing forces them to be addressed.
* Regenerating after a profile change can turn CI red, where the rejected `suggestion` design
  never could. That is the intended behaviour, but it makes regeneration a decision rather than a
  chore.
* "Enforced" means "zero violations measured against the pinned analyzer version". A version bump
  can make a silent rule fire on untouched code, so bumping it is a batch of work.
* The rule set lives in two files, and a reader must know that the generated one states membership
  while the hand-written one states exceptions.
* Three rules the profile counts cannot be read from the rules endpoint, so they are not
  configured. The script says so on every run; nothing can currently resolve it.
* Resolving drift takes a human running one command; the scheduled job stays red until then.

### Risks

* A scheduled job that only fails is one that can be muted. If it goes red for a week and nobody
  acts, it becomes noise and the mechanism dies without anyone deciding to kill it.
* The parked list can grow instead of shrinking. Nothing forces a rule out of `suggestion`, and a
  list that only ever gains lines would mean the arrangement bought a backlog and called it
  progress. The counts are there so that is legible.
* Deleting a parked line without having cleared that rule's sites turns unrelated pull requests
  red. Only review prevents it.
* A prolonged SonarCloud outage makes the scheduled job permanently red. It fails closed — it never
  writes on a bad answer — but a red check nobody can fix invites muting.
* Reading "the build now enforces everything Sonar asks for" would overstate it by 33 rules and 135
  sites. It enforces 342 of 375, and the gap is named.

## Follow-up Actions

* Clear the 33 parked rules family by family, each in its own pull request, deleting its line from
  `.editorconfig` as its sites go to zero — or moving it to `none` with a reason if the codebase
  refuses it.
* Decide the two Quality Gate questions separately: whether `sonar` should remain a *required*
  check, and whether the scanner should *read* the gate. The combination "not required, and reading
  the gate" was never evaluated on its own merits.
* Reconsider whether the scheduled job should open a pull request rather than fail, once there is
  evidence about how often the profile actually moves.

## References

* ADR-0055 — restating the compiler-expressible style rules where the build can see them.
* ADR-0056 — stating the rules where an agent can act on them; why a DotSettings-only rule
  drifted.
* ADR-0058 — declining a rule the support floor makes unsatisfiable, and the scoping
  principle.
* ADR-0060 — a refusal is recorded beside what it refuses.
* [`sonar-profile` workflow reference](../workflows/sonar-profile.en.md) — how the script,
  the generated file and the scheduled job are wired.
* [`sonar` workflow reference](../workflows/sonar.en.md) — the analysis this reconciles
  against.

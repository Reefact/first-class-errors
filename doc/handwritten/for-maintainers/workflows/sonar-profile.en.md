# `sonar-profile` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](sonar-profile.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/sonar-profile.yml`](../../../../.github/workflows/sonar-profile.yml)
**Script:** [`tools/sonar-profile/sync-profile.sh`](../../../../tools/sonar-profile/sync-profile.sh)
**Generated file:** [`build/sonar-profile.globalconfig`](../../../../build/sonar-profile.globalconfig)

## What it is for

It watches the SonarCloud **C# quality profile** and fails when the repository has drifted
from it.

The rules the SonarQube Cloud report is scored against live on the server. Nothing in this
repository used to know what they were, and the obvious remedy does not work on its own: the
`SonarAnalyzer.CSharp` NuGet package ships its **own, narrower default set**. Measured on this
codebase, that default leaves `S3776` (cognitive complexity) and `S1192` (duplicated literals)
**disabled** although the profile activates both — the two rules that accounted for most of the
report's C# findings. Adding the package alone would therefore have made the build and the
report disagree quietly.

So the profile is read and written down. That list is
`build/sonar-profile.globalconfig`, and this workflow is what notices when it rots.

## The two files, and which one decides

| File | Owner | Says |
| --- | --- | --- |
| `build/sonar-profile.globalconfig` | **generated** | which rules the profile activates — all at `warning`, so the default is **enforce** |
| `.editorconfig` | **hand-written** | the **exceptions**: `suggestion` for a rule whose violations are not cleared yet, `none` for one this codebase refuses, with its reason |

**The default is enforce.** `suggestion` was measured as the default and rejected: at that
severity a Sonar diagnostic prints **nothing** in `dotnet build`, at quiet or normal verbosity —
it reaches an IDE and the SARIF log and nobody else. A generated list at `suggestion` would have
been invisible to the reader it exists for. At `warning` the diagnostic appears in the console and
the CI ratchet in `Directory.Build.props` turns it into an error; both were verified end to end by
introducing a violation of an enforced rule.

**348 of the 377 rules are enforced** — they had zero violations in the tree, so promoting them
cost nothing — and **29 are parked** in `.editorconfig` at `suggestion`, together accounting for
104 outstanding sites.

That parked list **is** the backlog, and a rule leaves it by one of two doors:

* **Its sites are cleared.** Delete its line, and the generated file enforces it from the next
  build. Nothing else to write.
* **The few sites that remain are deliberate.** Each carries a `[SuppressMessage]` with its
  reason at the site, and the line goes anyway. This is the door to prefer whenever a handful of
  violations are defensible and the rest of the tree is clean, because the two states differ in
  what the rule does *tomorrow*: parked, it is silent everywhere, including on code not yet
  written; suppressed at five sites, it is enforced everywhere else.

A rule the codebase means to refuse *outright* is a third thing and does not belong in the
backlog at all: it goes with the declines at `none`, with its reason (ADR-0060). `suggestion`
means "not yet", never "no".

`.editorconfig` takes precedence over a global AnalyzerConfig, verified in both directions.
**Membership is generated; every exception is written down.** A reader asking "why does this rule
not block?" finds the answer, and a count, next to the rule.

## When it runs

- **Weekly**, Monday 05:47 UTC. Not nightly: the profile is SonarSource's built-in "Sonar way" —
  measured, `isBuiltIn` is true and `userUpdatedAt` is null, so nobody here has ever edited it and
  nobody can. Drift arrives with an analyzer release, a handful of times a year; a nightly would
  poll a vendor's cadence.
- On demand via **`workflow_dispatch`**.

It deliberately does **not** run on pull requests. Profile drift is not the fault of whatever
pull request happens to be open, and failing an innocent one would teach people to ignore the
check.

## How it runs

One job, `Quality profile drift`, on Linux: checkout, then
`tools/sonar-profile/sync-profile.sh --check`, which regenerates the list from the API and
diffs it against the committed file. On failure the diff is in the log and a step summary says
what to do about it.

The script takes two API calls: the quality profile bound to the project, then its active rules
(paginated). Both are read-only.

## Permissions & security

`contents: read`, declared **on the job** rather than at workflow level, so a job added later
inherits nothing it did not ask for (Sonar `githubactions:S8264`).

`SONAR_TOKEN` is passed from the same secret [`sonar`](sonar.en.md) uses, but is **not
required**: the project is public and the API answers unauthenticated. Passing it anyway is what
keeps this working the day the project stops being public, instead of failing on a 403.

## Handle with care

- **It reports; it does not repair.** The alternative — a scheduled job holding write access to
  the very file that governs which rules block a merge — is the shape a workflow-security audit
  flagged twice on this repository already. Promoting it to open a pull request is a small
  change if that trade is ever judged worth it, but it is a decision, not a convenience.
- **The script fails closed, three ways.** An empty or short answer aborts *without touching the
  file*: fewer than 100 rules is treated as "not a real profile", so one API hiccup cannot rewrite
  the rule set. A project key that disagrees with `sonar.yml` aborts. And a rule count that
  disagrees with the profile's own `activeRuleCount` is reported loudly — on this project the two
  differ by three, and the rules endpoint is self-consistent across every filter while the count
  is not, so **three rules cannot be read and are therefore not configured**. That is printed on
  every run rather than swallowed.
- **A hand-edit of the generated file is caught by the same mechanism**, because a hand-edit
  *is* drift. There is no separate guard and none is needed.
- **The project key and organization must match `sonar.yml`, and the script checks it.** They are
  duplicated in the script and overridable by environment variable; when the defaults are in use
  the script compares them against the scanner arguments in `sonar.yml` and aborts on a mismatch.
  Without that check a rename would leave this job validating a project nobody looks at, in green.
- **A rule with no Roslyn diagnostic id is reported, not dropped.** Sonar keys shaped other than
  `S<digits>` (a rule template, a non-Roslyn check) are printed to stderr with a count, so a
  silent omission cannot hide in the generated file.
- **Regenerating can turn CI red, and that is the point.** Because the default is enforce, a rule
  the profile adds arrives *blocking*. Whoever regenerates must then clean it or park it in
  `.editorconfig` with its count, deliberately, before it merges. The weekly job is the advance
  warning that makes that a decision rather than a surprise.
- **Parking is a temporary state and nothing enforces that.** A rule can sit at `suggestion`
  forever, and if the parked list only grows the arrangement has bought a list and no enforcement.
  The counts in `.editorconfig` exist so that trend is visible in a diff.
- **The analyzer version is pinned, and unpinning it is a lot.** "Enforced" means "zero violations
  measured against this analyzer version". A newer one can make a previously silent rule fire on
  untouched code, so a version bump has to be treated as a batch of work, not routine maintenance.

## The current backlog

The 29 rules parked in `.editorconfig` account for **104 sites**. Every other rule the profile
activates is already enforced, so this list is the whole of what Sonar asks for and this codebase
does not yet do. Promote family by family, deleting each line as its sites are cleared.

The concentration, largest first: `S3776` (19, cognitive complexity), `S1244` (15, floating
point equality — all in test projects, where exact equality is already justified), `S3878` (14,
arrays for `params`), `S3218` (8, inner members shadowing outer), `S107` (6, too many parameters —
a decision the repository has already recorded as deliberate).

Note what these counts are **not**: SonarCloud reports far fewer issues than the backlog has
sites, because it classifies the thirteen test projects as test code and does not raise most
rules there, while the build's rule set applies everywhere. A green SonarCloud is therefore a
milestone and not the finish line — this list is.

## Related

- [`sonar`](sonar.en.md) — the analysis this reconciles against. It reports; it has never
  enforced.
- [`ci`](ci.en.md) — where the warning ratchet turns a promoted rule into a blocking one.
- [`lint`](lint.en.md) — the same move for the files the C# compiler never sees.

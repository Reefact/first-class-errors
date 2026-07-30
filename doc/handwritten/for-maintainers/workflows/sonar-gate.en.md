# `sonar-gate` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](sonar-gate.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/sonar-gate.yml`](../../../../.github/workflows/sonar-gate.yml)
**Script:** [`tools/sonar-profile/check-gate.sh`](../../../../tools/sonar-profile/check-gate.sh)

## What it is for

It reads the **SonarCloud Quality Gate verdict** every night and fails when it is not green.

Until this existed, the gate was computed by SonarCloud and **enforced by nothing**.
`dotnet-sonarscanner end` uploads the analysis and returns; it neither waits for the gate nor
reads it, so the [`sonar`](sonar.en.md) job is green as soon as the upload succeeds, whatever
the verdict. No GitHub check carried the verdict either — of the 28 checks on a recent pull
request, the only Sonar one was the repository's own analysis job. Two findings typed
VULNERABILITY reached `main` behind that permanently green workflow.

## Why not make the scanner wait instead

`sonar.qualitygate.wait=true` looks like the obvious fix and is the wrong one *as posed*,
because it bundles two decisions: whether `sonar` should be a **required** check, and whether
the gate's verdict should be **read**.

As things stand `sonar` is required and already calls SonarCloud, so an outage already blocks
every merge while a red gate does not. Adding the wait extends that dependency instead of
removing it: the failure mode becomes "nobody can merge because a SaaS is down".

Reading the verdict from a **scheduled** job separates the two. The verdict gets enforced, and
an outage costs a red nightly rather than a frozen repository. That is the combination ADR-0062
recorded as never having been evaluated on its own merits — this workflow is it.

## Why this is not redundant with the build

`build/sonar-profile.globalconfig` enforces the C# rules the `SonarAnalyzer` NuGet package
implements. That is a strict **subset** of what the gate measures, and the gap is not academic:

* **Symbolic-execution rules the package does not run.** Measured: an `S2583` violation sat in
  this repository with the rule enforced at `warning` in the generated config, and the local
  build reported *nothing*. SonarCloud's engine found it; the analyzer package cannot.
* **Every non-C# family** — `githubactions`, `shell`, `secrets`, `xml`, `json`, `yaml`. Sonar
  analyses five languages here; C# is 85% of the lines and none of the others are covered by a
  Roslyn analyzer.
* **Coverage, duplication and hotspot review**, which no analyzer can answer at all.

Both findings that last turned this gate red were in those classes: a symbolic-execution bug
and a `githubactions` vulnerability. The build hardening and this check are complements, not
alternatives — and it was the absence of this check that let them sit unnoticed.

## When it runs

- **Nightly**, 04:11 UTC — in the gap left by the weekly jobs, which run Monday between 03:00
  and 06:30.
- On demand via **`workflow_dispatch`**.

Nightly, unlike the weekly [`sonar-profile`](sonar-profile.en.md) drift check: a quality profile
moves on a vendor's release cadence, but the gate moves with **every merge**.

It deliberately does **not** run on pull requests. That is the whole point: the verdict is
read and reported, and no merge ever waits on a third party.

## How it runs

One job, `Quality gate verdict`: checkout, then `tools/sonar-profile/check-gate.sh`, which
calls `/api/qualitygates/project_status` and exits non-zero when the status is not `OK`,
listing the failing conditions.

Ratings are translated from the raw `1..5` the API returns to the `A..E` the dashboard shows —
"C" tells a reader where they are, "3" does not — and the message states what a rating worse
than A actually means: reliability is a **Bug**, security a **Vulnerability**, maintainability a
**Code Smell**.

## Permissions & security

`contents: read`, declared **on the job** (Sonar `githubactions:S8264`).

The project is public, so this needs **no secret**. `SONAR_TOKEN` is passed from the same secret
`sonar.yml` uses so the check keeps working the day the project stops being public; a missing
secret is an empty string, which the script treats as unauthenticated. Every request refuses
non-HTTPS on the initial call *and* on redirects, which matters because the authenticated branch
sends the token.

## Handle with care

- **It never blocks a merge, and that is deliberate.** It is a standing alarm, not a gate. A red
  gate will produce a red run every night until somebody acts, which is the intended behaviour
  and also the way this check can be muted into uselessness.
- **It reads the project, not the branch.** The verdict reflects the last analysis of `main`, so a
  fix on an unmerged branch does not turn it green — only merging does. Expect a lag of one
  analysis after any fix lands.
- **A rating condition names a class, not a count.** `new_reliability_rating: C` means "at least
  one Major bug in the new-code period"; it does not say how many. Follow the link the script
  prints to see them.
- **It shares its script directory with `sonar-profile` but answers a different question.** Drift
  means "the rule list is stale, regenerate it"; a red gate means "something got through, go look".
  Different actions, which is why they are separate workflows on separate cadences.

## Related

- [`sonar`](sonar.en.md) — produces the analysis this reads. It uploads; it has never enforced.
- [`sonar-profile`](sonar-profile.en.md) — the weekly check that the committed rule list still
  matches the server's profile.
- [`ci`](ci.en.md) — where the warning ratchet enforces the C# rules the build *can* see.

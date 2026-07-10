# `dependency-review` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dependency-review.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/dependency-review.yml`](../../.github/workflows/dependency-review.yml)

## What it is for

`dependency-review` blocks a pull request that **introduces a known-vulnerable
dependency**. It diffs the base-vs-head dependency graph and fails the PR if a
change pulls in a package with an advisory at or above the configured severity.

It is the **PR-time** complement to Dependabot: Dependabot only reacts *after* a
vulnerable dependency is already on `main`, whereas this catches the problem at
the moment a PR would add it — which is exactly when it is cheapest to fix.

## When it runs

- On every **pull request targeting `main`**.

## How it runs

One job, `review`: checkout, then run
`actions/dependency-review-action` with `fail-on-severity: moderate`.

## Permissions & security

`contents: read` only — the action reads the repository's dependency graph. It
posts **no** PR comment (that would need `pull-requests: write`); the failed
check is the signal.

## Handle with care

- **It requires the repository's Dependency graph to be enabled.** This is a
  GitHub repository setting, not something the workflow can turn on. If it is
  off, the action fails with *"Dependency review is not supported on this
  repository… ensure that Dependency graph is enabled"* — that is a
  configuration error, not a workflow bug. (On a private repo it also needs
  Advanced Security.)
- **It only sees changes the PR makes.** A CVE published overnight against an
  *existing* dependency does not fail this check — that stays a warning through
  the NuGet audit (see [`Directory.Build.props`](../../Directory.Build.props)).
  This workflow blocks only at the point of introduction, on purpose.
- **`fail-on-severity: moderate` is the tuning knob.** Lower it to `low` to be
  stricter, raise it to `high` to be laxer. Because it only inspects the PR's own
  dependency changes, `moderate` is a genuine gate rather than noise.
- **It gates nothing unless it is required.** As with the other checks, it must
  be marked **required** in branch protection on `main` to actually block a merge.

## Related

- [`codeql`](codeql.en.md) — the code-side security scanner.
- [`dependabot-automerge`](dependabot-automerge.en.md) — the post-merge
  dependency-update side.

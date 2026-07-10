# `dependabot-automerge` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dependabot-automerge.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/dependabot-automerge.yml`](../../.github/workflows/dependabot-automerge.yml)

## What it is for

For Dependabot pull requests, this workflow **enables GitHub auto-merge on patch
and minor updates**, so they merge on their own once the required checks pass.
**Major** updates are deliberately left untouched, to wait for human review. It
is the low-friction lane of the dependency-update policy: routine bumps do not
need a human, risky ones do.

The Dependabot configuration itself (which ecosystems, schedule, ignored
packages) lives in [`.github/dependabot.yml`](../../.github/dependabot.yml), not
here.

## When it runs

- On every **pull request targeting `main`**, but the job is gated on
  `github.actor == 'dependabot[bot]'`, so it acts only on Dependabot's PRs.

## How it runs

One job, `automerge`:

1. `dependabot/fetch-metadata` reads the update type (patch / minor / major).
2. For **patch or minor** updates, `gh pr merge --auto` enables auto-merge. Major
   updates fall through the condition and stay open.

## Permissions & security

Workflow default `contents: read`; the job widens to `contents: write` and
`pull-requests: write` — the scopes needed to enable auto-merge on the PR.

## Handle with care

- **This workflow only *enables* auto-merge; it does not decide when to merge.**
  GitHub merges the PR only once the branch's **required** status checks pass.
  **Without a branch-protection rule on `main` that marks the CI checks
  required, auto-merge would merge immediately** — before CI. The required checks
  are the safety gate, not this workflow. This is the single most important thing
  to understand before relying on it.
- **The `major` exclusion is intentional.** Only `semver-patch` and
  `semver-minor` get auto-merge; majors are left for a human because they are the
  ones most likely to break. Do not broaden the condition to majors.
- **The actor guard matters.** `if: github.actor == 'dependabot[bot]'` keeps the
  elevated `contents: write` / `pull-requests: write` path from running on
  human PRs.

## Related

- [`.github/dependabot.yml`](../../.github/dependabot.yml) — what Dependabot
  updates and what it ignores (e.g. the frozen `Microsoft.CodeAnalysis.*`; see
  [`analyzers`](analyzers.en.md)).
- [`dependency-review`](dependency-review.en.md) — the PR-time vulnerability gate
  that a Dependabot PR also passes through.

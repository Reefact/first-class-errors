# `scorecard` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](scorecard.fr.md)

> Maintainer documentation — part of the [workflow reference](README.en.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/scorecard.yml`](../../.github/workflows/scorecard.yml)

## What it is for

`scorecard` runs [OpenSSF Scorecard](https://securityscorecards.dev), which
scores the repository's **security posture** against a set of automated checks —
pinned actions, token permissions, branch protection, signed releases,
dependency-update tooling, and more. It uploads the findings to the
code-scanning dashboard and **publishes the score to securityscorecards.dev**,
which powers the OpenSSF Scorecard badge in the README.

Where `codeql` scores the *code* and `dependency-review` scores the
*dependencies*, Scorecard scores the *project's practices* — including several
of the very conventions the other workflows implement.

## When it runs

It runs on the **default branch only** — it assesses the repository, not a PR
diff, so there is **no `pull_request` trigger**:

- On **`branch_protection_rule`** — re-score when a branch-protection rule
  changes (this feeds Scorecard's Branch-Protection check).
- **Weekly** on a `schedule` (`cron: 23 5 * * 1`).
- On **push to `main`**.

## How it runs

One job, `analysis`: credential-free checkout → run `ossf/scorecard-action` with
`publish_results: true` → upload the SARIF both as a build artifact and to the
code-scanning dashboard.

## Permissions & security

Top-level `permissions: read-all`; the job widens only two write scopes:

- `security-events: write` — upload the SARIF to code-scanning.
- `id-token: write` — publish results to the public OpenSSF API via OIDC; **this
  is what enables the badge**.

The checkout uses **`persist-credentials: false`** — a credential-free checkout
that Scorecard's own Token-Permissions check rewards.

## Handle with care

- **It does not run on pull requests, by design.** Do not add a `pull_request`
  trigger to "test it on a PR" — Scorecard evaluates the whole repository, and it
  needs the default-branch context and the OIDC identity to publish. A PR run
  would be meaningless and could not publish. To exercise a change, merge it and
  watch the first push-to-`main` run.
- **The badge shows "no data" until the first `main` run publishes.** That is
  expected on first setup, not a failure.
- **`publish_results: true` is public-repo-appropriate.** It publishes the score
  to securityscorecards.dev for transparency and the badge. Keep it as-is for a
  public repo; if the repo ever goes private, drop it (and the badge).
- **The permission comment is load-bearing.** This repo is public, so
  `contents: read` / `actions: read` are not needed in the token. The commented
  lines in the YAML explain what to **uncomment for a private repository** — do
  not delete them.
- **Improving the score is done in the *other* workflows and in repo settings**,
  not here. A low Branch-Protection sub-score, for instance, is fixed by marking
  checks required on `main`, not by editing this file.

## Related

- [`codeql`](codeql.en.md) — shares the `github/codeql-action/upload-sarif`
  action used to push results to code-scanning.
- The README badge links to the public viewer at `securityscorecards.dev`.

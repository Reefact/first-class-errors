# `dependabot-automerge` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dependabot-automerge.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/dependabot-automerge.yml`](../../../../.github/workflows/dependabot-automerge.yml)

## What it is for

For Dependabot pull requests, this workflow **enables GitHub auto-merge on patch
and minor updates**, so they merge on their own once the required checks pass.
**Major** updates are deliberately left untouched, to wait for human review. It
is the low-friction lane of the dependency-update policy: routine bumps do not
need a human, risky ones do.

The Dependabot configuration itself (which ecosystems, schedule, ignored
packages) lives in [`.github/dependabot.yml`](../../../../.github/dependabot.yml), not
here.

## When it runs

- On every **pull request targeting `main`**, but the job is gated on
  `github.event.pull_request.user.login == 'dependabot[bot]'` — the pull
  request's **author** — so it acts only on Dependabot's PRs. That is
  deliberately not `github.actor`; see *Handle with care*.

## How it runs

One job, `automerge`:

1. `dependabot/fetch-metadata` reads the update type (patch / minor / major).
2. The **head commit of this event** is inspected and classified: Dependabot's
   own GitHub-signed commit, Dependabot-authored but unsigned, or foreign.
3. For a **signed** head and a **patch or minor** update, `gh pr merge --auto`
   enables auto-merge. Major updates fall through the condition and stay open.
4. For a **foreign** head, auto-merge is **withdrawn** (`--disable-auto`). An
   unsigned Dependabot-authored head — what `dependabot-autofix` leaves behind
   after a reword or a rebase — is left exactly as it is.

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
- **The guard is the pull request's AUTHOR, and it must not go back to
  `github.actor`.** Both keep the elevated `contents: write` /
  `pull-requests: write` path off human PRs, but `github.actor` names whoever
  triggered the run, so a push by someone else made the job *skip*. Auto-merge
  survives later pushes to the head branch, so skipping left it armed on a tip
  nobody re-checked. The author of a pull request never changes, so the job now
  runs on every event of a Dependabot PR — which is exactly when it needs to act.
- **The two head guards are asymmetric on purpose.** *Arming* requires
  Dependabot's own GitHub-signed commit: commit author names are `git config`
  values and forge freely, GitHub's signature does not. *Withdrawing* triggers on
  the weaker signal — an author that is not Dependabot — because withdrawing is
  the fail-safe direction; at worst a human merges by hand. Do not "tidy" this
  into one symmetric check: keying the withdrawal on the signature would fight
  [`dependabot-autofix`](dependabot-autofix.en.md), whose `--amend` and `rebase`
  keep Dependabot as the author but drop the signature, and which deliberately
  keeps auto-merge on after a trivial fix.
- **`dependabot/fetch-metadata` is a second gate, but not this one.** It
  re-checks the PR author, the **first** commit's author and that commit's
  signature, never consults `github.actor`, and fails closed by emitting no
  outputs (both its `skip-*-verification` inputs default to `false`). What it
  does not check is the **tip** — and the tip is what auto-merge merges.

## Related

- [`dependabot-autofix`](dependabot-autofix.en.md) — the diagnostic companion:
  when a Dependabot PR stays red, it triages why and comments a ready-to-apply fix.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — what Dependabot
  updates and what it ignores (e.g. the frozen `Microsoft.CodeAnalysis.*`; see
  [`analyzers`](analyzers.en.md)).
- [`dependency-review`](dependency-review.en.md) — the PR-time vulnerability gate
  that a Dependabot PR also passes through.

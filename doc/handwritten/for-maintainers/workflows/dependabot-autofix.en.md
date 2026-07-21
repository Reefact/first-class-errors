# `dependabot-autofix` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dependabot-autofix.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/dependabot-autofix.yml`](../../../../.github/workflows/dependabot-autofix.yml)

## What it is for

Dependabot pull requests sometimes go red for reasons that are quick to fix but
tedious to chase: a bumped analyzer that now emits a warning-as-error, a small API
change a library made, a branch that has fallen behind `main`. This workflow is the
**repair** companion to [`dependabot-automerge`](dependabot-automerge.en.md):
automerge decides *when* a green PR merges; this decides *why* a red one is red,
**fixes it**, and pushes the fix.

When a Dependabot PR's checks have run, it asks the model (Claude, via the same
`curl` pattern as [`changelog`](changelog.en.md) and [`adr-check`](adr-check.en.md))
for a verdict — **healthy**, **fixable with a small low-risk change**, or **needs a
human** — and, when it is fixable, applies one of a fixed set of actions and pushes
it.

**The auto-merge rule follows the risk of the fix:**

| Fix | Action | Auto-merge |
| --- | --- | --- |
| Rewrite a commit header | `rewrite_commit_message` | **kept** (trivial) |
| Retitle the pull request | `retitle_pr` | **kept** (trivial) |
| Rebase onto `main` | `rebase` | **kept** (trivial) |
| Change product/test code | `apply_patch` | **disabled** — a human reviews |

A **trivial** fix changes only history or metadata, so the PR stays eligible to
merge on its own once checks pass. A **code** fix changes file contents, so
auto-merge is turned off and the AI-authored change waits for human review. The
workflow decides trivial-vs-code **from the action it actually took**, never from
the model's say-so, and it **never merges** anything itself.

## When it runs

- On **`workflow_run: completed`** of every gating workflow a Dependabot PR passes
  through (`ci`, `sonar`, `analyzers`, `commit-lint`, `dummies`,
  `dependency-review`, `codeql`). Any one completing re-runs it against the head
  commit's **combined** check status.
- Gated to **Dependabot's own pull requests from a branch in this repository**
  (`workflow_run.actor == 'dependabot[bot]'`, non-fork head). Human and fork PRs are
  ignored.

## How it runs

One job, `autofix`:

1. **Resolve the PR**, then **classify** the head commit's combined check status:
   *failing* (act), *pending* (wait for the next completion), *green* (remove any
   stale comment).
2. On *failing*, [`collect-context.sh`](../../../../tools/dependabot-autofix/collect-context.sh)
   gathers the PR diff, the failing check names, and the failing job logs — **all
   through the API, nothing built** — and one Anthropic call returns a JSON verdict
   (verdict, action, `explanation`, and any `patch` / `commit_message` / `pr_title`).
3. When the verdict is **fixable**, a *second* checkout of the PR head branch (with
   a push-capable token) lets [`apply-fix.sh`](../../../../tools/dependabot-autofix/apply-fix.sh)
   perform the action and push it. A **code** fix then has auto-merge disabled.
4. The workflow **composes the comment itself** from the verdict and the action it
   actually took (so the comment never claims a fix that did not land) and upserts
   the single marked comment (`<!-- dependabot-autofix -->`).

Everything is **best-effort and safe-by-omission**: a patch that does not apply, a
rebase that conflicts, an API error, a non-JSON reply — each leaves the PR
untouched and degrades to a *suggested-fix* or *needs-a-human* comment rather than
pushing a broken change or failing red. A **loop guard** stops it acting twice on
the same push: once the head commit's *committer* is `github-actions[bot]`, it waits
for the next Dependabot push before acting again.

## Permissions & security

Workflow default `contents: read`. The job widens to `contents: write` (push the
fix; disable auto-merge), `pull-requests: write` (comment, retitle), and
`checks: read` + `actions: read` (read the check-runs and failed logs).

Two secrets:

- **`ANTHROPIC_API_KEY`** (required) — an Actions secret; `workflow_run` reads
  Actions secrets, so no Dependabot-scoped copy is needed. Absent → the workflow
  warns and does nothing.
- **`DEPENDABOT_AUTOFIX_TOKEN`** (recommended) — a fine-grained PAT or GitHub App
  token with **contents: write** + **pull-requests: write** on this repo, used only
  for the push. GitHub does **not** re-trigger workflows for a push made with the
  default `GITHUB_TOKEN`; a dedicated token makes `ci` re-run on the fix, which a
  *kept* auto-merge needs to proceed. Without it the fix is still pushed, but the
  checks must be re-triggered by hand (e.g. close/reopen the PR).

**The supply-chain boundary is deliberate.** The repair does only git operations —
apply, reword, rebase, push — and **never builds the bumped dependency**. The write
token and the API key therefore never meet freshly bumped third-party code; the
pushed commit is validated by the ordinary `ci` run, in its own read-only Dependabot
context.

## Handle with care

- **Trivial keeps auto-merge; code disables it — and that split is enforced from
  the action, not the model's claim.** Do not let `apply_patch` be treated as
  trivial: a code change must always face human review.
- **The push token choice is load-bearing.** With only `GITHUB_TOKEN`, a pushed fix
  does not re-run `ci`, so a kept auto-merge will sit until the checks are
  re-triggered. Set `DEPENDABOT_AUTOFIX_TOKEN` for the intended behaviour.
- **It does nothing until it is on `main`.** `workflow_run` only fires for workflow
  files on the default branch.
- **It never merges, and never *enables* auto-merge.** Enabling is
  [`dependabot-automerge`](dependabot-automerge.en.md)'s job; this only *disables*
  auto-merge on a code fix.
- **It never changes a dependency version**, and failures that are not code-fixable
  (`sonar`/coverage without a secret, a `dependency-review`/CodeQL policy block) are
  *diagnosed*, not fixed.
- **The loop guard and the actor/non-fork guard matter.** They keep it from acting
  twice on one push and keep the write path off human and fork PRs.

## Related

- [`dependabot-automerge`](dependabot-automerge.en.md) — enables auto-merge on a
  green Dependabot patch/minor PR; this repairs a red one (and disables auto-merge
  when its fix is code).
- [`commit-lint`](commit-lint.en.md) — exempts Dependabot-authored commits, so a
  long `bump …` header no longer fails on its own; this handles the rest.
- [`dependency-review`](dependency-review.en.md) — a block there is *needs a human*,
  never auto-worked-around.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — what Dependabot
  updates and ignores.
- Prompt: [`.github/dependabot-autofix-prompt.md`](../../../../.github/dependabot-autofix-prompt.md).

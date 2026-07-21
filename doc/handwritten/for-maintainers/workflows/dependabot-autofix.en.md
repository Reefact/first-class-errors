# `dependabot-autofix` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dependabot-autofix.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/dependabot-autofix.yml`](../../../../.github/workflows/dependabot-autofix.yml)

## What it is for

Dependabot pull requests sometimes go red for reasons that are quick to fix but
tedious to diagnose: an over-long `bump …` commit header, a bumped analyzer that
now emits a warning-as-error, a small API change a library made. This workflow is
the **diagnostic companion** to [`dependabot-automerge`](dependabot-automerge.en.md):
automerge decides *when* a green PR merges; this decides *why* a red one is red and
*how* to fix it.

When a Dependabot PR's checks have run, it asks the model (Claude, via the same
`curl` pattern as [`changelog`](changelog.en.md) and [`adr-check`](adr-check.en.md))
for one of three verdicts — **healthy**, **fixable with a small low-risk change**,
or **needs a human** — and posts a single comment. When the verdict is *fixable*,
that comment carries a **ready-to-apply patch** and a convention-conforming commit
message, so applying the fix is a copy-paste, not an investigation.

**It never applies, pushes, or merges anything.** Every verdict is advisory — a
comment a human acts on, exactly like `adr-check`. This keeps it inside the
repository's "no agent merges" rule and, more importantly, inside its supply-chain
boundary (see *Handle with care*).

## When it runs

- On **`workflow_run: completed`** of every gating workflow a Dependabot PR passes
  through (`ci`, `sonar`, `analyzers`, `commit-lint`, `dummies`,
  `dependency-review`, `codeql`). Any one completing re-runs the triage against the
  head commit's **combined** check status, so the single comment refines itself as
  the slower checks finish.
- The job is gated to **Dependabot's own pull requests from a branch in this
  repository** (`workflow_run.actor == 'dependabot[bot]'` and a non-fork head). A
  human PR, or a fork, is ignored.

### Why `workflow_run`, not `pull_request`

A `pull_request` run *raised by Dependabot* is deliberately sandboxed by GitHub:
it gets a **read-only** token and **no repository secrets** (only the separate
Dependabot secrets store). It could neither read `ANTHROPIC_API_KEY` nor comment.
`workflow_run` runs in the **base-branch context** after the checks finish: it has
the repository secrets and a writable token — and it does **not** check out or
execute the pull request's code. The bumped dependency's code already ran, in the
read-only `ci` context; this triage only *reads* the result.

## How it runs

One job, `triage`:

1. **Resolve the PR** from the `workflow_run` payload (falling back to
   `gh pr list --head`).
2. **Classify the combined check status** of the head commit from its check-runs:
   *failing* (act now), *pending* (wait for the next completion), or *green*.
3. On **green**, remove any stale triage comment (the PR recovered).
4. On **failing**, [`collect-context.sh`](../../../../tools/dependabot-autofix/collect-context.sh)
   assembles the PR diff, the failing check names, and the failing job logs —
   **all through the GitHub API, nothing checked out** — and one Anthropic call
   returns the verdict, an optional patch, and the comment body.
5. [`upsert-comment.sh`](../../../../tools/dependabot-autofix/upsert-comment.sh) posts,
   refreshes, or removes the **single** marked comment (`<!-- dependabot-autofix -->`).

Like `adr-check`, every external call is **best-effort**: a missing log, an API
error, a refusal, a truncated or non-JSON reply each become a `::warning::` and a
no-op, never a red check. The workflow is advisory; it must not manufacture a
failure of its own.

## Permissions & security

Workflow default `contents: read`. The job widens to `checks: read` +
`actions: read` (to read the check-runs and the failed runs' logs) and
`pull-requests: write` (to manage its one comment) — and nothing else. It needs the
**`ANTHROPIC_API_KEY` repository secret** (an Actions secret; `workflow_run` reads
Actions secrets, so — unlike a `pull_request` Dependabot run — it does *not* need a
Dependabot-scoped copy).

## Handle with care

- **It is advisory. It never applies the patch, pushes, or merges.** The patch in
  the comment is for a human to apply and review. Do not "upgrade" this workflow to
  push commits without reading the next point.
- **The supply-chain boundary is the whole design.** This workflow never checks out
  or builds the PR, so the writable token and the API key never come within reach
  of a freshly bumped third-party package. An auto-apply variant would have to
  build the bumped code to verify a fix, executing untrusted code *with* those
  credentials — a real, deliberate trade-off, not a tweak. Decide it consciously
  before changing the trigger to `pull_request_target` or adding a build step.
- **It does nothing until it is on `main`.** `workflow_run` only fires for workflow
  files on the repository's default branch. On a feature branch the workflow is
  inert; it starts triaging once merged.
- **It reads the checks; it does not re-run them.** Failures that are not
  code-fixable — `sonar`/coverage that cannot read a secret on a Dependabot run,
  a `dependency-review`/CodeQL policy block — are *diagnosed*, not fixed. Fixing
  those is a repository-configuration or human-judgement matter.
- **The actor + non-fork guard matters.** It keeps the elevated `pull-requests:
  write` path off human and fork PRs.
- **Required checks are still the real gate.** This posts a comment; it changes no
  check status. What blocks a bad merge is branch protection on `main`.

## Related

- [`dependabot-automerge`](dependabot-automerge.en.md) — enables auto-merge on a
  green Dependabot patch/minor PR; this explains a red one.
- [`commit-lint`](commit-lint.en.md) — now **exempts Dependabot-authored commits**,
  so a long `bump …` header no longer fails the lint on its own; this workflow
  handles the residual failures.
- [`dependency-review`](dependency-review.en.md) — the PR-time vulnerability gate a
  Dependabot PR also passes through; a block there is *needs a human*, never
  auto-worked-around.
- [`.github/dependabot.yml`](../../../../.github/dependabot.yml) — what Dependabot
  updates and what it ignores.
- Prompt: [`.github/dependabot-autofix-prompt.md`](../../../../.github/dependabot-autofix-prompt.md).

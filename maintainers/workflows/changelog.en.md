# `changelog` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](changelog.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/changelog.yml`](../../.github/workflows/changelog.yml)

## What it is for

`changelog` drafts the `[Unreleased]` section of a release train's changelog from
the pull requests merged since that train's last tag, and opens a pull request
with the result. It never publishes anything and never writes to a released
section — it produces a **draft for a human to review and merge**.

It fills a gap the [`release`](release.en.md) path leaves open.
[`tools/packaging/release-notes.sh`](../../tools/packaging/release-notes.sh)
already generates the GitHub Release body, but those notes are **commit-oriented
and raw** ("`- feat(core): … (abc1234)`"), scoped to one train. This workflow
produces the complementary artifact: a **narrative, user-facing changelog** in
[Keep a Changelog](https://keepachangelog.com/) shape, grouped by kind of change
(Breaking / Added / Changed / Fixed / Deprecated), written for the developer who
consumes the package from NuGet.

The two trains version independently and keep **separate** changelog files:

| Train | Scopes | Changelog file |
| --- | --- | --- |
| `lib` | `core`, `analyzers`, `testing` | [`CHANGELOG.md`](../../CHANGELOG.md) |
| `cli` | `cli`, `gendoc` | [`FirstClassErrors.Cli/CHANGELOG.md`](../../FirstClassErrors.Cli/CHANGELOG.md) |

## When it runs

- On **`workflow_dispatch`** only. It has to be dispatched by hand from the
  Actions tab — there is no automatic trigger, because rewriting a changelog is a
  human-reviewed editorial act, not something to fire on every merge.

Two inputs:

- **`component`** (`lib` | `cli`, required) — which train to draft for.
- **`from_ref`** (optional) — the previous tag to diff from. Blank auto-detects
  the train's latest tag; if the train has no tag yet, the whole history is taken.

## How it runs

One job, `draft-changelog`:

1. Checkout with **`fetch-depth: 0`** — the train's previous tag (and its commit
   timestamp, the lower bound of the pull-request range) must resolve.
2. **Collect** the train's pull requests with
   [`tools/changelog/collect-prs.sh`](../../tools/changelog/collect-prs.sh):
   `gh pr list` gathers the candidates merged into `main` after the tag's commit
   time, then each candidate is kept only if one of **its commits' Conventional
   Commit scopes** falls in the train's set. This is the **same partition
   `release-notes.sh` applies** — so the changelog and the Release notes describe
   the same set of changes, and scopeless infrastructure PRs (bare `ci:` /
   `chore:` / `docs:`) belong to neither train and are dropped from both.
3. **Draft** the entry: the merged PRs (number, title, body, labels, author) are
   sent to the Anthropic API under
   [`.github/changelog-prompt.md`](../../.github/changelog-prompt.md), which
   instructs the model to group the changes and **invent nothing**.
4. **Merge** the drafted block into the train's changelog file with
   [`tools/changelog/merge-unreleased.sh`](../../tools/changelog/merge-unreleased.sh),
   which **replaces** the `[Unreleased]` section in place.
5. **Open** (or refresh) a pull request from
   `chore/changelog-<component>-draft` via `gh`, for review.

If no train-scoped PR is found, the job stops after step 2 without opening
anything.

## Permissions & security

The top-level token is read-only (`contents: read`). The `draft-changelog` job
adds only `contents: write` (push the draft branch) and `pull-requests: write`
(open the review PR) — the least-privilege shape OpenSSF Scorecard rewards. The
pull request is opened with `gh` (preinstalled on the runner), so no third-party
action is pinned for it.

**Secret:** the drafting step needs **`ANTHROPIC_API_KEY`** (repository secret).
The step fails with an explicit message if it is missing. Because the only
trigger is `workflow_dispatch` — available to accounts with write access, never
to a pull request from a fork — the key is never exposed to fork PRs.

## Handle with care

- **The train partition lives in one place: [`tools/trains.sh`](../../tools/trains.sh).**
  `collect-prs.sh`, `release-notes.sh` and this workflow all *source* it, so the
  changelog and the GitHub Release notes can never disagree on which scopes belong
  to which train (`lib` → core/analyzers/testing, `cli` → cli/gendoc). Add a train,
  or move a scope between trains, **there** — not in the scripts. A whole new train
  also needs the static edits GitHub forces (tag trigger, choice options,
  commit-lint scopes): follow [Adding a release train](../AddingAReleaseTrain.en.md).
- **The `[Unreleased]` block is *replaced*, never prepended.** `merge-unreleased.sh`
  owns the block: on every run it swaps the whole `## [Unreleased]` section for the
  freshly drafted one and leaves released `## [x.y.z]` sections untouched. This is
  what makes a re-run idempotent — prepending instead would stack duplicate
  `[Unreleased]` headings. The drafted entry must start with `## [Unreleased]`;
  the workflow trims anything the model emits before it, so the replacement can
  never accumulate stray text.
- **The human review *is* the safety mechanism.** The prompt is told to invent
  nothing, but a model can still infer a benefit a PR never stated. That is why
  the workflow opens a PR instead of committing to `main`: read the entry against
  the actual PRs before merging, and delete anything that was inferred rather than
  found. Do not wire this to auto-merge.
- **A truncated or refused draft fails the run — it does not open a partial PR.**
  The drafting step checks the API response and exits non-zero on an API error, a
  `refusal`, or a `max_tokens` truncation, rather than merging half a changelog.
  If you hit `max_tokens`, raise it or narrow the range with `from_ref`.
- **Untrusted PR text is handled as data.** PR titles and bodies come from
  contributors. They are JSON-escaped with `jq --arg`, wrapped in
  `<context>` / `<pull_requests>` delimiters, and the prompt is told to treat them
  as data, not instructions. The free-text `from_ref` input travels through the
  environment and is only ever handed to `git log`, never interpolated into a
  shell command.
- **`claude-sonnet-5` is a floating alias.** It resolves to the current Sonnet 5
  snapshot — desirable for a drafter (you want the latest), but it means output
  can shift over time. The human review absorbs that; do not treat the draft as
  reproducible.
- **It only appears in the Actions tab once it is on the default branch.** GitHub
  lists a `workflow_dispatch` workflow only from the default branch, so a change
  to this file is dispatchable only after it merges to `main`.

## Related

- [`release`](release.en.md) — the tag-driven publish, whose
  `release-notes.sh` produces the raw, commit-oriented GitHub Release body this
  changelog complements.
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md) — the Conventional Commit **scopes**
  the train partition is built on.
- The two changelog files: [`CHANGELOG.md`](../../CHANGELOG.md) (lib) and
  [`FirstClassErrors.Cli/CHANGELOG.md`](../../FirstClassErrors.Cli/CHANGELOG.md)
  (cli).

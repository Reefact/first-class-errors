# `adr-check` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](adr-check.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/adr-check.yml`](../../.github/workflows/adr-check.yml)

## What it is for

`adr-check` checks a **branch** against the ADR base and reports whether it embarks
an architectural decision the project records as an [ADR](../adr/README.md). It is
the **manual fallback** for a contributor **without Claude Code** — whose coding
session performs the same check automatically (see
[`AGENTS.md`](../../AGENTS.md) → "Architecture decisions"). Same three outcomes,
run on demand from the Actions tab instead of inside a session.

It is **advisory**: it **never blocks** anything and **never writes an ADR**. It
surfaces one of three findings for a human to act on:

- **New decision** — a lasting decision that is not yet recorded;
- **Supersedes** — a change to a decision an existing ADR already records;
- **Conflicts** — a contradiction with an accepted ADR.

Drafting the ADR (as `Status: Proposed`) and accepting, superseding, or
deprecating it stay with a human or an agent.

## When it runs

- On **`workflow_dispatch`** only — dispatched by hand from the Actions tab against
  the branch you want to check. There is no automatic per-pull-request trigger:
  recording a decision is a human-reviewed act, exactly like the sibling
  [`changelog`](changelog.en.md) workflow, and an autonomous model call on every
  pull request is neither wanted nor needed.
- One input, **`base`** (default `main`) — the ref the branch is diffed against.
- Like any `workflow_dispatch`, it only appears in the Actions tab once this file
  is on the **default branch**.

## How it runs

One job, `adr-check`:

1. Checkout the dispatched branch with `fetch-depth: 0`, so the merge-base with the
   base ref resolves.
2. **Skip when there is no key.** If `ANTHROPIC_API_KEY` is missing, the job notes
   it (in the log and the run summary) and exits 0.
3. Resolve `base` into the branch's **fork point** (`git merge-base`), so the diff
   is exactly what the branch introduces.
4. **Collect context** with
   [`tools/adr-check/collect-context.sh`](../../tools/adr-check/collect-context.sh):
   the changed-file list and the unified diff (from `git`, byte-capped) plus the
   current ADR base (index and each ADR, byte-capped), as one delimited bundle.
5. **Ask the model** under
   [`.github/adr-check-prompt.md`](../../.github/adr-check-prompt.md), which defines
   the three outcomes, biases hard toward silence, and requires a single JSON
   verdict `{ analysis, needs_report, outcomes, report }`.
6. **Write the verdict to the run summary** (`$GITHUB_STEP_SUMMARY`) — the primary
   output, readable whether or not a pull request exists. A clean branch gets a
   "nothing to flag" line.
7. **Mirror it to the pull request, if one is open** for the branch: when
   `needs_report` is true the report is posted (or refreshed) as the single comment
   found by the hidden `<!-- adr-check -->` marker, via
   [`tools/adr-check/upsert-comment.sh`](../../tools/adr-check/upsert-comment.sh);
   when false, any stale comment is removed. With no open pull request, the summary
   is the only output.

## Permissions & security

The top-level token is read-only (`contents: read`). The job adds only
`pull-requests: write`, and only to post the optional comment when a pull request
is open — it writes nothing to the repository.

- **Secret:** `ANTHROPIC_API_KEY` (repository secret), shared with the
  [`changelog`](changelog.en.md) workflow. Because the only trigger is
  `workflow_dispatch` — available to accounts with write access, never to a fork —
  the key is never exposed to a fork.
- **Untrusted diff and ADR text is handled as data.** The bundle is JSON-escaped
  through `jq --arg` and wrapped in delimiters; the prompt is told to treat every
  block as data, not instructions.

## Handle with care

- **It is advisory by construction — keep it that way.** It is manual and only
  writes a run summary (and an optional comment); it has no way to block a merge,
  and every failure mode (no secret, API error, refusal, `max_tokens`, unparseable
  output) is a `::warning::` with `exit 0`. Do not wire it to `pull_request` or make
  it a required check — that is the whole point of scoping it to Claude Code plus a
  manual run.
- **The verdict is a nudge, not a ruling.** The model can misjudge significance. A
  human decides; the maintainer owns the ADR and its status. The report says so.
- **Precision over recall on the nudge.** The prompt is tuned to stay silent on
  routine changes (bug fixes, tests, docs, dependency bumps, refactors with no
  contract change). If it cries wolf, tighten the prompt's non-triggers.
- **`claude-sonnet-5` is a floating alias.** It resolves to the current Sonnet 5
  snapshot, so a verdict can shift over time. Acceptable for an advisory; do not
  treat it as reproducible.
- **The context is byte-capped.** `DIFF_MAX_BYTES` and `ADR_MAX_BYTES` bound the
  payload; a truncation is announced in-band. A very large diff is judged on its
  first slice plus the changed-file list.
- **Same machine as [`changelog`](changelog.en.md).** Both are manual-dispatch LLM
  workflows (API via `jq`-built payload, untrusted text as data, human in the loop);
  keep them consistent when either changes.

## Related

- [`AGENTS.md`](../../AGENTS.md) — the code-authoring agent's own ADR check, run
  automatically in a Claude Code session; this workflow is its manual counterpart
  for contributors without Claude Code.
- [ADR reference](../adr/README.md) — the format, conventions, and "when is an ADR
  written?" note.
- [`changelog`](changelog.en.md) — the sibling manual-dispatch LLM-in-CI workflow
  this one mirrors.

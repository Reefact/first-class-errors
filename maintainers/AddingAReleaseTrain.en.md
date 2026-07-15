# Adding a release train

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](AddingAReleaseTrain.fr.md)

> Maintainer documentation — how to add a new independently-versioned package
> (a "release train") to FirstClassErrors. Not part of the user documentation
> under `doc/`.

## What a train is

A **release train** is a package (or lockstep group of packages) that versions and
publishes on its own tag prefix. Today there are two:

| Train | Tag prefix | Scopes | Package(s) | Changelog |
| --- | --- | --- | --- | --- |
| `lib` | `lib-v*` | `core`, `analyzers`, `testing` | FirstClassErrors + FirstClassErrors.Testing | `CHANGELOG.md` |
| `cli` | `cli-v*` | `cli`, `gendoc` | FirstClassErrors.Cli (the `fce` tool) | `FirstClassErrors.Cli/CHANGELOG.md` |

The train → (prefix, scopes, package, changelog file) mapping lives in **one place**,
[`tools/trains.sh`](../tools/trains.sh), which the release-notes generator, the
changelog collector, and the changelog workflow all *source*. The rest of the edits
below exist only because GitHub workflow triggers and choice inputs, the commit-lint
scope allowlist, and the packing logic **cannot** be data-driven from that file —
they are static by nature.

> **Scope vs train.** Adding a *scope* to an **existing** train (a new component
> that ships inside `lib`, say) is much lighter: add the scope to that train's row
> in `trains.sh` and to the commit-lint allowlist (steps 1–2 below), and stop. You
> only need the full checklist when the new package gets its **own tag prefix**.

## The one data edit

**1. Add a row to [`tools/trains.sh`](../tools/trains.sh).** One line, pipe-separated:

```
<id>|<tag-prefix>|<scopes csv>|<changelog file>|<package label>
```

e.g. `docs|docs-v|gendoc|FirstClassErrors.GenDoc/CHANGELOG.md|FirstClassErrors.GenDoc`.
This is all that `release-notes.sh`, `collect-prs.sh`, and the changelog workflow's
"Resolve component" step need — they pick the new train up with no further change.
The scopes must be a subset of the commit-lint allowlist (next step).

## The static edits GitHub and the tooling force

**2. Commit scopes** — if the train introduces new scopes, add them to the closed
list in [`tools/commit-lint/lint-commit-message.sh`](../tools/commit-lint/lint-commit-message.sh)
(`SCOPES` **and** `SCOPES_HUMAN`) and to the scope table in
[`CONTRIBUTING.md`](../CONTRIBUTING.md). Without this, commits for the new component
fail the commit-lint check.

**3. [`.github/workflows/release.yml`](../.github/workflows/release.yml)** — three
spots:
- the tag trigger list: add `- '<prefix>*.*.*'` (e.g. `- 'docs-v*.*.*'`);
- the `component` `workflow_dispatch` choice: add `- <id>`;
- the version-resolution `case` on `REF_NAME`: add
  `<prefix>*) COMPONENT="<id>"; VERSION="${REF_NAME#<prefix>}" ;;`, and add `<id>`
  to the `lib|cli)` allowlist `case` just below it.

**4. [`tools/packaging/pack.sh`](../tools/packaging/pack.sh)** — add a `<id>)` branch
selecting which projects that train packs. This is genuinely train-specific packing
logic (which `.csproj` to pack, whether a symbol package ships), so it is not driven
from `trains.sh`.

**5. [`.github/workflows/changelog.yml`](../.github/workflows/changelog.yml)** — add
`- <id>` to the `component` `workflow_dispatch` choice `options`. (The workflow reads
everything else about the train from `trains.sh`.)

## What happens on its own

- [`tools/packaging/release-notes.sh`](../tools/packaging/release-notes.sh),
  [`tools/changelog/collect-prs.sh`](../tools/changelog/collect-prs.sh), and the
  changelog workflow's "Resolve component" step read the new row directly — no edit.
- The train's **changelog file is created on the first drafting run**
  (`merge-unreleased.sh` lays down the Keep a Changelog preamble if the file is
  missing). You may pre-create it by hand for a tidier first pull request, but you
  do not have to.

## Verify

- **Commit convention:** make a commit under a new scope and confirm
  `tools/commit-lint/lint-commit-message.sh` accepts it (the local hook and the
  `commit-lint` workflow share it).
- **Release notes:** run `tools/packaging/release-notes.sh <id> <prefix>0.0.0 HEAD`
  locally; it should list that train's commits and nothing from the other trains.
- **Packing:** the [`release-dryrun`](workflows/release-dryrun.en.md) workflow packs
  on every PR; confirm it packs the new train's project(s). Or run
  `tools/packaging/pack.sh 0.0.0-dry.1 <id>` locally.
- **Changelog:** once merged to the default branch, dispatch the
  [`changelog`](workflows/changelog.en.md) workflow for the new component and review
  the drafted pull request.

## Related

- [`tools/trains.sh`](../tools/trains.sh) — the single source of truth this runbook
  is built around.
- [`changelog`](workflows/changelog.en.md) and [`release`](workflows/release.en.md)
  workflow pages.
- [`CONTRIBUTING.md`](../CONTRIBUTING.md) — the Conventional Commit scopes.

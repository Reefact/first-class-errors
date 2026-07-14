# Catalog Versioning

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./CatalogVersioning.fr.md)

An error code does not stay inside the system that emits it. Client applications branch on it, dashboards alert on it, support procedures reference it. Removing or renaming a code is therefore a **breaking change** — of the same nature as removing a public API member — and it deserves the same guardrail: a committed reference, and a CI step that fails when the contract drifts by accident.

FirstClassErrors provides that guardrail through two commands: `fce catalog update` and `fce catalog diff`.

## 🧾 The contract snapshot

The unit of comparison is the **canonical snapshot**: a small, deterministic JSON projection of the catalog containing only what constitutes the contract.

| Tracked | Role |
| --- | --- |
| `code` | The identity of the error. Its removal is breaking. |
| `context` (key name + value type) | The structured data attached to occurrences. Log pipelines and dashboards read these by name; a removal or a type change is breaking. |
| `title`, `source` | Documentation identity — changes are reported as informational, and matching titles are used to hint at probable renames. |

Messages, explanations, business rules and diagnostics are deliberately **not** tracked: they are documentation, extracted from live examples, and free to evolve without touching the contract.

The snapshot is independent of the renderer: whether you publish the human-facing catalog as HTML, Markdown, JSON or a custom format, the same contract file drives versioning. It is deterministic — errors ordered by code, context keys by name, pinned line endings — so the committed file never depends on the machine that produced it. The `fce catalog` commands always extract it under the `en` culture, so the baseline stays culture-independent even when your catalog is localized (see [Internationalization](Internationalization.en.md)).

## 📌 Create the baseline

```bash
fce catalog update --solution MyApp.sln
```

This extracts the catalog, projects it into its snapshot, and writes `errors-baseline.json` (override with `--baseline`, or set `baseline` in `fce.json`). **Commit this file**: it is the accepted contract, and every change to it goes through code review like any other contract change.

## 🔍 Detect drift in CI

```bash
fce catalog diff --solution MyApp.sln
```

The command extracts the current catalog, compares it against the baseline, and writes a report to standard output. Its exit code is designed for pipelines:

| Exit code | Meaning |
| --- | --- |
| `0` | No change at or above the `--fail-on` threshold. |
| `2` | The contract drifted: at least one change reaches the threshold. |
| `1` | Execution error — a missing baseline, a failed extraction, or a baseline written by a newer schema (see below). |
| `130` | Cancelled before completing (Ctrl+C). |

`--fail-on` selects the policy: `breaking` (default), `any` (any change at all fails, including additions), or `none` (report only). `--report` selects the output: `text` (default), `markdown` (ready to post as a pull-request comment) or `json` (for tooling).

## 🧮 How changes are classified

| Change | Impact |
| --- | --- |
| Error code removed | 💥 Breaking |
| Context key removed | 💥 Breaking |
| Context key value type changed | 💥 Breaking |
| Error code added | ✅ Compatible |
| Context key added | ✅ Compatible |
| Title or source changed | ℹ️ Informational |

A **rename** is a removal plus an addition — and stays breaking, because consumers know the old code. When exactly one added error shares the removed error's title, the report adds a hint: *"possibly renamed to 'NEW_CODE', which has the same title"*.

## ✍️ Accepting a change deliberately

When a contract change is intentional, refresh the baseline and commit it:

```bash
fce catalog update --solution MyApp.sln
```

The command summarizes what it absorbs (`1 breaking, 2 compatible and 0 documentation change(s) accepted`), and the pull request then shows the baseline diff — a removed code appears as a removed line. The accident becomes impossible; the deliberate change becomes visible and reviewable. This is the same discipline as a public-API baseline file, applied to the error catalog.

## 🛡️ Baseline resilience & schema versioning

The baseline is a checked-in file, so over a project's life it can be corrupted by a bad merge or produced by a different version of the tool. `fce catalog update` handles each case deliberately rather than silently:

* **A corrupt or unreadable baseline is regenerated, never fatal.** Updating is exactly how a baseline is (re)built, so an existing file that cannot be parsed is rewritten from the current catalog with a warning — a broken baseline never blocks you.
* **A baseline written by a _newer_ schema is refused, never downgraded.** Every snapshot carries a `schema` version. If a teammate committed a baseline with a newer tool, an older tool will not overwrite it with an older schema — that would silently drop information — so it stops with an error telling you to upgrade. `fce catalog diff` refuses it the same way. Upgrading the tool, or aligning versions across the team, resolves it.

So `fce catalog update` exits `0` when the baseline is created, already up to date, or refreshed (including a self-healed one); `1` on an execution error or a newer-schema baseline; and `130` if cancelled.

## ⚙️ Snapshots without a baseline

Two more ways to produce and compare snapshots:

* `fce generate --snapshot <path>` also writes the canonical snapshot next to whatever format you render — one generation, both the human catalog and the contract file. It reflects the render `--language`; when that is not English the command warns, because a committed baseline should stay culture-independent — use `fce catalog update` (or `--language en`) for that.
* `fce catalog diff --against <path>` compares the baseline against a snapshot **file** instead of extracting from the source — useful for comparing two release artifacts.

Both `baseline` and `snapshot` can be set in `fce.json` so the paths need not be repeated on every run.

## 🚦 Wiring it into CI/CD: a complete example

The goal of the CI integration is simple: **contract drift must be visible where the change is reviewed** — in the pull request itself, not in a log nobody reads. The loop looks like this:

1. Every pull request runs `fce catalog diff` against the committed baseline.
2. **No change** → the job passes silently.
3. **Compatible or documentation changes** → the job still passes (with the default `--fail-on breaking`); the report can be posted for awareness.
4. **Breaking change** → exit code `2` fails the job, and the Markdown report lands as a pull-request comment. The author then has exactly two honest ways out: fix the accidental removal, or accept it deliberately with `fce catalog update` — in which case the reviewer sees the baseline diff (the removed code appears as a removed line) and approves a breaking change *knowingly*.

Walked through on a concrete scenario: a developer renames `PAYMENT.DECLINED` to `PAYMENT.REFUSED` while refactoring. Without the guardrail, the rename ships silently and every dashboard and client branching on the old code breaks in production. With it, the pull request fails with:

```
Breaking changes (1):
  - [removed] PAYMENT.DECLINED — error removed (possibly renamed to 'PAYMENT.REFUSED', which has the same title)
Compatible changes (1):
  - [added] PAYMENT.REFUSED — new error 'Payment declined' (source: Payment)
```

If the rename was accidental, the developer reverts it. If it was deliberate, they run `fce catalog update`, commit `errors-baseline.json`, and the contract change becomes an explicit, reviewable part of the pull request.

### GitHub Actions

```yaml
name: error-catalog

on:
  pull_request:
    branches: [main]

jobs:
  catalog-diff:
    runs-on: ubuntu-latest
    permissions:
      pull-requests: write # needed to post the report as a comment
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Make fce available on the runner (dotnet tool install, a cached
      # build from source, or your internal distribution).

      - name: Compare the catalog against the baseline
        run: fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
        # Exit code 2 (breaking change) fails this step, and therefore the job.

      - name: Post the report on the pull request
        if: failure() # comment only when the contract drifted
        run: gh pr comment ${{ github.event.pull_request.number }} --body-file catalog-diff.md
        env:
          GH_TOKEN: ${{ github.token }}
```

Two behaviors worth noting: the report is generated *before* the step fails (the redirection captures standard output, the exit code fails the step afterwards), and the comment step runs only `if: failure()` — a quiet pipeline stays quiet.

### GitLab CI

```yaml
error-catalog:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    # Make fce available on the runner (dotnet tool install, a cached
    # build from source, or your internal distribution).
    - fce catalog diff --solution MyApp.sln --report markdown > catalog-diff.md
  artifacts:
    when: always # keep the report even when the job fails on a breaking change
    paths:
      - catalog-diff.md
```

The exit code drives the job result exactly as on GitHub; `artifacts: when: always` keeps the report downloadable from the failed pipeline, and it can be posted on the merge request with a call to the [notes API](https://docs.gitlab.com/ee/api/notes.html) if you want the comment automation too.

### Beyond pull requests

Running the same `fce catalog diff` on the main branch (on push or on a schedule) catches drift that bypassed the pull-request flow, and `fce catalog diff --against` lets a release pipeline compare two published snapshots — for example, the snapshot shipped with the previous release against the candidate one — to generate release notes for the error contract.

---

<div align="center">
<a href="OperationalIntegration.en.md">← CI/CD and Operational Integration</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="ArchitectureOfTheDocumentationPipeline.en.md">Architecture of the Documentation Pipeline →</a>
</div>

---

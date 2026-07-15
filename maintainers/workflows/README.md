# CI/CD workflow reference

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

↑ Part of the [maintainer documentation](../README.md).

> Maintainer documentation. This describes the GitHub Actions workflows that
> build, check, and publish FirstClassErrors. It is **not** part of the
> library's user documentation under `doc/`.

## What this is

Each workflow under [`.github/workflows/`](../../.github/workflows/) carries a
fair amount of intent that is easy to break by "cleaning it up": a permission
that is narrow on purpose, a step ordering that guards a specific failure, a
version that is frozen for a product reason. The workflow files themselves hold
the line-by-line rationale in comments — those comments are the source of truth
closest to the code. **These pages are the pedagogical layer above them:** what
each workflow is *for*, when and how it runs, and the handful of things you must
not change without understanding why.

Read the page for a workflow before you touch it. When the page and the YAML
disagree, the YAML wins — and the page should be corrected.

## The cross-cutting conventions

A few decisions are shared by (almost) every workflow. They are documented once
here instead of being repeated on every page.

- **Actions are pinned by commit SHA, not by tag.** A tag like `@v4` can be
  moved by its owner to point at new code; a 40-hex SHA cannot. Every `uses:`
  therefore pins a SHA with the human-readable tag in a trailing comment
  (`# v4`). When you bump an action, change **both**. Dependabot's
  `github-actions` ecosystem proposes these bumps.
- **`permissions:` start read-only and widen per job.** The workflow-level block
  is the least privilege the workflow needs (usually `contents: read`); a job
  that must write something (upload SARIF, publish a release, enable auto-merge)
  re-declares a `permissions:` block that adds *only* that scope. Never widen the
  top-level block to satisfy one job.
- **Every job sets `timeout-minutes`.** The GitHub default is six hours; a hung
  step would otherwise hold a runner for that long. Each cap is set a few times
  the observed run time, noted in a comment next to it.
- **`concurrency` cancels superseded runs.** Pushing twice to the same branch or
  PR cancels the in-flight run. The one exception is `release`, which sets
  `cancel-in-progress: false` — you never want to cancel a half-finished
  publish.
- **Security scanners also run weekly on a `schedule`.** `codeql` and
  `scorecard` re-run against unchanged code so newly shipped queries/checks are
  applied even when nothing was pushed.
- **Forks cannot read secrets.** Workflows that need a secret (e.g. `sonar`)
  detect a fork PR and skip rather than fail; GitHub does not expose repository
  secrets to a PR raised from a fork.
- **Required checks are the real gate.** Several workflows (`dependency-review`,
  `dependabot-automerge`) only *signal* or *enable* — they merge nothing on their
  own. What actually blocks a bad merge is the branch-protection / ruleset
  configuration on `main` marking these checks as **required**. That is a
  repository setting, not something a workflow can enforce for itself.

## The workflows

### Build & quality

| Workflow | Purpose |
| --- | --- |
| [`ci`](ci.en.md) | Build and test the whole solution on Linux and Windows, with coverage. The primary gate. |
| [`sonar`](sonar.en.md) | SonarQube Cloud analysis — quality gate and coverage reporting. |
| [`analyzers`](analyzers.en.md) | Dogfood the bundled Roslyn analyzers, including on the oldest supported compiler (the Roslyn floor). |
| [`commit-lint`](commit-lint.en.md) | Enforce the Conventional Commits convention on every PR commit, using the same script as the local hook. |

### Security & supply chain

| Workflow | Purpose |
| --- | --- |
| [`codeql`](codeql.en.md) | GitHub CodeQL static analysis for C#, results on the code-scanning dashboard. |
| [`dependency-review`](dependency-review.en.md) | Block a PR that introduces a known-vulnerable dependency. |
| [`scorecard`](scorecard.en.md) | OpenSSF Scorecard — scores the repo's security posture and powers the README badge. |

### Release

| Workflow | Purpose |
| --- | --- |
| [`release`](release.en.md) | Build, attest, and publish the NuGet packages on a version tag (with a manual dry run). |
| [`release-dryrun`](release-dryrun.en.md) | Continuously rehearse the side-effect-free part of the release (pack + SBOM) on every PR and push. |
| [`changelog`](changelog.en.md) | Draft the `[Unreleased]` section of a train's changelog from merged PRs, on manual dispatch, and open a review PR. |

### Dependency maintenance

| Workflow | Purpose |
| --- | --- |
| [`dependabot-automerge`](dependabot-automerge.en.md) | Enable auto-merge on Dependabot patch/minor updates; leave majors for a human. |

## Related maintainer docs

- [Release dry run (manual)](../ReleaseDryRun.en.md) — the operational guide to
  the manual `release` dispatch dry run.
- [ADR 0001 — Lock the analyzer Roslyn floor](../adr/0001-lock-the-analyzer-roslyn-floor.md)
  — why the analyzer's Roslyn version is frozen, which the `analyzers` workflow
  enforces.
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md) — the commit and PR conventions the
  `commit-lint` workflow checks.

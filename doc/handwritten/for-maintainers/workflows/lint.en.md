# `lint` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](lint.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/lint.yml`](../../../../.github/workflows/lint.yml)

## What it is for

It statically analyses the files the C# compiler never sees: the POSIX shell
scripts under `tools/` and `.claude/hooks/`, and the workflow definitions in
`.github/workflows/` themselves.

Every other analysis in this repository runs **inside a build** — the Roslyn
analyzers, the explicit-type rule ADR-0055 restated in `.editorconfig`, the
warning ratchet in `Directory.Build.props` — so a contributor meets it at the
moment they write the code. Shell and YAML had no such moment. The only thing
reading them was the [`sonar`](sonar.en.md) scan, which reports **after** the
merge and enforces nothing: its job is green as soon as the analysis uploads,
whatever the Quality Gate says. Two findings typed VULNERABILITY reached `main`
that way, and 21 shell findings accumulated unseen.

This workflow closes that gap with tools that run on our own runners, so the
signal arrives before the merge and does not depend on a third-party service
being reachable.

## When it runs

- On every **push to `main`**.
- On every **pull request targeting `main`**.
- On demand via **`workflow_dispatch`**.

## How it runs

One job, `Lint scripts and workflows`, on Linux:

1. **shellcheck** over every `*.sh` in the repository. It ships preinstalled on
   the runner image, so there is nothing to fetch and no third-party action in
   the supply chain.
2. **actionlint** over `.github/workflows/`. It checks what YAML alone cannot:
   `${{ }}` expression types, action inputs against each action's own schema,
   `needs` and matrix references, cron syntax, and — through an embedded
   shellcheck — the shell of every `run:` block.

## Permissions & security

`contents: read`, declared **on the job** rather than at workflow level, so a job
added later inherits nothing it did not ask for (this is Sonar
`githubactions:S8264`, and the reason the two mutation workflows were changed the
same way).

actionlint is fetched as a **pinned release tarball verified by SHA-256**, not
run through a third-party action: an unpinned action is what OpenSSF Scorecard's
Pinned-Dependencies check counts against this repository. The version and the
checksum sit next to each other in the workflow and are bumped together.

## Handle with care

- **The bar is zero findings, `info` included.** The tree is clean at that bar,
  so anything new is genuinely new. A lower bar would let `info` accumulate
  exactly the way the Sonar report did — which is the problem this workflow
  exists to prevent, not to reproduce.
- **False positives are annotated in place, never disabled globally.** Three
  patterns are silenced with an inline `# shellcheck disable=` naming its reason:
  `SC2016` where a `printf` format carries Markdown backticks (read as command
  substitution), and `SC2317` on the two hook functions reached through the
  `"rule_${rule}"` dispatch shellcheck cannot follow. A repository-wide
  `.shellcheckrc` would blind the rules everywhere, including where they are
  right.
- **The scripts are `#!/bin/sh`, and shellcheck applies the POSIX dialect.**
  That is deliberate: `local`, arrays and `[[` are not available on the shells
  these run on, and the POSIX rules the scripts are held to are a recorded
  decision (ADR-0060).
- **actionlint audits correctness, not security posture.** It does not flag
  over-broad permissions, spoofable actor checks or dangerous triggers — the very
  class that produced this repository's two VULNERABILITY findings. A dedicated
  auditor (`zizmor`) covers that and is a separate decision, not something this
  workflow quietly provides.
- **This check only helps if it is required.** As with the other quality checks,
  it blocks a merge only when branch protection on `main` marks it **required**.

## Related

- [`sonar`](sonar.en.md) — the analysis this workflow brings forward. It stays
  the reporting and coverage view; it is not, and was never, an enforcement gate.
- [`ci`](ci.en.md) — where the warning ratchet enforces the equivalent bar on the
  C# side.

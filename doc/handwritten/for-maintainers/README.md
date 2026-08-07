# Maintainer documentation

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

> Documentation for **maintainers and operators** of FirstClassErrors — how the
> project is built, released, and kept healthy. It is **not** part of the
> library's user documentation, which lives under [`doc/`](../for-users/). The English
> version is canonical; the French pages are kept in sync.

## Contents

### [CI/CD workflow reference](workflows/README.md)

A page per GitHub Actions workflow — what it is for, when and how it runs, its
permissions, and the non-obvious decisions you must not change without
understanding why. Start at the [index](workflows/README.md); it also documents
the cross-cutting conventions (SHA-pinned actions, least-privilege permissions,
per-job timeouts, required checks as the real gate).

### [Release dry run (manual)](ReleaseDryRun.en.md)

The operational runbook for the manual `release` dispatch dry run: how to launch
it, what it touches (and what it deliberately does not), and when to use it. It
complements the workflow reference's [`release`](workflows/release.en.md) and
[`release-dryrun`](workflows/release-dryrun.en.md) pages, which describe those
workflows structurally. Also in [French](ReleaseDryRun.fr.md).

### [Adding a release train](AddingAReleaseTrain.en.md)

The checklist for adding a new independently-versioned package: the single data
edit in [`tools/trains.sh`](../../../tools/trains.sh) and the static edits GitHub and the
tooling force (tag trigger, choice options, commit-lint scopes, packing). Also in
[French](AddingAReleaseTrain.fr.md).

### [Architecture Decision Records](adr/README.md)

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: it is superseded by a newer ADR, not
edited in place. The [index](adr/README.md) defines the format every ADR
follows and provides a copy-ready [template](adr/template.md). *(English only.)*

- [ADR-0001 — Lock the analyzer's Roslyn floor](adr/0001-lock-the-analyzer-roslyn-floor.md)
  — why the analyzer's Roslyn version is frozen, enforced by the
  [`analyzers`](workflows/analyzers.en.md) workflow.
- [ADR-0002 — Floor the tooling runtime at the oldest supported LTS](adr/0002-floor-the-tooling-runtime.md)
  — why the tooling targets `net8.0` and rolls forward, enforced by the
  `floor` job of the [`ci`](workflows/ci.en.md) workflow.

## Related

- [`CONTRIBUTING.md`](../../../CONTRIBUTING.md) — commit and pull-request conventions
  (enforced by the [`commit-lint`](workflows/commit-lint.en.md) workflow).

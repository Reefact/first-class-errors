# Maintainer documentation

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

> Documentation for **maintainers and operators** of FirstClassErrors — how the
> project is built, released, and kept healthy. It is **not** part of the
> library's user documentation, which lives under [`doc/`](../doc/). The English
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

### [Architecture Decision Records](adr/)

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: it is superseded by a newer ADR, not
edited in place.

- [ADR 0001 — Lock the analyzer Roslyn floor](adr/0001-lock-the-analyzer-roslyn-floor.md)
  — why the analyzer's Roslyn version is frozen, enforced by the
  [`analyzers`](workflows/analyzers.en.md) workflow. *(English only.)*

## Related

- [`CONTRIBUTING.md`](../CONTRIBUTING.md) — commit and pull-request conventions
  (enforced by the [`commit-lint`](workflows/commit-lint.en.md) workflow).

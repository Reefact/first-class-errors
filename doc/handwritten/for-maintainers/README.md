# Maintainer documentation

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](README.fr.md)

> Documentation for **maintainers and operators** of FirstClassErrors — how the
> project is built, released, and kept healthy. It is **not** user documentation.
> The English version is canonical and the French pages are kept in sync.

## Contents

### [Maintainer specifications](specifications/README.md)

Mutable references for the current technical and operational implementation of
accepted architecture decisions: platform compatibility, ADR review, Request
Binder contracts, GenDoc catalog versioning, and Dummies generation. Change these
when mechanics evolve without changing the underlying decision.

### [CI/CD workflow reference](workflows/README.md)

A page per GitHub Actions workflow — purpose, triggers, permissions, exact
structure, and non-obvious constraints. The index also documents cross-cutting
workflow conventions.

### [Release dry run](ReleaseDryRun.en.md)

The operational runbook for the manual release dry run. It complements the
[`release`](workflows/release.en.md) and
[`release-dryrun`](workflows/release-dryrun.en.md) workflow references.

### [Adding a release train](AddingAReleaseTrain.en.md)

The checklist for adding an independently versioned package and updating the
static packaging, tag, choice, and commit-lint surfaces around `tools/trains.sh`.

### [Architecture Decision Records](adr/README.md)

Dated records of significant decisions — what was chosen, why, the alternatives,
and the consequences. Current implementation detail belongs in the specification
references above. Accepted ADRs are normally immutable; the one-time editorial
migration is recorded by [ADR-0023](adr/0023-extract-specifications-from-accepted-adrs.md).

## Related

- [`CONTRIBUTING.md`](../../../CONTRIBUTING.md) — commit and pull-request conventions.

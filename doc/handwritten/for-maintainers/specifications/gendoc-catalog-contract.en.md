# GenDoc catalog contract specification

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](gendoc-catalog-contract.fr.md)

This page describes how GenDoc's own first-class errors are versioned as part of
the `cli` release train, implementing
[ADR-0009](../adr/0009-report-the-toolings-failures-as-first-class-errors.md)
and [ADR-0010](../adr/0010-treat-gendocs-error-catalog-as-a-versioned-contract.md).

## Contract boundary

GenDoc's `GENDOC_` codes, public messages, context-key names and context-key types
are emitted by the `fce` tool. They therefore belong to the public compatibility
surface of the `cli` train even though GenDoc is not published as a standalone
NuGet package.

The existing catalog diff classification is authoritative:

* removing or renaming a code is breaking;
* removing a context key or changing its type is breaking;
* additive compatible changes and informational documentation changes follow the
  classifications produced by `fce catalog diff`.

## Baseline lifecycle

1. A committed baseline represents the catalog from the last successfully
   published `cli` release.
2. Pull-request documentation generation compares the current catalog with that
   baseline and exposes the pending impact for review.
3. The baseline does not advance during ordinary development.
4. After a successful `cli` publication, the release automation regenerates the
   baseline from the shipped state and commits that result to `main`.

The baseline must be generated only through `fce catalog update`; hand-editing it
invalidates the measured contract.

## Release gate

At release time, the workflow runs `fce catalog diff` against the committed
baseline. When the diff is breaking, the candidate `cli` version must carry a
major bump. The gate does not forbid breaking development; it forbids publishing
that break under a compatibility-preserving version.

If package publication succeeds but the baseline update cannot be pushed, the
release is already real and the baseline remains stale. The operator must restore
the baseline from the published catalog before the next release; rerunning a
publish step must not create a duplicate package version.

## Sources of truth

* `FirstClassErrors.GenDoc` documented error factories — current catalog.
* The committed GenDoc baseline under `doc/generated/` — last released contract.
* `.github/workflows/gendoc-docs.yml` — pull-request generation and diff reporting.
* `.github/workflows/release.yml` — version gate, publication order, and baseline
  advancement.
* [Catalog Versioning Reference](../../for-users/CatalogVersioningReference.en.md)
  — command semantics and compatibility classifications.

A change to what counts as breaking or to which release train owns the catalog
requires an ADR. Workflow rewiring that preserves those rules updates this page
and the workflow references.

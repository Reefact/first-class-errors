# ADR-0010 | Treat GenDoc's error catalog as a versioned contract

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0010-treat-gendocs-error-catalog-as-a-versioned-contract.fr.md)

**Status:** Accepted
**Date:** 2026-07-17
**Decision Makers:** Reefact

## Context

ADR-0009 established that `FirstClassErrors.GenDoc` documents its own failures as
first-class errors, giving each a stable `GENDOC_`-prefixed code and a structured
context. Those codes and context keys are emitted to callers at runtime and are
the identities external consumers — CI pipelines, integrators, support — match on.

GenDoc has no NuGet package of its own: it ships bundled inside the `fce` tool,
released on the `cli` train (`tools/packaging/pack.sh`). A change to GenDoc's own
error catalog — a code renamed or removed, a context key dropped or retyped —
is therefore a change to what the `cli` package emits, indistinguishable, from the
outside, from any other compatibility change of that package.

The library already ships the mechanics to treat a catalog as a versioned
contract: `fce catalog update` records a baseline snapshot, `fce catalog diff`
compares against it, and the comparison classifies each change as Breaking,
Compatible, or Informational (a removed code or a removed/retyped context key is
Breaking). Until now these were offered to consumers documenting their own errors,
but never applied to GenDoc's own catalog: nothing recorded GenDoc's baseline, and
nothing checked a change to it against the version the `cli` train was about to
publish. The two trains follow semantic versioning, and the repository already
enforces Conventional Commits, but neither mechanism connects a breaking change of
the *error catalog* to the *version number* that ships it.

## Decision

A breaking change to GenDoc's own error catalog, measured by `fce catalog diff`
against a committed baseline, requires a major version bump of the `cli` train,
enforced at release time.

## Rationale

* **The failure surface is a published contract, so it must be versioned like
  one.** ADR-0009 made GenDoc's codes stable identities that consumers depend on;
  a stable identity that can silently disappear under a compatible-looking version
  bump is not actually stable. Semantic versioning is the promise the `cli`
  package already makes, and a removed or renamed code is exactly the kind of
  break that promise exists to signal.
* **Enforce at the release, not at the pull request.** A breaking change to an
  error catalog is not wrong in itself — an intentional one is precisely what a
  major version is for. Only shipping it *silently*, under a version that promises
  compatibility, is the failure. Gating pull requests would fight normal
  incremental development; gating the release targets the single point where the
  compatibility promise is actually made, and leaves day-to-day work unblocked.
* **Compare against the last release, not a moving target.** The baseline advances
  only when a `cli` release publishes. Between releases it stays fixed, so the
  diff always answers "what changed since the last thing actually shipped" —
  which is the question the version number must answer — regardless of how many
  pull requests landed in between.
* **Reuse the existing contract mechanics, add no new judgement.** `fce catalog
  diff`'s Breaking classification is already defined and tested; this decision
  wires it to the release version rather than inventing a second notion of what
  "breaking" means for the tool's own errors.

## Alternatives Considered

### Leave it to Conventional Commits and reviewer discipline

Considered because the repository already requires a `!`/`BREAKING CHANGE:`
marker on breaking commits, checked in CI. Rejected because that marker is
authored by hand from the commit's intent, while a catalog break can be an
unintended side effect (a refactor that drops a context key); nothing tied the
marker to a mechanical measurement of the catalog, so a silent break could still
ship under a minor bump.

### Gate the pull request instead of the release

Considered because it surfaces the break earliest. Rejected because a breaking
change is legitimate mid-development as long as the eventual release carries the
major bump; blocking it per pull request would penalize normal iteration and
force premature version decisions, while the release gate catches the same break
at the only moment it actually matters.

### Publish GenDoc as its own package on its own train

Considered because a dedicated train would let the catalog version independently.
Rejected as disproportionate: GenDoc has no standalone consumer (it runs only
inside `fce`), and ADR-0002's tooling model deliberately keeps it bundled; a new
train would add release machinery for no consumer benefit.

## Consequences

### Positive

* A breaking change to GenDoc's own errors cannot ship under a non-major `cli`
  version: the release fails until the version or the change is reconciled.
* The catalog gains a committed baseline and a per-pull-request diff report, so
  the pending compatibility impact is visible during review.
* The living documentation the CI regenerates is backed by an explicit,
  release-anchored contract rather than a best-effort snapshot.

### Negative

* Releasing the `cli` train now depends on a committed baseline and a diff step;
  a maintainer must understand that accepting a breaking change means bumping the
  major version (or reverting), not overriding the gate.
* The baseline is refreshed by a direct push to `main` after a successful
  release — one automated write outside the normal pull-request flow, scoped to
  that moment.

### Risks

* A stale or hand-edited baseline could mis-measure a change. Mitigation: the
  baseline is only ever written by `fce catalog update`, run by the release after
  a real publish, so it always reflects the last shipped catalog.

## Follow-up Actions

* None beyond the workflow wiring itself (`gendoc-docs.yml` and the `release.yml`
  gate); the mechanism lives in the workflows and the `fce catalog` commands the
  reference documentation already covers.

## References

* ADR-0009 — GenDoc modeling its own failures as first-class errors, the codes
  this contract versions.
* ADR-0002 — the tooling runtime model that keeps GenDoc bundled in the `cli`
  train rather than shipping standalone.
* Issue [#167](https://github.com/Reefact/first-class-errors/issues/167) — the
  request this decision answers.
* [Catalog Versioning Reference](../../for-users/CatalogVersioningReference.en.md)
  — the `fce catalog update`/`diff` mechanics reused here.

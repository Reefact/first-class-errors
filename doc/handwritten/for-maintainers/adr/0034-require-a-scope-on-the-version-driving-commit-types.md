# ADR-0034 | Require a scope on the version-driving commit types

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0034-require-a-scope-on-the-version-driving-commit-types.fr.md)

**Status:** Accepted
**Date:** 2026-07-23
**Decision Makers:** Reefact

## Context

The repository publishes several independently-versioned packages grouped into **release trains**; `tools/trains.sh` is the single source of truth mapping each Conventional Commit scope to a train (`lib`, `cli`, `dum`). A train's GitHub Release notes and changelog are generated (`tools/packaging/release-notes.sh`, `tools/changelog/collect-prs.sh`) by selecting the commits whose scope belongs to that train. A commit with no scope belongs to no train and is excluded. That exclusion is intended: infrastructure commits (`ci`, `chore`, `build`, `docs`) carry no scope and should not appear in a package's release record.

The commit convention (CONTRIBUTING.md) makes the scope syntactically optional on every type. The two **version-driving** types are `feat` (drives `MINOR`) and `fix` (drives `PATCH`); by definition they denote a change visible to the consumer of a package. `commit-lint` — one script shared by the local `commit-msg` hook and the CI check — validates the header shape and, until now, accepted a `feat`/`fix` with no scope.

The consequence has already been observed and documented (issue #231, PR #292): a contributor following the letter of the convention can write an unscoped, user-facing `feat:`/`fix:`, which then silently disappears from both the release notes and the changelog. The change ships in the binaries yet never appears in the release record, with no error at any point.

## Decision

`commit-lint` requires a Conventional Commit scope on the two version-driving types, `feat` and `fix`, and rejects an unscoped one — identically at the `commit-msg` hook and in the CI check — while every other type keeps the scope optional.

## Rationale

The release record is partitioned solely by scope, so on the version-driving types the scope is not a readability aid but the routing key that decides whether a consumer-visible change is recorded at all. A `feat`/`fix` without it is not merely less readable — it is dropped from the public account of what shipped, silently.

Documentation alone cannot close that gap. The rule already stated in CONTRIBUTING depends on the author recalling an invisible consequence at the moment of writing the commit; the failure produces no signal and only surfaces later as a hole a user notices. Moving the rule into `commit-lint` turns a remembered convention into an enforced invariant, caught at write time and — for a bypassed local hook — again in CI, the same layered enforcement the convention already relies on for its other rules.

Confining the requirement to `feat` and `fix` is what keeps it correct rather than merely strict: only those two types drive a version and denote consumer-visible change. The non-version-driving types are exactly the infrastructure and repository-wide work that legitimately belongs to no train, so requiring a scope there would be noise and would misrepresent repository work as component work.

The cost — rejecting some messages that are valid today — is bounded and self-correcting: the author adds a scope the moment the hook or CI flags it, guided by a message that names the consequence. The header grammar and the shared hook/CI wiring are implementation, in `tools/commit-lint/lint-commit-message.sh` and the commit-lint workflow — not here.

## Alternatives Considered

### Keep documenting the rule, do not enforce it (the state after #231)

Considered because #231 / #292 already corrected the documentation, so the convention now states the requirement in prose. Rejected because a prose rule against a silent, invisible failure relies on the author remembering it exactly when nothing signals the mistake; the release record can still lose a user-facing change, which is the harm the rule exists to prevent.

### Require a scope on every type

Considered because a single, uniform "always scope" rule is the simplest to state and to implement. Rejected because the non-version-driving types are the legitimate unscoped case — infrastructure, repository-wide documentation, ADRs, the `FirstClassErrors.Usage` samples — and forcing a scope there would be noise and would falsely attribute repository work to a component and its train.

### Change the release tooling to keep unscoped feat/fix instead

Considered because the drop happens in `release-notes.sh` / `collect-prs.sh`, which could instead route an unscoped version-driving commit into a default train. Rejected because there is no correct default train for a scopeless consumer-visible change: the scope is precisely what identifies which package changed, so guessing would file the change under the wrong package's release record — a subtler, harder-to-notice error than refusing the ambiguous commit at the source.

## Consequences

### Positive

* A user-facing `feat`/`fix` can no longer vanish from the release notes or the changelog; the release record is complete by construction.
* The rule the convention already documents becomes enforced — at write time and in CI — closing the gap #231 could only describe.
* Infrastructure and repository-wide work stays scope-free, so the enforcement adds no friction where a scope would be meaningless.

### Negative

* Commit messages valid today (`feat: …` / `fix: …` with no scope) are now rejected; contributors must add a scope.
* The convention gains one type-dependent rule (scope required for `feat`/`fix`, optional otherwise) in place of a single uniform "scope optional", a small increase in the rule's surface.

### Risks

* A genuinely cross-cutting or sample-only user-facing change might have no natural single scope. Mitigation: a change crossing several components already carries all their scopes (the convention requires the full comma-separated list); a change that truly belongs to no component is, by definition, not a component `feat`/`fix` and takes a non-version-driving type.
* History and in-flight branches created before adoption may contain unscoped `feat`/`fix`. Mitigation: the rule applies from adoption on, to new commits, consistent with CONTRIBUTING → "Adoption"; prior history is not rewritten and merge commits stay exempt.

## Follow-up Actions

* Enforce the requirement in `tools/commit-lint/lint-commit-message.sh` (done in the implementing pull request).
* State the enforced rule in CONTRIBUTING.md and its French translation (done in the implementing pull request).
* No release-tooling change is needed: the partition semantics are unchanged; only the linter gains the requirement.

## References

* Issue [#293](https://github.com/Reefact/first-class-errors/issues/293) — this decision.
* Issue [#231](https://github.com/Reefact/first-class-errors/issues/231) and PR [#292](https://github.com/Reefact/first-class-errors/pull/292) — surfaced and documented the drift this decision enforces.
* `tools/trains.sh`, `tools/packaging/release-notes.sh`, `tools/changelog/collect-prs.sh` — the scope-based partition and the unscoped-commit drop.
* `tools/commit-lint/lint-commit-message.sh` and `.github/workflows/commit-lint.yml` — where the requirement is enforced.
* CONTRIBUTING.md → "Scope" and "Adoption".

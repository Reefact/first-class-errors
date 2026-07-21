# ADR-0027 | Repair Dependabot pull requests within a risk boundary

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0027-repair-dependabot-pull-requests-within-a-risk-boundary.fr.md)

**Status:** Proposed
**Date:** 2026-07-21
**Decision Makers:** Reefact

## Context

Dependabot opens routine dependency-update pull requests. They frequently fail
checks for low-value, mechanical reasons — an over-long `bump …` commit header, a
bumped analyzer newly promoting a warning to an error, a small library API change,
a branch fallen behind `main` — or for reasons unrelated to the update, such as a
secret that a Dependabot-triggered run cannot read.

The repository already runs a model (Claude) from CI through direct API calls for
advisory, non-reproducible tasks — drafting the changelog, and the ADR check — and
treats such output as advisory, never as an autonomous merge gate. [ADR-0004](0004-check-every-pull-request-against-the-adr-base.md)
records that the maintainer is the sole authority who merges pull requests and that
agents may analyse and draft but never merge.

Auto-merge is already enabled for patch and minor Dependabot updates once the
required checks pass, with major updates left for a human (the `dependabot-automerge`
workflow).

The repository is security-hardened: actions pinned by commit SHA, least-privilege
tokens, an OpenSSF Scorecard posture. A Dependabot pull request bumps third-party
code that executes during build and test. GitHub sandboxes Dependabot-triggered
runs — a read-only token and no repository secrets — and does not re-trigger
workflows for a push made with the default `GITHUB_TOKEN`.

Fixing a build or analyzer failure requires changing product or test code. Fixing a
mechanical failure requires changing only history or metadata: rewriting a commit
message, retitling the pull request, or rebasing onto `main`.

## Decision

An automated, model-driven triage may apply and push a bounded set of low-risk
repairs to a failing Dependabot pull request — keeping the pull request eligible for
auto-merge only when the repair changes no file contents, and disabling auto-merge
for any repair that changes code — and it never merges a pull request itself.

## Rationale

Removing the mechanical toil is the objective. An advisory comment still leaves the
maintainer to apply every fix by hand, so the triage must be allowed to apply and
push to deliver the benefit the maintainer asked for.

The blast radius must scale with the change. A repair that alters no file contents —
a commit-message rewrite, a retitle, a rebase — is history or metadata only, carries
no new reviewable logic, and is safe to let flow through the auto-merge that already
governs routine updates. A repair that changes code is AI-authored source and must
face human review, so auto-merge is disabled for it. Because a misclassification is
the dangerous case, the trivial-versus-code split is derived from the action the
workflow actually took, not from the model's assertion; a code change therefore
cannot ride through auto-merge by being labelled trivial.

The supply-chain boundary is preserved by refusing to build the bumped dependency in
the privileged context. The repair performs only version-control operations;
validation of the pushed commit is delegated to the ordinary CI run in its own
read-only Dependabot context. This is why the decision deliberately excludes
"verify by building": doing so would execute freshly bumped third-party code while a
write-capable token and an API key are present.

The agent never merges, consistent with ADR-0004. It applies fixes and, for a code
change, removes an auto-merge another workflow may have set; the maintainer and the
required checks remain the gate. Accepting a dedicated, scoped push credential is a
necessary cost, because GitHub will not re-run the checks on a fix pushed with the
default token and a kept auto-merge depends on those checks re-running.

## Alternatives Considered

### Advisory only — comment a suggested patch, never push

Considered because it is the least-privileged option and mirrors the ADR-check
precedent, which only comments.

Rejected because it does not remove the maintainer's per-pull-request effort, which
is the entire motivation: the human would still apply every fix.

### Full autonomy — apply code fixes and let them auto-merge

Considered because it would clear even code-level breakages with no human step.

Rejected because it would merge AI-authored source changes without review,
contradicting ADR-0004 and the repository's human-in-the-loop review culture.

### Verify the fix by building before pushing

Considered because building would catch a bad patch before it reaches the branch.

Rejected because building a freshly bumped dependency with a write-capable token and
the API key present crosses the supply-chain boundary the repository maintains; the
ordinary CI run already validates the pushed commit safely.

## Consequences

### Positive

* Routine Dependabot pull requests reach a green, mergeable state with reduced or no
  maintainer effort.
* Every failing Dependabot pull request receives a consistent, logged triage with a
  clear verdict.
* Trivial fixes continue to merge on their own, as routine updates already do.

### Negative

* A dedicated, scoped push credential must exist and be guarded.
* A trivial fix can merge with no human review — accepted, because it changes no file
  contents.
* Re-validating pushed fixes consumes additional CI runs.

### Risks

* The push credential could be abused if leaked — mitigated by scoping it to
  contents and pull-request write on this repository and using it only for the push.
* The model could mislabel a code change as trivial — mitigated by deriving the
  trivial-versus-code decision from the action actually taken, not the model.
* A repair could loop, or contend with Dependabot's own rebases — mitigated by acting
  at most once per Dependabot push and never changing the dependency version.

## Follow-up Actions

* Provision the scoped push credential so pushed fixes re-run CI.
* Ensure branch protection marks the CI checks as required, so a kept auto-merge
  cannot merge ahead of them.
* Review the triage's behaviour after a few real Dependabot pull requests and adjust
  the action set or the watched workflows if needed.

## References

* [`dependabot-autofix` workflow reference](../workflows/dependabot-autofix.en.md) —
  how the decision is implemented (triggers, guards, the push token, the comment).
* [`dependabot-automerge` workflow reference](../workflows/dependabot-automerge.en.md)
  — the auto-merge this decision keeps or disables.
* [ADR-0004](0004-check-every-pull-request-against-the-adr-base.md) — the maintainer
  is the sole merge authority; model output is advisory.

# ADR-0071 | Refresh an open branch only by rebasing it onto `main`

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0071-refresh-an-open-branch-only-by-rebasing-it.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-10
**Accepted:** 2026-08-10
**Decision Makers:** Reefact

## Context

[ADR-0070](0070-land-pull-requests-by-rebasing-and-keep-main-linear.md) made
`main`'s history linear and made rebase the way a pull request lands. It named
one thing it did not settle, and left it for a separate decision: `CONTRIBUTING.md`
("Branches") offers **two** ways to carry `main`'s progress into an open branch —
rebase it onto `origin/main` while the branch is yours alone, or merge
`origin/main` into it once others may have based work on it.

The second option produces exactly the commits ADR-0070 removed. Of the 401 merge
commits deleted from `main`, **87 were `Merge branch 'main' into …`** — created by
following this rule. They were not free either: 15 of them carried a hand-made
conflict resolution, and reproducing those resolutions was the delicate part of
the rewrite.

The rule also no longer does what it says under the new strategy. It was written
for a merge-commit repository, where a merge performed on a branch stayed on the
branch and only its result reached `main`. With *Rebase and merge*, GitHub
replays the branch's own commits onto `main` — so a branch that merged `main`
into itself carries that merge into what lands. The rule quietly re-imports the
noise the rewrite removed.

What the rule protects is real: rebasing a branch someone else has already pulled
discards their work. But that situation does not exist in this repository's
branch model. `CONTRIBUTING.md` states that a branch carries **one** pull
request, that it is "the disposable workspace of one pull request", and that its
name takes the form `<author>/<short-description>` — the owner is named in the
ref itself. A branch two people build on is already outside the model, and the
merge option is the only rule that assumes otherwise.

## Decision

An open branch is brought up to date with `main` **only by rebasing it onto
`origin/main`**. Merging `origin/main` into a branch is not done.

## Consequences

### Force-push becomes the ordinary way to refresh a branch

It already is, for the branches this repository actually has. `CONTRIBUTING.md`
permits rewriting a branch's history while it is yours alone and requires
`git push --force-with-lease` rather than a bare `--force`; refreshing is that
same operation. Nothing new is allowed, and one alternative is withdrawn.

### A branch someone else has pulled has no escape hatch, by design

If a second person has genuinely based work on an open branch, the answer is to
coordinate the force-push or to split the work into two branches — not to put a
merge commit into `main`'s future history. Making that case visible is the point:
under the previous rule it was resolved silently, by a commit nobody reviewed.

### The rewrite's cost is not re-incurred

ADR-0070 records that rewriting `main` is a one-time exception that closes at
1.0.0. Leaving the merge option open would let the exact commit shape it removed
reappear one branch at a time, with no second rewrite available to clean it up.

## Alternatives Considered

### Keep the merge option, and rely on GitHub to flatten it

Considered because *Rebase and merge* does replay a branch's commits, and one
might expect a merge commit to be dropped in the process. Rejected because the
result is not something to rely on: what a rebase does with a merge commit
depends on the branch's shape, and a rule whose safety depends on the platform's
current flattening behaviour is a rule that breaks silently when that behaviour
changes. The repository's own history is the evidence — 87 such commits reached
`main` under the previous strategy.

### Keep the merge option for genuinely shared branches only

Considered because the protection it offers is real when it applies. Rejected
because it applies to a branch this repository does not have, and because a rule
with an exception is read as a rule with an escape. The existing branch doctrine —
one pull request, one owner, cut fresh, deleted on merge — is what makes the
exception unnecessary; if that doctrine ever changes, this decision should be
revisited with it rather than pre-weakened for a case that does not occur.

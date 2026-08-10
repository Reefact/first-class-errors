# ADR-0070 | Land pull requests by rebasing, and keep `main`'s history linear

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0070-land-pull-requests-by-rebasing-and-keep-main-linear.fr.md)

**Status:** Proposed
**Proposed:** 2026-08-10
**Decision Makers:** Reefact

## Context

`main` carried 1366 commits, 401 of them merges. Of those merges, 314 are the
`Merge pull request #NN` commits GitHub's merge button writes, and 87 are
`Merge branch 'main' into …` commits created when a branch pulled `main` in
mid-review. The 965 remaining commits — 71% — are the ones that changed
something.

The merge commits are not only noise. They cost three concrete things:

* `git log main` interleaves branches that were developed in parallel, so
  reading the history means separating what happened from when branches met.
* `git bisect` and `git blame` walk trees that no one ever wrote: a merge
  commit's tree is a machine's reconciliation of two branches, not a state a
  contributor authored, reviewed or ran.
* The repository's own rules are justified by the merge commit. `AGENTS.md`,
  `CONTRIBUTING.md`, `CLAUDE.md` and the `/tidy-history` command all argue that a
  branch's history must be clean *because the merge commit preserves it*. The
  argument holds, but it names the wrong mechanism.

That last point matters most. This repository already requires each commit to
stand alone, lints every header in CI and asks an agent to tidy a branch before
review. A merge-commit strategy is the one part of the workflow that does not
follow from that doctrine: it preserves the *shape* of a branch — when it
started, when it was refreshed, when it landed — when everything else in the
process is about the *content* of its commits.

### Why now

The library has not reached 1.0.0. Its only tag is `lib-v0.1.0-preview.1`, no
release has been published, and no pull request is open. Nothing outside this
repository depends on a commit identifier: no consumer pins a SHA, no changelog
entry cites one, and no published package carries SourceLink pointing at one.

That is the entire window in which rewriting `main` is cheap, and it closes at
1.0.0. Once a stable version ships, its SourceLink, its release notes and the
references consumers pin make `main`'s identifiers part of what the project
promises; rewriting them then stops being a repository chore and becomes a
breaking change for everyone downstream. Doing it now buys a history the 1.0.0
line can be read against. Doing it later would mean not doing it at all.

## Decision

`main`'s history is **linear**. It has two parts.

**Pull requests land by rebase.** GitHub's *Rebase and merge* becomes the only
enabled strategy on this repository; merge commits and squash merges are
disabled. A pull request's commits are replayed onto `main` as they were
written, and no commit is created to record the landing.

**The existing history is rewritten linear, once.** The 401 merge commits are
removed and the 965 authored commits are replayed in first-parent order,
preserving each one's message, author, committer and dates.

The rewrite is not a plain `git rebase` of the whole history. It walks `main`'s
first-parent chain and, for each pull-request merge, replays that pull request's
commits and then **pins** the resulting tree to the tree the merge commit
actually had. That pinning is what makes the operation safe: 15 of the 401
merges carried a hand-made conflict resolution, and a naive replay would
silently drop it. Pinning restores the exact historical content at every point
where `main` advanced, so any drift is contained inside a single pull request
and can never cross a boundary.

The rewrite is accepted only against evidence, not against intent. The
verification that must pass, and did:

| Check | Result |
| --- | --- |
| Tree of `main`'s tip, before and after | identical |
| Tree at each of the 362 first-parent steps | 362 / 362 identical |
| Authored commits preserved (author, e-mail, date, subject) | 965 / 965 |
| Commit message bodies, compared as a multiset | 0 lost, 0 added |
| Replay conflicts | 0 |
| Merge commits remaining | 0 |
| Build and test suite on the rewritten tip | 974 tests, 0 failures |

The rewrite is a **one-time exception, and this record is not a precedent for a
second one**. From here `main` is append-only: its history is corrected by new
commits, never by rewriting published ones. A later rewrite would need its own
ADR, and would have to answer the objection this one escapes only by timing —
that by then the identifiers belong to the published contract.

## Consequences

### Every commit SHA on `main` changes

This is the price of the decision, and it is paid once. Anyone holding a clone
must re-clone or hard-reset; a clone that pulls will otherwise graft the old
history back on. There are no open pull requests to invalidate and no published
release to break: the only GitHub release is an untagged draft.

### The published history stays reachable

Old SHAs do not disappear. GitHub keeps `refs/pull/NN/head` for every pull
request ever opened, so the commits every merged pull request references remain
resolvable, and links in issues and reviews keep working. A backup ref of the
pre-rewrite `main` is pushed before the rewrite and is what a recovery would use.

### Refreshing a branch by merging `main` into it no longer fits

`CONTRIBUTING.md` ("Branches") currently offers two ways to carry `main`'s
progress into an open branch: rebase while the branch is yours alone, merge
`main` in once others may have based work on it. The second produces exactly the
`Merge branch 'main' into …` commits this decision removes. This ADR does not
resolve that: the branch-refresh rule protects a collaborator's pulled work,
which is a different concern from history shape, and this repository's branches
are in practice single-owner. It is named here so the next person to hit it
knows it is a known edge and not an oversight.

### Two tags move, and 28 signatures are lost

`lib-v0.1.0-preview.1` and `archive/justdummies-adr` are re-pointed at the
equivalent commits of the rewritten history, the latter keeping its annotation,
tagger and date. 28 commits carried a signature; a rewritten commit cannot keep
one, and they become unsigned. No branch protection required signed commits.

### Author dates are no longer monotonic

75 consecutive pairs of commits have a later author date before an earlier one.
That is inherent to a linear history built from work done in parallel, and is
what a rebase strategy would have produced from the start.

## Alternatives Considered

### Switch the strategy going forward and keep the existing merge commits

The cheapest option, and it was rejected on the reason the decision exists.
Half a history is not a readable history: `git log` would still interleave, and
`git bisect` would still walk trees nobody wrote, for the 1366 commits that
represent the whole life of the project so far. The cost of the rewrite is
bounded and paid once; the cost of the noise is paid at every read.

### Squash each pull request into a single commit

Rejected. It produces a linear history mechanically — one commit per pull
request, no replay, no conflict — but it destroys the per-commit discipline this
repository spends real effort enforcing. The 965 authored commits, each with a
linted header and a single intention, would collapse into 314 whose messages are
the pull-request titles. The doctrine says the commit is the unit of the change;
squashing makes the pull request that unit.

### Rewrite with a plain `git rebase --root`

Rejected as unsafe rather than wrong. It would replay the same commits, but it
offers no guarantee about the result: nothing checks that the tree at each
landing matches what `main` actually held, so the 15 hand-resolved merges could
be silently lost and the divergence would surface much later, as a bug with no
apparent cause. The pinning-and-verifying method costs a script and yields a
proof.

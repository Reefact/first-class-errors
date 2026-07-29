# ADR-0055 | Enforce the style rules the compiler can express, and keep the DotSettings authoritative for the rest

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0055-enforce-the-style-rules-the-compiler-can-express.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-28
**Accepted:** 2026-07-29
**Decision Makers:** Reefact

## Context

The repository's code style, naming conventions and inspection severities are recorded in
`FirstClassErrors.sln.DotSettings`, a ReSharper/Rider artifact. Among its rules is a requirement that
types be written explicitly rather than inferred, declared at error severity since the file was
introduced.

That file is read by Rider and by nothing else. No compiler, no CI job, no command-line formatter and
no automated agent editing this repository can parse it. A rule recorded there is therefore enforced
only for as long as a human has the solution open in Rider, and only on the files that human touches.

Contributions to this repository now include automated agents, which edit source directly and cannot
read the file under any circumstance. The explicit-type rule drifted accordingly: 203 violations
accumulated across 17 files — all of them in test projects — while the rule was nominally at error
severity and nothing ever reported one.

Roslyn ships code-style analyzers, configured through `.editorconfig`, that cover a subset of what the
DotSettings expresses. The explicit-type preference is in that subset. Several of the repository's
other style rules are not: the file layout patterns, the column alignment of consecutive declarations,
and the region naming conventions have no Roslyn equivalent and cannot be expressed in `.editorconfig`
at all.

Those analyzers do not run during a build unless a build property enables them. Configuring the rule in
`.editorconfig` alone changes nothing: measured on this repository, a full solution build with the rule
configured and the property unset emits zero diagnostics.

The repository already promotes every compiler warning to an error in CI, so any diagnostic reported as
a warning is blocking on the way in while staying advisory during local iteration.

Two properties of the previous arrangement bear on the decision. The `.editorconfig` carried a header
asserting that it deliberately defined no C# style rules "so the two configurations can never
disagree". And the DotSettings disables EditorConfig support, meaning Rider does not read
`.editorconfig` at all — so the two configurations were already independent, and already disagreed on
at least one point of whitespace hygiene.

The ReSharper engine is also distributed as a command-line tool, which reads the DotSettings directly
and would in principle make the whole configuration executable outside Rider. It was evaluated
empirically before this decision; the measurements are recorded in the referenced pull request.

## Decision

Style rules that Roslyn can express are restated in `.editorconfig` and enforced by the build, while
`FirstClassErrors.sln.DotSettings` remains the source of truth for the rules Roslyn cannot express.

## Rationale

A rule that only an IDE enforces is a rule that only some authors are subject to, and the drift
measures what that costs: 203 violations under a rule already set to error severity. The failure was
not that the rule was unclear or unrecorded — it was recorded, and at the strongest severity the tool
offers. The failure was that nothing outside one editor could observe it.

That gap cannot be closed by documentation. Some of this repository's authors are agents that cannot
parse the DotSettings, and describing its rules in prose elsewhere would produce a third statement of
the same rule with no mechanism behind it. What reaches every author, human or automated, is a
diagnostic the compiler emits. Restating the rule in the one dialect the compiler understands is the
only way to make it apply to whoever is actually writing the code.

Enabling the analyzers during the build, rather than only in CI, follows from the same reasoning. The
purpose is to put the rule in front of whoever writes the code at the moment they write it — a
contributor without ReSharper, or an agent that builds to check its work — instead of surfacing it once
a pull request is already open and the diff already shaped. Reporting it as a warning keeps local
iteration workable, while the existing CI ratchet makes it blocking before anything merges. Leaving it
non-blocking was considered and rejected: an unenforced warning is precisely the state the rule was
already in.

The duplication this introduces is accepted deliberately. Partial coverage that is enforced is worth
more than complete coverage that is not, and the two statements say the same thing rather than
overlapping ambiguously. It also costs less than it appears to: the guarantee the old `.editorconfig`
header claimed — that the two configurations could never disagree — was not true when it was written,
since Rider ignores `.editorconfig` entirely. What is lost is the appearance of a single source of
truth, not the property itself. Each file now names the other and states which tool reads which, so a
change to one side is visibly a change that the other side has to follow.

The scope stays narrow on purpose. Only rules with a genuine Roslyn equivalent move; the rest remain
Rider-only and keep drifting for agents, which this decision does not pretend to solve. Claiming
otherwise would be worse than the current state, because a contributor would reasonably infer that a
green build means the whole style is respected.

## Alternatives Considered

### Keep the DotSettings as the sole configuration, and describe its rules in prose for agents

Considered because it preserves a single source of truth, which is what the previous arrangement was
designed around, and requires no build change.

Rejected because prose is not enforcement. The rule that drifted was already recorded, already at error
severity, and already unambiguous; restating it in a third place would not have detected a single one
of the 203 violations. It also decays: a description maintained by hand alongside the file it describes
is one more thing to keep in sync, with nothing to notice when it falls behind.

### Run the ReSharper command-line engine, so the DotSettings itself becomes executable

Considered because it is the only option that keeps one configuration and enforces all of it, including
the alignment and layout rules Roslyn cannot express. It was the preferred option until it was measured.

Rejected on evidence. Cleaning a single file takes minutes, because the engine loads and analyses the
entire solution regardless of how narrowly the target is specified — far too slow to run after each
edit, which was the use case. Worse, applying the repository's own cleanup profile does not preserve
the code: it removes casts that are load-bearing for overload resolution, leaving the solution unable
to compile, and it rewrites approval-test baselines, which silently decouples them from what the
generator produces. The alternative fails on correctness before it fails on speed.

### Move the whole configuration to `.editorconfig` and retire the DotSettings

Considered because it would restore a single source of truth on the other side.

Rejected because the mapping does not exist. The file layout patterns, the column alignment of
consecutive declarations and the region conventions are a substantial part of what the DotSettings
encodes, and `.editorconfig` cannot express any of them. The result would not be one configuration but
one configuration and a silent loss of rules.

## Consequences

### Positive

* The rule applies to every author and every tool, not only to whoever has Rider open.
* Drift becomes detectable at the moment it is introduced rather than at review time, or never.
* Automated contributors are subject to the rule for the first time, without depending on them reading
  any documentation.
* The `.editorconfig` header now states what is actually true about how the two files are read.

### Negative

* One rule is now stated in two places and has to be kept in sync by hand.
* Only a subset of the repository's style is enforced, and a green build may be read as meaning more
  than it does.
* The repository no longer has a single source of truth for code style, in appearance if not in fact.

### Risks

* The two configurations could diverge silently: nothing checks that the `.editorconfig` rule and its
  DotSettings counterpart still agree.
* A contributor may assume every rule in the DotSettings is enforced, and be surprised by the ones that
  are not.
* Column alignment remains unenforced outside Rider and will keep drifting, including in the
  declaration groups this decision's implementation rewrote.

## Follow-up Actions

* Decide whether the other DotSettings rules with a Roslyn equivalent — modifier order and accessibility
  modifiers among them — should follow the same path, or whether the boundary stays where this decision
  puts it.
* Realign the declaration groups left stale by the explicit-type rewrite, which only the ReSharper
  engine can do correctly.
* Consider whether Rider should be allowed to read `.editorconfig`, so the shared whitespace hygiene the
  repository already committed applies on both sides.

## References

* [ADR-0031](0031-name-any-factories-after-their-clr-type.md) — the same move in a
  different register: a convention made machine-checkable rather than left to attention.
* [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md) — a rule enforced by a
  reflection convention, for the same reason.
* Pull request [#360](https://github.com/Reefact/first-class-errors/pull/360) — the implementation, and
  the measurements the rejected alternatives rest on.

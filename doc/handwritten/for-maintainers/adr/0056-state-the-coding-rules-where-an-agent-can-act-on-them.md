# ADR-0056 | State the coding rules where an agent can act on them, and check them at the edit

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0056-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-28
**Accepted:** 2026-07-29
**Decision Makers:** Reefact

## Context

`FirstClassErrors.sln.DotSettings` records this repository's code style. It is a
ReSharper/Rider artifact: Rider reads it, and no compiler, CI job or automated agent can.

A substantial and growing share of the code in this repository is written by automated
agents. Until now the instructions given to them delegated the whole subject to that file
— *"code style and inspection severities are defined in `FirstClassErrors.sln.DotSettings`;
follow it"*. That sentence reads as an instruction but is not one for a reader that cannot
open the file it points at.

The consequence was measured rather than supposed. The explicit-type rule — declared at
error severity in the `.DotSettings` since it was introduced — drifted to 203 violations
across 17 files, all written by agents, while the instruction to follow it was in place.

[ADR-0055](0055-enforce-the-style-rules-the-compiler-can-express.md) closed part of that
gap by restating the explicit-type rule in `.editorconfig` and enforcing it during the
build, with CI promoting it to an error. That gate is authoritative, but it fires only
when someone builds: an agent that edits a file and does not build carries the violation
to the pull request, where the cost of the correction is highest and a red pull request is
what the maintainer sees first.

Only a minority of the `.DotSettings` rules have a Roslyn equivalent at all. The column
alignment of consecutive declarations, the file layout patterns and the region conventions
cannot be expressed in `.editorconfig`, so no build-time gate will ever cover them; if
they are to reach an agent, prose is the only channel.

The repository already runs hooks on agent activity, configured in a committed
`.claude/settings.json` and implemented as shell scripts under `.claude/hooks/`. The
existing one reads the branch and reports; it never rewrites anything, leaving both the
judgement and the correction to the agent.

## Decision

The coding rules an agent must follow are written out in `CLAUDE.md`, each one stating how
it is checked, and a hook checks them against the file the agent has just written.

## Rationale

An instruction that its reader cannot act on is not an instruction, and the 203 violations
are what that costs. Replacing the pointer with the rules themselves is the minimum
correction: it puts the rule where it is read, in the form it has to be applied, before a
single line is written. Everything else in this decision is a safety net under that.

Checking at the edit rather than only at the build follows from where the cost sits. The
build-time gate of ADR-0055 catches the same violation, but later and less reliably — an
agent decides when to build, and an agent that skips it discovers the problem from a red
pull request. A check that fires on the write closes the loop at the moment the mistake is
made, while the context that produced it is still current and the correction is one edit.
The two are complementary rather than redundant: the hook is immediate and advisory, the
build gate is authoritative and blocking.

The hook reports and does not rewrite, following the convention the repository's existing
hook already sets. That choice matters more here than it looks: a hook that silently
fixed the output would leave the agent believing it had written conforming code, and would
teach it nothing for the next file. Leaving the correction to the agent keeps the agent's
own output the thing under correction.

Writing the rules in `CLAUDE.md` rather than in a new document keeps them where an agent
already looks, and pairing each rule with the mechanism that checks it prevents the failure
this decision is about: a rule stated with nothing behind it is exactly what the
`.DotSettings` pointer was. Where no mechanism can exist — the layout and alignment rules
Roslyn cannot express — the prose says so, and asks for restraint rather than compliance:
do not reformat what you did not change.

The `.DotSettings` keeps its role unchanged. It remains what Rider applies and what the
maintainer edits; nothing in this decision asks anyone to maintain style in two places by
hand, because `CLAUDE.md` states only the subset an agent can act on and names the check
that keeps each one honest.

## Alternatives Considered

### Leave the pointer to the `.DotSettings` and rely on the build-time gate alone

Considered because ADR-0055 already makes the explicit-type rule blocking, so nothing
non-conforming can merge, and because it keeps the instruction set small.

Rejected because it moves the discovery of every violation to the latest possible point.
The gate fires only when someone builds; an agent that edits and pushes without building
turns a one-line correction into a red pull request. It also covers nothing outside the
Roslyn subset, which is most of the repository's style.

### Have the hook fix the violation instead of reporting it

Considered because it would guarantee conforming output regardless of what the agent
wrote, and because a formatter is the obvious tool for the job.

Rejected on two grounds. The repository's existing hook establishes read-and-report as the
convention, and diverging from it silently would make hook behaviour unpredictable. More
importantly, a hook that patches behind the agent leaves the agent believing its output was
correct, so the same mistake returns on the next file; the drift this decision addresses is
a learning failure, not a formatting one. The rewriting option was also independently
rejected for the ReSharper engine in ADR-0055, on evidence that it does not preserve the
code.

### Put the rules in a dedicated document rather than in `CLAUDE.md`

Considered because the list is expected to grow, and a coding-standards document is the
conventional home for it.

Rejected because `CLAUDE.md` is what an agent reads without being asked. A separate
document would need a pointer from `CLAUDE.md` to be found — which is precisely the
indirection that failed here.

## Consequences

### Positive

* A rule an agent must follow is now stated in a form it can act on, before it writes.
* Violations surface at the edit, where the correction is one line, rather than on a red
  pull request.
* The rules that no tool can check — layout, alignment, regions — reach an agent for the
  first time, as a request for restraint.
* The list has an obvious home, so the next rule is added rather than assumed.

### Negative

* The explicit-type rule is now stated in three places: the `.DotSettings`, `.editorconfig`
  and `CLAUDE.md`. Each has a distinct reader, but they must agree.
* The hook runs after every file edit, so its cost is paid constantly and its checks must
  stay cheap.
* A textual check cannot be as precise as a compiler, so some judgement stays with the
  agent.

### Risks

* The three statements of the rule could diverge; nothing checks that they agree.
* A noisy hook is an ignored hook. If false positives accumulate as rules are added, the
  reports stop being read and the mechanism quietly stops working.
* Rules with no mechanism behind them rely on the agent's restraint, which is what this
  decision otherwise argues against relying on. They are a mitigation, not a guarantee.

## Follow-up Actions

* Add the remaining rules an agent can act on as they are identified, each with its check.
* Watch the hook's false-positive rate as rules are added, and drop or narrow any check
  that cannot stay quiet on conforming code.
* Reconsider whether the `.DotSettings` rules with no Roslyn equivalent can be checked
  textually at all, or whether restraint remains the only available answer.

## References

* [ADR-0055](0055-enforce-the-style-rules-the-compiler-can-express.md) — the build-time
  half of the same problem, and the measurements behind it.
* [just-dummies ADR-0024](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0024-guard-public-and-internal-arguments-against-null.md) — a convention made
  observable rather than left to attention.

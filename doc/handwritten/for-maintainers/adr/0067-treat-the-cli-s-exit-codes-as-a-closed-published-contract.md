# ADR-0067 | Treat the CLI's exit codes as a closed, published contract

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0067-treat-the-cli-s-exit-codes-as-a-closed-published-contract.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-31
**Accepted:** 2026-07-31
**Decision Makers:** Reefact

## Context

`fce` is a build-pipeline tool before it is an interactive one. `fce catalog diff` exists to fail a
job when the error catalog drifts: its report goes to standard output, and the pipeline branches on
what the process returns. The user documentation says so and shows it — the catalog-versioning
reference lists an exit-code table per command, and the CI guide's example pipelines propagate the
code to decide the job result. For that command the exit code is not a side effect of running; it is
the answer.

Four codes are published today: `0` (the command did what it was asked), `1` (execution error), `2`
(`catalog diff` found changes at or above the threshold `--fail-on` selected), and `130` (the run was
interrupted). `2` is deliberately distinct from `1`: a pipeline must be able to tell "the tool
worked, and the catalog moved" from "the tool could not run". `130` is the conventional value for a
process killed by SIGINT, `128 + 2`.

Until recently those numbers were bare literals at 32 sites across nine command files, and the three
commands returning `130` each carried their own prose comment explaining what it meant. They are now
named in one `ExitCodes` type per executable, and the command tests assert them — including `130`.
Naming them is what makes the set legible for the first time; it is not what makes it a promise.

The set is not in fact closed today. The CLI delegates command-line parsing to Spectre.Console.Cli,
which owns the failure path for an unparseable command line and answers with a code of its own
choosing. An unknown subcommand exits `-1` — reported as 255 by a POSIX shell — writing nothing to
either standard stream. That fifth value appears in no exit-code table, was chosen by a dependency
rather than by this repository, and reaches a caller who asked for something the tool does not have.
The 2026-07-20 architecture and design audit already recorded this as an item to normalize and
document.

Nothing mechanically constrains the set. The tests assert the values the commands return today,
which is a different statement from "no command may return a sixth". A new command added tomorrow
compiles just as well returning `3`, and a script that reads `2` as "changes found" breaks silently
if some later command borrows `2` for something else.

## Decision

The exit codes `fce` and its worker return are a closed set with fixed published meanings, owned by
this repository, and extended or changed only as a deliberate, documented act.

## Rationale

An exit code is the one part of a command-line tool that a machine consumes. Everything else `fce`
emits — the report, the log lines, the diagnostics — is read by a person who can adapt; the exit
code is read by a pipeline that cannot. That places it in the same category as a public API
signature: the repository already treats renamed error codes and public types as breaking changes,
and an exit code a CI job branches on carries the same weight. It is already published, so the
promise exists whether or not it is recorded; what was missing is the record of what the promise
covers and what breaking it costs.

Recording it as a decision rather than leaving it a habit is what the `-1` hole demonstrates. An
implicit contract does not stay whole on its own: nobody decided that an unparseable command line
should exit `-1` silently, and nobody noticed for as long as the numbers were literals scattered
across nine files. A contract stated once can be checked against; a contract that exists only in the
sum of its call sites drifts without anyone taking a decision to let it.

The decision is about the set, not about how the set is spelled. If `ExitCodes` became an enum, or
the CLI moved off Spectre.Console.Cli, or the commands were rewritten entirely, "these codes mean
these things and the set is closed" would still hold and this record would not need editing — which
is the test this base applies to decide whether a decision belongs here at all.

Closing the set costs the freedom to add a code casually, and that cost is the point. A sixth code is
cheap to add and expensive to take back, because the tool cannot know which pipelines already read
the fifth. Making the addition deliberate — a documentation change in both languages, weighed like
any other compatibility change — puts the cost where it is visible, at the moment the choice is made
rather than at the moment a user's build breaks.

## Alternatives Considered

### Leave the codes implicit, as they were

Considered because the values are already asserted by the command tests and already listed in the
user documentation, so a reader who looks in the right two places can reconstruct the set.

Rejected because reconstructing is not promising. The tests pin what the commands do; they do not
forbid a sixth code, and they said nothing while the parse-error path answered `-1` outside every
published table. That is the failure mode of an unrecorded contract, observed in this repository
rather than imagined.

### Treat exit codes as an implementation detail of each command

Considered because each command decides its own outcome, and the codes could be argued to belong to
the command rather than to the tool.

Rejected as contradicted by the published documentation and by the tool's purpose. The reference
tables are per command, but a pipeline reads one number from one process, and `0`, `1` and `130`
mean the same thing across every command by design. Scattering ownership is how `130` came to be
explained by three separate comments saying the same thing.

### Model the set as an enum rather than named integer constants

Considered because an enum would make the set a type, and a value outside it would need a cast.

Rejected because the values must reach the process as raw integers: the command framework's contract
returns `int`, so every command would cast at its return, and the compiler still could not stop a
cast of `3`. The typing is nominal while the friction is real — and the decision recorded here is
about the set being closed and published, which no C# construct expresses either way.

### Fix the parse-error code as part of this decision

Considered because that hole is what exposed the problem, and closing it here would settle the
matter in one move.

Rejected because which value an unparseable command line should return, and what it should print, is
a design choice with its own trade-offs — reusing `1`, or reserving a distinct code so a pipeline can
tell a bad invocation from a failed run. This record settles that the set is closed and owned; it
leaves the choice of that value to the follow-up that closes it, which is specification.

## Consequences

### Positive

* A pipeline branching on `fce`'s exit code has a promise it can rely on across versions, and the
  catalog-versioning CI recipes rest on something recorded rather than on current behaviour.
* A new command has an answer to "what do I return" that does not require reading nine other files.
* Extending the set becomes visible: it is a decision with a documentation change in both languages,
  not a literal typed at a `return`.

### Negative

* Adding an exit code now costs more than typing a number — the reference tables in English and
  French move with it, and the addition is weighed as a compatibility change.
* The repository owns a promise it did not previously admit to owning, including for the paths a
  dependency currently answers for.

### Risks

* The `-1` parse-error path contradicts the decision the day it is proposed. Until the follow-up
  closes it, the published set and the tool's actual behaviour disagree — the record makes that
  disagreement visible, it does not remove it.
* Nothing checks the rule. A future command can return `3` and compile, and no test or analyzer will
  object; this record is a rule the reviewer applies, not one the build enforces — the arrangement
  ADR-0056 warns about, accepted here because the surface is small and reviewed.

## Follow-up Actions

* Normalize and document the exit code for an unparseable command line, and give it a diagnostic on
  standard error — the item already raised by the 2026-07-20 architecture and design audit.
* Keep the exit-code tables in the catalog-versioning reference, English and French, in step with the
  `ExitCodes` types whenever the set changes.

## References

* [ADR-0056](0056-state-the-coding-rules-where-an-agent-can-act-on-them.md) — what becomes of a rule
  nothing can act on, which is the risk this record accepts.
* Catalog-versioning command reference, English and French — the published exit-code tables.
* Catalog-versioning CI guide — the example pipelines that branch on the code.
* 2026-07-20 architecture and design audit — the parse-error exit code raised as an item.

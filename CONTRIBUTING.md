# Contributing to FirstClassErrors

FirstClassErrors treats errors as first-class, documented, diagnosable
concepts. The history of the repository should be as legible as the errors the
library produces. This guide defines how commits are written here.

## Building and testing

* Target framework: **.NET Standard 2.0**.
* Build: `dotnet build FirstClassErrors.sln`
* Test: `dotnet test FirstClassErrors.sln`
* Analyzer tests, when touching analyzers: `dotnet test FirstClassErrors.Analyzers.UnitTests`

See [`CLAUDE.md`](CLAUDE.md) for the project layout and the broader change
guidelines.

## Enabling the commit-message hook

A `commit-msg` hook checks every message against the convention below before it
is recorded. It is versioned under `.githooks/`; enable it once per clone:

```
git config core.hooksPath .githooks
```

The same check runs in CI on every pull request, so a bypassed hook
(`git commit --no-verify`) is caught before merge. Merge commits are exempt.
The check itself lives in `tools/commit-lint/lint-commit-message.sh`, shared by
the hook and CI so the two never diverge.

## Commit messages

This section adapts the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
specification. The key words MUST, MUST NOT, SHOULD, and MAY are to be
interpreted as described in [BCP 14](https://www.rfc-editor.org/info/bcp14), and
only when they appear in capitals.

### Why

A release is prepared. One needs to know what a branch contains, what to carry
into it, and which version number comes out.

```
a3f1c2e fix bug
8b41d90 update renderer
1d0e4aa wip
```

This history teaches nothing. Every question forces a diff open.

```
a3f1c2e fix(gendoc): render error examples with the invariant culture
8b41d90 feat(gendoc): emit an RFC 9457 problem type in examples
1d0e4aa refactor(gendoc): extract output routing into a writer
```

This one answers three questions without opening a single diff: what the branch
contains, which commit to carry into a release, and whether the version moves
from `1.4.2` to `1.4.3` or to `1.5.0`. That is the reading of the reviewer, and
of whoever prepares the release. Tomorrow it will be a tool's.

### The rule

The rule bears on **each commit**, not on a merge message. A commit travels
alone: it is cherry-picked onto a release branch, listed in a
`git log --oneline`, read in isolation six months later. Its message MUST stand
on its own.

#### Form

```
<type>[(<scope>)][!]: <description>

[body]

[footers]
```

* The commit MUST begin with a type, optionally followed by a scope and a `!`,
  then a colon and a space.
* Everything written in the message MUST be in English — header, body, and
  footers.
* A commit MUST carry a single type, that of its intention. Two independent
  intentions MUST be two commits: the message forces the split that ought to
  happen.

#### Types

Ordered here with the two version-driving types first, then the rest
alphabetically. The list is closed.

| Type | When to use | Minimal effect on the version |
|---|---|---|
| `feat` | A new capability, visible to the consumer of the package | `MINOR` |
| `fix` | The correction of a defective behaviour | `PATCH` |
| `build` | Build system, dependencies, packaging, deployment artefacts | none imposed |
| `chore` | What touches neither production code nor its delivery | none imposed |
| `ci` | Pipeline configuration | none imposed |
| `docs` | Documentation only | none imposed |
| `perf` | A performance gain, at constant observable behaviour | none imposed |
| `refactor` | Restructuring, at constant observable behaviour | none imposed |
| `revert` | The reversal of an earlier commit | per what it reverts |
| `style` | Formatting with no semantic effect | none imposed |
| `test` | Tests only | none imposed |

The type MUST be lowercase and belong to this table. A breaking change carried
by any of these types produces a `MAJOR`.

#### Scope

The scope MAY be provided. When present it MUST be lowercase and MUST be one of:

| Scope | Covers |
|---|---|
| `core` | `FirstClassErrors` — the runtime library (`Error`, `Outcome`, `ErrorCode`, `ErrorContextKey`, …) |
| `analyzers` | `FirstClassErrors.Analyzers` — the Roslyn analyzers and their `FCExxx` diagnostics |
| `cli` | `FirstClassErrors.Cli` — the command-line tool |
| `gendoc` | `FirstClassErrors.GenDoc` and its worker — the documentation generator |
| `testing` | `FirstClassErrors.Testing` — the test-support package |

This list lives here, in the repository, where a tool can check it. A scope
MUST NOT be a file name or a class name: those move; the zone they inhabit does
not. `fix(core):`, never `fix(ErrorCode.cs):`.

The scope is optional here because the two published packages
(`FirstClassErrors` and `FirstClassErrors.Testing`) share a single tag-driven
version: the scope carries no versioning weight, only readability. What does not
belong to a component takes **no** scope — repository infrastructure (the
solution, `Directory.Build.props`, the workflows, `.gitignore`, `CLAUDE.md`),
repository-wide documentation, ADRs, and the `FirstClassErrors.Usage` samples:
`ci: …`, `docs: …`.

When one atomic change crosses several components, the commit MUST carry all
their scopes, comma-separated with no space and ordered alphabetically. The
order is alphabetical so a given pair is always written the same way, and found
again with a single `git log --grep`.

```
fix(cli,gendoc): thread cancellation through the generate command
```

#### Description

* It MUST be in the imperative present: `add`, not `added` nor `adds`. The
  description completes one sentence — *If applied, this commit will …* — and
  only the imperative fits it: *…will add Outcome.Map*.
* It MUST begin with a lowercase letter and MUST NOT end with a period. The
  header line is not a sentence; it is a title.
* The full header line — type, optional scope, optional `!`, colon and
  description — MUST fit in 72 characters. Beyond that, once the abbreviated
  hash is prefixed, it overflows the 80 columns of a terminal in a
  `git log --oneline`.

#### Body

The body MAY be provided, after a blank line. It explains **why** the change
happens — the constraint, the symptom, the trade-off. The *what* is already in
the diff; repeating it is noise.

When that why is not readable from the diff, the body SHOULD be provided.
Abstaining is paid for six months later, on a commit no one can interpret any
more.

#### Footers

Footers MAY be provided, after a blank line. Each footer MUST take the form
`Token: value`. The token MUST be words separated by hyphens, **each word
capitalized**: `Co-Authored-By`, `Reviewed-By`, `Refs`, `Reverts`.
`BREAKING CHANGE` is the sole exception to this form.

> This "every word capitalized" casing is a deliberate departure from the usual
> single-initial convention. It exists so that hand-written footers stay
> consistent with the trailers this repository's automated commits already
> carry — `Co-Authored-By`, `Claude-Session`. One rule for every footer beats
> two.

When an issue exists, its number MUST live in a `Refs:` footer, and MUST NOT
appear in the description — the description states the change, not where it was
requested. The footer carries the key (`#142`), never the URL: a commit message
is not rewritten, and the number survives what an address does not. (A tooling
footer such as `Claude-Session` is a URL by nature, and is exempt from that last
point.)

A commit is **not** the place to close an issue. Closing is a
repository-workflow concern: put `Closes #142` in the pull request description,
and GitHub closes the issue on merge. The commit itself stays neutral, carrying
at most a `Refs:`.

#### Breaking changes

A breaking change MUST be signalled twice: by a `!` placed just before the
colon, and by a `BREAKING CHANGE:` footer in capitals.

```
feat(core)!: fail Outcome<T>.To with an Outcome instead of throwing

BREAKING CHANGE: Outcome<T>.To returns a failed Outcome<TTarget> where it
used to throw on a null conversion. Callers must handle the Outcome instead
of catching.
```

The `!` is what one sees in a `git log --oneline`. The footer is what one reads
when it is time to migrate. The two have different readers; neither replaces the
other.

What is breaking reads on the **published contract**, not on internal code. In
this repository that contract explicitly includes error codes, diagnostic IDs
(`FCExxx`), and public types: renaming any of them is a breaking change (see
`CLAUDE.md`). Renaming an `internal` type breaks nothing.

#### Reverts

A revert commit MUST carry the `revert` type, repeat the description of the
reverted commit, and reference its SHA in a `Reverts:` footer.

```
revert(gendoc): emit an RFC 9457 problem type in examples

Reverts: b36765a
```

A revert's effect on the version is qualified like any commit's: from the
consumer, on the published contract. Reverting a change not yet released
neutralizes its effect. Removing a capability already released is a breaking
change, and the commit MUST then carry the `!` and the `BREAKING CHANGE` footer.

### The doctrine

**The issue is the unit of the request, the commit the unit of the change.** An
issue produces as many commits as it carries intentions: the feature, the
refactor that prepared it, the fix found in review. Each carries its own type,
all carry the same `Refs:`.

**The type is the intention, not the content of the diff.** A feature arrives
with its tests, its API documentation, its sample: the commit stays a `feat`.
`test` and `docs` designate a change that touches *only* tests, *only*
documentation. Splitting a `feat` into five commits because it spans five
directories manufactures commits that do not compile alone.

**`feat` or `fix` is decided from outside the component.** The criterion is not
the size of the diff, it is what the consumer of the package observes. Three
lines that restore the promised behaviour are a `fix`. One line that opens a new
capability is a `feat`.

**`refactor` and `perf` make a promise: observable behaviour does not change.** A
`refactor` that fixes a bug in passing is a mislabelled `fix` — and the
correction becomes invisible to whoever prepares the release.

**`chore` is not the bin.** Everything that fits nowhere lands there, and the
type ends up meaning nothing. Before writing `chore`, reread the table.

**What is breaking reads on the published contract**, not on internal code. A
renamed `internal` type breaks nothing. A changed return type, a renamed
serialized field, an error code, a diagnostic ID — those do.

**The wrong type is fixed before the merge.** A `git rebase -i` rewrites the
message while the commit has not reached a shared branch. After that, the cost
of the correction exceeds the cost of the error: leave it and move on.

**The version number is decided by reading the history.** Whoever prepares the
release reads the increment there: a lone `fix` gives a `PATCH`, a `feat`
imposes at least a `MINOR`, a breaking change imposes a `MAJOR`.
`FirstClassErrors` and `FirstClassErrors.Testing` share one tag-driven version,
so the highest increment across the release applies to both.

**Who decides what.** The author of the commit chooses the type and the scope.
The reviewer of the pull request refuses a non-conforming message as they refuse
non-conforming code. The maintainers own the list of scopes and the list of
types.

### Examples

**A feature, with scope and issue.**

```
feat(analyzers): add FCE016 for an undocumented error code

Refs: #142
```

**A fix whose why is not readable from the diff.**

```
fix(gendoc): render error examples with the invariant culture

Sample amounts were formatted with the host's culture, so the Verify
baselines matched on an invariant machine and failed on a comma-decimal one.
CI and developers disagreed on the very same commit.

Refs: #128
```

**A refactor, promising nothing but iso-behaviour.** Neither body nor footer:
the diff says it all.

```
refactor(core): extract transience computation into TransienceCalculator
```

**A breaking change, with the migration instruction.**

```
feat(core)!: fail Outcome<T>.To with an Outcome instead of throwing

BREAKING CHANGE: Outcome<T>.To returns a failed Outcome<TTarget> where it
used to throw on a null conversion. Callers must handle the Outcome instead
of catching.

Refs: #150
```

### Anti-patterns

Ordered as the rules are: type, scope, description, body, breaking, issue.

| Message | What is wrong |
|---|---|
| `chore: handle a null error code` | A `fix` in disguise. The release preparer will not see it, and the version will not move when it should. |
| `feat: refactor the extraction reader` | The type contradicts the description. One of the two is lying. |
| `fix(core): correct transience and add a CI cache` | Two changes, two commits. No version can describe this one. |
| `fix(ErrorCode.cs): formatting` | The scope names a file. It designates a zone: `core`. |
| `feat(gendoc, cli): carry the source description` | A space after the comma, and the order is not alphabetical. Two spellings for one pair — write `feat(cli,gendoc):`. |
| `fix(core): Fixed the null dereference.` | Capital, past tense, trailing period. Three form rules broken, one useful word. |
| `feat(core): add support` | Support for what? The description must stand alone in a `git log`. |
| `fix(core): change line 42 of Error` | The description names a line. It should name a change. |
| `fix(gendoc): render with the invariant culture` — body: `Replaced DateTime.Now with CultureInfo.InvariantCulture` | The body repeats the diff. It should say why the culture varied across hosts. |
| `feat(core)!: fail Outcome<T>.To with an Outcome` — no footer | The `!` warns; it migrates no one. |
| `feat(core): add Outcome.Map (#142)` | The issue eats the 72 characters of the description. Its place is a footer. |
| `refs: #142` | Lowercase token. The footer token is `Refs`. |

### Adoption

This guide is the rule for commits in this repository. Deviating from it
requires a justification — an ADR under `doc/adr/`, or an update to this guide.

It applies from its adoption on, to every commit created after. Prior history is
not rewritten.

Enforcement stands today at pull-request review, which refuses a non-conforming
message and lets the author correct it before the merge. The repository will
lock the rule in turn: a `commit-msg` hook that refuses the message at write
time, doubled with a CI check, since a local hook can be bypassed. Merge
commits, generated by GitHub, are exempt.

### Credits

This section adapts the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
specification, published under [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).

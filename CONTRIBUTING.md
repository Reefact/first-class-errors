# Contributing to FirstClassErrors

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](doc/handwritten/for-users/CONTRIBUTING.fr.md)

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

## Public API baseline

The shipping libraries — `FirstClassErrors`, `FirstClassErrors.Testing`,
`FirstClassErrors.RequestBinder` (the `lib` train) and `Dummies` (the `dum`
train) — carry a committed public-API baseline, so every change to their public
surface is a reviewed diff and an accidental breaking change (a removed overload,
a narrowed return type, a renamed member) cannot ship silently under a version
number that promises compatibility. Two guards, wired once in
[`build/PublicApiBaseline.props`](build/PublicApiBaseline.props):

* **`Microsoft.CodeAnalysis.PublicApiAnalyzers`** tracks the surface in committed
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` files under each project's
  `PublicAPI/<target-framework>/` folder. An undeclared public symbol raises
  `RS0016`; a declared symbol that no longer exists raises `RS0017`. Both are
  warnings locally and errors in CI (the warnings-as-errors ratchet), so a
  surface change fails the build until the same change updates the baseline.
* **Package validation** (`EnablePackageValidation`) runs ApiCompat during
  `dotnet pack`. With no baseline version set it performs the same-package
  cross-target-framework check (it proves `Dummies`' net8.0 surface never drops
  API a netstandard2.0 consumer sees). To additionally gate against a published
  version, set `PackageValidationBaselineVersion`; `0.1.0-preview.1` is the first
  such baseline available for the `lib` train.

**Accepting an intended surface change.** Update the baseline in the same commit:

* **In an IDE**: apply the *"Add to public API"* code fix on the `RS0016`
  diagnostic.
* **From the CLI**: `dotnet format analyzers <project> --diagnostics RS0016`
  appends the new entries to `PublicAPI.Unshipped.txt`; for a **removal**, delete
  the matching line by hand.
* **Multi-target (`Dummies`)**: each framework has its own baseline under
  `PublicAPI/<tfm>/`, and `dotnet format` rewrites only one framework's file per
  run — update `net8.0` and `netstandard2.0` separately when a change touches both.
* **At release**: promote the accumulated `PublicAPI.Unshipped.txt` entries into
  `PublicAPI.Shipped.txt` (the *"mark shipped"* fix, or by hand).

`RS0026`/`RS0027` (the "optional parameters across overloads" design rules) are
disabled deliberately: they fire on the library's central fluent APIs
(`Outcome.Then`/`Recover`/`Finally`/`Try`, `Any.Reproducibly`), where enforcing
them would demand a breaking redesign — a separate decision, not a baseline chore.

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

The hook lets `fixup!`, `squash!`, and `amend!` commits through so you can build
an autosquash rebase; CI rejects them, so squash them away before merge.

## Branches

### Why

A pull request is read against its branch. Two branches carry the same feature.
The first was cut from `origin/main` an hour ago and holds three commits — its
pull request diff *is* the feature, and nothing else. The second was cut from a
local `main` three weeks stale, then revived for a second idea once the first
had merged; its diff carries fifteen commits, twelve already on `main`, and the
reviewer cannot tell the request from the residue.

```
$ git log --oneline origin/main..HEAD    # the second branch
a1b2c3d feat(core): the change the pull request is for
9f8e7d6 feat(gendoc): a renderer already merged, dragged along
...twelve more commits already on main...
```

The branch is not the work. It is the disposable workspace of **one** pull
request — cut fresh from the remote, used once, discarded on merge. Everything
below follows from that.

### The rule

* A branch carries **one pull request**, and that pull request carries one
  coherent unit of work. Work unrelated to that unit MUST take its own branch —
  the branch-level reading of *two intentions, two commits*.
* `main` is written **only** by merge. No commit lands on `main` directly; it
  moves when a reviewed pull request is merged.
* A branch MUST be cut from the **tip of `origin/main`**, freshly fetched —
  never from a local `main` that may lag, nor from another topic branch:

  ```
  git fetch origin
  git switch -c <author>/<short-description> origin/main
  ```
* A branch name MUST take the form `<author>/<short-description>`. The
  `<author>` is the branch owner's GitHub handle — the person or the tool the
  work belongs to: `sylvain/…`, `claude/…`, `dependabot/…`. The
  `<short-description>` MUST be English, lowercase, kebab-case, and name the
  change, not the file it touches: `sylvain/gendoc-invalid-culture`, never
  `sylvain/GenDoc.cs`.
* A tool that generates its own branches owns its namespace and keeps its
  native layout beneath it — `dependabot/nuget/Newtonsoft.Json-13.0.1`,
  `renovate/…`. The `<author>/<short-description>` form binds the branches a
  person or an agent cuts by hand; a generator's scheme is the generator's to
  define, and fighting it buys nothing.
* The branch name carries **no type**. The type is a property of each commit,
  checked there by the hook and by CI; a branch gathers commits of several
  types, and a single prefix would name one and hide the rest — the same reason
  a multi-intention pull request title takes no `type:` (see *Pull request
  titles* below). The owner is what the name adds, because the owner is what the
  commits do not carry.
* A branch lives exactly as long as its pull request stays **open**, and MAY be
  reused only for that same request — review fixes, changes asked for on the
  pull request.
* Once the pull request is **merged or closed**, the branch is finished. It MUST
  NOT be revived, not even for follow-up on the same topic: a merged pull
  request cannot describe new work, and a closed one was set aside. Follow-up is
  a new branch, cut fresh from `origin/main`.
* To carry `main`'s progress into an open branch: while the branch is yours
  alone, **rebase** it onto `origin/main`; once others may have based work on
  it, **merge** `origin/main` in instead. Either keeps the branch current
  without rewriting what a collaborator has already pulled.
* Rewriting a branch's history — a force-push, a `git rebase -i` — is fine
  while the branch is **yours alone**, and is how a commit message the lint or
  a reviewer rejected gets fixed, even mid-review: a rejected message cannot be
  corrected by a follow-up commit (see *Commit messages*). Once anyone else may
  have **based work on it**, its history MUST NOT be rewritten — a force-push
  discards what was built on top. Work that is not yours is not yours to
  rewrite or delete.
* Before opening the pull request, **read the branch** against a fresh
  `origin/main`:

  ```
  git fetch origin
  git log  --oneline origin/main..HEAD     # the commits the request adds
  git diff --stat    origin/main...HEAD    # the files it touches
  ```

  If either shows something the request is not about, the branch has drifted —
  split it before review, not after.

### The doctrine

**The branch is the unit of work in progress; the pull request is what it
becomes.** One branch, one pull request, one unit of work — the same one-to-one
the doctrine draws between the commit and its change.

**The name says who, the commits say what.** A branch owns a pull request that
may carry a feature, the refactor that prepared it, and its tests at once; no
single type names it honestly. The type lives on each commit, where the hook
enforces it. The branch name adds the one thing the commits omit — whose work it
is — so `claude/…` and `dependabot/…` are not exceptions but the rule itself,
read the same on a human or a machine.

**A branch is disposable.** Its history is preserved by the merge commit that
lands it; the ref itself is cut fresh and deleted on merge. Nothing of value
lives only on a branch.

**A merged branch is spent.** Reviving it stacks new work on settled history and
forks from a `main` that has moved. The reviewer pays the cost, reading the
residue as if it were the request.

**Cut from the remote, not the local.** A local `main` lags silently; a branch
cut from it drags that lag into every diff. `origin/main`, freshly fetched, is
the only base.

**Unrelated work is a new branch, not a passenger.** A branch that carries two
changes forces a pull request that can describe neither — the branch-level form
of the commit that carries two intentions.

### Examples

| Branch | Why it fits |
|---|---|
| `sylvain/add-html-renderer` | Owner and change, named plainly. The type it will carry lives in its commits. |
| `claude/gendoc-invalid-culture` | An agent's branch; the description names the zone, not `GenDoc.cs`. |
| `dependabot/nuget/Newtonsoft.Json-13.0.1` | A generator keeps its native layout under its `dependabot/` namespace. |
| `sylvain/security-policy` | The description alone carries the subject; the branch needs no type. |

### Anti-patterns

| Branch or move | What is wrong |
|---|---|
| a commit pushed straight to `main` | `main` moves only by merge. Even a one-line fix takes a branch and a pull request. |
| `patch-1`, `my-work`, `tmp` | No owner, and it names nothing. A branch name is read in the pull-request list; it MUST say who owns what. |
| `feat/add-html-renderer` | A type in the owner's slot. The type belongs on the commits; the branch prefix is the owner: `sylvain/add-html-renderer`. |
| `sylvain/GenDoc.cs` | Names a file. It should name the change: `sylvain/gendoc-invalid-culture`. |
| `sylvain/corrige-le-rendu` | Not English. |
| reviving a merged `claude/add-html-renderer` for a follow-up | A merged branch is spent. Cut the follow-up fresh from `origin/main`. |
| a branch cut from a three-week-old local `main` | The pull request diff fills with commits already on `main`. Fetch first; cut from `origin/main`. |
| one branch carrying a feature and an unrelated CI tweak | No single pull request describes both. Two branches, two requests. |
| force-pushing a branch others have built on | Rewrites shared history and discards the work pushed on top. Rewrite only while the branch is yours alone. |

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
| `binder` | `FirstClassErrors.RequestBinder` — the request binder for the primary-adapter boundary |
| `cli` | `FirstClassErrors.Cli` — the command-line tool |
| `dummies` | `Dummies` — the standalone arbitrary-test-value generator |
| `gendoc` | `FirstClassErrors.GenDoc` and its worker — the documentation generator |
| `testing` | `FirstClassErrors.Testing` — the test-support package |

This list lives here, in the repository, where a tool can check it. A scope
MUST NOT be a file name or a class name: those move; the zone they inhabit does
not. `fix(core):`, never `fix(ErrorCode.cs):`.

The scope is load-bearing for the release record. The tooling partitions commits
into **release trains** by scope — `tools/trains.sh` is the single source of
truth — and each train publishes independently: `lib` (scopes `core`, `analyzers`,
`testing`, `binder` → `FirstClassErrors`, `FirstClassErrors.Testing` and
`FirstClassErrors.RequestBinder`), `cli` (scopes `cli`, `gendoc` → the `fce` tool)
and `dum` (scope `dummies` → `Dummies`). A commit's scope decides which train's
release notes and changelog it lands in; see
[Adding a release train](doc/handwritten/for-maintainers/AddingAReleaseTrain.en.md).

Because of that, a user-facing change **must** carry a scope: an unscoped `feat:`
or `fix:` matches no train and is **silently dropped from every release note and
changelog**, so the change would vanish from the release record. Only what
genuinely belongs to no component stays unscoped — repository infrastructure (the
solution, `Directory.Build.props`, the workflows, `.gitignore`, `CLAUDE.md`),
repository-wide documentation, ADRs, and the `FirstClassErrors.Usage` samples —
and those use non-version-driving types anyway: `ci: …`, `docs: …`, `chore: …`.

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
requires a justification — an ADR under `doc/handwritten/for-maintainers/adr/`, or an update to this
guide.

It applies from its adoption on, to every commit created after. Prior history is
not rewritten.

Enforcement is layered. A `commit-msg` hook (versioned under `.githooks/`, enabled
once per clone) refuses a non-conforming message at write time, and — because a
local hook can be bypassed — a CI check re-runs the same validation on every pull
request, so a bypassed hook is still caught before the merge. Merge commits,
generated by GitHub, are exempt.

### Credits

This section adapts the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
specification, published under [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).

## Pull request titles

The convention above governs each commit. A pull request needs a line of its
own, and it is not the same object: the commit is the unit of the change, the
**pull request the unit of the request** — the relation the doctrine already
draws between the commit and the issue. A pull request MAY therefore gather
several commits, of several types.

Its title is read in three places: the list of open pull requests, the
`Merge pull request #NN` commit GitHub writes when the branch lands (this
repository merges with a merge commit), and the draft of the release notes. It
earns the same care as a commit header. Unlike a commit, it is **not** linted;
it stands on the review, as the code does.

### The rule

* The title MUST be in **English**, like everything else recorded here.
* It MUST name the **whole** pull request, not one of its commits. The per-commit
  types live in the commits, where the hook and CI check them; the title says
  what the branch delivers.
* Its shape follows from how many intentions the pull request carries:
  * **One intention** — the branch does a single kind of thing. The title MUST
    mirror the commit header it collapses to:
    `<type>[(<scope>)][!]: <description>`, under the very rules of the section
    above — imperative present, lowercase after the colon, no trailing period. A
    one-commit pull request's title is that commit's header, verbatim.
  * **Several intentions** — the branch carries a feature and the refactor that
    prepared it, or a fix and the test that pins it. The title MUST NOT borrow a
    single `type:` prefix: it would name one commit and hide the rest. It states
    the subject in plain words, as a title — an initial capital, no trailing
    period. A topical prefix (`Release supply chain: …`) is welcome; a
    Conventional-Commits type is not, unless it is honestly the only one.
* Keep the title within the **72 characters** a commit header targets, so the
  pull request list shows it whole.
* The issue reference lives in the **description**, never the title: `Closes #NN`
  when the pull request closes the issue, so GitHub closes it on merge; `Refs:
  #NN` otherwise. The title states the change, not where it was asked. A breaking
  change is signalled the same way it is on a commit — the `!` and the
  `BREAKING CHANGE:` note ride the commit, and the template's "Breaking change"
  box repeats it — not the title.

### Examples

| Title | Why it fits |
|---|---|
| `ci: add dependency review on pull requests` | One intention. The title is the commit header. |
| `feat(analyzers): add FCE016 for an undocumented error code` | One intention, scoped. The issue it closes lives in the description, not here. |
| `Adopt and enforce a Conventional Commits convention` | The guide, the hook, and the CI gate — several commits of several types. A plain title names them all. |
| `Release supply chain: build provenance + embedded SBOM` | Several intentions under one topic. A topical prefix carries it; no single `type:` would be honest. |

### Anti-patterns

| Title | What is wrong |
|---|---|
| `feat: various improvements` | A type on a grab-bag. Either it is one intention — name it — or it is several, and `feat:` hides them. |
| `fix(core): Fixed the null dereference.` | The single-intention form, wearing the commit header's own faults: capital, past tense, trailing period. |
| `Add Outcome.Map (#142)` | The issue number belongs in the description's `Closes`/`Refs`, where GitHub reads it — not eating the title. |
| `Corrige le rendu des exemples` | Not English. |

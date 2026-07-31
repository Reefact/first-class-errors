# ADR-0069 | Consume JustDummies from its own repository

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0069-consume-justdummies-from-its-own-repository.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-31
**Accepted:** 2026-07-31
**Decision Makers:** Reefact
**Supersedes:** [ADR-0011](0011-host-dummies-as-a-standalone-package.md) (its colocation half), and the
embedding workaround accepted by [ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md)

## Context

[ADR-0011](0011-host-dummies-as-a-standalone-package.md) decided that JustDummies is an independent package
that must never reference a FirstClassErrors project, **and** that it lives in this repository, to reuse this
repository's CI, packaging, release, SBOM, SourceLink and governance infrastructure. It recorded that the
no-reference rule exists so that "a later repository extraction [stays] mechanical rather than architectural",
and it rejected an immediate separate repository on cost grounds rather than on principle.

That extraction has now happened. The product — the library, its 28 analyzers, its xUnit v3 adapter, its two
test suites, its documentation, its ADRs and its specified `dum` scaffolder — was filtered out of this
repository's history into **`Reefact/just-dummies`** at
`fbf523b86acebdd34ba0bbfd437683864be3cb9c`, preserving authors, dates, messages and the rename from `Dummies`
to `JustDummies`. Nothing was deleted here.

This repository still depends on JustDummies in four places, and one of them ships it:

| Project | How it depends |
| --- | --- |
| `FirstClassErrors.Testing` | private `ProjectReference` **plus** a pack target that embeds `JustDummies.dll` inside its own `lib/` |
| `FirstClassErrors.UnitTests` | `ProjectReference`, test-only |
| `FirstClassErrors.RequestBinder.UnitTests` | `ProjectReference`, test-only |
| `FirstClassErrors.Testing.UnitTests` | `ProjectReference`, test-only |

Every project in the repository also loads the JustDummies analyzers at build time (ADR-0061).

[ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) accepted the embedding as explicitly temporary:
JustDummies "is not yet on NuGet (ADR-0011), so reference it privately and embed its assembly in this
package […]; switch to a NuGet PackageReference once JustDummies is published."

**JustDummies has never been published.** No `dum-v*` tag was ever pushed from this repository, and
`Reefact/just-dummies` has not released either — its nuget.org trusted-publishing policy does not exist yet.

## Decision

This repository becomes a **consumer** of the `JustDummies` and `JustDummies.Analyzers` packages published
from `Reefact/just-dummies`, and stops being their source.

The cutover is **gated on the first publication** and is deliberately not performed by the extraction. Until a
restorable `JustDummies` package exists on nuget.org, the source stays here exactly as it is: replacing a
`ProjectReference` with a `PackageReference` to a version nobody can restore would break the build for every
contributor and every CI run, to no benefit.

When that version exists, in one pull request:

1. add `<PackageVersion Include="JustDummies" Version="X.Y.Z" />` to `Directory.Packages.props`;
2. replace the four `ProjectReference`s on `JustDummies` with `PackageReference`s;
3. replace the analyzer `ProjectReference`s — the analyzers ship inside the library package under
   `analyzers/dotnet/cs`, so a plain `PackageReference` delivers them and the `OutputItemType="Analyzer"`
   plumbing goes away;
4. delete the `IncludeJustDummiesInPackage` target and its `TargetsForTfmSpecificBuildOutput` hook from
   `FirstClassErrors.Testing.csproj`, and drop `PrivateAssets="all"` so the package declares an honest
   `JustDummies` dependency;
5. remove the seven `JustDummies.*` projects from `FirstClassErrors.sln`, the `dum` train from
   `tools/trains.sh`, `pack.sh` and `release.yml`, the `justdummies` scope from
   `tools/commit-lint/lint-commit-message.sh`, the three `build/stryker/justdummies*.json` configurations,
   and `.github/workflows/justdummies.yml` and `justdummies-mutation.yml`;
6. delete the source directories and `tools/justdummies-check/` last, once nothing references them;
7. run the full build and test suite.

The order matters: deleting the directories first turns every other step into a broken-build debugging
session.

## Consequences

### `FirstClassErrors.Testing` gains a real dependency

Today the package silently carries a copy of `JustDummies.dll` with no `<dependency>` entry, so a consumer
who also references `JustDummies` directly can end up with two copies at different versions and no diagnostic.
After the cutover the package declares its dependency, NuGet resolves one assembly, and the version becomes
visible and reviewable. This is the point of the change, not a side effect.

### The documentation that stays here changes subject, not owner

ADR-0011, ADR-0026 and ADR-0061 are **not** deleted, and neither is ADR-0006. They record decisions this
repository genuinely made, and the reasoning behind `FirstClassErrors.Testing`'s current shape is unreadable
without them. ADR-0011 and ADR-0022 also exist in `Reefact/just-dummies`, because they bind both products —
numbered [ADR-0003](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0003-host-dummies-as-a-standalone-package.md) and
[ADR-0007](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0007-floor-the-library-on-net-framework-4-7-2.md) there, since that repository renumbered its
base into a contiguous sequence after the extraction.

`doc/handwritten/for-users/ArbitraryTestValues.{en,fr}.md` documents this repository's testing package and
mentions JustDummies as its engine; it stays, and gains a link to the new repository.

### Issue references keep working in one direction only

Commit messages in `Reefact/just-dummies` older than 2026-07-31 cite issue and pull-request numbers of **this**
repository. Nothing there can be renumbered, so those references resolve here and must keep resolving:
this repository's issues must not be deleted, only closed.

### Until publication, both repositories carry the source

That duplication is real and is the cost of not shipping a broken build. It ends with the first
`JustDummies` release. Divergence risk in the interim is low — this repository should treat its copy as
frozen and land JustDummies changes in `Reefact/just-dummies` — but it is not zero, and it is the reason the
cutover should not wait long.

## Alternatives Considered

### Do the cutover now, against an unpublished version

Rejected: `dotnet restore` would fail with NU1102 for every contributor and every CI run until a package
exists. A repository that cannot build is worse than one that carries a temporary duplicate.

### Keep consuming JustDummies as a Git submodule, or by Git URL

Rejected. Both reintroduce the coupling the extraction removed, in a form that is harder to reason about than
the current `ProjectReference`: a submodule pins a commit rather than a version, and neither is expressible in
the published package's dependency graph — so `FirstClassErrors.Testing` would still have to embed the
assembly it cannot declare.

### Publish JustDummies from this repository one last time, then cut over

Considered because it would unblock the cutover immediately. Rejected because the first published version of a
package fixes where it is released from: the trusted-publishing policy, the repository URL in its metadata and
the SourceLink commits would all point here, and the very next release would have to contradict them.

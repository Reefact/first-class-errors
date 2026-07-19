# ADR implementation reference

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](adr-implementation-reference.fr.md)

This document owns implementation details extracted from Architecture Decision Records. ADRs remain the authoritative source for **what was decided and why**; this reference describes the current technical realization and may evolve without changing those decisions.

## Analyzer compatibility floor

Related decisions: [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.md).

The analyzer is compiled against the Roslyn floor declared by `RoslynFloorVersion` in `Directory.Build.props`. The package keeps the analyzer under `analyzers/dotnet/cs/`.

The current realization uses complementary guards:

* the analyzer package reference is pinned to the declared floor;
* `RoslynFloorTests` inspects assembly metadata and rejects newer `Microsoft.CodeAnalysis*` references;
* the analyzer workflow packs the real NuGet artifact and builds a sample with the floor SDK, proving both loading and packaging;
* Dependabot ignores automated updates for the floor-defining Roslyn packages.

When the floor changes, update the central property, the floor SDK used by the workflow and floor-check project, and the documented compiler requirement. The architectural change itself requires a new ADR that supersedes ADR-0001.

## Tooling runtime floor

Related decisions: [ADR-0002](../adr/0002-floor-the-tooling-runtime.md), [ADR-0022](../adr/0022-floor-the-library-on-net-framework-4-7-2.md).

The command-line tooling and out-of-process worker target the oldest supported .NET LTS runtime. The ordinary CI suite runs on the current development SDK, while dedicated floor jobs execute the shipped tooling on the oldest supported runtime.

The netstandard2.0 libraries have a separate support floor: .NET Framework 4.7.2. Dedicated Windows tests exercise the relevant libraries on the real .NET Framework runtime. Tooling projects remain modern-.NET-only.

## ADR pull-request check

Related decision: [ADR-0004](../adr/0004-check-every-pull-request-against-the-adr-base.md).

The ADR check is a maintainer and agent procedure, documented in `AGENTS.md`, that compares a change against accepted decisions and identifies whether it records, supersedes, or conflicts with an ADR.

The current GitHub workflow is manually dispatchable and therefore supports the procedure but does not, by itself, guarantee that every pull request was checked. Any future automated enforcement belongs in the workflow documentation and configuration rather than ADR-0004.

## Request Binder implementation contracts

Related decisions: [ADR-0007](../adr/0007-name-the-binder-terminals-new-and-create.md), [ADR-0008](../adr/0008-bind-nullable-value-type-properties-through-a-struct-constrained-overload.md), [ADR-0012](../adr/0012-fix-the-binder-options-before-binding-begins.md), [ADR-0014](../adr/0014-bind-a-required-list-by-presence-not-cardinality.md), [ADR-0017](../adr/0017-provide-a-configurable-application-wide-default-for-the-binder-options.md), [ADR-0018](../adr/0018-bundle-the-binders-structural-error-code-and-messages.md), [ADR-0019](../adr/0019-document-overridden-binder-errors-in-the-consumers-catalog.md), [ADR-0021](../adr/0021-bind-out-of-dto-arguments-as-peers-through-a-source-agnostic-entry.md).

Nullable value-type properties are selected through struct-constrained overloads so converter method groups operate on the underlying value type rather than `Nullable<T>`.

Binder options are fixed before binding starts. `Bind.WithOptions(...)` returns a reusable configured entry point and stores no per-request state. The application-wide default is frozen on first read and rejects later mutation.

Structural binder failures are represented by bundled definitions containing the error code and public/diagnostic messages. Consumers that override these definitions document them in their own catalog through the public documentation surface described by ADR-0019.

Out-of-DTO values enter through the source-agnostic binding entry and participate as peers in the same accumulation and construction flow as DTO-derived values. Exact overloads, generic constraints, names, and examples are API reference material and belong in the Request Binder user documentation and source code.

## GenDoc catalog compatibility

Related decision: [ADR-0010](../adr/0010-treat-gendocs-error-catalog-as-a-versioned-contract.md).

The generated error catalog is treated as a versioned compatibility artifact. Release automation compares the generated catalog against the baseline associated with the last compatible release and reports incompatible changes before publication.

The baseline is updated only by the release process after a successful compatible release. Workflow steps, commands, artifact paths, and recovery procedures are maintained in the workflow reference. In particular, maintainers must account for the failure mode where publication succeeds but the subsequent baseline update does not.

## Dummies generation contracts

Related decisions: [ADR-0006](../adr/0006-supply-arbitrary-test-values-from-a-seedable-source.md), [ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.md), [ADR-0013](../adr/0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md), [ADR-0015](../adr/0015-cap-any-combine-at-arity-eight.md), [ADR-0020](../adr/0020-materialize-dummies-only-through-generate.md).

Dummies is shipped as a standalone package with no dependency on the FirstClassErrors runtime package. Generation is unseeded by default; reproducible generation is selected explicitly and exposes the seed needed to replay failures.

Distinct collection generation first compares the requested count against the element generator's cardinality hint, when `ICardinalityHint` can provide one, net of any values pinned outside that domain via `Containing(...)` and any opaque draws requested via `ContainingAny(...)` — both widen what the generator itself must still supply rather than counting against it. A floating-point or decimal range is not treated as cheaply countable, since enumerating its representable values is type-specific bit-arithmetic disproportionate to the dummy use case, so such a generator only participates in the eager check when pinned to an explicit allow-list or a single value (`OneOf`, `Zero`, `Between(x, x)`), never through a wider range. When cardinality is unknown, generation uses a bounded draw and fails explicitly rather than looping forever. The bound is a safety mechanism, not a proof that every foreign or biased generator will succeed whenever enough distinct values theoretically exist. `CollectionState` and `ICardinalityHint` unify cardinality and membership behind one interface, so a generator with a finite domain cannot drift out of the eager perimeter through a comparer.

`Any.Combine` provides overloads up to arity eight. Higher arities are intentionally outside the supported convenience surface and should use composition or a domain-specific factory.

Materialization occurs only through `Generate()`. Builder operations describe generation and do not produce hidden side effects.

## Documentation-only public surfaces

Related decision: [ADR-0019](../adr/0019-document-overridden-binder-errors-in-the-consumers-catalog.md).

Public members introduced solely to make analyzer or documentation extraction possible must remain minimal, stable, and clearly tied to the catalog contract. Before adding another such member, consider whether metadata, generated descriptors, or analyzer-side discovery can satisfy the same need without expanding the runtime API.

## Maintenance rules

* Change this reference when implementation mechanics change but the decisions remain valid.
* Write a new ADR when the architectural choice, compatibility promise, or accepted trade-off changes.
* Keep links from each affected ADR to the relevant section of this reference.
* Do not move rationale, rejected alternatives, or architectural consequences out of ADRs.

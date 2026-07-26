# ADR-0026 | Rebase the testing package's arbitrary values on JustDummies

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0026-rebase-testing-arbitrary-values-on-dummies.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

`FirstClassErrors.Testing` is a published companion package (currently
`0.1.0-preview.1`). It supplies arbitrary test values through a static `Any`
facade backed by a private, context-local, seedable pseudo-random source, and
that same source also backs the `UseAny()` variants of its two other seams — a
freezable clock and freezable instance ids. This surface was decided in ADR-0006,
whose follow-up anticipated extracting the generic value engine into a
standalone, error-agnostic utility "if a second consumer appears", and which kept
the engine internally separable to that end.

Since ADR-0006, that utility shipped. ADR-0011 introduced `JustDummies`: a standalone,
dependency-free, error-agnostic fluent generator library (`IAny<T>`, materialized
through `Generate()`), hosted in this repository but referencing no
FirstClassErrors project — a boundary enforced by an architecture test. ADR-0011
named its intended early consumers as "this repository's own test projects, and
possibly `FirstClassErrors.Testing` later", and left an explicit open follow-up:
"Decide separately whether `FirstClassErrors.Testing` later re-bases its internal
value engine on `JustDummies`."

The current state carries the facts that bear on that decision:

* **Two independent engines coexist.** `FirstClassErrors.Testing` draws from its
  own seedable source; `JustDummies` draws from its own. Each exposes its own
  `Reproducibly`/seed scope, and the two seeds are unrelated: a test that mixes
  both facades cannot be replayed from a single reported seed.
* **The two `Any` facades collide as type names.** `FirstClassErrors.Testing.Any`
  and `JustDummies.Any` are both `static class Any`; a test file importing both
  namespaces cannot name `Any` unqualified.
* **`JustDummies` already covers every capability the rebase needs.** Its enum
  generator excludes members (`Except`, `DifferentFrom`, `OneOf`); its static
  `Any.*` entry points draw from an ambient context that `Any.Reproducibly(...)`
  pins for a scope (with `Any.WithSeed` for an isolated one); and `As`/`Combine`
  turn constrained primitives into domain values. ADR-0020 made `Generate()` the
  sole materialization, removing the implicit conversions.
* **The error vocabulary cannot move into `JustDummies`.** `Testing`'s `ErrorCode`,
  meaningful-enum (`Transience`, `InteractionDirection`), and message helpers
  reference FirstClassErrors types, which the ADR-0011 boundary forbids `JustDummies`
  to reference.
* **`JustDummies` is not yet on NuGet.** ADR-0011 gives it no release train until its
  first publication; within this repository it is consumed only through a project
  reference.

## Decision

`FirstClassErrors.Testing` sources every arbitrary value from `JustDummies` instead
of a private engine: its `Any` facade and seedable source are removed, its
freezable clock and instance-id seams draw from JustDummies' ambient reproducible
context, and the error vocabulary it still owns is exposed as named domain
factories — `ErrorCodeFactory`, `TransienceFactory`, `DiagnosticMessageFactory`,
and peers — each returning a materialized value directly (the common case) and
exposing an `IAny<T>` generator through a distinct method where composition is
needed.

## Rationale

* **One engine, one seed story — the single-source spirit of ADR-0006 at repository
  scale.** Two engines meant two `Reproducibly` scopes whose seeds do not compose,
  so a test drawing on both facades could not be replayed from one seed. Sourcing
  every value from JustDummies' ambient context puts primitives, domain values, the
  clock, and the instance ids under a single `Any.Reproducibly(...)`, so one
  reported seed replays the whole run. This is the same "a single source, seeded
  once" property ADR-0006 chose, extended to cover the generic engine that now
  lives outside the package.
* **It realizes the follow-up ADR-0006 and ADR-0011 both anticipated.** The
  generic engine ADR-0006 kept "internally separable" now exists as `JustDummies`, and
  the "second consumer" it waited for has arrived — this repository's test
  projects. Re-basing `Testing` on `JustDummies` resolves ADR-0011's open question
  rather than maintaining a parallel engine indefinitely.
* **The rebase adds no capability gap.** Every behaviour the package needs already
  exists in JustDummies: member exclusion for meaningful enums, an ambient reproducible
  context for the clock and ids, and `As`/`Combine` for assembling domain values.
  Nothing has to be added to JustDummies as a precondition, so the package is shaped by
  a real consumer rather than a finalization taken in the abstract.
* **The vocabulary stays in `Testing`, but as factories, not a facade.** Because
  the ADR-0011 boundary forbids the error vocabulary from living in `JustDummies`, it
  remains in `Testing`; re-expressing it as named `…Factory` types rather than a
  second `Any` removes the `Testing.Any`/`JustDummies.Any` type collision, leaving
  `JustDummies.Any` as the one `Any` a test names while domain values come from
  clearly-named factories.
* **Value by default, generator on demand — without reviving the hazard ADR-0020
  removed.** The dominant call needs one arbitrary value, so a factory returns it
  directly — matching the arbitrary-value test helpers the repository already uses —
  and exposes an `IAny<T>` generator through a distinct method only for the minority
  of sites that compose (`Any.ListOf`, `Combine`, `OrNull`). A named method that
  internally calls `Generate()` is not the implicit conversion ADR-0020 removed —
  the draw is an explicit, visible call, not a widening disguised as an assignment —
  so the terse value form costs none of that decision's guarantees. Reserving the
  plain, common-case name for the value echoes ADR-0005.
* **This is the cheapest moment.** `Testing` is `0.1.0-preview.1`, a pre-stable
  package with no compatibility guarantee, so removing `Any` costs no consumer
  migration ceremony now; the same pre-1.0 reasoning ADR-0020 used to drop the
  JustDummies conversions applies here.

## Alternatives Considered

### Keep `Testing`'s own engine and let the test projects use `JustDummies` directly

Considered because it is the least work and ships today: nothing in `Testing`
changes, and the test projects simply add `JustDummies` for the values its facade does
not cover. Rejected because it institutionalizes the two-engine state — two
`Reproducibly` seeds that do not compose and a `Testing.Any`/`JustDummies.Any` type
collision — which is the cross-test fragmentation the single-source spirit of
ADR-0006 exists to avoid, now reproduced at repository scale.

### Move the error vocabulary into `JustDummies`

Considered because it would leave exactly one `Any` and one home for arbitrary
values. Rejected because `JustDummies` is error-agnostic by ADR-0011, a boundary an
architecture test enforces; the error vocabulary references FirstClassErrors types
and cannot live there without breaking that promise.

### Delete the error vocabulary and inline `As(...).Generate()` at call sites

Considered because it removes a public surface outright. Rejected because it
scatters the error vocabulary — and its "recognizable as arbitrary" convention —
across every consumer, and strips shipped helpers from a published package for no
gain over keeping them as thin factories.

### Keep the `Any` facade name in `Testing`, only rebased

Considered to minimize call-site churn by preserving the familiar `Any.ErrorCode()`
shape. Rejected because two `static class Any` in two namespaces remain ambiguous
whenever both are imported, which forfeits the very clarity the rebase is meant to
deliver — a single `Any`.

### Return `IAny<T>` uniformly from every factory

Considered for strict consistency with JustDummies' recipe-versus-value model.
Rejected because it taxes the dominant "give me one arbitrary value" case with a
mandatory `.Generate()` for no benefit: a named value method is not the
implicit-conversion hazard ADR-0020 removed, so value-by-default keeps that
decision's guarantees while staying terse.

## Consequences

### Positive

* A single arbitrary-value engine and a single `Reproducibly`/seed scope across
  the repository: a test mixing primitives, domain values, the clock, and instance
  ids replays from one reported seed.
* `JustDummies.Any` is the only `Any`; the type-name collision is gone, and domain
  values are read from explicitly-named factories.
* `Testing` stops maintaining a parallel value engine; the generic machinery lives
  once, in `JustDummies`, shaped by a real in-repository consumer.

### Negative

* A breaking change to a published package: `Testing.Any` and its `Reproducibly`
  are removed, and consumers move to `JustDummies.Any` and the factories. Acceptable
  at `0.1.0-preview.1`, and the reason the decision is taken before a stable
  release.
* `Testing` gains a dependency on `JustDummies`. While `JustDummies` is not on NuGet, the
  `Testing` package must carry `JustDummies` inside its own artifact to stay
  restorable — an interim arrangement, not the end state.
* The test suite migrates from `Any.*` to `JustDummies.Any.*().Generate()` and the new
  factories, and the reproducibility/determinism coverage of the removed facade is
  reworked onto the new surface.

### Risks

* Carrying `JustDummies` inside the `Testing` artifact becomes a type-identity hazard
  the moment `JustDummies` is independently referenceable on NuGet — two `JustDummies`
  assemblies of the same identity but distinct origin — precisely because JustDummies
  types appear in `Testing`'s public API. Mitigated by the follow-up to switch to a
  real NuGet dependency at JustDummies' first publication.
* A caller carrying the old `Testing.Any` mental model may reach for a removed
  member; the risk is bounded because the omission is a compile-time error with an
  actionable message, never a silent wrong value — the same class of risk ADR-0020
  accepted.
* The "value by default, generator through a distinct method" rule holds by review
  and documentation until, if ever, tooling enforces it — the same reliance ADR-0006
  already accepted for the "arbitrary ⇒ use the source" habit.

## Follow-up Actions

* On acceptance, ADR-0006 is superseded by this ADR (its status flips to
  *Superseded* with a link here), and ADR-0011's open follow-up — whether
  `Testing` re-bases on `JustDummies` — is resolved by this decision.
* Switch the `JustDummies` dependency `Testing` carries inside its package to a NuGet
  `PackageReference` the moment `JustDummies` is first published, unwinding the interim
  arrangement and removing the double-assembly hazard.
* Update the user testing guide and the `Testing` package README, in English and
  French in lockstep: `Any` is removed, arbitrary values are sourced from
  `JustDummies`, and the domain factories are introduced.
* Add a small `FirstClassErrors.Testing.UnitTests` project holding only the
  behavioural contract tests the package still owns — clock and instance-id
  reproducibility under `Any.Reproducibly`, and meaningful-enum factories never
  yielding the sentinel — and drop the wrapper-value assertions now covered
  transitively and by `JustDummies.UnitTests`.

## References

* ADR-0006 — Supply arbitrary test values from a single seedable source: the
  decision this supersedes, and the follow-up (extract the generic engine for a
  second consumer) this realizes.
* ADR-0011 — Host JustDummies as a standalone package: the error-agnostic boundary that
  keeps the vocabulary in `Testing`, and the open follow-up this resolves.
* ADR-0020 — Materialize dummies only through `Generate()`: the implicit-conversion
  hazard a named value factory does not reintroduce, and the pre-1.0 reasoning
  reused here.
* ADR-0005 — Reserve the plain factory name for the Outcome-returning variant: the
  prior naming decision in the same spirit, the plain name serving the common case.

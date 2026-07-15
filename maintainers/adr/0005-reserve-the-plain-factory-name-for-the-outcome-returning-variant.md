# ADR-0005 | Reserve the plain factory name for the Outcome-returning variant

**Status:** Proposed
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

FirstClassErrors exists to make the failure path of an operation explicit: an
error is a value a caller returns and inspects (`Outcome`/`Outcome<T>`), not an
exception that travels invisibly. Throwing is still supported as an opt-in
convenience — the README frames it as a choice ("you choose how it travels") —
but returning an `Outcome` is the library's default, first-class path.

Factory-style operations therefore commonly come in two variants over the same
inputs: one returns `Outcome<T>`, one throws on failure (e.g.
`Temperature.FromKelvin` / `TryFromKelvin`, the throwing one implemented as the
Outcome one followed by `GetResultOrThrow()`). C# cannot overload on return type
alone, so the two variants must carry distinct names — one of them has to be
marked.

The .NET BCL already has a pervasive convention for one of these forms. `TryXxx`
means `bool TryXxx(..., out T result)`: it returns `bool`, yields its value
through an `out` parameter, and reads inline in an `if`
(`if (int.TryParse(s, out var n))`). Developers, IDEs, and analyzers all assume
that exact shape on sight of a `Try` prefix. Two facts follow:

* **Shape.** A `TryXxx` in this library returns `Outcome<T>` — it borrows the
  name without the `bool`+`out` shape, so it cannot be used inline in an `if`
  and quietly breaks the expectation the name sets.
* **Arrangement.** In the BCL the *plain* name is the throwing one (`Parse`) and
  the *marked* name is the safe one (`TryParse`). The current code follows that
  arrangement (`FromKelvin` throws, `TryFromKelvin` is safe) — placing the
  plain, unmarked name on the throwing path, i.e. the path this library treats
  as the exception rather than the default.

`Outcome<T>` already exposes `GetResultOrThrow()`, so "throw on failure" has
established vocabulary in the library.

## Decision

The Outcome-returning variant carries the plain factory name (`FromKelvin`,
returning `Outcome<T>`); the throwing variant is marked with an `OrThrow` suffix
(`FromKelvinOrThrow`).

## Rationale

* **One principle, applied to this library's default.** The convention worth
  keeping from the BCL is not the word `Try` but the rule underneath it: *the
  variant that departs from the prevailing default carries the marker.* The
  BCL's default is to throw, so its safe variant is marked (`Try`); this
  library's default is to return an `Outcome`, so its throwing variant is the
  departure and takes the marker (`OrThrow`). Same rule, opposite baseline —
  which is precisely why reusing `Try` here would be incoherent, not merely a
  name clash.
* **The risky call is the visible one.** The library's whole thesis is that an
  exception is the failure mode that hides at the call site. `OrThrow` puts it
  back in the name: `FromKelvinOrThrow(k)` announces it can throw; `FromKelvin(k)`
  announces it hands back a value to inspect. The plain, easy-to-reach name is
  the safe one.
* **No borrowed expectation.** Dropping `Try` removes the false promise of a
  `bool`+`out` inline shape the method never had.
* **Reuses existing vocabulary.** `OrThrow` echoes `GetResultOrThrow()`, and the
  throwing factory is literally `Xxx(...).GetResultOrThrow()` — the name mirrors
  the implementation instead of introducing a new term.

## Alternatives Considered

The real choice is *which of the two variants carries the marker*, not merely
which word to use.

### Mark the safe variant, leave the throwing one plain (the status-quo arrangement)

This is the BCL arrangement: the plain name throws, the marked name returns an
`Outcome`. Whatever marker is chosen for the safe side — `Try` (status quo),
`XxxOrError`, `AttemptXxx`, `XxxSafe` — the arrangement shares one flaw: it
leaves the *throwing* call unmarked, so the one call that can fail invisibly has
the shortest, most default-looking name. For a library whose reason to exist is
making the failure path explicit, that is backwards. `Try` additionally clashes
with the BCL shape; the others avoid the clash but not the underlying flaw.

### Mark the throwing variant with a different word (`XxxThrows`, `XxxUnsafe`)

Same arrangement as the decision, different suffix. `OrThrow` is preferred only
because it already exists in the API surface (`GetResultOrThrow`); introducing a
synonym would fragment the vocabulary.

## Consequences

### Positive

* Every factory's failure mode is legible from its name at the call site: a
  plain `FromKelvin` hands back an `Outcome` to inspect, while `FromKelvinOrThrow`
  announces up front that it can throw.
* The plain, most-reached-for name now belongs to the library's first-class
  path, so the variant a caller falls into by default is the inspectable one.
* No method advertises the BCL `Try`/`out` shape it does not implement.

### Negative

* The existing factories in `FirstClassErrors.Usage` must be renamed: the
  Outcome-returning `TryFromKelvin` / `TryFromCelsius` drop the `Try` prefix, and
  the throwing `FromKelvin` / `FromCelsius` take the `OrThrow` suffix. Their call
  sites (e.g. the `AbsoluteZero` initializer) and their XML-doc summaries — the
  "Attempts to create…" wording — must be updated to match.
* The documentation examples that use the `Try` prefix (`GettingStarted`,
  `UsagePatterns`, in both EN and FR) must be reworded to the new names.
* This is a breaking rename of public members. The library is pre-release and
  unpublished on NuGet with no external consumers, so it carries no downstream
  migration cost today.

### Risks

* The English and French docs must be renamed in lockstep; a partial pass leaves
  the canonical docs and their translation describing different APIs.
* Until the convention is enforced by tooling, it holds only through review and
  documentation, so a new `TryXxx`-returning-`Outcome` method can slip back in
  unnoticed.

## Follow-up Actions

* Rename `TryFromKelvin` / `TryFromCelsius` to the plain name, add the `OrThrow`
  siblings, and update their call sites and XML-doc summaries.
* Reword the EN + FR documentation examples that use the `Try` prefix
  (`GettingStarted`, `UsagePatterns`) in lockstep.
* Enforcement of the convention by an analyzer is deferred to a later revision.

## References

* `README.md` — the "you choose how it travels" framing that makes `Outcome` the
  default path.
* ADR-0003 — related prior decision on Outcome API naming (context only; not a
  precedent for this choice).

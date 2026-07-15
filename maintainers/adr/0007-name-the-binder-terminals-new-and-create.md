# ADR-0007 | Name the binder terminals New and Create

**Status:** Proposed
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

* `FirstClassErrors.RequestBinder` binds each property of a request DTO into a value
  object, then a **terminal** assembles the bound values into the command (or query)
  and returns `Outcome<TCommand>`.
* Consumers assemble that command in two shapes that differ in whether the assembly
  step can itself fail: a **total constructor** (`new Command(...)`) cannot fail,
  because every field was already validated one by one during binding; a
  **validating factory** (`Command.Create(...)` returning `Outcome<TCommand>`) can
  still fail, because it enforces a cross-field rule — such as check-out being after
  check-in — that no single field could check on its own.
* C# forbids overloading on return type alone, and its overload resolution does not
  traverse the implicit `TCommand` → `Outcome<TCommand>` conversion. Two same-named
  terminal overloads — one taking a `TCommand`-returning assembler, one taking an
  `Outcome<TCommand>`-returning assembler — are therefore ambiguous for a lambda that
  returns `Outcome<TCommand>`: both are applicable with a different inferred
  `TCommand`, and the compiler rejects the call.
* A single terminal taking only a `TCommand`-returning assembler forces a consumer
  whose factory returns `Outcome<TCommand>` to nest the result as
  `Outcome<Outcome<TCommand>>`, and leaves a cross-field rule no place to run.
* The library already reserves the plain factory name for the `Outcome`-returning
  variant (ADR-0005); across the codebase the `Parse` / `Create` factories of value
  objects return `Outcome<T>`, so "a plainly-named factory hands back an `Outcome`"
  is established vocabulary.
* The assembler receives a `BindingScope`, a `readonly ref struct`; a ref struct
  cannot be a generic type argument, so an assembler cannot be a `Func<>` and must be
  a named delegate type.
* The library is pre-release, unpublished on NuGet with no external consumers, so a
  naming choice on this new API carries no downstream migration cost.

## Decision

The request binder exposes two distinctly-named terminals — `New`, taking a total
constructor that returns the command, and `Create`, taking a validating factory that
returns `Outcome<TCommand>` whose result is flattened — instead of a single terminal
or two same-named overloads.

## Rationale

* Distinct names give each terminal exactly one signature, so the overload ambiguity
  that two same-named terminals would raise for an `Outcome<TCommand>`-returning
  lambda cannot arise: the problem is removed at the name rather than worked around at
  the call site.
* Keeping two terminals — rather than one `Outcome`-only terminal — lets a total
  constructor stay total: the common case constructs the command directly without
  wrapping it in a success `Outcome`, while `Create` flattens the validating case so a
  factory's `Outcome<TCommand>` is never nested. This addresses both failures the
  Context describes (the nested outcome and the cross-field rule with nowhere to run).
* Naming each terminal after the assembler it takes makes the choice self-selecting: a
  consumer writing a `new` reaches for `New`, one writing a `.Create` factory reaches
  for `Create`. The name reuses the constructor-versus-factory distinction every C#
  developer already holds, so it needs no separate lookup.
* The mnemonic is coherent with ADR-0005 rather than in tension with it: ADR-0005
  reserves the plain factory name for the `Outcome`-returning variant, and `Create`
  here is precisely the `Outcome`-returning terminal. ADR-0005's axis
  (`Outcome`-returning versus throwing) is orthogonal to this one (a bare-value
  assembler versus an `Outcome`-returning assembler); neither terminal throws, so no
  `OrThrow` marker applies.
* `Create` returns the factory's failure unchanged rather than re-wrapping it, because
  a cross-field rule is a domain concern the factory owns, whereas the binder's
  envelope groups argument-binding failures. Keeping the two channels separate lets a
  caller tell a malformed argument from a rejected valid combination.
* The pre-release status means the naming is settled now, when there are no consumers
  to migrate.

## Alternatives Considered

### One terminal taking an Outcome-returning assembler

Considered because it is the smallest surface — a single name, with the flatten
covering both cases once the caller wraps a total constructor in a success `Outcome`.

Rejected because it forces the common, cannot-fail case to wrap every construction in
a success `Outcome`, surfacing result-plumbing the total case does not otherwise need.

### Two same-named `Build` overloads

Considered because overloading is the idiomatic C# way to accept two argument shapes
under one verb.

Rejected because a lambda returning `Outcome<TCommand>` is applicable to both
overloads with a different inferred `TCommand`, overload resolution does not prefer
one over the other, and the call therefore does not compile.

### `Build` and `BuildValidated` (one bare name, one suffixed)

Considered as distinct names that already avoid the ambiguity.

Rejected as asymmetric: a bare verb beside a suffixed variant reads as "the real one
and a special case", when the two are peers over different assembler shapes.

### Semantics-first symmetric pairs (`Assemble` / `Validate`, `BuildFrom` / `BuildThrough`)

Considered because the words state the can-fail / cannot-fail semantics for a reader
discovering the API.

Rejected in favour of `New` / `Create`, which optimise instead for the developer
writing the call: the name matches the constructor or factory already in hand, reusing
an existing convention rather than teaching new vocabulary.

## Consequences

### Positive

* The overload ambiguity is impossible: one name, one signature each.
* A total constructor stays unwrapped, and a validating factory composes without a
  nested `Outcome`.
* The terminal name is a usage mnemonic tied to the constructor or factory the
  consumer already writes, and reuses the codebase's `Create`-returns-`Outcome`
  convention (ADR-0005).
* Argument-binding failures and a factory's cross-field failure stay distinguishable
  at the call site.

### Negative

* `New` and `Create` are near-synonyms in English, so "only `Create` can fail" is
  carried by the constructor-versus-factory convention rather than by the words; a
  reader unfamiliar with the convention must consult the API docs.
* `New` is an unusual method identifier — valid in C# (only the lowercase `new` is the
  keyword) but requiring escaping (`[New]`) to call from VB.NET.
* Two public terminals and two public assembler delegate types to document rather than
  one.

### Risks

* Without tooling that enforces it, a later terminal for a third assembler shape could
  drift from the convention and dilute the mnemonic; mitigated for now by this ADR and
  by review.

## References

* ADR-0005 — reserve the plain factory name for the Outcome-returning variant, the
  convention `Create` here reuses.
* ADR-0003 — unify Outcome value mapping under `Then`, context on naming the Outcome
  surface for intent rather than mechanics.
* Pull request #126 — the request binder feature this terminal belongs to.

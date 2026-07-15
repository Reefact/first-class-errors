# ADR-0003 | Unify Outcome value mapping under Then

**Status:** Accepted
**Date:** 2026-07-15
**Decision Makers:** Reefact

## Context

* `Outcome` and `Outcome<T>` are the library's success-or-failure result type; they compose through a small fluent surface.
* Two composition operations existed under distinct names: `Then` continued with a function that may itself fail (it returns an `Outcome`), and `To` transformed a successful value with a function that cannot fail (it returns a plain value). These are the *bind* and *map* operations of functional programming.
* The library's stated goal is an API named for **intent**, not for functional-programming mechanics; the FAQ defends this exact choice when contrasting `Outcome` with a generic `Result<T, Error>`.
* Whether a composition step can fail is already expressed by the return type of the function the caller passes: an `Outcome`-returning function can fail; a value-returning function cannot.
* C# forbids overloading by return type alone, and its overload resolution selects the more specific parameter type when several candidates apply. The map/bind distinction is therefore enforced by the type system at each call site regardless of the method name.
* The library is pre-release: unpublished on NuGet, with no external consumers, so a breaking rename carries no downstream migration cost today.

## Decision

The value-mapping operation on `Outcome` is exposed as an overload of `Then` instead of as a separate `To` method.

## Rationale

* Keeping two names surfaces the functional-programming map/bind mechanics in the API, which contradicts the goal of naming operations for intent. A single `Then` reads as "then do this next" — the caller's actual intent — whichever kind of function follows.
* The information the `To`/`Then` names encoded — *can this step fail?* — is redundant with the return type of the passed function, which the reader already sees at the call site. Dropping the second verb loses no information the code does not already carry.
* Merging the two under one name is safe: C# selects the value-mapping overload for a value-returning function and the binding overload for an `Outcome`-returning one, so a result always flattens and never nests into `Outcome<Outcome<T>>`. This resolution is deterministic and was verified stable across the C# language versions consumers compile with (see the pull request and its resolution lock-in tests).
* The pre-release status means the cost of the change — a breaking rename — is paid now, when there are no consumers, rather than later.

## Alternatives Considered

### Keep `To` and `Then` as distinct operations (status quo)

Considered because it mirrors the established `map`/`bind` naming familiar to functional-programming practitioners and keeps each operation unambiguous on its own.

Rejected because it exposes the very mechanics the library deliberately hides, and the fail/no-fail distinction it encodes is already carried by the passed function's return type — the second name adds vocabulary without adding information.

### Keep `To` as a deprecated alias of `Then`

Considered as a softer, non-breaking migration path.

Rejected because there are no consumers to migrate, and an alias would preserve the map/bind duality the decision exists to remove.

## Consequences

### Positive

* The composition surface is one intent-named verb, consistent with the rest of the API.
* Call sites read as a single intent whether or not a step can fail.
* The map/bind distinction is still enforced where it matters — by the type system at each call — with no risk of nested outcomes.

### Negative

* `To` is removed: a breaking change to the public API.
* The map operation can no longer be named distinctly at a call site; a reader infers "cannot fail" from the passed function's return type rather than from the verb.
* Deliberately obtaining a nested `Outcome<Outcome<T>>` through the mapping overload is no longer possible (never an intended use).

### Risks

* A future change in C# overload resolution could, in principle, alter which overload is selected. Mitigated by lock-in tests that assert the resolution and fail — at compile time and at runtime — if it ever regresses.

## Follow-up Actions

* Update the user-facing documentation (Outcome guide, Core Concepts, Usage Patterns) to use `Then` for value mapping.

## References

* Pull request: [#127](https://github.com/Reefact/first-class-errors/pull/127)
* FAQ — "Why not use a generic `Result<T, Error>`?" (the intent-over-mechanics rationale).

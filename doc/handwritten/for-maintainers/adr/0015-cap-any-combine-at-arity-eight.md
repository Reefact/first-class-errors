# ADR-0015 | Cap Any.Combine at arity eight

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0015-cap-any-combine-at-arity-eight.fr.md)

**Status:** Accepted
**Date:** 2026-07-19
**Decision Makers:** Reefact

## Context

Dummies composes differently typed generators into larger objects through `Any.Combine`, preserving constructor-based domain validation without reflection.

C# has no heterogeneous variadic generics, so each supported arity requires a distinct public overload. Low arities alone force nested composition or positional tuples for larger constructors, while an unlimited surface would create repetitive API and documentation with diminishing value.

Very wide constructors can also indicate missing intermediate domain concepts.

## Decision

`Any.Combine` provides flat heterogeneous overloads from arity two through arity eight and deliberately stops there.

## Rationale

A flat call with named lambda parameters is materially clearer than nested composition or positional tuple access for the object sizes commonly encountered in domain code.

Eight is a pragmatic convenience ceiling rather than a mathematical property of DDD. It covers the intended large-object use cases while keeping the manually maintained surface bounded and allowing wider constructors to remain a design signal.

The unavoidable parameter-count warnings on the largest overloads are an explicit local trade-off, not a general relaxation of the repository's code-quality rules.

Exact signatures, documentation, and analyzer suppressions are implementation details recorded in the [ADR implementation reference](../specifications/adr-implementation-reference.md#dummies-generation-contracts) and the Dummies API reference.

## Alternatives Considered

### Keep only the smallest overloads

Considered because it minimizes API surface. Rejected because larger compositions become substantially less readable through nested lambdas or positional tuple members.

### Use a fluent tuple-accumulating builder

Considered to avoid one overload per arity. Rejected because it moves the same complexity into the builder and still exposes positional structure at the call site.

### Extend to the maximum arity supported by `Func`

Considered for completeness. Rejected because the maintenance cost and normalization of extremely wide constructors outweigh the marginal convenience.

### Accept only homogeneous generators through `params`

Considered because it is naturally variadic. Rejected because it does not serve the differently typed constructor parameters for which `Combine` exists.

## Consequences

### Positive

* Common large objects compose in one readable, reflection-free call.
* The convenience API remains deliberately bounded.
* Extremely wide construction stays visible as a possible design problem.

### Negative

* Several hand-maintained overloads remain part of the public surface.
* The largest overloads require localized analyzer suppressions.
* The ceiling is heuristic and may not fit every domain.

### Risks

* A recurring legitimate need above arity eight may appear. Mitigation: higher arities can be added compatibly through a new decision if evidence shows that the current ceiling is too low.

## Follow-up Actions

* Keep the supported arity range explicit in the Dummies documentation.
* Consider homogeneous variadic composition separately if a real use case emerges.

## References

* [ADR implementation reference — Dummies generation contracts](../specifications/adr-implementation-reference.md#dummies-generation-contracts)
* [ADR-0011](0011-host-dummies-as-a-standalone-package.md)
* [ADR-0024](0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md) — authorizes this editorial extraction.

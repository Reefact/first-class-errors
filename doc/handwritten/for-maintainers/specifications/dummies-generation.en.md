# Dummies generation contract specification

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](dummies-generation.fr.md)

This page describes the current generation mechanics behind
[ADR-0011](../adr/0011-host-dummies-as-a-standalone-package.md),
[ADR-0013](../adr/0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md),
[ADR-0015](../adr/0015-cap-any-combine-at-arity-eight.md), and
[ADR-0020](../adr/0020-materialize-dummies-only-through-generate.md).

## Package boundary

`Dummies` is an independent `netstandard2.0` package with no project reference to
FirstClassErrors. An architecture test guards that boundary. Its public generator
contract is `IAny<T>` and values are materialised explicitly through `Generate()`.

## Constraint and generation model

* Generator specifications are immutable; applying a constraint returns a new
  specification.
* Contradictory constraints fail when declared with
  `ConflictingAnyConstraintException`.
* A satisfiable value is laid out directly from the complete specification; scalar
  generation is not implemented as generate-then-filter or as an unbounded retry.
* Reproducible execution is available through `Any.Reproducibly(seed, ...)`; a
  generation failure carries the seed when one is active.

## Distinct collections

Distinctness is enforced in two layers.

### Known cardinality

Internal generators may implement `ICardinalityHint` and advertise an upper bound
on the number of distinct values they can produce. When the requested minimum or
exact count exceeds that bound, declaration fails eagerly.

The hint is intentionally not part of public `IAny<T>`: foreign and derived
generators may be unable to report a sound bound.

### Unknown or comparer-reduced cardinality

Generation deduplicates into a `HashSet<T>` using the configured comparer. It is
bounded by a collision budget and throws `AnyGenerationException` if enough fresh
values are not obtained.

The current budget in `CollectionState<T>` is:

* `64 × cardinality` when a known cardinality is at most 1,000,000;
* otherwise `64 × target count`;
* with a minimum of 10,000 and a maximum of `int.MaxValue`.

For the main fill, the collision counter resets after every fresh value. A
`ContainingAny(...)` generator receives the same budget while finding its one
fresh contribution. Exhaustion reports the reached count, target count, source of
the collision, and replay seed when available.

A bounded probabilistic draw may fail for a satisfiable but highly biased foreign
generator. That is an accepted consequence of supporting generators whose domain
and distribution are unknowable; it is not described as impossible or
astronomically unlikely. Real false exhaustion is a signal to revisit the budget
or expose a richer capability.

## Heterogeneous composition

`Any.Combine` provides flat, reflection-free overloads for two through eight
generators. Each overload generates its parts and invokes the caller's composer.
Arity seven and eight exceed Sonar rule S107 because the composer is an additional
parameter; the local suppressions are intentional and must retain their
justification.

Higher heterogeneous arities are not part of the public contract. Adding them is
non-breaking but requires revisiting the architectural ceiling. Homogeneous
`params` composition remains an independent possible feature.

## Sources of truth

* `Dummies/CollectionState.cs` and `Dummies/ICardinalityHint.cs` — distinctness,
  budget, and cardinality mechanics.
* `Dummies/Any.cs` — public facade and `Combine` overload ceiling.
* Dummies unit and property tests — eager conflict detection, reproducibility,
  comparer behaviour, and arity coverage.
* Dummies user documentation — supported fluent surface.

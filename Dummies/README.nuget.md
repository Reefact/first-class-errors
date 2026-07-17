# Dummies

A fluent DSL for generating arbitrary yet **valid** test values — *dummies*: values a
test needs but never asserts on.

## The idea

A test's `Arrange` is full of values the test does not check: an order reference, a
quantity, a label. A hand-picked literal reads as significant even when it is not.
`Dummies` makes the incidental legible as incidental — and, when the value must cross
an invariant (a value object, a contract precondition), the constraints express *that
invariant*, never what the test asserts:

    string code = Any.String()
        .NonEmpty()
        .WithMaxLength(50)
        .StartingWith("ORD-");

Read it as: *any* string that satisfies these constraints. The exact value does not
matter — and that is the point.

## What's inside

- **Fluent, typed generators** (`Any.String()`, `Any.Int32()`, ...) implementing
  `IAny<T>`, with implicit conversion to the generated type.
- **Values built to satisfy the constraints** — never generate-then-filter, no retry
  loops.
- **Conflicting constraints fail fast** with a clear, actionable
  `ConflictingAnyConstraintException` at the moment the conflicting constraint is
  declared — for example `Any.String().WithLength(3).StartingWith("ORD-")`.
- **Composition without reflection**: `.As(factory)` turns a constrained primitive
  into a domain value object; `Any.Combine(...)` assembles larger objects through
  constructor lambdas.
- **Reproducible runs**: wrap a test in `Any.Reproducibly(...)` and a failing run
  reports the seed to replay; `Any.WithSeed(seed)` gives an isolated, deterministic
  context.

## Example

    using Dummies;

    OrderReference reference = Any.String()
        .StartingWith("ORD-")
        .WithLength(12)
        .As(OrderReference.Create)
        .Generate();

## What it is not

No realistic fake data (names, emails, addresses), no object-graph auto-filling, no
reflection. Small, deterministic, explicit.

## Documentation

Full documentation on GitHub:

https://github.com/Reefact/first-class-errors

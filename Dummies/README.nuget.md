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

- **Fluent, typed generators** implementing `IAny<T>`, with implicit conversion to
  the generated type, across the .NET simple types: `String`, `Char`, every integer
  width (`SByte`/`Byte`/`Int16`/`UInt16`/`Int32`/`UInt32`/`Int64`/`UInt64`),
  `Double`/`Single`/`Decimal` (finite values only — never NaN or infinities),
  `Bool`, `Guid`, `Enum<T>` (declared members only), `TimeSpan`, `DateTime` (UTC)
  and `DateTimeOffset`. On modern targets (`net8.0`) the surface extends to
  `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half`; the package also targets
  `netstandard2.0` for the widest reach.
- **Domain vocabulary where it belongs**: dates constrain with
  `After`/`Before`/`Between`, quantities with `Positive`/`Between`/`NonZero`,
  identities with `NonEmpty`/`DifferentFrom` — and deliberately no clock-relative
  constraints: a reproducible test pins its reference instants explicitly.
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

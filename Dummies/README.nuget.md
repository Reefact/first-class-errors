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
        .StartingWith("ORD-")
        .Generate();

Read it as: *any* string that satisfies these constraints. The exact value does not
matter — and that is the point.

## What's inside

- **Fluent, typed generators** implementing `IAny<T>`, materialized through
  `.Generate()`, across the .NET simple types: `String`, `Char`, every integer
  width (`SByte`/`Byte`/`Int16`/`UInt16`/`Int32`/`UInt32`/`Int64`/`UInt64`),
  `Double`/`Single`/`Decimal` (finite values only — never NaN or infinities),
  `Boolean`, `Guid`, `Enum<T>` (declared members only), `TimeSpan`, `DateTime` (UTC)
  and `DateTimeOffset`. On modern targets (`net8.0`) the surface extends to
  `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half`; the package also targets
  `netstandard2.0` and runs on **.NET Framework 4.7.2+**, .NET Core 2.0+ and .NET 5+
  for the widest reach — with the .NET Framework 4.7.2 floor exercised in CI, not
  merely advertised.
- **Strings from a regex**: `Any.StringMatching(pattern)` generates arbitrary strings
  that match a regular expression — the dummy for a format-validated value object.
  Home-grown (zero dependencies) over the regular subset of the pattern language; a
  non-regular construct (a lookaround, a backreference) is refused with a clear error
  rather than a silently non-matching value.
- **Custom alphabets**: `Any.String().WithChars("αβγδε")` draws the string from an
  explicit character pool — the general form of the built-in `Alpha`/`Numeric`/
  `AlphaNumeric` sets, and the way to reach non-ASCII text (accents, Greek, Cyrillic,
  CJK) without a `StringMatching` literal. It stays within the Basic Multilingual Plane
  and rejects a surrogate: an emoji or other astral character is an atomic grapheme, not
  a character family, so draw those as whole strings with `OneOf("😀", "🎉")` instead.
  Anchored fragments must be drawn from the pool, or the conflict is reported at
  declaration.
- **Strings from an explicit set**: `Any.String().OneOf("EUR", "USD", "GBP")` draws from
  a fixed, closed list — the dummy for a value whose domain is a short enumeration (a
  currency code, a well-known name). A *terminal* generator, like `StringMatching`: the
  set is the whole specification, duplicates collapse, and the draw is uniform and
  reproducible under a seed.
- **Any value from an explicit pool**: `Any.OneOf(eur, usd, gbp)` draws one value from a
  caller-supplied set of arbitrary values or domain objects, and `Any.ElementOf(orders)`
  does the same from a collection already held (a list, a LINQ result). This is the
  seed-aware answer to "any of these" — replacing a hand-rolled
  `pool[new Random().Next(...)]` that would ignore the seed and break `Reproducibly`.
  Terminal and uniform like the string set: duplicates collapse under the default
  comparer, the pool's distinct count gates distinct collections, and a `null` element is
  refused — make the whole draw optional with `.OrNull()` instead.
- **Domain vocabulary where it belongs**: dates constrain with
  `After`/`Before`/`Between`, quantities with `Positive`/`Between`/`NonZero`,
  identities with `NonEmpty`/`DifferentFrom` — and deliberately no clock-relative
  constraints: a reproducible test pins its reference instants explicitly.
- **Values built to satisfy the constraints** — a scalar is constructed directly,
  never generated-then-filtered. The one exception is excluding values from a string
  (`Any.String().DifferentFrom(...)`/`Except(...)`): a string has no ordinal mapping to
  build the exclusion into, so it is met by a **bounded** redraw — the same escape a
  *distinct* collection uses to skip a duplicate, never an unbounded retry loop. An
  exclusion tight enough to leave the shape unsatisfiable surfaces at generation as a
  seed-bearing `AnyGenerationException`.
- **Conflicting constraints fail fast** with a clear, actionable
  `ConflictingAnyConstraintException` at the moment the conflicting constraint is
  declared — for example `Any.String().WithLength(3).StartingWith("ORD-")`.
- **Composition without reflection**: `.As(factory)` turns a constrained primitive
  into a domain value object; `Any.Combine(...)` assembles larger objects through
  constructor lambdas — from two up to eight constrained parts.
- **Collections over any element generator**: `Any.ListOf(item)`, `ArrayOf`,
  `SequenceOf`, `SetOf` and `DictionaryOf`, constrained with
  `WithCount`/`NonEmpty`/`Distinct`/`Containing`. Ask a distinct collection for more
  distinct elements than its effective domain — the element generator plus any values
  pinned outside it with `Containing` — can supply, and it fails fast, just like any
  other conflict, wherever that domain is countable; where it is not, the same
  shortfall instead surfaces at generation as an `AnyGenerationException` naming the
  seed to replay. `Any.PairOf`/`TripleOf` pair generators into value tuples.
- **Optional values**: `.OrNull()` turns any generator into one that is `null` about
  half the time and otherwise a constrained value — the dummy for an optional field,
  for value types (`int?`, `Guid?`, ...) and reference types alike.
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

# JustDummies.Xunit

The [xUnit v3](https://xunit.net) companion of [JustDummies](https://www.nuget.org/packages/JustDummies).
Mark a test `[Reproducible]` and its arbitrary values are drawn from a pinned
seed — reported **only when the test fails**, so a red test names the exact seed
to replay while a green one stays silent.

## Why

`JustDummies` makes a run reproducible by wrapping the test body in a delegate:

    [Fact]
    public void Order_reference_is_accepted() {
        Any.Reproducibly(() => {
            string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
            // ... act, assert ...
        });
    }

That works on every test framework, and stays the portable form. This package
removes the ceremony for xUnit v3:

    [Fact, Reproducible]
    public void Order_reference_is_accepted() {
        string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        // ... act, assert ...
    }

Values still vary between runs — which is what surfaces a test secretly
depending on one — but a failure is now recoverable even though the body was
never wrapped in advance.

## Replaying a failure

A failing test writes its seed to the test output:

    [JustDummies] These arbitrary values were seeded with 1234. Reproduce this run with [Reproducible(Seed = 1234)].

Pin it to replay:

    [Fact, Reproducible(Seed = 1234)]
    public void Order_reference_is_accepted() { /* ... */ }

The same snippet is what a generation failure names, so a diagnostic never
points at a call the test does not contain.

## Where it applies

- **A test**: `[Fact, Reproducible]` or `[Theory, Reproducible]`.
- **A class**: `[Reproducible]` on the class covers every test it declares.
- **A whole suite**: `[assembly: Reproducible]`.

The hooks run once per test *case*, so each case of a theory draws its own seed
rather than sharing one with its siblings. When several levels apply, the most
specific one wins for the duration of the test and the outer ones are restored
after it — an assembly-wide `[Reproducible]` can pin the suite while one test
replays a particular seed.

## Notes

- Values drawn from an explicit `Any.WithSeed(...)` context are unaffected: that
  context is isolated by design and does not draw from the ambient source this
  attribute pins.
- The seed is pinned through `Any.UseSeed(...)`, a public handle any test-framework
  adapter can use — this package holds no privileged access to `JustDummies`.
- xUnit v3 only. On xUnit v2, NUnit, MSTest or anything else, use
  `Any.Reproducibly(...)`: it is unaffected by this package and works everywhere.

## Links

- [Repository](https://github.com/Reefact/first-class-errors)
- [JustDummies](https://www.nuget.org/packages/JustDummies)

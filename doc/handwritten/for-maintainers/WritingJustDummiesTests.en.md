# Writing JustDummies tests

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](WritingJustDummiesTests.fr.md)

> Where a new test for `JustDummies` belongs, and how to write it. The boundary
> between the two suites is recorded in
> [ADR-0040](adr/0040-split-the-justdummies-test-bed-between-example-and-property-suites.md);
> this page is how to apply it.

## The two suites

| Project | Owns | Style |
|---|---|---|
| `JustDummies.PropertyTests` | Invariants that hold for **every** legal constraint argument | FsCheck properties over generated constraints |
| `JustDummies.UnitTests` | Contracts whose subject is a **specific, named case** | xUnit + NFluent examples |

Both run on `net10.0` and on the .NET Framework 4.7.2 floor, so both prove their
half against the `netstandard2.0` asset consumers actually load.

## The one question to ask

> *Does my assertion have an input space?*

Something a caller could have passed differently — a bound, a length, a count, a
pool, a seed, a pattern, an offset — and the assertion should still hold?

* **Yes** → it is a property. Generate that input and quantify over it.
* **No** → it is an example. Pin it and assert it directly.

That is the whole rule. Everything below is it applied.

### Goes in the property suite

* **Containment and strictness.** `Between(min, max)` contains; `GreaterThan` is
  strict; `GreaterThanOrEqualTo` admits its own bound. The bound is the input space.
* **Shape.** `WithLength(n)` produces exactly `n`; `StartingWith(prefix)` actually
  starts with it; `WithCount(n)` yields exactly `n` elements.
* **Grids.** `MultipleOf`, `WithScale`, `WithGranularity` — the step is the input
  space, and the anchor differs per type, which is where these go wrong.
* **Round trips.** A value generated from a pattern is matched by the real engine;
  a generated URI parses and carries the shape asked for.
* **Determinism.** Two contexts on the same seed agree — for *every* seed, not for
  12345.
* **Composition.** `As`, `OrNull`, `Combine`, the explicit pools: the composed
  value carries each part's constraint, whatever the parts were constrained to.
* **Value-dependent legality.** When the same call is legal or illegal depending on
  its argument, a property is the only honest way to state it — branch on the
  value, never on the call shape.

### Stays in the example suite

* **Message content.** A conflict must name *both* offending constraints. The
  wording is direction-aware; assert it on a pinned case, and assert the exception
  **type** anywhere else.
* **Null and blank arguments.** `null` has no input space.
* **Named domain extremes.** `int.MinValue`, `byte.MaxValue`, an empty pool — a
  specific coordinate, cheaper and clearer pinned than quantified.
* **Reachability.** That a bounded range is actually reached, that both branches of
  a coin flip are observed. These are statistical, not universal — pin a seed.
* **Structural conventions.** The `Any` ↔ `AnyContext` mirror, factory naming, the
  standalone assembly boundary. Reflection over a fixed expectation table; there is
  no input to generate.
* **Dated regressions.** A defect that actually occurred, pinned at the coordinates
  where it occurred. A property covering the same ground does **not** retire it —
  the specificity is the value. Reference the issue in a comment.

## Adding a feature

1. Write the example tests first — they are how you discover the shape, and they
   own the conflict messages your new constraint must produce.
2. Then ask the question above of each invariant you wrote. Anything that holds for
   every argument moves to a property, and the example that pinned one argument goes
   away with it.
3. If your constraint interacts with an existing one, the interaction is almost
   always a property: it has two input spaces.

## Fixing a defect

1. Pin the defect as an example at the coordinates where it was found, with the
   issue number in a comment. That is the regression, and it stays forever.
2. Then ask whether the defect had an input space the example does not cover.
   Issue #206 did — the decimal midpoint bug was found on one interval and lived on
   all of them. Add the property too.
3. Both land. The regression proves the exact case; the property proves the class.

## Writing the property

Use the shared helpers in `PropertyTestSupport.cs`:

* `Generators.OrderedPair(values)` — a well-formed `(min, max)`, degenerate pairs
  included. Pinned intervals are a historically fragile corner; do not filter them
  out, branch on them.
* `Generators.WithEdges(values, edges)` — FsCheck's numeric generators are
  size-bounded and cluster near zero, so the domain ends would otherwise almost
  never be drawn. That is exactly where an off-by-one lives.
* `Expect.EveryDraw(generator, invariant)` — a generator is a recipe, not a value,
  so one draw per case tests almost none of its randomness. Eight draws per case,
  over a hundred cases, is the default.
* `Expect.Draws(generator, count)` — when the property reasons over a batch
  (distinctness, reachability) rather than over each value alone.

Rules that keep a property honest:

* **Assert exception types, never message text.** Messages are direction-aware and
  will change; that assertion belongs to an example.
* **Know when the exception is thrown.** Conflicts throw at the fluent *call*, not
  at `Generate()`. Argument validation precedes conflict checking and wins when both
  would apply.
* **Guard the degenerate corners** your generated arguments can produce — an empty
  interval, a pinned interval, a zero count, an exhausted pool. Either keep the
  generator off them or branch in the predicate.
* **Pin a seed for anything statistical.** "Both halves are reached", "null is
  eventually drawn" are probabilistic. Under a pinned seed they are deterministic;
  without one they flake. Say so in a comment.
* **Keep it fast.** A hundred cases times eight draws is already eight hundred
  draws. Cap lengths and counts in the tens, not the thousands.

## Before you push

* `dotnet test JustDummies.PropertyTests` and `dotnet test JustDummies.UnitTests`.
* The floor legs, which CI runs and a plain `dotnet test` does not:
  `dotnet build JustDummies.PropertyTests -c Release -f net472 -p:EnableNet472Floor=true`.
  Anything using .NET 8+ API belongs in `ModernTypeInvariantProperties.cs`, which the
  project file excludes from that leg.
* **Check your property can fail.** A property that returns `true` for the wrong
  reason passes silently and proves nothing. Break it on purpose once — invert the
  comparison, drop a bound — confirm it goes red, then put it back. This is the only
  cheap defence against a suite that is green because it asserts nothing.

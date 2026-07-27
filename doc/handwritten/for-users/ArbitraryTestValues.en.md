# Arbitrary Test Values

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ArbitraryTestValues.fr.md)

A large part of a test's `Arrange` is usually values the test never checks — an error code, a diagnostic message, an occurrence instant. Spelled out as literals they read as if they mattered, and a constant reused across a suite can let a test pass for the wrong reason. An *arbitrary* value supplies a valid-but-incidental input instead, so the one input that matters stands out and the rest announce themselves as incidental.

Two sources cover this, and both draw from the same ambient random source:

- **[`JustDummies`](https://github.com/Reefact/first-class-errors)** — a fluent generator of arbitrary primitives (`JustDummies.Any.Int32()`, `JustDummies.Any.String()`, ...). A `JustDummies.Any.*` call returns a *recipe*; call `.Generate()` to draw the value.
- **Domain factories** in **`FirstClassErrors.Testing`** — `ErrorCodeFactory.Any()`, `DiagnosticMessageFactory.Any()`, and peers — for the error vocabulary a raw primitive cannot express. Each returns the value directly.

Because both flow through the same source, a single `JustDummies.Any.Reproducibly(...)` makes a whole test replayable, and — like the clock and instance-id overrides — the source is scoped, context-local, and safe under parallel tests. For freezing values a test *does* assert on, see [Deterministic Error Tests](DeterministicTesting.en.md).

## Supply an arbitrary value

Compare a test that hard-codes every input with one that keeps only the value under assertion explicit:

```csharp
// 😐 Before — which of these values does the test actually check?
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), "Order 42 was not found.")
    .WithPublicMessage("The order does not exist.");

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

```csharp
// 🙂 After — the code is the subject; the messages are arbitrary.
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), DiagnosticMessageFactory.Any())
    .WithPublicMessage(ShortMessageFactory.Any());

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

A value is only incidental when it cannot steer the code under test. If it feeds a branch, a validation, a serialization, or an ordering, it shapes the behavior even though the test never asserts it — and it cannot safely be left arbitrary. Reach for an arbitrary value for inputs the test carries but does not act on.

## The error vocabulary: domain factories

For the parts of an error a test needs but never asserts on, `FirstClassErrors.Testing` ships a factory per concept. Each `Any()` returns a value that is **valid for its type** — non-blank, and recognizable as arbitrary — drawn from the ambient source:

| Factory | Returns |
| --- | --- |
| `ErrorCodeFactory.Any()` | a valid, non-blank `ErrorCode` of the form `ANY_CODE_` + 6 uppercase alphanumerics |
| `DiagnosticMessageFactory.Any()` / `ShortMessageFactory.Any()` / `DetailedMessageFactory.Any()` | a non-blank message, recognizable as arbitrary |
| `TransienceFactory.Any()` / `InteractionDirectionFactory.Any()` | a *meaningful* value — never the `Unknown` sentinel |
| `ErrorOriginFactory.Any()` | any `ErrorOrigin`; all its values are meaningful, so there is no sentinel to exclude |

A factory returns the value directly — the common case needs no `.Generate()`. Use the meaningful-enum factories (`TransienceFactory`, `InteractionDirectionFactory`) when the test needs a value that actually drives behavior; reach for a plain `JustDummies.Any.Enum<TEnum>()` draw only when any member — a sentinel included — will do.

## Primitives: JustDummies

For arbitrary primitives, use **`JustDummies`** directly. A `JustDummies.Any.*` call returns a *generator* — an immutable recipe — and `.Generate()` draws one value from it:

```csharp
int    quantity  = JustDummies.Any.Int32().Generate();
string reference = JustDummies.Any.String().NonEmpty().Generate();
Guid   id        = JustDummies.Any.Guid().Generate();
```

Constraints chained on the generator express what the surrounding code *requires* of the value — a length, a range, a prefix — never what the test asserts. The full generator surface (constraints, collections, composition through `As`/`Combine`, `.OrNull()`) is documented with `JustDummies` itself.

The guarantees stop at type validity. A generator does not target a domain precondition — `JustDummies.Any.Int32()` may be negative, `JustDummies.Any.String()` is not a well-formed email — so a value object with a stricter contract is built by turning a constrained primitive into it: `JustDummies.Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate()`.

## Reproduce a failing run

The source is unseeded by default, so the values differ between runs. That is deliberate: a test that passes only for one particular value is relying on something it never states, and varying the value surfaces that coupling.

When a run matters enough to reproduce, wrap the test body in `JustDummies.Any.Reproducibly`. It pins a fresh seed for the run and, if the body throws, **reports that seed** before the failure propagates — so a red test tells you exactly how to replay it:

```csharp
[Fact]
public void Some_value_sensitive_test() =>
    JustDummies.Any.Reproducibly(() => {
        // ... arrange with the factories and JustDummies.Any, act, assert ...
    });
```

On failure the seed is written to `Console.Error` by default; pass your framework's writer (for example xUnit's `ITestOutputHelper.WriteLine`) to route it there instead. Replay the run by handing the reported seed back:

```csharp
JustDummies.Any.Reproducibly(1234, () => {
    // ... the same body ...
});
```

Reproducing a run needs the same sequence of draws, so a body whose order depends on non-deterministic external state is not fully replayable from the seed alone. There is also an asynchronous form, `JustDummies.Any.ReproduciblyAsync(Func<Task>)`, for `async` test bodies — await it, or the body's failures are silently dropped (the analyzer enforces this). Because the factories, the primitives, and the clock and id seams below all draw from the same ambient source, one `Reproducibly` scope replays them together.

### Pinning the seed without a body to wrap

`Reproducibly` needs a delegate. A caller that observes a test from the outside — a test-framework adapter running code *before* and *after* the test method — has no such delegate, so it pins the ambient source with a scope it opens and disposes itself:

```csharp
IDisposable scope = JustDummies.Any.UseSeed(1234);
// ... the test runs ...
scope.Dispose();
```

The scope flows with the execution context and nests exactly like `Reproducibly`, and disposing restores whatever was pinned before. What it does **not** do is report the seed when the test fails: whoever opens the scope owns telling the reader which seed to replay.

That ownership extends to the replay snippet. When a generator itself fails, the `AnyGenerationException` message names how to replay the run — by default `Any.Reproducibly(1234, ...)`, which is the wrong instruction for a test that contains no such call. A caller that pins the seed from outside says so, and its instruction is quoted verbatim instead:

```csharp
JustDummies.Any.UseSeed(1234, "[Reproducible(Seed = 1234)]");
```

Inside a test body, prefer `Reproducibly`: it reports the seed for you. Reach for `UseSeed` only when there is no body to wrap.

### On xUnit v3: `[Reproducible]`

The `JustDummies.Xunit` companion package does the wrapping for you. Mark a test, a class, or the whole assembly, and its arbitrary values are drawn from a pinned seed reported **only when the test fails**:

```csharp
[Fact, Reproducible]
public void Some_value_sensitive_test() {
    // ... arrange with the factories and JustDummies.Any, act, assert ...
}
```

A failing run writes `Reproduce this run with [Reproducible(Seed = 1234)]` to the test output; pin `[Reproducible(Seed = 1234)]` to replay it. Each case of a theory draws its own seed, and a method-level declaration overrides a class- or assembly-level one. This is convenience only: `Reproducibly` remains the portable form and works on every framework.

## Arbitrary `OccurredAt` and `InstanceId`

Occurrence data is arbitrary in the same sense: a test often needs it stable without asserting the exact instant or id. The clock and instance-id seams therefore pair a `UseAny` with their `UseFixed`. `Clock.UseAny()` freezes a single arbitrary instant for the scope, while `InstanceIds.UseAny()` hands each error its own distinct arbitrary id:

```csharp
DomainError NewError() =>
    DomainError.Create(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()).WithPublicMessage(ShortMessageFactory.Any());

using (Clock.UseAny())
using (InstanceIds.UseAny()) {
    DomainError first  = NewError();
    DomainError second = NewError();

    Check.That(second.OccurredAt).IsEqualTo(first.OccurredAt);    // one arbitrary instant, shared
    Check.That(second.InstanceId).IsNotEqualTo(first.InstanceId); // distinct arbitrary ids
}
```

Both draw from the same ambient source as `JustDummies.Any`, so running them inside `JustDummies.Any.Reproducibly` makes their instant and ids reproducible too. To pin a *specific* instant or id instead, use `UseFixed` — see [Deterministic Error Tests](DeterministicTesting.en.md).

## Scope and parallel tests

`JustDummies.Any.Reproducibly`, `Clock.UseAny`, and `InstanceIds.UseAny` all take effect only for the run or `using` block they wrap, and the arbitrary source is restored when it exits. That source is stored in an `AsyncLocal`, so it follows the test's own execution flow and never leaks into other tests running at the same time.

### Inside a test that parallelises

Following the test's execution flow means the source also reaches the threads the test itself starts — a `Parallel.For`, a `Task.WhenAll`. Drawing from several of them at once is safe: the values stay arbitrary and well-formed however many threads take them.

What parallelism costs is the *replay*. Concurrent draws interleave, so a seed no longer pins which value lands in which call, and a run that parallelises does not reproduce from its seed alone. If you only need dummies, nothing to do. If you need the run to replay, give each unit of work its own scope and derive its seed from the run's:

```csharp
// runSeed is the number you record by hand: keep it to replay a run, change it to explore new ones.
const int runSeed = 20240501;

Parallel.For(0, 64, index => {
    // A distinct, deterministic sub-seed per work item. Floor-safe: System.HashCode does not exist on netstandard2.0.
    using (Any.UseSeed(unchecked(runSeed * 397 ^ index))) {
        sut.Handle(Any.String().NonEmpty().Generate());
    }
});
```

Each iteration owns its own sequence, keyed on its index, so the whole run replays for a given `runSeed`. There is no outer `Any.Reproducibly` here: every draw happens inside a per-item scope, so a seed reported by an enclosing runner would replay nothing — the recorded `runSeed` is what you keep.

## Review checklist

Before reaching for an arbitrary value, verify that:

- the value does **not** change the functional path the test exercises — it must not feed a branch, a validation, a serialization, or an ordering, even indirectly;
- the value is genuinely not checked by the test — otherwise use a literal;
- a meaningful-enum factory (`TransienceFactory`, `InteractionDirectionFactory`) is used when the test needs a meaningful value, rather than a plain `JustDummies.Any.Enum<TEnum>()` draw;
- a value-sensitive test is wrapped in `JustDummies.Any.Reproducibly` so a failing run reports the seed to replay;
- `Clock.UseAny` / `InstanceIds.UseAny` are used for stable-but-irrelevant occurrence data, and `UseFixed` when the exact value is asserted.

---

<div align="center">
<a href="DeterministicTesting.en.md">← Deterministic Error Tests</a> · <a href="../../../README.md#-documentation">↑ Table of contents</a> · <a href="OperationalIntegration.en.md">Generating and Publishing the Catalog →</a>
</div>

---

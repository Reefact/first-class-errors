# Arbitrary Test Values

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ArbitraryTestValues.fr.md)

A large part of a test's `Arrange` is usually values the test never checks — an error code, a diagnostic message, an occurrence instant. Spelled out as literals they read as if they mattered, and a constant reused across a suite can let a test pass for the wrong reason. An *arbitrary* value supplies a valid-but-incidental input instead, so the one input that matters stands out and the rest announce themselves as incidental.

Two sources cover this, and both draw from the same ambient random source:

- **[`Dummies`](https://github.com/Reefact/first-class-errors)** — a fluent generator of arbitrary primitives (`Dummies.Any.Int32()`, `Dummies.Any.String()`, ...). A `Dummies.Any.*` call returns a *recipe*; call `.Generate()` to draw the value.
- **Domain factories** in **`FirstClassErrors.Testing`** — `ErrorCodeFactory.Any()`, `DiagnosticMessageFactory.Any()`, and peers — for the error vocabulary a raw primitive cannot express. Each returns the value directly.

Because both flow through the same source, a single `Dummies.Any.Reproducibly(...)` makes a whole test replayable, and — like the clock and instance-id overrides — the source is scoped, context-local, and safe under parallel tests. For freezing values a test *does* assert on, see [Deterministic Error Tests](DeterministicTesting.en.md).

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

A factory returns the value directly — the common case needs no `.Generate()`. Use the meaningful-enum factories (`TransienceFactory`, `InteractionDirectionFactory`) when the test needs a value that actually drives behavior; reach for a plain `Dummies.Any.Enum<TEnum>()` draw only when any member — a sentinel included — will do.

## Primitives: Dummies

For arbitrary primitives, use **`Dummies`** directly. A `Dummies.Any.*` call returns a *generator* — an immutable recipe — and `.Generate()` draws one value from it:

```csharp
int    quantity  = Dummies.Any.Int32().Generate();
string reference = Dummies.Any.String().NonEmpty().Generate();
Guid   id        = Dummies.Any.Guid().Generate();
```

Constraints chained on the generator express what the surrounding code *requires* of the value — a length, a range, a prefix — never what the test asserts. The full generator surface (constraints, collections, composition through `As`/`Combine`, `.OrNull()`) is documented with `Dummies` itself.

The guarantees stop at type validity. A generator does not target a domain precondition — `Dummies.Any.Int32()` may be negative, `Dummies.Any.String()` is not a well-formed email — so a value object with a stricter contract is built by turning a constrained primitive into it: `Dummies.Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create).Generate()`.

## Reproduce a failing run

The source is unseeded by default, so the values differ between runs. That is deliberate: a test that passes only for one particular value is relying on something it never states, and varying the value surfaces that coupling.

When a run matters enough to reproduce, wrap the test body in `Dummies.Any.Reproducibly`. It pins a fresh seed for the run and, if the body throws, **reports that seed** before the failure propagates — so a red test tells you exactly how to replay it:

```csharp
[Fact]
public void Some_value_sensitive_test() =>
    Dummies.Any.Reproducibly(() => {
        // ... arrange with the factories and Dummies.Any, act, assert ...
    });
```

On failure the seed is written to `Console.Error` by default; pass your framework's writer (for example xUnit's `ITestOutputHelper.WriteLine`) to route it there instead. Replay the run by handing the reported seed back:

```csharp
Dummies.Any.Reproducibly(1234, () => {
    // ... the same body ...
});
```

Reproducing a run needs the same sequence of draws, so a body whose order depends on non-deterministic external state is not fully replayable from the seed alone. There is also an asynchronous overload, `Dummies.Any.Reproducibly(Func<Task>)`, for `async` test bodies. Because the factories, the primitives, and the clock and id seams below all draw from the same ambient source, one `Reproducibly` scope replays them together.

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

Both draw from the same ambient source as `Dummies.Any`, so running them inside `Dummies.Any.Reproducibly` makes their instant and ids reproducible too. To pin a *specific* instant or id instead, use `UseFixed` — see [Deterministic Error Tests](DeterministicTesting.en.md).

## Scope and parallel tests

`Dummies.Any.Reproducibly`, `Clock.UseAny`, and `InstanceIds.UseAny` all take effect only for the run or `using` block they wrap, and the arbitrary source is restored when it exits. That source is stored in an `AsyncLocal`, so it follows the test's own execution flow and never leaks into other tests running at the same time.

## Review checklist

Before reaching for an arbitrary value, verify that:

- the value does **not** change the functional path the test exercises — it must not feed a branch, a validation, a serialization, or an ordering, even indirectly;
- the value is genuinely not checked by the test — otherwise use a literal;
- a meaningful-enum factory (`TransienceFactory`, `InteractionDirectionFactory`) is used when the test needs a meaningful value, rather than a plain `Dummies.Any.Enum<TEnum>()` draw;
- a value-sensitive test is wrapped in `Dummies.Any.Reproducibly` so a failing run reports the seed to replay;
- `Clock.UseAny` / `InstanceIds.UseAny` are used for stable-but-irrelevant occurrence data, and `UseFixed` when the exact value is asserted.

---

<div align="center">
<a href="DeterministicTesting.en.md">← Deterministic Error Tests</a> · <a href="../../../README.md#-documentation">↑ Table of contents</a> · <a href="OperationalIntegration.en.md">Generating and Publishing the Catalog →</a>
</div>

---

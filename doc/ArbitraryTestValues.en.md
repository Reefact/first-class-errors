# Arbitrary Test Values

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./ArbitraryTestValues.fr.md)

A large part of a test's `Arrange` is usually values the test never checks — an error code, a diagnostic message, an occurrence instant. Spelled out as literals they read as if they mattered, and a constant reused across a suite can let a test pass for the wrong reason. `Any` supplies a valid-but-arbitrary value instead, so the one input that matters stands out and the rest announce themselves as incidental.

`Any` lives in **`FirstClassErrors.Testing`**; it adds no dependency and, like the clock and instance-id overrides, is scoped, context-local, and safe under parallel tests. For freezing values a test *does* assert on, see [Deterministic Error Tests](DeterministicTesting.en.md).

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
// 🙂 After — the code is the subject; the messages are Any.
DomainError error = DomainError
    .Create(ErrorCode.Create("ORDER_NOT_FOUND"), Any.DiagnosticMessage())
    .WithPublicMessage(Any.ShortMessage());

Outcome<Order>.Failure(error).ShouldFail().WithCode("ORDER_NOT_FOUND");
```

A value is only incidental when it cannot steer the code under test. If it feeds a branch, a validation, a serialization, or an ordering, it shapes the behavior even though the test never asserts it — and it cannot safely be left arbitrary. Reach for `Any` for inputs the test carries but does not act on.

## What `Any` offers

Every helper returns a value that is **valid for its type** — non-blank strings and messages, a real UTC instant, an error code that is never blank:

| Helper | Returns |
| --- | --- |
| `Any.ErrorCode()` | a valid, non-blank code of the form `ANY_CODE_` + 6 uppercase alphanumerics |
| `Any.DiagnosticMessage()` / `Any.ShortMessage()` / `Any.DetailedMessage()` | a non-blank message of bounded length |
| `Any.String()` | a non-empty string of bounded length (`any-` + 8 lowercase alphanumerics); no whitespace |
| `Any.Guid()` | an arbitrary `Guid` |
| `Any.Instant()` | an arbitrary UTC instant (offset zero) between 1 January 2000 and around 2068 |
| `Any.Int()` | an arbitrary `int` — it may be negative or zero |
| `Any.Bool()` | `true` or `false` |
| `Any.Enum<TEnum>()` | any member of the enum — a sentinel such as `Unknown` included |
| `Any.Transience()` / `Any.InteractionDirection()` | a *meaningful* value — never the `Unknown` sentinel |
| `Any.ErrorOrigin()` | any `ErrorOrigin`; all three values are meaningful, so there is no sentinel to exclude |

The guarantees stop at type validity. A helper does not target a domain precondition — `Any.Int()` may be negative, `Any.String()` is not a well-formed email — so a value object with a stricter contract needs its own arbitrary factory rather than a raw primitive.

Use `Any.Enum<TEnum>()` when any member will do — a sentinel included — and the named enum helpers when the test needs a value that actually drives the behavior under test.

## Reproduce a run with a seed

The source is unseeded by default, so the values differ between runs. That is deliberate: a test that passes only for one particular value is relying on something it never states, and varying the value surfaces that coupling.

The cost is reproducibility. The default seed is **not surfaced today**, so a run that failed on a particular unseeded value cannot be replayed from its output alone. Keep the arbitrary value out of what decides pass or fail — or, when a specific run must be reproducible, pin a seed on the narrowest useful scope; a suite-wide seed is fine when full stability is preferable to variation between runs. Every `Any` call inside the scope then becomes deterministic:

```csharp
using (Any.UseSeed(1234)) {
    ErrorCode first  = Any.ErrorCode();
    ErrorCode second = Any.ErrorCode(); // the same two values on every run
}
```

Seeds nest: an inner `Any.UseSeed(...)` uses its own sequence and restores the outer one when it exits. Outside any scope the source is unseeded and every run differs.

## Arbitrary `OccurredAt` and `InstanceId`

Occurrence data is arbitrary in the same sense: a test often needs it stable without asserting the exact instant or id. The clock and instance-id seams therefore pair a `UseAny` with their `UseFixed`. `Clock.UseAny()` freezes a single arbitrary instant for the scope, while `InstanceIds.UseAny()` hands each error its own distinct arbitrary id:

```csharp
DomainError NewError() =>
    DomainError.Create(Any.ErrorCode(), Any.DiagnosticMessage()).WithPublicMessage(Any.ShortMessage());

using (Clock.UseAny())
using (InstanceIds.UseAny()) {
    DomainError first  = NewError();
    DomainError second = NewError();

    Check.That(second.OccurredAt).IsEqualTo(first.OccurredAt);    // one arbitrary instant, shared
    Check.That(second.InstanceId).IsNotEqualTo(first.InstanceId); // distinct arbitrary ids
}
```

Both take an optional seed (`Clock.UseAny(1234)`, `InstanceIds.UseAny(1234)`) to make the chosen values reproducible. To pin a *specific* instant or id instead, use `UseFixed` — see [Deterministic Error Tests](DeterministicTesting.en.md).

## Scope and parallel tests

`Any.UseSeed`, `Clock.UseAny`, and `InstanceIds.UseAny` all take effect only inside their `using` block and are restored when the scope exits. The seeded source is stored in an `AsyncLocal`, so it follows the test's own execution flow and never leaks into other tests running at the same time.

## Review checklist

Before reaching for an arbitrary value, verify that:

- the value does **not** change the functional path the test exercises — it must not feed a branch, a validation, a serialization, or an ordering, even indirectly;
- the value is genuinely not checked by the test — otherwise use a literal;
- a named enum helper is used when the test needs a meaningful value, rather than `Any.Enum<TEnum>()`;
- a seed is pinned whenever a failing run would otherwise be irreproducible;
- `Clock.UseAny` / `InstanceIds.UseAny` are used for stable-but-irrelevant occurrence data, and `UseFixed` when the exact value is asserted.

---

<div align="center">
<a href="DeterministicTesting.en.md">← Deterministic Error Tests</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="OperationalIntegration.en.md">Generating and Publishing the Catalog →</a>
</div>

---

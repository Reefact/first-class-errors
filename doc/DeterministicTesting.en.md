# Deterministic Error Tests

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./DeterministicTesting.fr.md)

Every error occurrence records two values that should vary in production:

- `OccurredAt`, the UTC instant at which the error was created;
- `InstanceId`, a unique identifier for that occurrence.

Those values improve observability, but they make whole-object assertions and snapshots (tests comparing a serialized object against an approved reference file) unstable. `FirstClassErrors.Testing` lets you temporarily replace the two generators behind them — the clock and the instance-id source — only within the scope of a test that deliberately needs deterministic values.

For fluent assertions on outcomes and errors, start with [Testing Outcomes and Errors](Testing.en.md).

## Shared setup for the examples

Every example below tells the same small story — looking up a missing order — through one factory and two stubs, so each snippet stays short:

```csharp
// The documented error every example reports.
private static DomainError MakeError() =>
    DomainError
        .Create(ErrorCode.Create("ORDER_NOT_FOUND"), "Order 42 was not found.")
        .WithPublicMessage("The order does not exist.");

private static readonly DateTimeOffset start = new(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

// A repository whose lookup fails for missingOrderId, returning a failed Outcome<Order>.
private readonly IOrderRepository orders = /* test double */;
private readonly OrderId missingOrderId  = /* an id with no match */;
```

`MakeError()` returns the concrete `DomainError`. When an example instead pulls the error out of a failed outcome with `ShouldFail().Subject`, it comes back typed as the base `Error` — that is why some snippets below say `DomainError` and others `Error`. It is the same object seen at two levels of its type hierarchy, not two interchangeable types.

## Freeze `OccurredAt`

Testing against the real clock usually requires an imprecise time window:

```csharp
DateTimeOffset before = DateTimeOffset.UtcNow;
DomainError error = MakeError();
DateTimeOffset after = DateTimeOffset.UtcNow;

Check.That(error.OccurredAt >= before && error.OccurredAt <= after).IsTrue();
```

`Clock.UseFixed(...)` makes the expected instant explicit:

```csharp
[Fact]
public void An_error_records_the_fixed_occurrence_time() {
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant)) {
        DomainError error = MakeError();

        Check.That(error.OccurredAt).IsEqualTo(instant);
    }
}
```

The override applies only inside the `using` scope. Disposing the scope restores the previous clock.

## Use a custom clock

When a test needs several controlled instants, implement `IClock` and pass it to `Clock.Use(...)`:

```csharp
sealed class StepClock : IClock {
    private DateTimeOffset _now;

    public StepClock(DateTimeOffset start) => _now = start;

    public DateTimeOffset UtcNow {
        get {
            DateTimeOffset current = _now;
            _now = _now.AddSeconds(1);
            return current;
        }
    }
}
```

```csharp
using (Clock.Use(new StepClock(start))) {
    DomainError first = MakeError();
    DomainError second = MakeError();

    Check.That(first.OccurredAt).IsEqualTo(start);
    Check.That(second.OccurredAt).IsEqualTo(start.AddSeconds(1));
}
```

Use a custom clock only when progression itself matters. A fixed clock is simpler for most tests.

## Freeze `InstanceId`

A random `Guid` is correct in production but unstable in a snapshot:

```csharp
[Fact]
public void A_missing_order_error_has_a_stable_instance_id() {
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (InstanceIds.UseFixed(id)) {
        Error error = orders.Find(missingOrderId).ShouldFail().Subject;

        Check.That(error.InstanceId).IsEqualTo(id);
    }
}
```

As with the clock, the override ends when the scope is disposed.

## Generate several stable identifiers

When one test creates several errors, provide a deterministic source:

```csharp
static Func<Guid> SequentialIds() {
    int value = 0;
    return () => new Guid(++value, 0, 0, new byte[8]);
}
```

```csharp
using (InstanceIds.Use(SequentialIds())) {
    DomainError first = MakeError();
    DomainError second = MakeError();

    Check.That(first.InstanceId.ToString()).IsEqualTo("00000001-0000-0000-0000-000000000000");
    Check.That(second.InstanceId.ToString()).IsEqualTo("00000002-0000-0000-0000-000000000000");
}
```

Prefer identifiers that are visibly synthetic so they cannot be confused with production values.

## Freeze `OccurredAt` and `InstanceId` together

```csharp
[Fact]
public void A_missing_order_error_is_fully_deterministic() {
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (Clock.UseFixed(instant)) {
        using (InstanceIds.UseFixed(id)) {
            Error error = orders.Find(missingOrderId)
                                .ShouldFail()
                                .WithCode("ORDER_NOT_FOUND")
                                .WithContextEntry("OrderId", missingOrderId)
                                .Subject;

            Check.That(error.OccurredAt).IsEqualTo(instant);
            Check.That(error.InstanceId).IsEqualTo(id);
        }
    }
}
```

## Arbitrary occurrence data

When a test needs `OccurredAt` and `InstanceId` to be stable but does not assert their exact values, freeze them to an *arbitrary* value with `Clock.UseAny()` and `InstanceIds.UseAny()` rather than `UseFixed`; both take an optional seed for reproducibility. They belong to the broader `Any` factory for values a test does not assert on — see [Arbitrary Test Values](ArbitraryTestValues.en.md).

## Scope and parallel tests

An override takes effect when its `using` opens and is undone when the `using` is disposed. Outside that block, the clock and instance ids are back to their real behavior.

The override is not a shared global: it follows the test's own execution flow (internally it is stored in an `AsyncLocal`). So a value frozen inside one test is invisible to any code running outside that test — including other tests running at the same time. Two tests can each freeze the clock to a different instant and run in parallel without disturbing each other.

Because of that, keep the `using` as tight as possible around the code that creates the errors: the frozen values then cover exactly what the test asserts, and nothing else.

## Common mistakes

### Forgetting to dispose the override

Always use `using`. Manual lifetime management makes tests harder to reason about.

### Freezing values in every test

Most tests should assert stable semantics through `ShouldSucceed()` and `ShouldFail()`. Freeze occurrence data only when occurrence data is part of the assertion.

### Treating test overrides as production configuration

`Clock` and `InstanceIds` overrides are test aids. Production should keep the real UTC clock and unique identifiers.

### Using the same fixed id for several errors when identity matters

If the test verifies distinct occurrences, use a deterministic sequence rather than one repeated identifier.

## Review checklist

Before approving a deterministic error test, verify that:

- the timestamp or instance id is genuinely relevant to the test;
- every override is enclosed in `using`;
- the scope is narrow;
- a fixed value is used when progression is unnecessary;
- a deterministic sequence is used when several distinct errors are created;
- synthetic values are obvious in snapshots;
- the test does not rely on real time or random identifiers accidentally.

---

<div align="center">
<a href="Testing.en.md">← Testing Outcomes and Errors</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="ArbitraryTestValues.en.md">Arbitrary Test Values →</a>
</div>

---
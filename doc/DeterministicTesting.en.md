# Deterministic Error Tests

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./DeterministicTesting.fr.md)

Every error occurrence records two values that should vary in production:

- `OccurredAt`, the UTC instant at which the error was created;
- `InstanceId`, a unique identifier for that occurrence.

Those values improve observability, but they make whole-object assertions and snapshots (tests comparing a serialized object against an approved reference file) unstable. `FirstClassErrors.Testing` provides scoped overrides for tests that deliberately need deterministic values.

For fluent assertions on outcomes and errors, start with [Testing Outcomes and Errors](Testing.en.md).

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
public void An_error_records_the_fixed_occurrence_time()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant))
    {
        DomainError error = DomainError
            .Create(ErrorCode.Create("ORDER_NOT_FOUND"), "Order 42 was not found.")
            .WithPublicMessage("The order does not exist.");

        Check.That(error.OccurredAt).IsEqualTo(instant);
    }
}
```

The override applies only inside the `using` scope. Disposing the scope restores the previous clock.

## Use a custom clock

When a test needs several controlled instants, implement `IClock` and pass it to `Clock.Use(...)`:

```csharp
sealed class StepClock : IClock
{
    private DateTimeOffset _now;

    public StepClock(DateTimeOffset start) => _now = start;

    public DateTimeOffset UtcNow
    {
        get
        {
            DateTimeOffset current = _now;
            _now = _now.AddSeconds(1);
            return current;
        }
    }
}
```

```csharp
using (Clock.Use(new StepClock(start)))
{
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
public void A_missing_order_error_has_a_stable_instance_id()
{
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (InstanceIds.UseFixed(id))
    {
        Error error = orders.Find(missingOrderId).ShouldFail().Subject;

        Check.That(error.InstanceId).IsEqualTo(id);
    }
}
```

As with the clock, the override ends when the scope is disposed.

## Generate several stable identifiers

When one test creates several errors, provide a deterministic source:

```csharp
static Func<Guid> SequentialIds()
{
    int value = 0;
    return () => new Guid(++value, 0, 0, new byte[8]);
}
```

```csharp
using (InstanceIds.Use(SequentialIds()))
{
    Error first = MakeError();
    Error second = MakeError();

    Check.That(first.InstanceId.ToString()).IsEqualTo("00000001-0000-0000-0000-000000000000");
    Check.That(second.InstanceId.ToString()).IsEqualTo("00000002-0000-0000-0000-000000000000");
}
```

Prefer identifiers that are visibly synthetic so they cannot be confused with production values.

## Freeze both values for a snapshot

```csharp
[Fact]
public void A_missing_order_error_is_fully_deterministic()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (Clock.UseFixed(instant))
    using (InstanceIds.UseFixed(id))
    {
        Error error = orders.Find(missingOrderId)
                            .ShouldFail()
                            .WithCode("ORDER_NOT_FOUND")
                            .WithContextEntry("OrderId", missingOrderId)
                            .Subject;

        Check.That(error.OccurredAt).IsEqualTo(instant);
        Check.That(error.InstanceId).IsEqualTo(id);
    }
}
```

This is useful when serializing or snapshotting the complete error. If the test only cares about the error code or context, do not freeze unrelated fields merely because the helpers exist.

## Scope and parallel tests

The overrides are:

- disposable and intended for `using` scopes;
- local to the current execution context;
- restored when the scope ends;
- inactive outside the scope;
- designed not to leak into unrelated parallel tests.

Keep the scope as narrow as possible around the code that creates the errors.

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
<a href="Testing.en.md">← Testing Outcomes and Errors</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="OperationalIntegration.en.md">Generating and Publishing the Catalog →</a>
</div>

---
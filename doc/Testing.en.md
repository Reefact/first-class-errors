# Testing Guide

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./Testing.fr.md)

Errors and outcomes are values, so your tests should read like tests about
values — not like plumbing. The companion package **`FirstClassErrors.Testing`**
gives you three small things that make that easy:

* **Fluent assertions** on `Outcome`, `Outcome<T>` and `Error`.
* **A freezable clock**, so `OccurredAt` is deterministic.
* **Freezable instance ids**, so `InstanceId` is deterministic.

Two promises the package keeps:

* **It imposes no test or assertion framework.** The assertions throw a plain
  exception that xUnit, NUnit, MSTest — anything — reports as a failure.
* **It adds nothing to your production dependencies.** Everything lives in a
  separate test-only package, and the overrides only ever affect code running
  inside their `using` scope.

```xml
<!-- in your test project -->
<PackageReference Include="FirstClassErrors.Testing" Version="..." />
```

```csharp
using FirstClassErrors;
using FirstClassErrors.Testing;
```

> The examples below use xUnit for the scaffolding (`[Fact]`, `Assert`), but the
> FirstClassErrors assertions work the same in any framework.

---

## ✅ Asserting on outcomes

This is the part you'll use every day. Testing code that returns an `Outcome`
usually means unwrapping it by hand and reaching for the null-forgiving
`!` operator:

```csharp
// 😐 Before
Outcome<Receipt> outcome = checkout.Pay(order);

Assert.True(outcome.IsFailure);
Assert.Equal("PAYMENT.DECLINED", outcome.Error!.Code.ToString());
```

With the testing package, the intent comes first and the boilerplate disappears:

```csharp
// 🙂 After
checkout.Pay(order)
        .ShouldFail()
        .WithCode("PAYMENT.DECLINED");
```

### Successes return their value

`ShouldSucceed()` asserts the outcome succeeded and hands you the carried value,
ready to assert on:

```csharp
[Fact]
public void Paying_a_valid_order_produces_a_receipt()
{
    Outcome<Receipt> outcome = checkout.Pay(order);

    Receipt receipt = outcome.ShouldSucceed();

    Assert.Equal(order.Total, receipt.AmountCharged);
}
```

For the non-generic `Outcome`, `ShouldSucceed()` simply asserts success:

```csharp
inventory.Reserve(sku).ShouldSucceed();
```

### Failures return a fluent handle

`ShouldFail()` asserts the outcome failed and returns an `ErrorAssertion` you can
chain expectations on. Each step checks one facet of the error:

```csharp
[Fact]
public void Declining_a_payment_reports_a_diagnosable_error()
{
    Outcome<Receipt> outcome = checkout.Pay(declinedCard);

    outcome.ShouldFail()
           .WithCode("PAYMENT.DECLINED")
           .WithShortMessage("Your payment was declined.")
           .WithDiagnosticMessage("Issuer refused authorization (code 51).")
           .WithContextEntry("CardNetwork", "VISA");
}
```

Available checks on `ErrorAssertion`:

| Method | Asserts |
| --- | --- |
| `WithCode("...")` / `WithCode(errorCode)` | the error's `Code` |
| `WithShortMessage("...")` | the public `ShortMessage` |
| `WithDiagnosticMessage("...")` | the internal `DiagnosticMessage` |
| `WithContextEntry("key")` | a context entry is present |
| `WithContextEntry("key", value)` | a context entry is present **and** equals `value` |

Need something the fluent surface doesn't cover? `Subject` gives you the raw
`Error` back:

```csharp
Error error = outcome.ShouldFail().WithCode("ORDER.NOT_FOUND").Subject;

Assert.Empty(error.InnerErrors);
```

### Failure messages read well

When an expectation is not met, the assertions throw `OutcomeAssertionException`
with a message that tells you what actually happened — not the domain's own
exception:

```text
Expected the outcome to succeed, but it failed with [PAYMENT.DECLINED]: Issuer refused authorization (code 51).
```

```text
Expected the error to have code "ORDER.NOT_FOUND", but it was "ORDER.LOCKED".
```

---

## 🕒 Freezing the clock

Every `Error` records the moment it occurred in `OccurredAt`. In a test, the real
clock forces you into a time window:

```csharp
// 😐 Before
DateTimeOffset before = DateTimeOffset.UtcNow;
DomainError    error  = MakeError();
DateTimeOffset after  = DateTimeOffset.UtcNow;

Assert.True(error.OccurredAt >= before && error.OccurredAt <= after);
```

`Clock.UseFixed(...)` pins the time to an exact instant for the duration of a
`using` scope, so you can assert equality:

```csharp
// 🙂 After
[Fact]
public void An_error_records_when_it_occurred()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant))
    {
        DomainError error = DomainError
            .Create(ErrorCode.Create("ORDER.NOT_FOUND"), "Order 42 was not found.")
            .WithPublicMessage("This order does not exist.");

        Assert.Equal(instant, error.OccurredAt);
    }
}
```

Need a clock you control across several reads (for example a clock that
advances)? Implement `IClock` and pass it to `Clock.Use(...)`:

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
            _now = _now.AddSeconds(1); // each read advances by one second
            return current;
        }
    }
}

using (Clock.Use(new StepClock(start)))
{
    // errors created here get start, start + 1s, start + 2s, ...
}
```

**How the scope behaves**

* Outside a scope — i.e. in production — the clock is always the real system
  clock. This type only affects code running inside a `using` block.
* Disposing the scope restores the previous clock. Always use `using`.
* The override flows with the current execution context, so it never leaks into
  tests running in parallel.

---

## 🔢 Freezing instance ids

Each error occurrence gets a unique `InstanceId` (a random `Guid`). That is
exactly what you want in production and exactly what breaks a snapshot or an
equality assertion over a whole error. `InstanceIds` lets you pin it.

Pin a single id:

```csharp
[Fact]
public void A_not_found_error_snapshots_cleanly()
{
    var id = new Guid("11111111-1111-1111-1111-111111111111");

    using (InstanceIds.UseFixed(id))
    {
        DomainError error = orders.Find(missingId).ShouldFail().Subject as DomainError;

        Assert.Equal(id, error!.InstanceId);
    }
}
```

Or provide your own source — a counter, for instance, when a test creates
several errors and you want stable, distinct ids:

```csharp
static Func<Guid> Sequential()
{
    int n = 0;
    return () => new Guid(++n, 0, 0, new byte[8]); // 00000001-..., 00000002-...
}

using (InstanceIds.Use(Sequential()))
{
    // first error  -> 00000001-0000-0000-0000-000000000000
    // second error -> 00000002-0000-0000-0000-000000000000
}
```

`InstanceIds` follows the same scope rules as `Clock`: disposable, context-local,
and inert outside a `using` block.

---

## 🧪 Putting it together

Fixing the clock and the id turns a whole error into a completely deterministic
value — ideal for a single, readable assertion or a snapshot:

```csharp
[Fact]
public void Looking_up_a_missing_order_fails_deterministically()
{
    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);
    var id      = new Guid("11111111-1111-1111-1111-111111111111");

    using (Clock.UseFixed(instant))
    using (InstanceIds.UseFixed(id))
    {
        Outcome<Order> outcome = orders.Find(missingId);

        ErrorAssertion failure = outcome.ShouldFail()
                                        .WithCode("ORDER.NOT_FOUND")
                                        .WithContextEntry("OrderId", missingId);

        Assert.Equal(instant, failure.Subject.OccurredAt);
        Assert.Equal(id,      failure.Subject.InstanceId);
    }
}
```

---

## 📎 Good to know

* Everything is **scoped and disposable** — always reach for `using`.
* Overrides are **context-local**: they apply to the current async flow only, so
  parallel tests don't interfere with each other.
* Nothing here changes production behavior, and the package pulls **no test or
  assertion framework** into your project.

---

<div align="center">
<a href="BestPractices.en.md">← Best Practices</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="OperationalIntegration.en.md">CI/CD and Operational Integration →</a>
</div>

---

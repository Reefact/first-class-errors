# Testing Outcomes and Errors

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./Testing.fr.md)

Errors and outcomes are values. Their tests should therefore describe success, failure, and error semantics directly—not the plumbing used to inspect them.

The companion package **`FirstClassErrors.Testing`** provides framework-agnostic fluent assertions for `Outcome`, `Outcome<T>`, and `Error`.

## Install the testing package

```xml
<PackageReference Include="FirstClassErrors.Testing" Version="..." />
```

```csharp
using FirstClassErrors;
using FirstClassErrors.Testing;
```

The examples use xUnit for test scaffolding, but the FirstClassErrors assertions do not depend on xUnit, NUnit, MSTest, or another assertion library.

The package belongs only in test projects and adds nothing to production dependencies.

## Assert a successful `Outcome<T>`

Without the testing package, a test usually checks the state and unwraps the result separately:

```csharp
Outcome<Receipt> outcome = checkout.Pay(order);

Assert.True(outcome.IsSuccess);
Receipt receipt = outcome.GetResultOrThrow();
Assert.Equal(order.Total, receipt.AmountCharged);
```

`ShouldSucceed()` performs the state assertion and returns the carried value:

```csharp
[Fact]
public void Paying_a_valid_order_produces_a_receipt()
{
    Receipt receipt = checkout.Pay(order).ShouldSucceed();

    Assert.Equal(order.Total, receipt.AmountCharged);
}
```

For non-generic `Outcome`, it simply asserts success:

```csharp
inventory.Reserve(sku).ShouldSucceed();
```

## Assert a failure

`ShouldFail()` asserts failure and returns an `ErrorAssertion`:

```csharp
[Fact]
public void Declining_a_payment_reports_a_diagnosable_error()
{
    checkout.Pay(declinedCard)
            .ShouldFail()
            .WithCode("PAYMENT_DECLINED")
            .WithShortMessage("Your payment was declined.")
            .WithDiagnosticMessage("Issuer refused authorization (code 51).")
            .WithContextEntry("CardNetwork", "VISA");
}
```

This keeps the expected contract visible in one place: stable code, public message, internal diagnostic, and occurrence context.

## Available error checks

| Method | Asserts |
| --- | --- |
| `WithCode("...")` | the error code as text |
| `WithCode(errorCode)` | the strongly typed `ErrorCode` |
| `WithShortMessage("...")` | the public short message |
| `WithDiagnosticMessage("...")` | the internal diagnostic message |
| `WithContextEntry("key")` | that a context entry exists |
| `WithContextEntry("key", value)` | that the entry exists and equals the expected value |

Use only the assertions that express the behavior owned by the test. Avoid asserting every field mechanically when only the code and one context value matter.

## Access the underlying error

`Subject` returns the asserted `Error` when the fluent surface does not cover a property:

```csharp
Error error = outcome.ShouldFail()
                     .WithCode("ORDER_NOT_FOUND")
                     .Subject;

Assert.Empty(error.InnerErrors);
Assert.Equal(Transience.NonTransient, ((InfrastructureError)error).Transience);
```

Use `Subject` for targeted assertions, not to immediately rebuild all the manual plumbing that the fluent API removed.

## Failure messages

When an expectation fails, the package throws `OutcomeAssertionException` with a message describing the mismatch:

```text
Expected the outcome to succeed, but it failed with [PAYMENT_DECLINED]: Issuer refused authorization (code 51).
```

```text
Expected the error to have code "ORDER_NOT_FOUND", but it was "ORDER_LOCKED".
```

The assertion failure is distinct from the domain exception. A failed test therefore reports what the test expected and what the outcome actually contained.

## What should a test assert?

Prefer assertions on the stable behavior of the error:

- the error code;
- the error category when relevant;
- public wording only when it is part of the intended contract;
- diagnostic wording when the exact diagnostic is deliberately specified;
- context keys and values used by consumers or operations;
- inner errors when composition is itself the behavior under test.

Avoid coupling every test to incidental prose or timestamps unless those values are the subject of the test.

## Complete example

```csharp
[Fact]
public void Looking_up_a_missing_order_returns_the_expected_error()
{
    Outcome<Order> outcome = orders.Find(missingOrderId);

    Error error = outcome.ShouldFail()
                         .WithCode("ORDER_NOT_FOUND")
                         .WithShortMessage("The order does not exist.")
                         .WithContextEntry("OrderId", missingOrderId)
                         .Subject;

    Assert.IsType<DomainError>(error);
    Assert.Empty(error.InnerErrors);
}
```

The test reads as a description of the failure contract rather than a sequence of nullable checks and casts.

## Deterministic timestamps and instance ids

Every error occurrence contains an `OccurredAt` timestamp and a unique `InstanceId`. When a test or snapshot must assert those values, use the scoped overrides provided by the testing package.

See [Deterministic Error Tests](DeterministicTesting.en.md) for `Clock.UseFixed(...)`, `InstanceIds.UseFixed(...)`, custom sources, parallel-test behavior, and full-error snapshots.

## Review checklist

Before approving an error test, verify that:

- it asserts behavior rather than implementation plumbing;
- success values are obtained through `ShouldSucceed()`;
- failures are asserted through `ShouldFail()`;
- the error code is checked when it identifies the scenario;
- exact prose is asserted only when intentionally contractual;
- `Subject` is used only for properties outside the fluent surface;
- time and identifiers are frozen only when the test needs them.

---

<div align="center">
<a href="BestPractices.en.md">← Best Practices</a> · <a href="../README.md#-next-steps">↑ Table of contents</a> · <a href="DeterministicTesting.en.md">Deterministic Error Tests →</a>
</div>

---
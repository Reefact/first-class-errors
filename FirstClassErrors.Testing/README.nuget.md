# FirstClassErrors.Testing

Testing helpers for **FirstClassErrors** — so your tests about errors and outcomes read like tests about values.

## What's inside

- **Fluent, framework-agnostic assertions** on `Outcome`, `Outcome<T>` and `Error`.
  `outcome.ShouldSucceed()` returns the carried value; `outcome.ShouldFail()` returns
  a handle you chain expectations on (`WithCode`, `WithShortMessage`,
  `WithDiagnosticMessage`, `WithContextEntry`). A failed expectation throws a plain
  exception any test framework reports — no test or assertion library is imposed.
- **A freezable clock** (`Clock.UseFixed(...)`) so an error's `OccurredAt` is
  deterministic instead of asserted over a time window.
- **Freezable instance ids** (`InstanceIds.UseFixed(...)`) so `InstanceId` is stable
  for snapshot and equality assertions.

Overrides are scoped (`using`), context-local (safe under parallel tests), and never
affect production behavior.

## Example

    using FirstClassErrors.Testing;

    var instant = new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero);

    using (Clock.UseFixed(instant))
    {
        Outcome<Order> outcome = orders.Find(id);

        outcome.ShouldFail()
               .WithCode("ORDER.NOT_FOUND")
               .WithContextEntry("OrderId", id);
    }

## Documentation

Full testing guide and the rest of the documentation on GitHub:

https://github.com/Reefact/first-class-errors

# Amount currency mismatch

- **Code:** `AMOUNT_CURRENCY_MISMATCH`
- **Source:** `Amount`

This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.

> **Business rule:** All monetary operations must involve amounts expressed in the same currency.

## Diagnostics

- **Amounts were used in a monetary operation without having been converted to the same currency.** — _origin:_ Internal — Verify whether all amounts involved in the operation were converted to a common currency before being used together.
- **Amounts expected to be expressed in the same currency were provided with different currencies.** — _origin:_ InternalOrExternal — Check the currencies associated with each amount and confirm whether a common currency was expected for this operation.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:amount-currency-mismatch",
  "title": "Currency mismatch",
  "detail": "The two amounts use different currencies and cannot be combined.",
  "code": "AMOUNT_CURRENCY_MISMATCH"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [Amount] Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD. error.code=AMOUNT_CURRENCY_MISMATCH
```


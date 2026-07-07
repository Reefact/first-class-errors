# Invalid money transfer

- **Code:** `MONEY_TRANSFER_INVALID`
- **Source:** `MoneyTransfer`

This error aggregates every domain rule violated while validating a money transfer, so the caller sees all the problems at once rather than one at a time.

> **Business rule:** A money transfer must satisfy every domain rule (a strictly positive amount, matching currencies, ...).

## Diagnostics

- **One or more domain rules were violated by the requested transfer.** — _origin:_ External — Inspect the aggregated inner errors to see each individual rule violation.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:money-transfer-invalid",
  "title": "Invalid money transfer.",
  "detail": "The money transfer does not satisfy all the required rules.",
  "code": "MONEY_TRANSFER_INVALID"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] The money transfer is invalid: it violates one or more domain rules. error.code=MONEY_TRANSFER_INVALID
```


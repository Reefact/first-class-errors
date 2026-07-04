# Non-positive transfer amount

- **Code:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Source:** `MoneyTransfer`

This error occurs when a money transfer is requested with an amount that is zero or negative.

> **Business rule:** A money transfer amount must be strictly positive.

## Diagnostics

- **The amount was entered or computed as zero or a negative value.** — _origin:_ External — Check the requested transfer amount and confirm it is greater than zero.

## Examples

**Public response (RFC 9457)**

```json
{
  "title": "Transfer amount must be positive.",
  "detail": "The transfer amount must be greater than zero.",
  "code": "MONEY_TRANSFER_AMOUNT_NOT_POSITIVE"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] Cannot transfer -25 EUR: the amount must be strictly positive. error.code=MONEY_TRANSFER_AMOUNT_NOT_POSITIVE
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | The monetary amount of the attempted transfer. | `-25 EUR` |


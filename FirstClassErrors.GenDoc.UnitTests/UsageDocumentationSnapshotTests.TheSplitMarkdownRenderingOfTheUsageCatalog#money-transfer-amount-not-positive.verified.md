# Non-positive transfer amount

- **Code:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Source:** `MoneyTransfer`

This error occurs when a money transfer is requested with an amount that is zero or negative.

> **Business rule:** A money transfer amount must be strictly positive.

## Diagnostics

- **The amount was entered or computed as zero or a negative value.** — _origin:_ External — Check the requested transfer amount and confirm it is greater than zero.

## Examples

- Cannot transfer -25 EUR: the amount must be strictly positive. _(Transfer amount must be positive.)_

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | The monetary amount of the attempted transfer. | `-25 EUR` |


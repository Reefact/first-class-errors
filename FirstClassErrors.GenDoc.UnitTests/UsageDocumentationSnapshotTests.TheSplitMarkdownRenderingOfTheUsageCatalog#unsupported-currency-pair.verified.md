# Unsupported currency pair

- **Code:** `UNSUPPORTED_CURRENCY_PAIR`
- **Source:** `ExchangeRateProvider`

This error occurs when the exchange-rate provider does not quote a rate for the requested source/target currency pair.

> **Business rule:** A currency conversion can only be performed for a pair the provider quotes.

## Diagnostics

- **The requested currency pair is not offered by the provider.** — _origin:_ External — Confirm the provider supports both the source and target currencies before requesting a conversion.

## Examples

- The exchange-rate provider does not quote the EUR to USD currency pair. _(Unsupported currency pair.)_

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The source currency of the conversion. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The target currency of the conversion. | `USD` |


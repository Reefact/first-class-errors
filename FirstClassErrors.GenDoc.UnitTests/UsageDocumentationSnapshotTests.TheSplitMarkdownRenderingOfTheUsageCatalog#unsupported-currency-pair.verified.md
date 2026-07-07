# Unsupported currency pair

- **Code:** `UNSUPPORTED_CURRENCY_PAIR`
- **Source:** `ExchangeRateProvider`

This error occurs when the exchange-rate provider does not quote a rate for the requested source/target currency pair.

> **Business rule:** A currency conversion can only be performed for a pair the provider quotes.

## Diagnostics

- **The requested currency pair is not offered by the provider.** — _origin:_ External — Confirm the provider supports both the source and target currencies before requesting a conversion.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:unsupported-currency-pair",
  "title": "Unsupported currency pair.",
  "detail": "The requested currency pair is not supported.",
  "code": "UNSUPPORTED_CURRENCY_PAIR"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider does not quote the EUR to USD currency pair. error.code=UNSUPPORTED_CURRENCY_PAIR
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The source currency of the conversion. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The target currency of the conversion. | `USD` |


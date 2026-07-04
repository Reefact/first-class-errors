# Exchange-rate service unavailable

- **Code:** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Source:** `ExchangeRateProvider`

This error occurs when the external exchange-rate provider cannot be reached (a timeout, a connection reset, or a 5xx response). It is transient: the call can be retried.

> **Business rule:** Currency conversion depends on a reachable exchange-rate provider.

## Diagnostics

- **The provider timed out or returned a server error.** — _origin:_ External — Check the provider's health and retry the call, ideally with a backoff.
- **The outgoing network path to the provider is disrupted.** — _origin:_ InternalOrExternal — Verify outbound connectivity and any proxy or firewall between the service and the provider.

## Examples

- The exchange-rate provider 'acme-fx' is unavailable (correlation 22222222-2222-2222-2222-222222222222). _(Exchange-rate service unavailable.)_

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | The external provider that was called. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | The correlation identifier of the outgoing call. | `22222222-2222-2222-2222-222222222222` |


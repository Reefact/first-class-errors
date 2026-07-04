# Statement upload rate-limited

- **Code:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Source:** `StatementUploadEndpoint`

This error occurs when too many statement uploads arrive in a short window and the endpoint throttles the request. It is transient: the same request can be retried later.

> **Business rule:** Callers must stay within the endpoint's upload rate limit.

## Diagnostics

- **The caller exceeded the allowed request rate.** — _origin:_ External — Back off and retry after the delay indicated in the message.

## Examples

- The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. _(Statement upload rate-limited.)_

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |


# Statement upload rate-limited

- **Code:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Source:** `StatementUploadEndpoint`

This error occurs when too many statement uploads arrive in a short window and the endpoint throttles the request. It is transient: the same request can be retried later.

> **Business rule:** Callers must stay within the endpoint's upload rate limit.

## Diagnostics

- **The caller exceeded the allowed request rate.** — _origin:_ External — Back off and retry after the delay indicated in the message.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:statement-upload-rate-limited",
  "title": "Statement upload rate-limited.",
  "detail": "Too many statement uploads were sent in a short period; please retry later.",
  "code": "STATEMENT_UPLOAD_RATE_LIMITED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. error.code=STATEMENT_UPLOAD_RATE_LIMITED
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |


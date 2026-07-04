# Malformed statement payload

- **Code:** `MALFORMED_STATEMENT_PAYLOAD`
- **Source:** `StatementUploadEndpoint`

This error occurs when the statement upload endpoint receives a request whose body is missing a required field or carries an invalid value.

> **Business rule:** An uploaded statement request must carry every required field with a valid value.

## Diagnostics

- **The client sent an incomplete or malformed request body.** — _origin:_ External — Inspect the field named in the context and confirm the client sends it with a valid value.

## Examples

**Public response (RFC 9457)**

```json
{
  "title": "Malformed statement payload.",
  "detail": "The uploaded statement request is missing a required field or contains an invalid value.",
  "code": "MALFORMED_STATEMENT_PAYLOAD"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. error.code=MALFORMED_STATEMENT_PAYLOAD
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | The request field that failed validation. | `statementPeriod` |


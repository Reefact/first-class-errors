# Malformed statement payload

- **Code:** `MALFORMED_STATEMENT_PAYLOAD`
- **Source:** `StatementUploadEndpoint`

This error occurs when the statement upload endpoint receives a request whose body is missing a required field or carries an invalid value.

> **Business rule:** An uploaded statement request must carry every required field with a valid value.

## Diagnostics

- **The client sent an incomplete or malformed request body.** — _origin:_ External — Inspect the field named in the context and confirm the client sends it with a valid value.

## Examples

- The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. _(Malformed statement payload.)_

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | The request field that failed validation. | `statementPeriod` |


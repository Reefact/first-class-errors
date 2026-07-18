# Non-positive number of nights

- **Code:** `NIGHT_COUNT_NOT_POSITIVE`
- **Source:** `NightCount`

An incoming request asks for zero or a negative number of nights, which is not a valid stay length.

> **Business rule:** A booking must be for at least one night.

## Diagnostics

- **The client sent a number of nights of zero or below.** — _origin:_ External — Send a number of nights of one or more.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:night-count-not-positive",
  "title": "The number of nights is invalid.",
  "detail": "The requested number of nights must be one or more.",
  "code": "NIGHT_COUNT_NOT_POSITIVE"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [NightCount] A booking must be for at least one night, but 0 was requested. error.code=NIGHT_COUNT_NOT_POSITIVE
```


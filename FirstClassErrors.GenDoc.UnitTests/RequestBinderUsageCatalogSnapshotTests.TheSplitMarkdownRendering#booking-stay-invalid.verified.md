# Booking stay invalid

- **Code:** `BOOKING_STAY_INVALID`
- **Source:** `BookingEndpoint`

The stay sub-object of the request could not be bound: one or both of its dates were missing or malformed. Its failures are grouped under this nested envelope, with paths prefixed by Stay.

> **Business rule:** Both stay dates must be present and valid ISO dates.

## Diagnostics

- **The client sent a stay with a missing or malformed check-in or check-out date.** — _origin:_ External — Read the inner errors under the Stay path for the failing date.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-stay-invalid",
  "title": "We could not read the stay dates.",
  "detail": "The check-in or check-out date of the stay is missing or invalid.",
  "code": "BOOKING_STAY_INVALID"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The stay is invalid: one or more of its dates failed to bind. error.code=BOOKING_STAY_INVALID
```


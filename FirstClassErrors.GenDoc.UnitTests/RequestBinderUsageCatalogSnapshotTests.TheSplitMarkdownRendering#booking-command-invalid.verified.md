# Booking request invalid

- **Code:** `BOOKING_COMMAND_INVALID`
- **Source:** `BookingEndpoint`

The endpoint could not bind the incoming request into a booking command: one or more arguments were missing or invalid. Every failure is collected under this envelope, each with its full argument path.

> **Business rule:** Every required argument must be present, and every argument must convert into its value object.

## Diagnostics

- **The client sent a request that violates the endpoint's contract (missing or malformed arguments).** — _origin:_ External — Read the inner errors: each names the failing argument and the rule it violated.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-command-invalid",
  "title": "We could not accept your booking request.",
  "detail": "One or more details of the booking request are missing or invalid.",
  "code": "BOOKING_COMMAND_INVALID"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The booking command is invalid: one or more request arguments failed to bind. error.code=BOOKING_COMMAND_INVALID
```


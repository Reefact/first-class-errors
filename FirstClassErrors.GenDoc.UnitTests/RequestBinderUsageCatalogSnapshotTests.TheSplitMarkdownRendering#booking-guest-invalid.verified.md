# Booking guest invalid

- **Code:** `BOOKING_GUEST_INVALID`
- **Source:** `BookingEndpoint`

A guest in the request's guests list could not be bound: its first name was missing or its e-mail was malformed. Its failures are grouped under this per-element envelope, with indexed paths such as Guests[1].

> **Business rule:** Each guest must have a first name, and any e-mail present must be valid.

## Diagnostics

- **The client sent a guest with a missing first name or a malformed e-mail address.** — _origin:_ External — Read the inner errors under the indexed Guests[i] path for the failing field.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-guest-invalid",
  "title": "A guest's information is invalid.",
  "detail": "One of the guests is missing a first name or has an invalid e-mail address.",
  "code": "BOOKING_GUEST_INVALID"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The guest is invalid: one or more of its fields failed to bind. error.code=BOOKING_GUEST_INVALID
```


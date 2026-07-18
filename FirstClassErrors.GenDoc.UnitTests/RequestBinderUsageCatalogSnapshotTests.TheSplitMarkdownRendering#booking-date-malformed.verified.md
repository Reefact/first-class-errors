# Malformed booking date

- **Code:** `BOOKING_DATE_MALFORMED`
- **Source:** `BookingDate`

An incoming request carries a value that is not an ISO yyyy-MM-dd date, so it cannot be parsed into a BookingDate value object.

> **Business rule:** A booking date must be an ISO calendar date in the yyyy-MM-dd format.

## Diagnostics

- **The client sent a date in a locale-specific or malformed format, or an impossible calendar date.** — _origin:_ External — Send dates in ISO 8601 yyyy-MM-dd form, for example 2026-08-10.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-date-malformed",
  "title": "The date is invalid.",
  "detail": "A booking date is not a valid ISO (yyyy-MM-dd) date.",
  "code": "BOOKING_DATE_MALFORMED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingDate] '2026-13-40' is not a valid ISO (yyyy-MM-dd) date. error.code=BOOKING_DATE_MALFORMED
```


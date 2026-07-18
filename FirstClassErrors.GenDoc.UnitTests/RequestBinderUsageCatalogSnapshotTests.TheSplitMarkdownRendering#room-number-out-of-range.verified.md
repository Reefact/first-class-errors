# Room number out of range

- **Code:** `ROOM_NUMBER_OUT_OF_RANGE`
- **Source:** `RoomNumber`

An element of the request's room-number list is outside the supported 1-999 range.

> **Business rule:** A room number must be between 1 and 999 inclusive.

## Diagnostics

- **The client sent a room number of zero, a negative value, or a value above 999.** — _origin:_ External — Send room numbers within the supported 1-999 range.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:room-number-out-of-range",
  "title": "A room number is invalid.",
  "detail": "One of the requested room numbers is outside the supported range.",
  "code": "ROOM_NUMBER_OUT_OF_RANGE"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [RoomNumber] Room number 1000 is outside the supported range 1-999. error.code=ROOM_NUMBER_OUT_OF_RANGE
```


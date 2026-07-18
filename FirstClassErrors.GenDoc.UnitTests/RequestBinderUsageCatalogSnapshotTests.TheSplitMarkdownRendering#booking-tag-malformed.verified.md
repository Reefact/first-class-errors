# Malformed booking tag

- **Code:** `BOOKING_TAG_MALFORMED`
- **Source:** `Tag`

An element of the request's tag list is empty, too long, or contains whitespace, so it cannot be parsed into a Tag value object.

> **Business rule:** A tag must be a single non-empty token of at most 32 characters, without whitespace.

## Diagnostics

- **The client sent a blank tag, a phrase containing spaces, or an over-long value.** — _origin:_ External — Send each tag as a single whitespace-free token; join multi-word tags with a hyphen.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-tag-malformed",
  "title": "A tag is invalid.",
  "detail": "One of the booking tags is not a valid single token.",
  "code": "BOOKING_TAG_MALFORMED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [Tag] 'late checkout' is not a valid tag. error.code=BOOKING_TAG_MALFORMED
```


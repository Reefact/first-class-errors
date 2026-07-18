# Malformed e-mail address

- **Code:** `EMAIL_ADDRESS_MALFORMED`
- **Source:** `EmailAddress`

An incoming request carries a value that is not a well-formed e-mail address, so it cannot be parsed into an EmailAddress value object.

> **Business rule:** A guest e-mail address must contain a single '@' with a non-empty local part and domain.

## Diagnostics

- **The client sent a misspelled or truncated address (missing '@', empty local part or domain).** — _origin:_ External — Validate the address on the client, and compare the sent value against the expected format.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:email-address-malformed",
  "title": "The e-mail address is invalid.",
  "detail": "The value provided is not a valid e-mail address.",
  "code": "EMAIL_ADDRESS_MALFORMED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [EmailAddress] 'not-an-email' is not a valid e-mail address. error.code=EMAIL_ADDRESS_MALFORMED
```


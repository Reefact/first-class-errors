# Malformed currency code

- **Code:** `CURRENCY_CODE_MALFORMED`
- **Source:** `Currency`

An incoming request carries a value that is not a well-formed three-letter currency code, so it cannot be parsed into a Currency value object.

> **Business rule:** A currency code must be exactly three upper-case ASCII letters (for example EUR).

## Diagnostics

- **The client sent a currency in the wrong shape (lower-case, a symbol, a name, or the wrong length).** — _origin:_ External — Send the ISO-4217 alphabetic code in upper case, for example USD or EUR.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:currency-code-malformed",
  "title": "The currency is invalid.",
  "detail": "The billing currency code is not a valid three-letter code.",
  "code": "CURRENCY_CODE_MALFORMED"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [Currency] 'EURO' is not a valid three-letter currency code. error.code=CURRENCY_CODE_MALFORMED
```


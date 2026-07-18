# Check-out not after check-in

- **Code:** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Source:** `Stay`

Both stay dates parse, but the check-out date is on or before the check-in date, so the stay has no positive length. This cross-field rule is enforced by the Stay.Create factory.

> **Business rule:** Check-out must be strictly after check-in.

## Diagnostics

- **The client sent a check-out date equal to or earlier than the check-in date.** — _origin:_ External — Send a check-out date at least one day after the check-in date.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:stay-checkout-not-after-checkin",
  "title": "The stay dates are invalid.",
  "detail": "The check-out date must be after the check-in date.",
  "code": "STAY_CHECKOUT_NOT_AFTER_CHECKIN"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [Stay] Check-out 2026-08-10 must be strictly after check-in 2026-08-14. error.code=STAY_CHECKOUT_NOT_AFTER_CHECKIN
```


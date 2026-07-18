# Error Catalog

## Table of contents

- [BookingEndpoint errors](#src-booking-endpoint)
  - [Booking request invalid](#err-booking-command-invalid)
  - [Booking guest invalid](#err-booking-guest-invalid)
  - [Booking stay invalid](#err-booking-stay-invalid)
- [BookingDate errors](#src-booking-date)
  - [Malformed booking date](#err-booking-date-malformed)
- [Tag errors](#src-tag)
  - [Malformed booking tag](#err-booking-tag-malformed)
- [Currency errors](#src-currency)
  - [Malformed currency code](#err-currency-code-malformed)
- [EmailAddress errors](#src-email-address)
  - [Malformed e-mail address](#err-email-address-malformed)
- [NightCount errors](#src-night-count)
  - [Non-positive number of nights](#err-night-count-not-positive)
- [RoomNumber errors](#src-room-number)
  - [Room number out of range](#err-room-number-out-of-range)
- [Stay errors](#src-stay)
  - [Check-out not after check-in](#err-stay-checkout-not-after-checkin)

<a id="src-booking-endpoint"></a>

## BookingEndpoint errors

Primary-port errors raised by the booking endpoint when it binds an incoming request into a command.

<a id="err-booking-command-invalid"></a>

### Booking request invalid

- **Code:** `BOOKING_COMMAND_INVALID`
- **Source:** `BookingEndpoint`

The endpoint could not bind the incoming request into a booking command: one or more arguments were missing or invalid. Every failure is collected under this envelope, each with its full argument path.

> **Business rule:** Every required argument must be present, and every argument must convert into its value object.

#### Diagnostics

- **The client sent a request that violates the endpoint's contract (missing or malformed arguments).** — _origin:_ External — Read the inner errors: each names the failing argument and the rule it violated.

#### Examples

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

<a id="err-booking-guest-invalid"></a>

### Booking guest invalid

- **Code:** `BOOKING_GUEST_INVALID`
- **Source:** `BookingEndpoint`

A guest in the request's guests list could not be bound: its first name was missing or its e-mail was malformed. Its failures are grouped under this per-element envelope, with indexed paths such as Guests[1].

> **Business rule:** Each guest must have a first name, and any e-mail present must be valid.

#### Diagnostics

- **The client sent a guest with a missing first name or a malformed e-mail address.** — _origin:_ External — Read the inner errors under the indexed Guests[i] path for the failing field.

#### Examples

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

<a id="err-booking-stay-invalid"></a>

### Booking stay invalid

- **Code:** `BOOKING_STAY_INVALID`
- **Source:** `BookingEndpoint`

The stay sub-object of the request could not be bound: one or both of its dates were missing or malformed. Its failures are grouped under this nested envelope, with paths prefixed by Stay.

> **Business rule:** Both stay dates must be present and valid ISO dates.

#### Diagnostics

- **The client sent a stay with a missing or malformed check-in or check-out date.** — _origin:_ External — Read the inner errors under the Stay path for the failing date.

#### Examples

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

<a id="src-booking-date"></a>

## BookingDate errors

Errors raised when parsing a booking date (check-in / check-out) from a request string.

<a id="err-booking-date-malformed"></a>

### Malformed booking date

- **Code:** `BOOKING_DATE_MALFORMED`
- **Source:** `BookingDate`

An incoming request carries a value that is not an ISO yyyy-MM-dd date, so it cannot be parsed into a BookingDate value object.

> **Business rule:** A booking date must be an ISO calendar date in the yyyy-MM-dd format.

#### Diagnostics

- **The client sent a date in a locale-specific or malformed format, or an impossible calendar date.** — _origin:_ External — Send dates in ISO 8601 yyyy-MM-dd form, for example 2026-08-10.

#### Examples

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

<a id="src-tag"></a>

## Tag errors

Errors raised when parsing a booking tag from an element of a request list.

<a id="err-booking-tag-malformed"></a>

### Malformed booking tag

- **Code:** `BOOKING_TAG_MALFORMED`
- **Source:** `Tag`

An element of the request's tag list is empty, too long, or contains whitespace, so it cannot be parsed into a Tag value object.

> **Business rule:** A tag must be a single non-empty token of at most 32 characters, without whitespace.

#### Diagnostics

- **The client sent a blank tag, a phrase containing spaces, or an over-long value.** — _origin:_ External — Send each tag as a single whitespace-free token; join multi-word tags with a hyphen.

#### Examples

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

<a id="src-currency"></a>

## Currency errors

Errors raised when parsing a billing currency code from a request string.

<a id="err-currency-code-malformed"></a>

### Malformed currency code

- **Code:** `CURRENCY_CODE_MALFORMED`
- **Source:** `Currency`

An incoming request carries a value that is not a well-formed three-letter currency code, so it cannot be parsed into a Currency value object.

> **Business rule:** A currency code must be exactly three upper-case ASCII letters (for example EUR).

#### Diagnostics

- **The client sent a currency in the wrong shape (lower-case, a symbol, a name, or the wrong length).** — _origin:_ External — Send the ISO-4217 alphabetic code in upper case, for example USD or EUR.

#### Examples

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

<a id="src-email-address"></a>

## EmailAddress errors

Errors raised when parsing a guest e-mail address from a request string.

<a id="err-email-address-malformed"></a>

### Malformed e-mail address

- **Code:** `EMAIL_ADDRESS_MALFORMED`
- **Source:** `EmailAddress`

An incoming request carries a value that is not a well-formed e-mail address, so it cannot be parsed into an EmailAddress value object.

> **Business rule:** A guest e-mail address must contain a single '@' with a non-empty local part and domain.

#### Diagnostics

- **The client sent a misspelled or truncated address (missing '@', empty local part or domain).** — _origin:_ External — Validate the address on the client, and compare the sent value against the expected format.

#### Examples

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

<a id="src-night-count"></a>

## NightCount errors

Errors raised when building the number of nights from a request value.

<a id="err-night-count-not-positive"></a>

### Non-positive number of nights

- **Code:** `NIGHT_COUNT_NOT_POSITIVE`
- **Source:** `NightCount`

An incoming request asks for zero or a negative number of nights, which is not a valid stay length.

> **Business rule:** A booking must be for at least one night.

#### Diagnostics

- **The client sent a number of nights of zero or below.** — _origin:_ External — Send a number of nights of one or more.

#### Examples

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

<a id="src-room-number"></a>

## RoomNumber errors

Errors raised when building a room number from an element of a request list.

<a id="err-room-number-out-of-range"></a>

### Room number out of range

- **Code:** `ROOM_NUMBER_OUT_OF_RANGE`
- **Source:** `RoomNumber`

An element of the request's room-number list is outside the supported 1-999 range.

> **Business rule:** A room number must be between 1 and 999 inclusive.

#### Diagnostics

- **The client sent a room number of zero, a negative value, or a value above 999.** — _origin:_ External — Send room numbers within the supported 1-999 range.

#### Examples

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

<a id="src-stay"></a>

## Stay errors

Errors raised when validating a stay's check-in and check-out dates together.

<a id="err-stay-checkout-not-after-checkin"></a>

### Check-out not after check-in

- **Code:** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Source:** `Stay`

Both stay dates parse, but the check-out date is on or before the check-in date, so the stay has no positive length. This cross-field rule is enforced by the Stay.Create factory.

> **Business rule:** Check-out must be strictly after check-in.

#### Diagnostics

- **The client sent a check-out date equal to or earlier than the check-in date.** — _origin:_ External — Send a check-out date at least one day after the check-in date.

#### Examples

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


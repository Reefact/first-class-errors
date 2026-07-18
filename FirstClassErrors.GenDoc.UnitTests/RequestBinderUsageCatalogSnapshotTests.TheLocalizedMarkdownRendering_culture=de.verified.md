# Fehlerkatalog

## Inhaltsverzeichnis

- [BookingEndpoint-Fehler](#src-booking-endpoint)
  - [Buchungsanforderung ungültig](#err-booking-command-invalid)
  - [Gast der Buchung ungültig](#err-booking-guest-invalid)
  - [Aufenthalt der Buchung ungültig](#err-booking-stay-invalid)
- [BookingDate-Fehler](#src-booking-date)
  - [Fehlerhaftes Buchungsdatum](#err-booking-date-malformed)
- [Tag-Fehler](#src-tag)
  - [Fehlerhaftes Buchungs-Tag](#err-booking-tag-malformed)
- [Currency-Fehler](#src-currency)
  - [Fehlerhafter Währungscode](#err-currency-code-malformed)
- [EmailAddress-Fehler](#src-email-address)
  - [Fehlerhafte E-Mail-Adresse](#err-email-address-malformed)
- [NightCount-Fehler](#src-night-count)
  - [Nicht positive Anzahl von Nächten](#err-night-count-not-positive)
- [RoomNumber-Fehler](#src-room-number)
  - [Zimmernummer außerhalb des Bereichs](#err-room-number-out-of-range)
- [Stay-Fehler](#src-stay)
  - [Abreise nicht nach Anreise](#err-stay-checkout-not-after-checkin)

<a id="src-booking-endpoint"></a>

## BookingEndpoint-Fehler

Primär-Port-Fehler, die vom Buchungsendpunkt ausgelöst werden, wenn er eine eingehende Anforderung an einen Befehl bindet.

<a id="err-booking-command-invalid"></a>

### Buchungsanforderung ungültig

- **Code:** `BOOKING_COMMAND_INVALID`
- **Quelle:** `BookingEndpoint`

Der Endpunkt konnte die eingehende Anforderung nicht an einen Buchungsbefehl binden: Ein oder mehrere Argumente fehlten oder waren ungültig. Jeder Fehler wird unter diesem Umschlag gesammelt, jeweils mit seinem vollständigen Argumentpfad.

> **Geschäftsregel:** Jedes erforderliche Argument muss vorhanden sein, und jedes Argument muss in sein Value-Object konvertiert werden.

#### Diagnosen

- **Der Client hat eine Anforderung gesendet, die den Vertrag des Endpunkts verletzt (fehlende oder fehlerhafte Argumente).** — _Ursprung:_ External — Lesen Sie die inneren Fehler: Jeder benennt das fehlerhafte Argument und die verletzte Regel.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-command-invalid",
  "title": "Wir konnten Ihre Buchungsanforderung nicht annehmen.",
  "detail": "Eine oder mehrere Angaben der Buchungsanforderung fehlen oder sind ungültig.",
  "code": "BOOKING_COMMAND_INVALID"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The booking command is invalid: one or more request arguments failed to bind. error.code=BOOKING_COMMAND_INVALID
```

<a id="err-booking-guest-invalid"></a>

### Gast der Buchung ungültig

- **Code:** `BOOKING_GUEST_INVALID`
- **Quelle:** `BookingEndpoint`

Ein Gast der Gästeliste der Anforderung konnte nicht gebunden werden: Sein Vorname fehlte oder seine E-Mail war fehlerhaft. Seine Fehler werden unter diesem Umschlag pro Element gruppiert, mit indizierten Pfaden wie Guests[1].

> **Geschäftsregel:** Jeder Gast muss einen Vornamen haben, und jede vorhandene E-Mail muss gültig sein.

#### Diagnosen

- **Der Client hat einen Gast mit einem fehlenden Vornamen oder einer fehlerhaften E-Mail-Adresse gesendet.** — _Ursprung:_ External — Lesen Sie die inneren Fehler unter dem indizierten Pfad Guests[i] für das fehlerhafte Feld.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-guest-invalid",
  "title": "Die Angaben eines Gastes sind ungültig.",
  "detail": "Einer der Gäste hat keinen Vornamen oder eine ungültige E-Mail-Adresse.",
  "code": "BOOKING_GUEST_INVALID"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The guest is invalid: one or more of its fields failed to bind. error.code=BOOKING_GUEST_INVALID
```

<a id="err-booking-stay-invalid"></a>

### Aufenthalt der Buchung ungültig

- **Code:** `BOOKING_STAY_INVALID`
- **Quelle:** `BookingEndpoint`

Das Aufenthalts-Unterobjekt der Anforderung konnte nicht gebunden werden: Eines oder beide seiner Daten fehlten oder waren fehlerhaft. Seine Fehler werden unter diesem verschachtelten Umschlag gruppiert, mit durch Stay präfixierten Pfaden.

> **Geschäftsregel:** Beide Aufenthaltsdaten müssen vorhanden und gültige ISO-Daten sein.

#### Diagnosen

- **Der Client hat einen Aufenthalt mit einem fehlenden oder fehlerhaften An- oder Abreisedatum gesendet.** — _Ursprung:_ External — Lesen Sie die inneren Fehler unter dem Stay-Pfad für das fehlerhafte Datum.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-stay-invalid",
  "title": "Wir konnten die Aufenthaltsdaten nicht lesen.",
  "detail": "Das An- oder Abreisedatum des Aufenthalts fehlt oder ist ungültig.",
  "code": "BOOKING_STAY_INVALID"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The stay is invalid: one or more of its dates failed to bind. error.code=BOOKING_STAY_INVALID
```

<a id="src-booking-date"></a>

## BookingDate-Fehler

Fehler, die beim Parsen eines Buchungsdatums (Anreise / Abreise) aus einer Anforderungszeichenfolge ausgelöst werden.

<a id="err-booking-date-malformed"></a>

### Fehlerhaftes Buchungsdatum

- **Code:** `BOOKING_DATE_MALFORMED`
- **Quelle:** `BookingDate`

Eine eingehende Anforderung enthält einen Wert, der kein ISO-Datum (yyyy-MM-dd) ist und daher nicht in ein BookingDate-Value-Object geparst werden kann.

> **Geschäftsregel:** Ein Buchungsdatum muss ein ISO-Kalenderdatum im Format yyyy-MM-dd sein.

#### Diagnosen

- **Der Client hat ein Datum in einem länderspezifischen oder fehlerhaften Format oder ein unmögliches Kalenderdatum gesendet.** — _Ursprung:_ External — Senden Sie Datumsangaben im Format ISO 8601 yyyy-MM-dd, zum Beispiel 2026-08-10.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-date-malformed",
  "title": "Das Datum ist ungültig.",
  "detail": "Ein Buchungsdatum ist kein gültiges ISO-Datum (yyyy-MM-dd).",
  "code": "BOOKING_DATE_MALFORMED"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingDate] '2026-13-40' is not a valid ISO (yyyy-MM-dd) date. error.code=BOOKING_DATE_MALFORMED
```

<a id="src-tag"></a>

## Tag-Fehler

Fehler, die beim Parsen eines Buchungs-Tags aus einem Element einer Anforderungsliste ausgelöst werden.

<a id="err-booking-tag-malformed"></a>

### Fehlerhaftes Buchungs-Tag

- **Code:** `BOOKING_TAG_MALFORMED`
- **Quelle:** `Tag`

Ein Element der Tag-Liste der Anforderung ist leer, zu lang oder enthält Leerzeichen und kann daher nicht in ein Tag-Value-Object geparst werden.

> **Geschäftsregel:** Ein Tag muss ein einzelnes, nicht leeres Token von höchstens 32 Zeichen ohne Leerzeichen sein.

#### Diagnosen

- **Der Client hat ein leeres Tag, einen Ausdruck mit Leerzeichen oder einen zu langen Wert gesendet.** — _Ursprung:_ External — Senden Sie jedes Tag als einzelnes Token ohne Leerzeichen; verbinden Sie mehrwortige Tags mit einem Bindestrich.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-tag-malformed",
  "title": "Ein Tag ist ungültig.",
  "detail": "Eines der Buchungs-Tags ist kein gültiges einzelnes Token.",
  "code": "BOOKING_TAG_MALFORMED"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Tag] 'late checkout' is not a valid tag. error.code=BOOKING_TAG_MALFORMED
```

<a id="src-currency"></a>

## Currency-Fehler

Fehler, die beim Parsen eines Abrechnungswährungscodes aus einer Anforderungszeichenfolge ausgelöst werden.

<a id="err-currency-code-malformed"></a>

### Fehlerhafter Währungscode

- **Code:** `CURRENCY_CODE_MALFORMED`
- **Quelle:** `Currency`

Eine eingehende Anforderung enthält einen Wert, der kein wohlgeformter dreistelliger Währungscode ist und daher nicht in ein Currency-Value-Object geparst werden kann.

> **Geschäftsregel:** Ein Währungscode muss aus genau drei ASCII-Großbuchstaben bestehen (zum Beispiel EUR).

#### Diagnosen

- **Der Client hat eine Währung in der falschen Form gesendet (Kleinbuchstaben, ein Symbol, ein Name oder die falsche Länge).** — _Ursprung:_ External — Senden Sie den alphabetischen ISO-4217-Code in Großbuchstaben, zum Beispiel USD oder EUR.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:currency-code-malformed",
  "title": "Die Währung ist ungültig.",
  "detail": "Der Abrechnungswährungscode ist kein gültiger dreistelliger Code.",
  "code": "CURRENCY_CODE_MALFORMED"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Currency] 'EURO' is not a valid three-letter currency code. error.code=CURRENCY_CODE_MALFORMED
```

<a id="src-email-address"></a>

## EmailAddress-Fehler

Fehler, die beim Parsen der E-Mail-Adresse eines Gastes aus einer Anforderungszeichenfolge ausgelöst werden.

<a id="err-email-address-malformed"></a>

### Fehlerhafte E-Mail-Adresse

- **Code:** `EMAIL_ADDRESS_MALFORMED`
- **Quelle:** `EmailAddress`

Eine eingehende Anforderung enthält einen Wert, der keine wohlgeformte E-Mail-Adresse ist und daher nicht in ein EmailAddress-Value-Object geparst werden kann.

> **Geschäftsregel:** Eine E-Mail-Adresse eines Gastes muss genau ein „@“ mit einem nicht leeren lokalen Teil und einer nicht leeren Domäne enthalten.

#### Diagnosen

- **Der Client hat eine falsch geschriebene oder abgeschnittene Adresse gesendet (fehlendes „@“, leerer lokaler Teil oder leere Domäne).** — _Ursprung:_ External — Validieren Sie die Adresse auf dem Client und vergleichen Sie den gesendeten Wert mit dem erwarteten Format.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:email-address-malformed",
  "title": "Die E-Mail-Adresse ist ungültig.",
  "detail": "Der angegebene Wert ist keine gültige E-Mail-Adresse.",
  "code": "EMAIL_ADDRESS_MALFORMED"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [EmailAddress] 'not-an-email' is not a valid e-mail address. error.code=EMAIL_ADDRESS_MALFORMED
```

<a id="src-night-count"></a>

## NightCount-Fehler

Fehler, die beim Erstellen der Anzahl der Nächte aus einem Anforderungswert ausgelöst werden.

<a id="err-night-count-not-positive"></a>

### Nicht positive Anzahl von Nächten

- **Code:** `NIGHT_COUNT_NOT_POSITIVE`
- **Quelle:** `NightCount`

Eine eingehende Anforderung verlangt null oder eine negative Anzahl von Nächten, was keine gültige Aufenthaltsdauer ist.

> **Geschäftsregel:** Eine Buchung muss mindestens eine Nacht umfassen.

#### Diagnosen

- **Der Client hat eine Anzahl von Nächten von null oder weniger gesendet.** — _Ursprung:_ External — Senden Sie eine Anzahl von Nächten von eins oder mehr.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:night-count-not-positive",
  "title": "Die Anzahl der Nächte ist ungültig.",
  "detail": "Die angeforderte Anzahl von Nächten muss eins oder mehr betragen.",
  "code": "NIGHT_COUNT_NOT_POSITIVE"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [NightCount] A booking must be for at least one night, but 0 was requested. error.code=NIGHT_COUNT_NOT_POSITIVE
```

<a id="src-room-number"></a>

## RoomNumber-Fehler

Fehler, die beim Erstellen einer Zimmernummer aus einem Element einer Anforderungsliste ausgelöst werden.

<a id="err-room-number-out-of-range"></a>

### Zimmernummer außerhalb des Bereichs

- **Code:** `ROOM_NUMBER_OUT_OF_RANGE`
- **Quelle:** `RoomNumber`

Ein Element der Zimmernummernliste der Anforderung liegt außerhalb des unterstützten Bereichs (1-999).

> **Geschäftsregel:** Eine Zimmernummer muss zwischen 1 und 999 (einschließlich) liegen.

#### Diagnosen

- **Der Client hat eine Zimmernummer von null, einen negativen Wert oder einen Wert über 999 gesendet.** — _Ursprung:_ External — Senden Sie Zimmernummern innerhalb des unterstützten Bereichs (1-999).

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:room-number-out-of-range",
  "title": "Eine Zimmernummer ist ungültig.",
  "detail": "Eine der angeforderten Zimmernummern liegt außerhalb des unterstützten Bereichs.",
  "code": "ROOM_NUMBER_OUT_OF_RANGE"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [RoomNumber] Room number 1000 is outside the supported range 1-999. error.code=ROOM_NUMBER_OUT_OF_RANGE
```

<a id="src-stay"></a>

## Stay-Fehler

Fehler, die bei der gemeinsamen Validierung von An- und Abreisedatum eines Aufenthalts ausgelöst werden.

<a id="err-stay-checkout-not-after-checkin"></a>

### Abreise nicht nach Anreise

- **Code:** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Quelle:** `Stay`

Beide Aufenthaltsdaten werden geparst, aber das Abreisedatum liegt vor oder auf dem Anreisedatum, sodass der Aufenthalt keine positive Dauer hat. Diese feldübergreifende Regel wird von der Stay.Create-Factory durchgesetzt.

> **Geschäftsregel:** Die Abreise muss streng nach der Anreise liegen.

#### Diagnosen

- **Der Client hat ein Abreisedatum gesendet, das gleich oder früher als das Anreisedatum ist.** — _Ursprung:_ External — Senden Sie ein Abreisedatum, das mindestens einen Tag nach dem Anreisedatum liegt.

#### Beispiele

**Öffentliche Antwort (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:stay-checkout-not-after-checkin",
  "title": "Die Aufenthaltsdaten sind ungültig.",
  "detail": "Das Abreisedatum muss nach dem Anreisedatum liegen.",
  "code": "STAY_CHECKOUT_NOT_AFTER_CHECKIN"
}
```

**Diagnose (intern — nicht für externe Weitergabe)**

```text
2026-07-04T13:42:18.734Z ERROR [Stay] Check-out 2026-08-10 must be strictly after check-in 2026-08-14. error.code=STAY_CHECKOUT_NOT_AFTER_CHECKIN
```


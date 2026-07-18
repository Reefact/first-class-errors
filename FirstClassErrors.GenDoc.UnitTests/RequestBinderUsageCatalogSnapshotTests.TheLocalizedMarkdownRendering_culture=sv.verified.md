# Felkatalog

## Innehållsförteckning

- [BookingEndpoint-fel](#src-booking-endpoint)
  - [Bokningsbegäran ogiltig](#err-booking-command-invalid)
  - [Bokningens gäst ogiltig](#err-booking-guest-invalid)
  - [Bokningens vistelse ogiltig](#err-booking-stay-invalid)
- [BookingDate-fel](#src-booking-date)
  - [Felaktigt bokningsdatum](#err-booking-date-malformed)
- [Tag-fel](#src-tag)
  - [Felaktig bokningstagg](#err-booking-tag-malformed)
- [Currency-fel](#src-currency)
  - [Felaktig valutakod](#err-currency-code-malformed)
- [EmailAddress-fel](#src-email-address)
  - [Felaktig e-postadress](#err-email-address-malformed)
- [NightCount-fel](#src-night-count)
  - [Icke-positivt antal nätter](#err-night-count-not-positive)
- [RoomNumber-fel](#src-room-number)
  - [Rumsnummer utanför intervallet](#err-room-number-out-of-range)
- [Stay-fel](#src-stay)
  - [Utcheckning inte efter incheckning](#err-stay-checkout-not-after-checkin)

<a id="src-booking-endpoint"></a>

## BookingEndpoint-fel

Primärportsfel som utlöses av bokningsslutpunkten när den binder en inkommande begäran till ett kommando.

<a id="err-booking-command-invalid"></a>

### Bokningsbegäran ogiltig

- **Kod:** `BOOKING_COMMAND_INVALID`
- **Källa:** `BookingEndpoint`

Slutpunkten kunde inte binda den inkommande begäran till ett bokningskommando: ett eller flera argument saknades eller var ogiltiga. Varje fel samlas under detta hölje, vart och ett med sin fullständiga argumentsökväg.

> **Affärsregel:** Varje obligatoriskt argument måste finnas, och varje argument måste konverteras till sitt värdeobjekt.

#### Diagnostik

- **Klienten skickade en begäran som bryter mot slutpunktens kontrakt (saknade eller felaktiga argument).** — _ursprung:_ External — Läs de inre felen: vart och ett namnger det felande argumentet och regeln som bryts.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-command-invalid",
  "title": "Vi kunde inte acceptera din bokningsbegäran.",
  "detail": "En eller flera uppgifter i bokningsbegäran saknas eller är ogiltiga.",
  "code": "BOOKING_COMMAND_INVALID"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The booking command is invalid: one or more request arguments failed to bind. error.code=BOOKING_COMMAND_INVALID
```

<a id="err-booking-guest-invalid"></a>

### Bokningens gäst ogiltig

- **Kod:** `BOOKING_GUEST_INVALID`
- **Källa:** `BookingEndpoint`

En gäst i begärans gästlista kunde inte bindas: dess förnamn saknades eller dess e-post var felaktig. Dess fel grupperas under detta hölje per element, med indexerade sökvägar som Guests[1].

> **Affärsregel:** Varje gäst måste ha ett förnamn, och varje e-post som finns måste vara giltig.

#### Diagnostik

- **Klienten skickade en gäst med ett saknat förnamn eller en felaktig e-postadress.** — _ursprung:_ External — Läs de inre felen under den indexerade sökvägen Guests[i] för det felande fältet.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-guest-invalid",
  "title": "En gästs uppgifter är ogiltiga.",
  "detail": "En av gästerna saknar förnamn eller har en ogiltig e-postadress.",
  "code": "BOOKING_GUEST_INVALID"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The guest is invalid: one or more of its fields failed to bind. error.code=BOOKING_GUEST_INVALID
```

<a id="err-booking-stay-invalid"></a>

### Bokningens vistelse ogiltig

- **Kod:** `BOOKING_STAY_INVALID`
- **Källa:** `BookingEndpoint`

Begärans vistelsedelobjekt kunde inte bindas: ett eller båda av dess datum saknades eller var felaktiga. Dess fel grupperas under detta nästlade hölje, med sökvägar prefixerade med Stay.

> **Affärsregel:** Båda vistelsedatumen måste finnas och vara giltiga ISO-datum.

#### Diagnostik

- **Klienten skickade en vistelse med ett saknat eller felaktigt in- eller utcheckningsdatum.** — _ursprung:_ External — Läs de inre felen under Stay-sökvägen för det felande datumet.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-stay-invalid",
  "title": "Vi kunde inte läsa vistelsedatumen.",
  "detail": "Vistelsens in- eller utcheckningsdatum saknas eller är ogiltigt.",
  "code": "BOOKING_STAY_INVALID"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The stay is invalid: one or more of its dates failed to bind. error.code=BOOKING_STAY_INVALID
```

<a id="src-booking-date"></a>

## BookingDate-fel

Fel som utlöses när ett bokningsdatum (incheckning / utcheckning) tolkas från en begäranssträng.

<a id="err-booking-date-malformed"></a>

### Felaktigt bokningsdatum

- **Kod:** `BOOKING_DATE_MALFORMED`
- **Källa:** `BookingDate`

En inkommande begäran innehåller ett värde som inte är ett ISO-datum (yyyy-MM-dd) och därför inte kan tolkas till ett BookingDate-värdeobjekt.

> **Affärsregel:** Ett bokningsdatum måste vara ett ISO-kalenderdatum i formatet yyyy-MM-dd.

#### Diagnostik

- **Klienten skickade ett datum i ett språkspecifikt eller felaktigt format, eller ett omöjligt kalenderdatum.** — _ursprung:_ External — Skicka datum i formatet ISO 8601 yyyy-MM-dd, till exempel 2026-08-10.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-date-malformed",
  "title": "Datumet är ogiltigt.",
  "detail": "Ett bokningsdatum är inte ett giltigt ISO-datum (yyyy-MM-dd).",
  "code": "BOOKING_DATE_MALFORMED"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingDate] '2026-13-40' is not a valid ISO (yyyy-MM-dd) date. error.code=BOOKING_DATE_MALFORMED
```

<a id="src-tag"></a>

## Tag-fel

Fel som utlöses när en bokningstagg tolkas från ett element i en begäranslista.

<a id="err-booking-tag-malformed"></a>

### Felaktig bokningstagg

- **Kod:** `BOOKING_TAG_MALFORMED`
- **Källa:** `Tag`

Ett element i begärans tagglista är tomt, för långt eller innehåller blanksteg och kan därför inte tolkas till ett Tag-värdeobjekt.

> **Affärsregel:** En tagg måste vara en enda icke-tom token på högst 32 tecken, utan blanksteg.

#### Diagnostik

- **Klienten skickade en tom tagg, ett uttryck med blanksteg eller ett för långt värde.** — _ursprung:_ External — Skicka varje tagg som en enda token utan blanksteg; koppla ihop flerordstaggar med bindestreck.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-tag-malformed",
  "title": "En tagg är ogiltig.",
  "detail": "En av bokningstaggarna är inte en giltig enskild token.",
  "code": "BOOKING_TAG_MALFORMED"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Tag] 'late checkout' is not a valid tag. error.code=BOOKING_TAG_MALFORMED
```

<a id="src-currency"></a>

## Currency-fel

Fel som utlöses när en faktureringsvalutakod tolkas från en begäranssträng.

<a id="err-currency-code-malformed"></a>

### Felaktig valutakod

- **Kod:** `CURRENCY_CODE_MALFORMED`
- **Källa:** `Currency`

En inkommande begäran innehåller ett värde som inte är en välformad valutakod med tre bokstäver och därför inte kan tolkas till ett Currency-värdeobjekt.

> **Affärsregel:** En valutakod måste bestå av exakt tre versala ASCII-bokstäver (till exempel EUR).

#### Diagnostik

- **Klienten skickade en valuta i fel form (gemener, en symbol, ett namn eller fel längd).** — _ursprung:_ External — Skicka den alfabetiska ISO-4217-koden i versaler, till exempel USD eller EUR.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:currency-code-malformed",
  "title": "Valutan är ogiltig.",
  "detail": "Faktureringsvalutakoden är inte en giltig kod med tre bokstäver.",
  "code": "CURRENCY_CODE_MALFORMED"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Currency] 'EURO' is not a valid three-letter currency code. error.code=CURRENCY_CODE_MALFORMED
```

<a id="src-email-address"></a>

## EmailAddress-fel

Fel som utlöses när en gästs e-postadress tolkas från en begäranssträng.

<a id="err-email-address-malformed"></a>

### Felaktig e-postadress

- **Kod:** `EMAIL_ADDRESS_MALFORMED`
- **Källa:** `EmailAddress`

En inkommande begäran innehåller ett värde som inte är en välformad e-postadress och därför inte kan tolkas till ett EmailAddress-värdeobjekt.

> **Affärsregel:** En gästs e-postadress måste innehålla exakt ett ”@” med en icke-tom lokal del och en icke-tom domän.

#### Diagnostik

- **Klienten skickade en felstavad eller avkortad adress (saknat ”@”, tom lokal del eller domän).** — _ursprung:_ External — Validera adressen på klienten och jämför det skickade värdet med det förväntade formatet.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:email-address-malformed",
  "title": "E-postadressen är ogiltig.",
  "detail": "Det angivna värdet är inte en giltig e-postadress.",
  "code": "EMAIL_ADDRESS_MALFORMED"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [EmailAddress] 'not-an-email' is not a valid e-mail address. error.code=EMAIL_ADDRESS_MALFORMED
```

<a id="src-night-count"></a>

## NightCount-fel

Fel som utlöses när antalet nätter byggs från ett begäransvärde.

<a id="err-night-count-not-positive"></a>

### Icke-positivt antal nätter

- **Kod:** `NIGHT_COUNT_NOT_POSITIVE`
- **Källa:** `NightCount`

En inkommande begäran begär noll eller ett negativt antal nätter, vilket inte är en giltig vistelselängd.

> **Affärsregel:** En bokning måste omfatta minst en natt.

#### Diagnostik

- **Klienten skickade ett antal nätter på noll eller lägre.** — _ursprung:_ External — Skicka ett antal nätter på en eller fler.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:night-count-not-positive",
  "title": "Antalet nätter är ogiltigt.",
  "detail": "Det begärda antalet nätter måste vara en eller fler.",
  "code": "NIGHT_COUNT_NOT_POSITIVE"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [NightCount] A booking must be for at least one night, but 0 was requested. error.code=NIGHT_COUNT_NOT_POSITIVE
```

<a id="src-room-number"></a>

## RoomNumber-fel

Fel som utlöses när ett rumsnummer byggs från ett element i en begäranslista.

<a id="err-room-number-out-of-range"></a>

### Rumsnummer utanför intervallet

- **Kod:** `ROOM_NUMBER_OUT_OF_RANGE`
- **Källa:** `RoomNumber`

Ett element i begärans lista över rumsnummer ligger utanför det intervall som stöds (1-999).

> **Affärsregel:** Ett rumsnummer måste vara mellan 1 och 999 inklusive.

#### Diagnostik

- **Klienten skickade ett rumsnummer på noll, ett negativt värde eller ett värde över 999.** — _ursprung:_ External — Skicka rumsnummer inom det intervall som stöds (1-999).

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:room-number-out-of-range",
  "title": "Ett rumsnummer är ogiltigt.",
  "detail": "Ett av de begärda rumsnumren ligger utanför det intervall som stöds.",
  "code": "ROOM_NUMBER_OUT_OF_RANGE"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [RoomNumber] Room number 1000 is outside the supported range 1-999. error.code=ROOM_NUMBER_OUT_OF_RANGE
```

<a id="src-stay"></a>

## Stay-fel

Fel som utlöses vid gemensam validering av en vistelses in- och utcheckningsdatum.

<a id="err-stay-checkout-not-after-checkin"></a>

### Utcheckning inte efter incheckning

- **Kod:** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Källa:** `Stay`

Båda vistelsedatumen tolkas, men utcheckningsdatumet är före eller lika med incheckningsdatumet, så vistelsen har ingen positiv längd. Denna regel över flera fält tillämpas av fabriken Stay.Create.

> **Affärsregel:** Utcheckningen måste vara strikt efter incheckningen.

#### Diagnostik

- **Klienten skickade ett utcheckningsdatum som är lika med eller tidigare än incheckningsdatumet.** — _ursprung:_ External — Skicka ett utcheckningsdatum minst en dag efter incheckningsdatumet.

#### Exempel

**Publikt svar (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:stay-checkout-not-after-checkin",
  "title": "Vistelsedatumen är ogiltiga.",
  "detail": "Utcheckningsdatumet måste vara efter incheckningsdatumet.",
  "code": "STAY_CHECKOUT_NOT_AFTER_CHECKIN"
}
```

**Diagnostik (intern — inte avsedd för extern exponering)**

```text
2026-07-04T13:42:18.734Z ERROR [Stay] Check-out 2026-08-10 must be strictly after check-in 2026-08-14. error.code=STAY_CHECKOUT_NOT_AFTER_CHECKIN
```


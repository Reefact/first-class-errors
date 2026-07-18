# Catalogue des erreurs

## Table des matières

- [Erreurs BookingEndpoint](#src-booking-endpoint)
  - [Requête de réservation invalide](#err-booking-command-invalid)
  - [Voyageur de la réservation invalide](#err-booking-guest-invalid)
  - [Séjour de la réservation invalide](#err-booking-stay-invalid)
- [Erreurs BookingDate](#src-booking-date)
  - [Date de réservation mal formée](#err-booking-date-malformed)
- [Erreurs Tag](#src-tag)
  - [Étiquette de réservation mal formée](#err-booking-tag-malformed)
- [Erreurs Currency](#src-currency)
  - [Code de devise mal formé](#err-currency-code-malformed)
- [Erreurs EmailAddress](#src-email-address)
  - [Adresse e-mail mal formée](#err-email-address-malformed)
- [Erreurs NightCount](#src-night-count)
  - [Nombre de nuits non positif](#err-night-count-not-positive)
- [Erreurs RoomNumber](#src-room-number)
  - [Numéro de chambre hors plage](#err-room-number-out-of-range)
- [Erreurs Stay](#src-stay)
  - [Départ non postérieur à l'arrivée](#err-stay-checkout-not-after-checkin)

<a id="src-booking-endpoint"></a>

## Erreurs BookingEndpoint

Erreurs de port primaire levées par le point d'entrée de réservation lorsqu'il lie une requête entrante en une commande.

<a id="err-booking-command-invalid"></a>

### Requête de réservation invalide

- **Code :** `BOOKING_COMMAND_INVALID`
- **Source :** `BookingEndpoint`

Le point d'entrée n'a pas pu lier la requête entrante en une commande de réservation : un ou plusieurs arguments étaient manquants ou invalides. Chaque échec est collecté sous cette enveloppe, avec son chemin d'argument complet.

> **Règle métier :** Chaque argument requis doit être présent, et chaque argument doit se convertir en son value object.

#### Diagnostics

- **Le client a envoyé une requête qui viole le contrat du point d'entrée (arguments manquants ou mal formés).** — _origine :_ External — Lisez les erreurs internes : chacune nomme l'argument défaillant et la règle qu'il viole.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-command-invalid",
  "title": "Nous n'avons pas pu accepter votre demande de réservation.",
  "detail": "Un ou plusieurs détails de la demande de réservation sont manquants ou invalides.",
  "code": "BOOKING_COMMAND_INVALID"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The booking command is invalid: one or more request arguments failed to bind. error.code=BOOKING_COMMAND_INVALID
```

<a id="err-booking-guest-invalid"></a>

### Voyageur de la réservation invalide

- **Code :** `BOOKING_GUEST_INVALID`
- **Source :** `BookingEndpoint`

Un voyageur de la liste de voyageurs de la requête n'a pas pu être lié : son prénom était manquant ou son e-mail mal formé. Ses échecs sont regroupés sous cette enveloppe par élément, avec des chemins indexés tels que Guests[1].

> **Règle métier :** Chaque voyageur doit avoir un prénom, et tout e-mail présent doit être valide.

#### Diagnostics

- **Le client a envoyé un voyageur avec un prénom manquant ou une adresse e-mail mal formée.** — _origine :_ External — Lisez les erreurs internes sous le chemin indexé Guests[i] pour le champ défaillant.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-guest-invalid",
  "title": "Les informations d'un voyageur sont invalides.",
  "detail": "L'un des voyageurs n'a pas de prénom ou a une adresse e-mail invalide.",
  "code": "BOOKING_GUEST_INVALID"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The guest is invalid: one or more of its fields failed to bind. error.code=BOOKING_GUEST_INVALID
```

<a id="err-booking-stay-invalid"></a>

### Séjour de la réservation invalide

- **Code :** `BOOKING_STAY_INVALID`
- **Source :** `BookingEndpoint`

Le sous-objet séjour de la requête n'a pas pu être lié : une ou deux de ses dates étaient manquantes ou mal formées. Ses échecs sont regroupés sous cette enveloppe imbriquée, avec des chemins préfixés par Stay.

> **Règle métier :** Les deux dates du séjour doivent être présentes et être des dates ISO valides.

#### Diagnostics

- **Le client a envoyé un séjour avec une date d'arrivée ou de départ manquante ou mal formée.** — _origine :_ External — Lisez les erreurs internes sous le chemin Stay pour la date défaillante.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-stay-invalid",
  "title": "Nous n'avons pas pu lire les dates du séjour.",
  "detail": "La date d'arrivée ou de départ du séjour est manquante ou invalide.",
  "code": "BOOKING_STAY_INVALID"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The stay is invalid: one or more of its dates failed to bind. error.code=BOOKING_STAY_INVALID
```

<a id="src-booking-date"></a>

## Erreurs BookingDate

Erreurs levées lors de l'analyse d'une date de réservation (arrivée / départ) à partir d'une chaîne de requête.

<a id="err-booking-date-malformed"></a>

### Date de réservation mal formée

- **Code :** `BOOKING_DATE_MALFORMED`
- **Source :** `BookingDate`

Une requête entrante porte une valeur qui n'est pas une date ISO yyyy-MM-dd : elle ne peut donc pas être analysée en un value object BookingDate.

> **Règle métier :** Une date de réservation doit être une date calendaire ISO au format yyyy-MM-dd.

#### Diagnostics

- **Le client a envoyé une date dans un format localisé ou mal formé, ou une date calendaire impossible.** — _origine :_ External — Envoyez les dates au format ISO 8601 yyyy-MM-dd, par exemple 2026-08-10.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-date-malformed",
  "title": "La date est invalide.",
  "detail": "Une date de réservation n'est pas une date ISO (yyyy-MM-dd) valide.",
  "code": "BOOKING_DATE_MALFORMED"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingDate] '2026-13-40' is not a valid ISO (yyyy-MM-dd) date. error.code=BOOKING_DATE_MALFORMED
```

<a id="src-tag"></a>

## Erreurs Tag

Erreurs levées lors de l'analyse d'une étiquette de réservation à partir d'un élément d'une liste de requête.

<a id="err-booking-tag-malformed"></a>

### Étiquette de réservation mal formée

- **Code :** `BOOKING_TAG_MALFORMED`
- **Source :** `Tag`

Un élément de la liste d'étiquettes de la requête est vide, trop long ou contient des espaces : il ne peut donc pas être analysé en un value object Tag.

> **Règle métier :** Une étiquette doit être un unique jeton non vide d'au plus 32 caractères, sans espace.

#### Diagnostics

- **Le client a envoyé une étiquette vide, une expression contenant des espaces, ou une valeur trop longue.** — _origine :_ External — Envoyez chaque étiquette comme un unique jeton sans espace ; reliez les étiquettes en plusieurs mots par un trait d'union.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-tag-malformed",
  "title": "Une étiquette est invalide.",
  "detail": "L'une des étiquettes de réservation n'est pas un jeton unique valide.",
  "code": "BOOKING_TAG_MALFORMED"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [Tag] 'late checkout' is not a valid tag. error.code=BOOKING_TAG_MALFORMED
```

<a id="src-currency"></a>

## Erreurs Currency

Erreurs levées lors de l'analyse d'un code de devise de facturation à partir d'une chaîne de requête.

<a id="err-currency-code-malformed"></a>

### Code de devise mal formé

- **Code :** `CURRENCY_CODE_MALFORMED`
- **Source :** `Currency`

Une requête entrante porte une valeur qui n'est pas un code de devise à trois lettres bien formé : elle ne peut donc pas être analysée en un value object Currency.

> **Règle métier :** Un code de devise doit être composé d'exactement trois lettres ASCII majuscules (par exemple EUR).

#### Diagnostics

- **Le client a envoyé une devise dans la mauvaise forme (minuscules, symbole, nom, ou mauvaise longueur).** — _origine :_ External — Envoyez le code alphabétique ISO-4217 en majuscules, par exemple USD ou EUR.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:currency-code-malformed",
  "title": "La devise est invalide.",
  "detail": "Le code de devise de facturation n'est pas un code à trois lettres valide.",
  "code": "CURRENCY_CODE_MALFORMED"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [Currency] 'EURO' is not a valid three-letter currency code. error.code=CURRENCY_CODE_MALFORMED
```

<a id="src-email-address"></a>

## Erreurs EmailAddress

Erreurs levées lors de l'analyse d'une adresse e-mail de voyageur à partir d'une chaîne de requête.

<a id="err-email-address-malformed"></a>

### Adresse e-mail mal formée

- **Code :** `EMAIL_ADDRESS_MALFORMED`
- **Source :** `EmailAddress`

Une requête entrante porte une valeur qui n'est pas une adresse e-mail bien formée : elle ne peut donc pas être analysée en un value object EmailAddress.

> **Règle métier :** Une adresse e-mail de voyageur doit contenir un seul « @ » avec une partie locale et un domaine non vides.

#### Diagnostics

- **Le client a envoyé une adresse mal orthographiée ou tronquée (« @ » manquant, partie locale ou domaine vide).** — _origine :_ External — Validez l'adresse côté client et comparez la valeur envoyée au format attendu.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:email-address-malformed",
  "title": "L'adresse e-mail est invalide.",
  "detail": "La valeur fournie n'est pas une adresse e-mail valide.",
  "code": "EMAIL_ADDRESS_MALFORMED"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [EmailAddress] 'not-an-email' is not a valid e-mail address. error.code=EMAIL_ADDRESS_MALFORMED
```

<a id="src-night-count"></a>

## Erreurs NightCount

Erreurs levées lors de la construction du nombre de nuits à partir d'une valeur de requête.

<a id="err-night-count-not-positive"></a>

### Nombre de nuits non positif

- **Code :** `NIGHT_COUNT_NOT_POSITIVE`
- **Source :** `NightCount`

Une requête entrante demande zéro ou un nombre négatif de nuits, ce qui n'est pas une durée de séjour valide.

> **Règle métier :** Une réservation doit porter sur au moins une nuit.

#### Diagnostics

- **Le client a envoyé un nombre de nuits nul ou négatif.** — _origine :_ External — Envoyez un nombre de nuits d'une ou plus.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:night-count-not-positive",
  "title": "Le nombre de nuits est invalide.",
  "detail": "Le nombre de nuits demandé doit être d'au moins une.",
  "code": "NIGHT_COUNT_NOT_POSITIVE"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [NightCount] A booking must be for at least one night, but 0 was requested. error.code=NIGHT_COUNT_NOT_POSITIVE
```

<a id="src-room-number"></a>

## Erreurs RoomNumber

Erreurs levées lors de la construction d'un numéro de chambre à partir d'un élément d'une liste de requête.

<a id="err-room-number-out-of-range"></a>

### Numéro de chambre hors plage

- **Code :** `ROOM_NUMBER_OUT_OF_RANGE`
- **Source :** `RoomNumber`

Un élément de la liste de numéros de chambre de la requête est en dehors de la plage prise en charge (1-999).

> **Règle métier :** Un numéro de chambre doit être compris entre 1 et 999 inclus.

#### Diagnostics

- **Le client a envoyé un numéro de chambre nul, négatif, ou supérieur à 999.** — _origine :_ External — Envoyez des numéros de chambre dans la plage prise en charge (1-999).

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:room-number-out-of-range",
  "title": "Un numéro de chambre est invalide.",
  "detail": "L'un des numéros de chambre demandés est en dehors de la plage prise en charge.",
  "code": "ROOM_NUMBER_OUT_OF_RANGE"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [RoomNumber] Room number 1000 is outside the supported range 1-999. error.code=ROOM_NUMBER_OUT_OF_RANGE
```

<a id="src-stay"></a>

## Erreurs Stay

Erreurs levées lors de la validation conjointe des dates d'arrivée et de départ d'un séjour.

<a id="err-stay-checkout-not-after-checkin"></a>

### Départ non postérieur à l'arrivée

- **Code :** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Source :** `Stay`

Les deux dates du séjour sont analysées, mais la date de départ est antérieure ou égale à la date d'arrivée : le séjour n'a donc pas de durée positive. Cette règle inter-champs est appliquée par la fabrique Stay.Create.

> **Règle métier :** Le départ doit être strictement postérieur à l'arrivée.

#### Diagnostics

- **Le client a envoyé une date de départ égale ou antérieure à la date d'arrivée.** — _origine :_ External — Envoyez une date de départ au moins un jour après la date d'arrivée.

#### Exemples

**Réponse publique (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:stay-checkout-not-after-checkin",
  "title": "Les dates du séjour sont invalides.",
  "detail": "La date de départ doit être postérieure à la date d'arrivée.",
  "code": "STAY_CHECKOUT_NOT_AFTER_CHECKIN"
}
```

**Diagnostic (interne — non destiné à l'exposition externe)**

```text
2026-07-04T13:42:18.734Z ERROR [Stay] Check-out 2026-08-10 must be strictly after check-in 2026-08-14. error.code=STAY_CHECKOUT_NOT_AFTER_CHECKIN
```


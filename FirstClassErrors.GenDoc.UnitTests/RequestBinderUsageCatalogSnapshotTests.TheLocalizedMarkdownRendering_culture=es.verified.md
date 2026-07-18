# Catálogo de errores

## Índice

- [Errores de BookingEndpoint](#src-booking-endpoint)
  - [Solicitud de reserva no válida](#err-booking-command-invalid)
  - [Huésped de la reserva no válido](#err-booking-guest-invalid)
  - [Estancia de la reserva no válida](#err-booking-stay-invalid)
- [Errores de BookingDate](#src-booking-date)
  - [Fecha de reserva mal formada](#err-booking-date-malformed)
- [Errores de Tag](#src-tag)
  - [Etiqueta de reserva mal formada](#err-booking-tag-malformed)
- [Errores de Currency](#src-currency)
  - [Código de moneda mal formado](#err-currency-code-malformed)
- [Errores de EmailAddress](#src-email-address)
  - [Dirección de correo electrónico mal formada](#err-email-address-malformed)
- [Errores de NightCount](#src-night-count)
  - [Número de noches no positivo](#err-night-count-not-positive)
- [Errores de RoomNumber](#src-room-number)
  - [Número de habitación fuera de rango](#err-room-number-out-of-range)
- [Errores de Stay](#src-stay)
  - [Salida no posterior a la entrada](#err-stay-checkout-not-after-checkin)

<a id="src-booking-endpoint"></a>

## Errores de BookingEndpoint

Errores de puerto primario generados por el endpoint de reserva al enlazar una solicitud entrante en un comando.

<a id="err-booking-command-invalid"></a>

### Solicitud de reserva no válida

- **Código:** `BOOKING_COMMAND_INVALID`
- **Fuente:** `BookingEndpoint`

El endpoint no pudo enlazar la solicitud entrante en un comando de reserva: uno o varios argumentos faltaban o no eran válidos. Cada fallo se recopila bajo este sobre, con su ruta de argumento completa.

> **Regla de negocio:** Todos los argumentos requeridos deben estar presentes, y cada argumento debe convertirse en su value object.

#### Diagnósticos

- **El cliente envió una solicitud que incumple el contrato del endpoint (argumentos faltantes o mal formados).** — _origen:_ External — Lea los errores internos: cada uno nombra el argumento fallido y la regla que incumple.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-command-invalid",
  "title": "No pudimos aceptar su solicitud de reserva.",
  "detail": "Uno o varios datos de la solicitud de reserva faltan o no son válidos.",
  "code": "BOOKING_COMMAND_INVALID"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The booking command is invalid: one or more request arguments failed to bind. error.code=BOOKING_COMMAND_INVALID
```

<a id="err-booking-guest-invalid"></a>

### Huésped de la reserva no válido

- **Código:** `BOOKING_GUEST_INVALID`
- **Fuente:** `BookingEndpoint`

Un huésped de la lista de huéspedes de la solicitud no pudo enlazarse: su nombre faltaba o su correo electrónico estaba mal formado. Sus fallos se agrupan bajo este sobre por elemento, con rutas indexadas como Guests[1].

> **Regla de negocio:** Cada huésped debe tener un nombre, y todo correo electrónico presente debe ser válido.

#### Diagnósticos

- **El cliente envió un huésped con un nombre faltante o una dirección de correo electrónico mal formada.** — _origen:_ External — Lea los errores internos bajo la ruta indexada Guests[i] para el campo fallido.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-guest-invalid",
  "title": "La información de un huésped no es válida.",
  "detail": "Uno de los huéspedes no tiene nombre o tiene una dirección de correo electrónico no válida.",
  "code": "BOOKING_GUEST_INVALID"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The guest is invalid: one or more of its fields failed to bind. error.code=BOOKING_GUEST_INVALID
```

<a id="err-booking-stay-invalid"></a>

### Estancia de la reserva no válida

- **Código:** `BOOKING_STAY_INVALID`
- **Fuente:** `BookingEndpoint`

El subobjeto de estancia de la solicitud no pudo enlazarse: una o ambas de sus fechas faltaban o estaban mal formadas. Sus fallos se agrupan bajo este sobre anidado, con rutas prefijadas por Stay.

> **Regla de negocio:** Ambas fechas de la estancia deben estar presentes y ser fechas ISO válidas.

#### Diagnósticos

- **El cliente envió una estancia con una fecha de entrada o de salida faltante o mal formada.** — _origen:_ External — Lea los errores internos bajo la ruta Stay para la fecha fallida.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-stay-invalid",
  "title": "No pudimos leer las fechas de la estancia.",
  "detail": "La fecha de entrada o de salida de la estancia falta o no es válida.",
  "code": "BOOKING_STAY_INVALID"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingEndpoint] The stay is invalid: one or more of its dates failed to bind. error.code=BOOKING_STAY_INVALID
```

<a id="src-booking-date"></a>

## Errores de BookingDate

Errores generados al analizar una fecha de reserva (entrada / salida) a partir de una cadena de la solicitud.

<a id="err-booking-date-malformed"></a>

### Fecha de reserva mal formada

- **Código:** `BOOKING_DATE_MALFORMED`
- **Fuente:** `BookingDate`

Una solicitud entrante contiene un valor que no es una fecha ISO yyyy-MM-dd, por lo que no puede analizarse en un value object BookingDate.

> **Regla de negocio:** Una fecha de reserva debe ser una fecha de calendario ISO en formato yyyy-MM-dd.

#### Diagnósticos

- **El cliente envió una fecha en un formato local o mal formado, o una fecha de calendario imposible.** — _origen:_ External — Envíe las fechas en formato ISO 8601 yyyy-MM-dd, por ejemplo 2026-08-10.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-date-malformed",
  "title": "La fecha no es válida.",
  "detail": "Una fecha de reserva no es una fecha ISO (yyyy-MM-dd) válida.",
  "code": "BOOKING_DATE_MALFORMED"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BookingDate] '2026-13-40' is not a valid ISO (yyyy-MM-dd) date. error.code=BOOKING_DATE_MALFORMED
```

<a id="src-tag"></a>

## Errores de Tag

Errores generados al analizar una etiqueta de reserva a partir de un elemento de una lista de la solicitud.

<a id="err-booking-tag-malformed"></a>

### Etiqueta de reserva mal formada

- **Código:** `BOOKING_TAG_MALFORMED`
- **Fuente:** `Tag`

Un elemento de la lista de etiquetas de la solicitud está vacío, es demasiado largo o contiene espacios, por lo que no puede analizarse en un value object Tag.

> **Regla de negocio:** Una etiqueta debe ser un único token no vacío de un máximo de 32 caracteres, sin espacios.

#### Diagnósticos

- **El cliente envió una etiqueta vacía, una expresión con espacios o un valor demasiado largo.** — _origen:_ External — Envíe cada etiqueta como un único token sin espacios; una las etiquetas de varias palabras con un guion.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:booking-tag-malformed",
  "title": "Una etiqueta no es válida.",
  "detail": "Una de las etiquetas de reserva no es un token único válido.",
  "code": "BOOKING_TAG_MALFORMED"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Tag] 'late checkout' is not a valid tag. error.code=BOOKING_TAG_MALFORMED
```

<a id="src-currency"></a>

## Errores de Currency

Errores generados al analizar un código de moneda de facturación a partir de una cadena de la solicitud.

<a id="err-currency-code-malformed"></a>

### Código de moneda mal formado

- **Código:** `CURRENCY_CODE_MALFORMED`
- **Fuente:** `Currency`

Una solicitud entrante contiene un valor que no es un código de moneda de tres letras bien formado, por lo que no puede analizarse en un value object Currency.

> **Regla de negocio:** Un código de moneda debe constar exactamente de tres letras ASCII mayúsculas (por ejemplo, EUR).

#### Diagnósticos

- **El cliente envió una moneda con la forma incorrecta (minúsculas, un símbolo, un nombre o una longitud incorrecta).** — _origen:_ External — Envíe el código alfabético ISO-4217 en mayúsculas, por ejemplo USD o EUR.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:currency-code-malformed",
  "title": "La moneda no es válida.",
  "detail": "El código de moneda de facturación no es un código de tres letras válido.",
  "code": "CURRENCY_CODE_MALFORMED"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Currency] 'EURO' is not a valid three-letter currency code. error.code=CURRENCY_CODE_MALFORMED
```

<a id="src-email-address"></a>

## Errores de EmailAddress

Errores generados al analizar la dirección de correo electrónico de un huésped a partir de una cadena de la solicitud.

<a id="err-email-address-malformed"></a>

### Dirección de correo electrónico mal formada

- **Código:** `EMAIL_ADDRESS_MALFORMED`
- **Fuente:** `EmailAddress`

Una solicitud entrante contiene un valor que no es una dirección de correo electrónico bien formada, por lo que no puede analizarse en un value object EmailAddress.

> **Regla de negocio:** Una dirección de correo electrónico de huésped debe contener una sola « @ » con una parte local y un dominio no vacíos.

#### Diagnósticos

- **El cliente envió una dirección mal escrita o truncada (falta « @ », parte local o dominio vacíos).** — _origen:_ External — Valide la dirección en el cliente y compare el valor enviado con el formato esperado.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:email-address-malformed",
  "title": "La dirección de correo electrónico no es válida.",
  "detail": "El valor proporcionado no es una dirección de correo electrónico válida.",
  "code": "EMAIL_ADDRESS_MALFORMED"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [EmailAddress] 'not-an-email' is not a valid e-mail address. error.code=EMAIL_ADDRESS_MALFORMED
```

<a id="src-night-count"></a>

## Errores de NightCount

Errores generados al construir el número de noches a partir de un valor de la solicitud.

<a id="err-night-count-not-positive"></a>

### Número de noches no positivo

- **Código:** `NIGHT_COUNT_NOT_POSITIVE`
- **Fuente:** `NightCount`

Una solicitud entrante pide cero o un número negativo de noches, lo que no es una duración de estancia válida.

> **Regla de negocio:** Una reserva debe ser de al menos una noche.

#### Diagnósticos

- **El cliente envió un número de noches de cero o inferior.** — _origen:_ External — Envíe un número de noches de una o más.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:night-count-not-positive",
  "title": "El número de noches no es válido.",
  "detail": "El número de noches solicitado debe ser de una o más.",
  "code": "NIGHT_COUNT_NOT_POSITIVE"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [NightCount] A booking must be for at least one night, but 0 was requested. error.code=NIGHT_COUNT_NOT_POSITIVE
```

<a id="src-room-number"></a>

## Errores de RoomNumber

Errores generados al construir un número de habitación a partir de un elemento de una lista de la solicitud.

<a id="err-room-number-out-of-range"></a>

### Número de habitación fuera de rango

- **Código:** `ROOM_NUMBER_OUT_OF_RANGE`
- **Fuente:** `RoomNumber`

Un elemento de la lista de números de habitación de la solicitud está fuera del rango admitido (1-999).

> **Regla de negocio:** Un número de habitación debe estar entre 1 y 999 inclusive.

#### Diagnósticos

- **El cliente envió un número de habitación de cero, negativo o superior a 999.** — _origen:_ External — Envíe números de habitación dentro del rango admitido (1-999).

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:room-number-out-of-range",
  "title": "Un número de habitación no es válido.",
  "detail": "Uno de los números de habitación solicitados está fuera del rango admitido.",
  "code": "ROOM_NUMBER_OUT_OF_RANGE"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [RoomNumber] Room number 1000 is outside the supported range 1-999. error.code=ROOM_NUMBER_OUT_OF_RANGE
```

<a id="src-stay"></a>

## Errores de Stay

Errores generados al validar conjuntamente las fechas de entrada y salida de una estancia.

<a id="err-stay-checkout-not-after-checkin"></a>

### Salida no posterior a la entrada

- **Código:** `STAY_CHECKOUT_NOT_AFTER_CHECKIN`
- **Fuente:** `Stay`

Ambas fechas de la estancia se analizan, pero la fecha de salida es anterior o igual a la fecha de entrada, por lo que la estancia no tiene una duración positiva. Esta regla entre campos la aplica la factory Stay.Create.

> **Regla de negocio:** La salida debe ser estrictamente posterior a la entrada.

#### Diagnósticos

- **El cliente envió una fecha de salida igual o anterior a la fecha de entrada.** — _origen:_ External — Envíe una fecha de salida al menos un día después de la fecha de entrada.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:booking-service:stay-checkout-not-after-checkin",
  "title": "Las fechas de la estancia no son válidas.",
  "detail": "La fecha de salida debe ser posterior a la fecha de entrada.",
  "code": "STAY_CHECKOUT_NOT_AFTER_CHECKIN"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Stay] Check-out 2026-08-10 must be strictly after check-in 2026-08-14. error.code=STAY_CHECKOUT_NOT_AFTER_CHECKIN
```


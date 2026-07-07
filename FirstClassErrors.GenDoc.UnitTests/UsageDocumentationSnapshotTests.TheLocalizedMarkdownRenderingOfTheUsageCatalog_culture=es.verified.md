# Catálogo de errores

## Índice

- [Errores de Amount](#src-amount)
  - [Discrepancia de moneda entre importes](#err-amount-currency-mismatch)
- [Errores de BankTransactionFileValidator](#src-bank-transaction-file-validator)
  - [Fecha de transacción fuera del período del extracto](#err-bank-transaction-file-date-out-of-statement-period)
  - [Discrepancia del importe total del extracto](#err-bank-transaction-file-statement-total-amount-mismatch)
- [Errores de ExchangeRateProvider](#src-exchange-rate-provider)
  - [Servicio de tipos de cambio no disponible](#err-exchange-rate-service-unavailable)
  - [Par de divisas no admitido](#err-unsupported-currency-pair)
- [Errores de StatementUploadEndpoint](#src-statement-upload-endpoint)
  - [Carga útil de extracto mal formada](#err-malformed-statement-payload)
  - [Subida de extracto con límite de frecuencia](#err-statement-upload-rate-limited)
- [Errores de MoneyTransfer](#src-money-transfer)
  - [Importe de transferencia no positivo](#err-money-transfer-amount-not-positive)
  - [Transferencia de dinero no válida](#err-money-transfer-invalid)
- [Errores de Temperature](#src-temperature)
  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)

<a id="src-amount"></a>

## Errores de Amount

Errores generados al realizar operaciones que combinan valores monetarios Amount.

<a id="err-amount-currency-mismatch"></a>

### Discrepancia de moneda entre importes

- **Código:** `AMOUNT_CURRENCY_MISMATCH`
- **Fuente:** `Amount`

Este error se produce al intentar usar juntos varios importes en una operación cuando están expresados en monedas diferentes.

> **Regla de negocio:** Todas las operaciones monetarias deben involucrar importes expresados en la misma moneda.

#### Diagnósticos

- **Se usaron importes en una operación monetaria sin haberlos convertido a la misma moneda.** — _origen:_ Internal — Compruebe si todos los importes involucrados en la operación se convirtieron a una moneda común antes de usarlos juntos.
- **Se proporcionaron con monedas diferentes importes que se esperaba estuvieran expresados en la misma moneda.** — _origen:_ InternalOrExternal — Compruebe las monedas asociadas a cada importe y confirme si se esperaba una moneda común para esta operación.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:amount-currency-mismatch",
  "title": "Discrepancia de moneda",
  "detail": "Los dos importes usan monedas diferentes y no pueden combinarse.",
  "code": "AMOUNT_CURRENCY_MISMATCH"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Amount] Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD. error.code=AMOUNT_CURRENCY_MISMATCH
```

<a id="src-bank-transaction-file-validator"></a>

## Errores de BankTransactionFileValidator

Errores generados al validar un archivo de extracto bancario cargado frente a sus metadatos declarados (período y totales del extracto).

<a id="err-bank-transaction-file-date-out-of-statement-period"></a>

### Fecha de transacción fuera del período del extracto

- **Código:** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Fuente:** `BankTransactionFileValidator`

Este error se produce al intentar validar un archivo de extracto bancario que contiene una o más transacciones con fecha fuera del período del extracto.

> **Regla de negocio:** Todas las transacciones deben producirse dentro del período del extracto.

#### Diagnósticos

- **La fecha de transacción proporcionada en el archivo del extracto es incorrecta o incoherente con la fecha real de la transacción.** — _origen:_ External — Verifique la fecha de transacción presente en el archivo de entrada y confirme su coherencia con la cronología real de la transacción.
- **El período del extracto definido en el archivo no coincide con el período de cobertura real de las transacciones.** — _origen:_ External — Compruebe si las fechas de inicio y fin del extracto en el archivo concuerdan con el período cubierto por las transacciones.
- **La transacción se registró después de generar el extracto, pero se incluyó por error en el archivo.** — _origen:_ InternalOrExternal — Determine si se incluyeron transacciones registradas tardíamente en el proceso de generación del extracto.
- **Un error de procesamiento interno desplazó la fecha de transacción durante la transformación o la importación de datos.** — _origen:_ Internal — Examine las etapas de importación y transformación de datos para confirmar que las fechas de transacción se conservan sin alteración.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:bank-transaction-file-date-out-of-statement-period",
  "title": "La fecha de transacción está fuera del período del extracto.",
  "detail": "Una fecha de transacción queda fuera del período del extracto.",
  "code": "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] Transaction dated 2024-02-02 is outside the statement period [2024-01-05;2024-01-31]. error.code=BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | La fecha de la transacción que se está procesando. | `02/02/2024` |

<a id="err-bank-transaction-file-statement-total-amount-mismatch"></a>

### Discrepancia del importe total del extracto

- **Código:** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Fuente:** `BankTransactionFileValidator`

Este error se produce al intentar validar un archivo de extracto bancario cuyo importe total declarado no coincide con la suma de los importes de cada transacción.

> **Regla de negocio:** El importe total del extracto debe ser igual a la suma de todos los importes de transacción incluidos en el extracto.

#### Diagnósticos

- **El importe total declarado en el archivo del extracto no coincide con la suma de los importes de cada transacción.** — _origen:_ External — Verifique el importe total declarado en el archivo y compárelo con la suma de todos los importes de transacción.
- **Faltan una o más transacciones, o están duplicadas, en el archivo del extracto.** — _origen:_ External — Compruebe si todas las transacciones esperadas están presentes exactamente una vez en el archivo del extracto.
- **Se produjo un error de redondeo o de precisión al calcular el importe total del extracto.** — _origen:_ InternalOrExternal — Examine cómo se aplicaron las reglas de redondeo y precisión al calcular el total del extracto.
- **Un error de procesamiento interno alteró los importes de transacción durante el análisis o la transformación del archivo.** — _origen:_ Internal — Inspeccione las etapas de análisis y transformación del archivo para confirmar que los importes de transacción permanecen sin cambios.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:bank-transaction-file-statement-total-amount-mismatch",
  "title": "Discrepancia del importe total del extracto.",
  "detail": "El total declarado del extracto no coincide con el total calculado.",
  "code": "BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] The declared statement total amount (1250 EUR) does not match the computed total amount from transactions (1249.5 EUR). error.code=BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH
```

<a id="src-exchange-rate-provider"></a>

## Errores de ExchangeRateProvider

Errores generados al llamar al proveedor externo de tipos de cambio (un adaptador saliente, de puerto secundario).

<a id="err-exchange-rate-service-unavailable"></a>

### Servicio de tipos de cambio no disponible

- **Código:** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Fuente:** `ExchangeRateProvider`

Este error se produce cuando no se puede contactar con el proveedor externo de tipos de cambio (un tiempo de espera agotado, un reinicio de conexión o una respuesta 5xx). Es transitorio: la llamada puede reintentarse.

> **Regla de negocio:** La conversión de divisas depende de un proveedor de tipos de cambio accesible.

#### Diagnósticos

- **El proveedor agotó el tiempo de espera o devolvió un error de servidor.** — _origen:_ External — Compruebe el estado del proveedor y reintente la llamada, idealmente con una espera progresiva.
- **La ruta de red saliente hacia el proveedor está interrumpida.** — _origen:_ InternalOrExternal — Verifique la conectividad saliente y cualquier proxy o cortafuegos entre el servicio y el proveedor.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:exchange-rate-service-unavailable",
  "title": "Servicio de tipos de cambio no disponible.",
  "detail": "El servicio de tipos de cambio no está disponible temporalmente; inténtelo de nuevo más tarde.",
  "code": "EXCHANGE_RATE_SERVICE_UNAVAILABLE"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider 'acme-fx' is unavailable (correlation 22222222-2222-2222-2222-222222222222). error.code=EXCHANGE_RATE_SERVICE_UNAVAILABLE
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | El proveedor externo al que se llamó. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | El identificador de correlación de la llamada saliente. | `22222222-2222-2222-2222-222222222222` |

<a id="err-unsupported-currency-pair"></a>

### Par de divisas no admitido

- **Código:** `UNSUPPORTED_CURRENCY_PAIR`
- **Fuente:** `ExchangeRateProvider`

Este error se produce cuando el proveedor de tipos de cambio no cotiza un tipo para el par de divisas origen/destino solicitado.

> **Regla de negocio:** Una conversión de divisas solo puede realizarse para un par que el proveedor cotice.

#### Diagnósticos

- **El par de divisas solicitado no lo ofrece el proveedor.** — _origen:_ External — Confirme que el proveedor admite tanto la moneda de origen como la de destino antes de solicitar una conversión.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:unsupported-currency-pair",
  "title": "Par de divisas no admitido.",
  "detail": "El par de divisas solicitado no es compatible.",
  "code": "UNSUPPORTED_CURRENCY_PAIR"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [ExchangeRateProvider] The exchange-rate provider does not quote the EUR to USD currency pair. error.code=UNSUPPORTED_CURRENCY_PAIR
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | La moneda de origen de la conversión. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | La moneda de destino de la conversión. | `USD` |

<a id="src-statement-upload-endpoint"></a>

## Errores de StatementUploadEndpoint

Errores generados por el endpoint HTTP que ingiere los extractos bancarios subidos (un adaptador entrante, de puerto primario).

<a id="err-malformed-statement-payload"></a>

### Carga útil de extracto mal formada

- **Código:** `MALFORMED_STATEMENT_PAYLOAD`
- **Fuente:** `StatementUploadEndpoint`

Este error se produce cuando el endpoint de subida de extractos recibe una solicitud cuyo cuerpo omite un campo obligatorio o contiene un valor no válido.

> **Regla de negocio:** Una solicitud de extracto subida debe incluir todos los campos obligatorios con un valor válido.

#### Diagnósticos

- **El cliente envió un cuerpo de solicitud incompleto o mal formado.** — _origen:_ External — Examine el campo indicado en el contexto y confirme que el cliente lo envía con un valor válido.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:malformed-statement-payload",
  "title": "Carga útil de extracto mal formada.",
  "detail": "La solicitud de extracto subida omite un campo obligatorio o contiene un valor no válido.",
  "code": "MALFORMED_STATEMENT_PAYLOAD"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. error.code=MALFORMED_STATEMENT_PAYLOAD
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | El identificador de la solicitud entrante. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | El campo de la solicitud que no superó la validación. | `statementPeriod` |

<a id="err-statement-upload-rate-limited"></a>

### Subida de extracto con límite de frecuencia

- **Código:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Fuente:** `StatementUploadEndpoint`

Este error se produce cuando llegan demasiadas subidas de extractos en un intervalo corto y el endpoint limita la solicitud. Es transitorio: la misma solicitud puede reintentarse más tarde.

> **Regla de negocio:** Los llamadores deben respetar el límite de frecuencia de subida del endpoint.

#### Diagnósticos

- **El llamador superó la frecuencia de solicitudes permitida.** — _origen:_ External — Espere y reintente tras el retardo indicado en el mensaje.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:statement-upload-rate-limited",
  "title": "Subida de extracto con límite de frecuencia.",
  "detail": "Se enviaron demasiadas subidas de extractos en poco tiempo; inténtelo de nuevo más tarde.",
  "code": "STATEMENT_UPLOAD_RATE_LIMITED"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [StatementUploadEndpoint] The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. error.code=STATEMENT_UPLOAD_RATE_LIMITED
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | El identificador de la solicitud entrante. | `11111111-1111-1111-1111-111111111111` |

<a id="src-money-transfer"></a>

## Errores de MoneyTransfer

Errores generados al validar una transferencia de dinero entre cuentas.

<a id="err-money-transfer-amount-not-positive"></a>

### Importe de transferencia no positivo

- **Código:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Fuente:** `MoneyTransfer`

Este error se produce cuando se solicita una transferencia con un importe cero o negativo.

> **Regla de negocio:** El importe de una transferencia debe ser estrictamente positivo.

#### Diagnósticos

- **El importe se introdujo o se calculó como cero o un valor negativo.** — _origen:_ External — Compruebe el importe de transferencia solicitado y confirme que es mayor que cero.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:money-transfer-amount-not-positive",
  "title": "El importe de la transferencia debe ser positivo.",
  "detail": "El importe de la transferencia debe ser mayor que cero.",
  "code": "MONEY_TRANSFER_AMOUNT_NOT_POSITIVE"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] Cannot transfer -25 EUR: the amount must be strictly positive. error.code=MONEY_TRANSFER_AMOUNT_NOT_POSITIVE
```

#### Contexto

| Clave | Tipo | Descripción | Valores de ejemplo |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | El importe monetario de la transferencia intentada. | `-25 EUR` |

<a id="err-money-transfer-invalid"></a>

### Transferencia de dinero no válida

- **Código:** `MONEY_TRANSFER_INVALID`
- **Fuente:** `MoneyTransfer`

Este error agrupa todas las reglas de dominio incumplidas al validar una transferencia, de modo que el llamador ve todos los problemas a la vez en lugar de uno por uno.

> **Regla de negocio:** Una transferencia debe cumplir todas las reglas de dominio (un importe estrictamente positivo, monedas coincidentes, ...).

#### Diagnósticos

- **La transferencia solicitada incumplió una o varias reglas de dominio.** — _origen:_ External — Examine los errores internos agregados para ver cada infracción de regla individual.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:money-transfer-invalid",
  "title": "Transferencia no válida.",
  "detail": "La transferencia no cumple todas las reglas requeridas.",
  "code": "MONEY_TRANSFER_INVALID"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [MoneyTransfer] The money transfer is invalid: it violates one or more domain rules. error.code=MONEY_TRANSFER_INVALID
```

<a id="src-temperature"></a>

## Errores de Temperature

Errors raised when constructing a Temperature value from an out-of-range input.

<a id="err-temperature-below-absolute-zero"></a>

### Temperature below absolute zero

- **Código:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Fuente:** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Regla de negocio:** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

#### Diagnósticos

- **The value entered manually by a user is invalid.** — _origen:_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _origen:_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _origen:_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _origen:_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _origen:_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

#### Ejemplos

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:temperature-below-absolute-zero",
  "title": "Temperature is invalid.",
  "detail": "The temperature -1 K is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -1 K is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```

**Respuesta pública (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:temperature-below-absolute-zero",
  "title": "Temperature is invalid.",
  "detail": "The temperature -280 °C is below absolute zero.",
  "code": "TEMPERATURE_BELOW_ABSOLUTE_ZERO"
}
```

**Diagnóstico (interno — no destinado a exposición externa)**

```text
2026-07-04T13:42:18.734Z ERROR [Temperature] Failed to instantiate temperature: the value -280 °C is below absolute zero. error.code=TEMPERATURE_BELOW_ABSOLUTE_ZERO
```


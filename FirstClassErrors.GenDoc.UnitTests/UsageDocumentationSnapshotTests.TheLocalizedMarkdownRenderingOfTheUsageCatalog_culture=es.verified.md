# Catálogo de errores

## Índice

- [Errores de Amount](#src-amount)
  - [Discrepancia de moneda entre importes](#err-amount-currency-mismatch)
- [Errores de BankTransactionFileValidator](#src-bank-transaction-file-validator)
  - [Fecha de transacción fuera del período del extracto](#err-bank-transaction-file-date-out-of-statement-period)
  - [Discrepancia del importe total del extracto](#err-bank-transaction-file-statement-total-amount-mismatch)
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

- No se pudo realizar la operación monetaria porque los importes involucrados están expresados en monedas diferentes: 127.33 EUR y 57689 USD. _(Discrepancia de moneda)_

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

- La transacción con fecha 2024-02-02 está fuera del período del extracto [2024-01-05;2024-01-31]. _(La fecha de transacción está fuera del período del extracto.)_

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

- El importe total declarado del extracto (1250 EUR) no coincide con el importe total calculado a partir de las transacciones (1249.5 EUR). _(Discrepancia del importe total del extracto.)_

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

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_


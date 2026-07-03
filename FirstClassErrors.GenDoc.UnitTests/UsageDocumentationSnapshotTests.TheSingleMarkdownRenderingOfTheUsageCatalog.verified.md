# Error Catalog

## Table of contents

- [Amount errors](#src-amount)
  - [Amount currency mismatch](#err-amount-currency-mismatch)
- [BankTransactionFileValidator errors](#src-bank-transaction-file-validator)
  - [Transaction date outside statement period](#err-bank-transaction-file-date-out-of-statement-period)
  - [Statement total amount mismatch](#err-bank-transaction-file-statement-total-amount-mismatch)
- [Temperature errors](#src-temperature)
  - [Temperature below absolute zero](#err-temperature-below-absolute-zero)

<a id="src-amount"></a>

## Amount errors

Errors raised when performing operations that combine monetary Amount values.

<a id="err-amount-currency-mismatch"></a>

### Amount currency mismatch

- **Code:** `AMOUNT_CURRENCY_MISMATCH`
- **Source:** `Amount`

This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.

> **Business rule:** All monetary operations must involve amounts expressed in the same currency.

#### Diagnostics

- **Amounts were used in a monetary operation without having been converted to the same currency.** — _origin:_ Internal — Verify whether all amounts involved in the operation were converted to a common currency before being used together.
- **Amounts expected to be expressed in the same currency were provided with different currencies.** — _origin:_ InternalOrExternal — Check the currencies associated with each amount and confirm whether a common currency was expected for this operation.

#### Examples

- Failed to perform the monetary operation because the involved amounts are expressed in different currencies: 127.33 EUR and 57689 USD. _(Currency mismatch)_

<a id="src-bank-transaction-file-validator"></a>

## BankTransactionFileValidator errors

Errors raised while validating an uploaded bank statement file against its declared metadata (statement period and totals).

<a id="err-bank-transaction-file-date-out-of-statement-period"></a>

### Transaction date outside statement period

- **Code:** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Source:** `BankTransactionFileValidator`

This error occurs when trying to validate a bank statement file that contains one or more transactions dated outside the statement period.

> **Business rule:** All transactions must occur within the statement period.

#### Diagnostics

- **The transaction date provided in the statement file is incorrect or inconsistent with the actual transaction date.** — _origin:_ External — Verify the transaction date present in the input file and confirm its consistency with the actual transaction timeline.
- **The statement period defined in the file does not match the actual coverage period of the transactions.** — _origin:_ External — Check whether the statement start and end dates in the file align with the period covered by the transactions.
- **The transaction was posted after the statement was generated but was mistakenly included in the file.** — _origin:_ InternalOrExternal — Determine whether late-posted transactions were included in the statement generation process.
- **An internal processing error caused the transaction date to be shifted during data transformation or import.** — _origin:_ Internal — Examine the data import and transformation stages to confirm that transaction dates are preserved without alteration.

#### Examples

- Transaction dated 2024-02-02 is outside the statement period [2024-01-05;2024-01-31]. _(Transaction date is outside the statement period.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | The date of the transaction being processed. | `02/02/2024` |

<a id="err-bank-transaction-file-statement-total-amount-mismatch"></a>

### Statement total amount mismatch

- **Code:** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Source:** `BankTransactionFileValidator`

This error occurs when trying to validate a bank statement file whose declared total amount does not match the sum of the individual transaction amounts.

> **Business rule:** The statement total amount must equal the sum of all transaction amounts included in the statement.

#### Diagnostics

- **The total amount declared in the statement file does not match the sum of the individual transaction amounts.** — _origin:_ External — Verify the declared total amount in the file and compare it with the sum of all transaction amounts.
- **One or more transactions are missing or duplicated in the statement file.** — _origin:_ External — Check whether all expected transactions are present exactly once in the statement file.
- **A rounding or precision error occurred when calculating the statement total amount.** — _origin:_ InternalOrExternal — Examine how rounding and precision rules were applied when computing the statement total.
- **An internal processing error altered transaction amounts during file parsing or transformation.** — _origin:_ Internal — Inspect the file parsing and transformation stages to confirm that transaction amounts remain unchanged.

#### Examples

- The declared statement total amount (1250 EUR) does not match the computed total amount from transactions (1249.5 EUR). _(Statement total amount mismatch.)_

<a id="src-temperature"></a>

## Temperature errors

Errors raised when constructing a Temperature value from an out-of-range input.

<a id="err-temperature-below-absolute-zero"></a>

### Temperature below absolute zero

- **Code:** `TEMPERATURE_BELOW_ABSOLUTE_ZERO`
- **Source:** `Temperature`

This error occurs when trying to instantiate a temperature with a value that is below absolute zero.

> **Business rule:** Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.

#### Diagnostics

- **The value entered manually by a user is invalid.** — _origin:_ External — Verify the value entered by the user and assess its compliance with domain rules.
- **The value received from an external system (API, message, etc.) is invalid.** — _origin:_ External — Check the data provided by the upstream system and evaluate its validity against domain rules.
- **The value was loaded from corrupted or outdated persisted data.** — _origin:_ External — Examine the persisted data source to determine whether stored values comply with current domain rules.
- **The value was computed internally without using domain-safe methods.** — _origin:_ Internal — Inspect the internal computation logic to confirm that domain invariants are preserved.
- **The value originates from system configuration or defaults that are incorrect or outdated.** — _origin:_ External — Review the relevant configuration or default parameters to assess their compliance with domain rules.

#### Examples

- Failed to instantiate temperature: the value -1 K is below absolute zero. _(Temperature is below absolute zero.)_
- Failed to instantiate temperature: the value -280 °C is below absolute zero. _(Temperature is below absolute zero.)_


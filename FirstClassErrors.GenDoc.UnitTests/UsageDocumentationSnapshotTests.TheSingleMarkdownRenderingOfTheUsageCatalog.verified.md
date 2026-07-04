# Error Catalog

## Table of contents

- [Amount errors](#src-amount)
  - [Amount currency mismatch](#err-amount-currency-mismatch)
- [BankTransactionFileValidator errors](#src-bank-transaction-file-validator)
  - [Transaction date outside statement period](#err-bank-transaction-file-date-out-of-statement-period)
  - [Statement total amount mismatch](#err-bank-transaction-file-statement-total-amount-mismatch)
- [ExchangeRateProvider errors](#src-exchange-rate-provider)
  - [Exchange-rate service unavailable](#err-exchange-rate-service-unavailable)
  - [Unsupported currency pair](#err-unsupported-currency-pair)
- [StatementUploadEndpoint errors](#src-statement-upload-endpoint)
  - [Malformed statement payload](#err-malformed-statement-payload)
  - [Statement upload rate-limited](#err-statement-upload-rate-limited)
- [MoneyTransfer errors](#src-money-transfer)
  - [Non-positive transfer amount](#err-money-transfer-amount-not-positive)
  - [Invalid money transfer](#err-money-transfer-invalid)
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

<a id="src-exchange-rate-provider"></a>

## ExchangeRateProvider errors

Errors raised while calling the external exchange-rate provider (an outgoing, secondary-port adapter).

<a id="err-exchange-rate-service-unavailable"></a>

### Exchange-rate service unavailable

- **Code:** `EXCHANGE_RATE_SERVICE_UNAVAILABLE`
- **Source:** `ExchangeRateProvider`

This error occurs when the external exchange-rate provider cannot be reached (a timeout, a connection reset, or a 5xx response). It is transient: the call can be retried.

> **Business rule:** Currency conversion depends on a reachable exchange-rate provider.

#### Diagnostics

- **The provider timed out or returned a server error.** — _origin:_ External — Check the provider's health and retry the call, ideally with a backoff.
- **The outgoing network path to the provider is disrupted.** — _origin:_ InternalOrExternal — Verify outbound connectivity and any proxy or firewall between the service and the provider.

#### Examples

- The exchange-rate provider 'acme-fx' is unavailable (correlation 22222222-2222-2222-2222-222222222222). _(Exchange-rate service unavailable.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `PROVIDER` | `System.String` | The external provider that was called. | `acme-fx` |
| `CORRELATION_ID` | `System.Guid` | The correlation identifier of the outgoing call. | `22222222-2222-2222-2222-222222222222` |

<a id="err-unsupported-currency-pair"></a>

### Unsupported currency pair

- **Code:** `UNSUPPORTED_CURRENCY_PAIR`
- **Source:** `ExchangeRateProvider`

This error occurs when the exchange-rate provider does not quote a rate for the requested source/target currency pair.

> **Business rule:** A currency conversion can only be performed for a pair the provider quotes.

#### Diagnostics

- **The requested currency pair is not offered by the provider.** — _origin:_ External — Confirm the provider supports both the source and target currencies before requesting a conversion.

#### Examples

- The exchange-rate provider does not quote the EUR to USD currency pair. _(Unsupported currency pair.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `FROM_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The source currency of the conversion. | `EUR` |
| `TO_CURRENCY` | `FirstClassErrors.Usage.Model.Currency` | The target currency of the conversion. | `USD` |

<a id="src-statement-upload-endpoint"></a>

## StatementUploadEndpoint errors

Errors raised by the HTTP endpoint that ingests uploaded bank statements (an incoming, primary-port adapter).

<a id="err-malformed-statement-payload"></a>

### Malformed statement payload

- **Code:** `MALFORMED_STATEMENT_PAYLOAD`
- **Source:** `StatementUploadEndpoint`

This error occurs when the statement upload endpoint receives a request whose body is missing a required field or carries an invalid value.

> **Business rule:** An uploaded statement request must carry every required field with a valid value.

#### Diagnostics

- **The client sent an incomplete or malformed request body.** — _origin:_ External — Inspect the field named in the context and confirm the client sends it with a valid value.

#### Examples

- The statement upload request 11111111-1111-1111-1111-111111111111 is malformed: the 'statementPeriod' field is missing or invalid. _(Malformed statement payload.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |
| `FIELD` | `System.String` | The request field that failed validation. | `statementPeriod` |

<a id="err-statement-upload-rate-limited"></a>

### Statement upload rate-limited

- **Code:** `STATEMENT_UPLOAD_RATE_LIMITED`
- **Source:** `StatementUploadEndpoint`

This error occurs when too many statement uploads arrive in a short window and the endpoint throttles the request. It is transient: the same request can be retried later.

> **Business rule:** Callers must stay within the endpoint's upload rate limit.

#### Diagnostics

- **The caller exceeded the allowed request rate.** — _origin:_ External — Back off and retry after the delay indicated in the message.

#### Examples

- The statement upload request 11111111-1111-1111-1111-111111111111 was rate-limited; retry after 30 seconds. _(Statement upload rate-limited.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `REQUEST_ID` | `System.Guid` | The identifier of the incoming request. | `11111111-1111-1111-1111-111111111111` |

<a id="src-money-transfer"></a>

## MoneyTransfer errors

Errors raised while validating a money transfer between accounts.

<a id="err-money-transfer-amount-not-positive"></a>

### Non-positive transfer amount

- **Code:** `MONEY_TRANSFER_AMOUNT_NOT_POSITIVE`
- **Source:** `MoneyTransfer`

This error occurs when a money transfer is requested with an amount that is zero or negative.

> **Business rule:** A money transfer amount must be strictly positive.

#### Diagnostics

- **The amount was entered or computed as zero or a negative value.** — _origin:_ External — Check the requested transfer amount and confirm it is greater than zero.

#### Examples

- Cannot transfer -25 EUR: the amount must be strictly positive. _(Transfer amount must be positive.)_

#### Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `TRANSFER_AMOUNT` | `FirstClassErrors.Usage.Model.Amount` | The monetary amount of the attempted transfer. | `-25 EUR` |

<a id="err-money-transfer-invalid"></a>

### Invalid money transfer

- **Code:** `MONEY_TRANSFER_INVALID`
- **Source:** `MoneyTransfer`

This error aggregates every domain rule violated while validating a money transfer, so the caller sees all the problems at once rather than one at a time.

> **Business rule:** A money transfer must satisfy every domain rule (a strictly positive amount, matching currencies, ...).

#### Diagnostics

- **One or more domain rules were violated by the requested transfer.** — _origin:_ External — Inspect the aggregated inner errors to see each individual rule violation.

#### Examples

- The money transfer is invalid: it violates one or more domain rules. _(Invalid money transfer.)_

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


# Transaction date outside statement period

- **Code:** `BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD`
- **Source:** `BankTransactionFileValidator`

This error occurs when trying to validate a bank statement file that contains one or more transactions dated outside the statement period.

> **Business rule:** All transactions must occur within the statement period.

## Diagnostics

- **The transaction date provided in the statement file is incorrect or inconsistent with the actual transaction date.** — _origin:_ External — Verify the transaction date present in the input file and confirm its consistency with the actual transaction timeline.
- **The statement period defined in the file does not match the actual coverage period of the transactions.** — _origin:_ External — Check whether the statement start and end dates in the file align with the period covered by the transactions.
- **The transaction was posted after the statement was generated but was mistakenly included in the file.** — _origin:_ InternalOrExternal — Determine whether late-posted transactions were included in the statement generation process.
- **An internal processing error caused the transaction date to be shifted during data transformation or import.** — _origin:_ Internal — Examine the data import and transformation stages to confirm that transaction dates are preserved without alteration.

## Examples

**Public response (RFC 9457)**

```json
{
  "type": "urn:problem:sample-service:bank-transaction-file-date-out-of-statement-period",
  "title": "Transaction date is outside the statement period.",
  "detail": "A transaction date falls outside the statement period.",
  "code": "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD"
}
```

**Diagnostic (internal — not for external exposure)**

```text
2026-07-04T13:42:18.734Z ERROR [BankTransactionFileValidator] Transaction dated 2024-02-02 is outside the statement period [2024-01-05;2024-01-31]. error.code=BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD
```

## Context

| Key | Type | Description | Example values |
| --- | --- | --- | --- |
| `TRANSACTION_DATE` | `System.DateOnly` | The date of the transaction being processed. | `02/02/2024` |


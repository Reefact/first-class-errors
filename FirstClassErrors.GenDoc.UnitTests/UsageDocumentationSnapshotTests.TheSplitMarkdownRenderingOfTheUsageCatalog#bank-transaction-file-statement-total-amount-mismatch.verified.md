# Statement total amount mismatch

- **Code:** `BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH`
- **Source:** `BankTransactionFileValidator`

This error occurs when trying to validate a bank statement file whose declared total amount does not match the sum of the individual transaction amounts.

> **Business rule:** The statement total amount must equal the sum of all transaction amounts included in the statement.

## Diagnostics

- **The total amount declared in the statement file does not match the sum of the individual transaction amounts.** — _origin:_ External — Verify the declared total amount in the file and compare it with the sum of all transaction amounts.
- **One or more transactions are missing or duplicated in the statement file.** — _origin:_ External — Check whether all expected transactions are present exactly once in the statement file.
- **A rounding or precision error occurred when calculating the statement total amount.** — _origin:_ InternalOrExternal — Examine how rounding and precision rules were applied when computing the statement total.
- **An internal processing error altered transaction amounts during file parsing or transformation.** — _origin:_ Internal — Inspect the file parsing and transformation stages to confirm that transaction amounts remain unchanged.

## Examples

- The declared statement total amount (1250 EUR) does not match the computed total amount from transactions (1249.5 EUR). _(Statement total amount mismatch.)_


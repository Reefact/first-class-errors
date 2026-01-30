#region Usings declarations

using Reefact.DiagnosableExceptions.Usage.Model;

#endregion

namespace Reefact.DiagnosableExceptions.Usage.Infrastructure.Adapters;

[ProvidesErrorsFor(typeof(BankTransactionFileValidator))]
public sealed class NonCompliantBankTransactionFileException : PrimaryAdapterException {

    #region Static members

    [DocumentedBy(nameof(TransactionDateOutOfStatementPeriodDocumentation))]
    internal static NonCompliantBankTransactionFileException DateOutOfStatementPeriod(DateOnly periodStart, DateOnly periodEnd, DateOnly transactionDate) {
        return new NonCompliantBankTransactionFileException(
            Code.DateOutOfStatementPeriod,
            new ErrorDescription {
                ShortMessage    = "Transaction date is outside the statement period.",
                DetailedMessage = DocumentationFormatter.Format("Transaction dated {0} is outside the statement period [{1};{2}].", transactionDate, periodStart, periodEnd)
            });
    }

    [DocumentedBy(nameof(StatementTotalAmountMismatchDocumentation))]
    internal static NonCompliantBankTransactionFileException StatementTotalAmountMismatch(Amount declaredTotalAmount,
                                                                                          Amount computedTotalAmount) {
        return new NonCompliantBankTransactionFileException(
            Code.StatementTotalAmountMismatch,
            new ErrorDescription {
                ShortMessage    = "Statement total amount mismatch.",
                DetailedMessage = DocumentationFormatter.Format("The declared statement total amount ({0}) does not match the computed total amount from transactions ({1}).", declaredTotalAmount, computedTotalAmount)
            });
    }

    private static ErrorDocumentation TransactionDateOutOfStatementPeriodDocumentation() {
        return DescribeError.WithTitle("Transaction date outside statement period")
                            .WithExplanation("This error occurs when trying to validate a bank statement file that contains one or more transactions dated outside the statement period.")
                            .WithBusinessRule("All transactions must occur within the statement period.")
                            .WithDiagnostics(
                                 new ErrorDiagnostic {
                                     Cause = "The transaction date provided in the statement file is incorrect or inconsistent with the actual transaction date.",
                                     Type  = ErrorCauseType.Input,
                                     Fix   = "Correct the transaction date in the input file so that it falls within the statement period."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "The statement period defined in the file does not match the actual coverage period of the transactions.",
                                     Type  = ErrorCauseType.Input,
                                     Fix   = "Review and correct the statement start and end dates so they accurately reflect the transaction coverage period."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "The transaction was posted after the statement was generated but was mistakenly included in the file.",
                                     Type  = ErrorCauseType.SystemOrInput,
                                     Fix   = "Exclude late-posted transactions from the statement file or regenerate the statement after all transactions are finalized."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "An internal processing error caused the transaction date to be shifted during data transformation or import.",
                                     Type  = ErrorCauseType.System,
                                     Fix   = "Review the data import and transformation logic to ensure transaction dates are preserved correctly."
                                 })
                            .WithExamples(() => DateOutOfStatementPeriod(new DateOnly(2024, 01, 05), new DateOnly(2024, 01, 31), new DateOnly(2024, 02, 02)));
    }

    private static ErrorDocumentation StatementTotalAmountMismatchDocumentation() {
        return DescribeError.WithTitle("Statement total amount mismatch")
                            .WithExplanation("This error occurs when trying to validate a bank statement file whose declared total amount does not match the sum of the individual transaction amounts.")
                            .WithBusinessRule("The statement total amount must equal the sum of all transaction amounts included in the statement.")
                            .WithDiagnostics(
                                 new ErrorDiagnostic {
                                     Cause = "The total amount declared in the statement file does not match the sum of the individual transaction amounts.",
                                     Type  = ErrorCauseType.Input,
                                     Fix   = "Correct the declared total amount in the statement file so that it matches the sum of all transaction amounts."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "One or more transactions are missing or duplicated in the statement file.",
                                     Type  = ErrorCauseType.Input,
                                     Fix   = "Verify that all transactions are present exactly once and that no transaction is missing or duplicated."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "A rounding or precision error occurred when calculating the statement total amount.",
                                     Type  = ErrorCauseType.SystemOrInput,
                                     Fix   = "Ensure that the same rounding and precision rules are applied consistently when computing the statement total."
                                 },
                                 new ErrorDiagnostic {
                                     Cause = "An internal processing error altered transaction amounts during file parsing or transformation.",
                                     Type  = ErrorCauseType.System,
                                     Fix   = "Review the file parsing and transformation logic to ensure transaction amounts are preserved accurately."
                                 }
                             )
                            .WithExamples(() => StatementTotalAmountMismatch(new Amount(1250.00m, Currency.EUR), new Amount(1249.50m, Currency.EUR)));
    }

    #endregion

    #region Constructors & Destructor

    /// <inheritdoc />
    private NonCompliantBankTransactionFileException(string errorCode, ErrorDescription errorDescription) : base(errorCode, errorDescription) { }

    #endregion

    #region Nested types

    private static class Code {

        public const string DateOutOfStatementPeriod     = "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD";
        public const string StatementTotalAmountMismatch = "BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH";

    }

    #endregion

}
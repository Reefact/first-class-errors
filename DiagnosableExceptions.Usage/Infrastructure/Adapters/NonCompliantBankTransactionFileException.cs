#region Usings declarations

using DiagnosableExceptions.Usage.Model;
using DiagnosableExceptions.Usage.Utils;

#endregion

namespace DiagnosableExceptions.Usage.Infrastructure.Adapters;

[ProvidesErrorsFor(typeof(BankTransactionFileValidator))]
public sealed class NonCompliantBankTransactionFileException : PrimaryAdapterException {

    #region Statics members declarations

    [DocumentedBy(nameof(TransactionDateOutOfStatementPeriodDocumentation))]
    internal static NonCompliantBankTransactionFileException DateOutOfStatementPeriod(DateOnly periodStart, DateOnly periodEnd, DateOnly transactionDate) {
        return new NonCompliantBankTransactionFileException(
            Code.DateOutOfStatementPeriod,
            DocumentationFormatter.Format("Transaction dated {0} is outside the statement period [{1};{2}].", transactionDate, periodStart, periodEnd),
            "Transaction date is outside the statement period.");
    }

    [DocumentedBy(nameof(StatementTotalAmountMismatchDocumentation))]
    internal static NonCompliantBankTransactionFileException StatementTotalAmountMismatch(Amount declaredTotalAmount, Amount computedTotalAmount) {
        return new NonCompliantBankTransactionFileException(
            Code.StatementTotalAmountMismatch,
            DocumentationFormatter.Format("The declared statement total amount ({0}) does not match the computed total amount from transactions ({1}).", declaredTotalAmount, computedTotalAmount),
            "Statement total amount mismatch.");
    }

    private static ErrorDocumentation TransactionDateOutOfStatementPeriodDocumentation() {
        return DescribeError.WithTitle("Transaction date outside statement period")
                            .WithDescription("This error occurs when trying to validate a bank statement file that contains one or more transactions dated outside the statement period.")
                            .WithRule("All transactions must occur within the statement period.")
                            .WithDiagnostic("The transaction date provided in the statement file is incorrect or inconsistent with the actual transaction date.",
                                            ErrorCauseType.Input,
                                            "Verify the transaction date present in the input file and confirm its consistency with the actual transaction timeline.")
                            .AndDiagnostic("The statement period defined in the file does not match the actual coverage period of the transactions.",
                                           ErrorCauseType.Input,
                                           "Check whether the statement start and end dates in the file align with the period covered by the transactions.")
                            .AndDiagnostic("The transaction was posted after the statement was generated but was mistakenly included in the file.",
                                           ErrorCauseType.SystemOrInput,
                                           "Determine whether late-posted transactions were included in the statement generation process.")
                            .AndDiagnostic("An internal processing error caused the transaction date to be shifted during data transformation or import.",
                                           ErrorCauseType.System,
                                           "Examine the data import and transformation stages to confirm that transaction dates are preserved without alteration.")
                            .WithExamples(() => DateOutOfStatementPeriod(new DateOnly(2024, 01, 05), new DateOnly(2024, 01, 31), new DateOnly(2024, 02, 02)));
    }

    private static ErrorDocumentation StatementTotalAmountMismatchDocumentation() {
        return DescribeError.WithTitle("Statement total amount mismatch")
                            .WithDescription("This error occurs when trying to validate a bank statement file whose declared total amount does not match the sum of the individual transaction amounts.")
                            .WithRule("The statement total amount must equal the sum of all transaction amounts included in the statement.")
                            .WithDiagnostic("The total amount declared in the statement file does not match the sum of the individual transaction amounts.",
                                            ErrorCauseType.Input,
                                            "Verify the declared total amount in the file and compare it with the sum of all transaction amounts."
                             )
                            .AndDiagnostic("One or more transactions are missing or duplicated in the statement file.",
                                           ErrorCauseType.Input,
                                           "Check whether all expected transactions are present exactly once in the statement file."
                             )
                            .AndDiagnostic("A rounding or precision error occurred when calculating the statement total amount.",
                                           ErrorCauseType.SystemOrInput,
                                           "Examine how rounding and precision rules were applied when computing the statement total."
                             )
                            .AndDiagnostic("An internal processing error altered transaction amounts during file parsing or transformation.",
                                           ErrorCauseType.System,
                                           "Inspect the file parsing and transformation stages to confirm that transaction amounts remain unchanged."
                             )
                            .WithExamples(() => StatementTotalAmountMismatch(new Amount(1250.00m, Currency.EUR), new Amount(1249.50m, Currency.EUR)));
    }

    #endregion

    #region Constructors declarations

    /// <inheritdoc />
    public NonCompliantBankTransactionFileException(string errorCode, string errorMessage, string? shortMessage = null) : base(errorCode, errorMessage, shortMessage) { }

    #endregion

    #region Nested types declarations

    private static class Code {

        public const string DateOutOfStatementPeriod     = "BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD";
        public const string StatementTotalAmountMismatch = "BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH";

    }

    #endregion

}
#region Usings declarations

using FirstClassErrors.Usage.Model;
using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

[ProvidesErrorsFor(nameof(BankTransactionFileValidator),
                   Description = "Bank_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
public static class NonCompliantBankTransactionFileError {

    #region Statics members declarations

    [DocumentedBy(nameof(TransactionDateOutOfStatementPeriodDocumentation))]
    internal static PrimaryPortError DateOutOfStatementPeriod(DateOnly periodStart, DateOnly periodEnd, DateOnly transactionDate) {
        return PrimaryPortError.Create(
                                   Code.DateOutOfStatementPeriod,
                                   DocumentationFormatter.Format("Transaction dated {0} is outside the statement period [{1};{2}].", transactionDate, periodStart, periodEnd),
                                   Transience.NonTransient,
                                   ctx => ctx.Add(ErrCtxKey.TransactionDate, transactionDate))
                               .WithPublicMessage(
                                   UsageErrorMessages.Get("Bank_DateOutOfPeriod_ShortMessage"),
                                   UsageErrorMessages.Get("Bank_DateOutOfPeriod_DetailedMessage"));
    }

    [DocumentedBy(nameof(StatementTotalAmountMismatchDocumentation))]
    internal static PrimaryPortError StatementTotalAmountMismatch(Amount declaredTotalAmount, Amount computedTotalAmount) {
        return PrimaryPortError.Create(
                                   Code.StatementTotalAmountMismatch,
                                   DocumentationFormatter.Format("The declared statement total amount ({0}) does not match the computed total amount from transactions ({1}).", declaredTotalAmount, computedTotalAmount),
                                   Transience.NonTransient)
                               .WithPublicMessage(
                                   UsageErrorMessages.Get("Bank_TotalMismatch_ShortMessage"),
                                   UsageErrorMessages.Get("Bank_TotalMismatch_DetailedMessage"));
    }

    private static ErrorDocumentation TransactionDateOutOfStatementPeriodDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Title"))
                            .WithDescription(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Description"))
                            .WithRule(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("Bank_DateOutOfPeriod_Hint1"))
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Cause2"),
                                           ErrorOrigin.External,
                                           UsageErrorMessages.Get("Bank_DateOutOfPeriod_Hint2"))
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Cause3"),
                                           ErrorOrigin.InternalOrExternal,
                                           UsageErrorMessages.Get("Bank_DateOutOfPeriod_Hint3"))
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_DateOutOfPeriod_Cause4"),
                                           ErrorOrigin.Internal,
                                           UsageErrorMessages.Get("Bank_DateOutOfPeriod_Hint4"))
                            .WithExamples(() => DateOutOfStatementPeriod(new DateOnly(2024, 01, 05), new DateOnly(2024, 01, 31), new DateOnly(2024, 02, 02)));
    }

    private static ErrorDocumentation StatementTotalAmountMismatchDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("Bank_TotalMismatch_Title"))
                            .WithDescription(UsageErrorMessages.Get("Bank_TotalMismatch_Description"))
                            .WithRule(UsageErrorMessages.Get("Bank_TotalMismatch_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("Bank_TotalMismatch_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("Bank_TotalMismatch_Hint1")
                             )
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_TotalMismatch_Cause2"),
                                           ErrorOrigin.External,
                                           UsageErrorMessages.Get("Bank_TotalMismatch_Hint2")
                             )
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_TotalMismatch_Cause3"),
                                           ErrorOrigin.InternalOrExternal,
                                           UsageErrorMessages.Get("Bank_TotalMismatch_Hint3")
                             )
                            .AndDiagnostic(UsageErrorMessages.Get("Bank_TotalMismatch_Cause4"),
                                           ErrorOrigin.Internal,
                                           UsageErrorMessages.Get("Bank_TotalMismatch_Hint4")
                             )
                            .WithExamples(() => StatementTotalAmountMismatch(new Amount(1250.00m, Currency.EUR), new Amount(1249.50m, Currency.EUR)));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        // ReSharper disable MemberHidesStaticFromOuterClass
        public static readonly ErrorCode DateOutOfStatementPeriod     = ErrorCode.Create("BANK_TRANSACTION_FILE_DATE_OUT_OF_STATEMENT_PERIOD");
        public static readonly ErrorCode StatementTotalAmountMismatch = ErrorCode.Create("BANK_TRANSACTION_FILE_STATEMENT_TOTAL_AMOUNT_MISMATCH");
        // ReSharper restore MemberHidesStaticFromOuterClass

    }

    #endregion

}
#region Usings declarations

using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Provides factory methods for the domain errors raised while validating a money transfer, including an aggregated
///     error that collects several rule violations at once.
/// </summary>
[ProvidesErrorsFor(nameof(MoneyTransfer),
                   Description = "MoneyTransfer_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
public static class InvalidMoneyTransferError {

    #region Statics members declarations

    [DocumentedBy(nameof(AmountNotPositiveDocumentation))]
    internal static DomainError AmountNotPositive(Amount amount) {
        return DomainError.Create(
                              Code.AmountNotPositive,
                              DocumentationFormatter.Format("Cannot transfer {0}: the amount must be strictly positive.", amount),
                              ctx => ctx.Add(ErrCtxKey.TransferAmount, amount))
                          .WithPublicMessage(
                              UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_ShortMessage"),
                              UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_DetailedMessage"));
    }

    [DocumentedBy(nameof(InvalidDocumentation))]
    internal static DomainError Invalid(Amount amount, Amount other) {
        // Aggregate several domain rule violations into a single error so the caller sees them all at once.
        DomainError[] violations = {
            AmountNotPositive(amount),
            InvalidAmountOperationError.CurrencyMismatch(amount, other)
        };

        return DomainError.Create(
                              Code.Invalid,
                              "The money transfer is invalid: it violates one or more domain rules.",
                              violations)
                          .WithPublicMessage(
                              UsageErrorMessages.Get("MoneyTransfer_Invalid_ShortMessage"),
                              UsageErrorMessages.Get("MoneyTransfer_Invalid_DetailedMessage"));
    }

    private static ErrorDocumentation AmountNotPositiveDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_Title"))
                            .WithDescription(UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_Description"))
                            .WithRule(UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("MoneyTransfer_AmountNotPositive_Hint1"))
                            .WithExamples(() => AmountNotPositive(new Amount(-25.00m, Currency.EUR)));
    }

    private static ErrorDocumentation InvalidDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("MoneyTransfer_Invalid_Title"))
                            .WithDescription(UsageErrorMessages.Get("MoneyTransfer_Invalid_Description"))
                            .WithRule(UsageErrorMessages.Get("MoneyTransfer_Invalid_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("MoneyTransfer_Invalid_Cause1"),
                                            ErrorOrigin.External,
                                            UsageErrorMessages.Get("MoneyTransfer_Invalid_Hint1"))
                            .WithExamples(() => Invalid(new Amount(-10.00m, Currency.EUR), new Amount(5.00m, Currency.USD)));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode AmountNotPositive = ErrorCode.Create("MONEY_TRANSFER_AMOUNT_NOT_POSITIVE");
        public static readonly ErrorCode Invalid           = ErrorCode.Create("MONEY_TRANSFER_INVALID");

        #endregion

    }

    #endregion

}

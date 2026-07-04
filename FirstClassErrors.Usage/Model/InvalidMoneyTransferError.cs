#region Usings declarations

using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Provides factory methods for the domain errors raised while validating a money transfer, including an aggregated
///     error that collects several rule violations at once.
/// </summary>
[ProvidesErrorsFor(nameof(MoneyTransfer),
                   Description = "Errors raised while validating a money transfer between accounts.")]
public static class InvalidMoneyTransferError {

    #region Statics members declarations

    [DocumentedBy(nameof(AmountNotPositiveDocumentation))]
    internal static DomainError AmountNotPositive(Amount amount) {
        return new DomainError(
            Code.AmountNotPositive,
            DocumentationFormatter.Format("Cannot transfer {0}: the amount must be strictly positive.", amount),
            "Transfer amount must be positive.",
            ctx => ctx.Add(ErrCtxKey.TransferAmount, amount));
    }

    [DocumentedBy(nameof(InvalidDocumentation))]
    internal static DomainError Invalid(Amount amount, Amount other) {
        // Aggregate several domain rule violations into a single error so the caller sees them all at once.
        DomainError[] violations = {
            AmountNotPositive(amount),
            InvalidAmountOperationError.CurrencyMismatch(amount, other)
        };

        return new DomainError(
            Code.Invalid,
            "The money transfer is invalid: it violates one or more domain rules.",
            violations,
            "Invalid money transfer.");
    }

    private static ErrorDocumentation AmountNotPositiveDocumentation() {
        return DescribeError.WithTitle("Non-positive transfer amount")
                            .WithDescription("This error occurs when a money transfer is requested with an amount that is zero or negative.")
                            .WithRule("A money transfer amount must be strictly positive.")
                            .WithDiagnostic("The amount was entered or computed as zero or a negative value.",
                                            ErrorOrigin.External,
                                            "Check the requested transfer amount and confirm it is greater than zero.")
                            .WithExamples(() => AmountNotPositive(new Amount(-25.00m, Currency.EUR)));
    }

    private static ErrorDocumentation InvalidDocumentation() {
        return DescribeError.WithTitle("Invalid money transfer")
                            .WithDescription("This error aggregates every domain rule violated while validating a money transfer, so the caller sees all the problems at once rather than one at a time.")
                            .WithRule("A money transfer must satisfy every domain rule (a strictly positive amount, matching currencies, ...).")
                            .WithDiagnostic("One or more domain rules were violated by the requested transfer.",
                                            ErrorOrigin.External,
                                            "Inspect the aggregated inner errors to see each individual rule violation.")
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

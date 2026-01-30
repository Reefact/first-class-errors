namespace Reefact.DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents an exception that is thrown when an invalid operation is performed on monetary amounts.
/// </summary>
[ProvidesErrorsFor(typeof(Amount))]
public sealed class InvalidAmountOperationException : DomainException {

    #region Static members

    /// <summary>
    ///     Creates an <see cref="InvalidAmountOperationException" /> to indicate that a monetary operation failed due to a
    ///     currency mismatch between the involved amounts.
    /// </summary>
    /// <param name="amount1">The first amount involved in the operation.</param>
    /// <param name="amount2">The second amount involved in the operation.</param>
    /// <returns>An instance of <see cref="InvalidAmountOperationException" /> describing the currency mismatch error.</returns>
    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static InvalidAmountOperationException CurrencyMismatch(Amount amount1, Amount amount2) {
        return new InvalidAmountOperationException(
            Code.CurrencyMismatch,
            new ErrorDescription {
                ShortMessage    = "Currency mismatch",
                DetailedMessage = DocumentationFormatter.Format("Failed to perform the monetary operation because the involved amounts are expressed in different currencies: {0} and {1}.", amount1, amount2)
            });
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError.WithTitle("Amount currency mismatch")
                            .WithExplanation("This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.")
                            .WithBusinessRule("All monetary operations must involve amounts expressed in the same currency.")
                            .WithDiagnostics(new ErrorDiagnostic {
                                                 Cause = "Amounts were used in a monetary operation without having been converted to the same currency.",
                                                 Type  = ErrorCauseType.System,
                                                 Fix   = "Convert all amounts to a common currency before using them together in a monetary operation."
                                             },
                                             new ErrorDiagnostic {
                                                 Cause = "Amounts expected to be expressed in the same currency were provided with different currencies.",
                                                 Type  = ErrorCauseType.SystemOrInput,
                                                 Fix   = "Ensure that all amounts involved in the operation are expressed in the same currency when no conversion is expected."
                                             })
                            .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
    }

    #endregion

    #region Constructors & Destructor

    /// <inheritdoc />
    private InvalidAmountOperationException(string errorCode, ErrorDescription errorDescription) : base(errorCode, errorDescription) { }

    #endregion

    #region Nested types

    private static class Code {

        public const string CurrencyMismatch = "AMOUNT_CURRENCY_MISMATCH";

    }

    #endregion

}
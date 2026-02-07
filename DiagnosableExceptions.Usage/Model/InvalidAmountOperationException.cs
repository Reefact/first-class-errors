#region Usings declarations

using DiagnosableExceptions.Usage.Utils;

#endregion

namespace DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents an exception that is thrown when an invalid operation is performed on monetary amounts.
/// </summary>
[ProvidesErrorsFor(typeof(Amount))]
public sealed class InvalidAmountOperationException : DomainException {

    #region Statics members declarations

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
            DocumentationFormatter.Format("Failed to perform the monetary operation because the involved amounts are expressed in different currencies: {0} and {1}.", amount1, amount2),
            "Currency mismatch"
        );
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError.WithTitle("Amount currency mismatch")
                            .WithDescription("This error occurs when trying to use multiple amounts together in an operation while they are expressed in different currencies.")
                            .WithRule("All monetary operations must involve amounts expressed in the same currency.")
                            .WithDiagnostic("Amounts were used in a monetary operation without having been converted to the same currency.",
                                            ErrorCauseType.System,
                                            "Verify whether all amounts involved in the operation were converted to a common currency before being used together."
                             )
                            .AndDiagnostic("Amounts expected to be expressed in the same currency were provided with different currencies.",
                                           ErrorCauseType.SystemOrInput,
                                           "Check the currencies associated with each amount and confirm whether a common currency was expected for this operation."
                             )
                            .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
    }

    #endregion

    #region Constructors declarations

    /// <inheritdoc />
    public InvalidAmountOperationException(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    /// <inheritdoc />
    public InvalidAmountOperationException(string errorCode, string errorMessage, string? shortMessage = null) : base(errorCode, errorMessage, shortMessage) { }

    #endregion

    #region Nested types declarations

    private static class Code {

        public const string CurrencyMismatch = "AMOUNT_CURRENCY_MISMATCH";

    }

    #endregion

}
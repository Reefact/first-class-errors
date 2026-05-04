#region Usings declarations

using DiagnosableExceptions.Usage.Utils;

#endregion

namespace DiagnosableExceptions.Usage.Model;

/// <summary>
///     Provides factory methods for creating errors related to invalid operations on monetary amounts.
/// </summary>
[ProvidesErrorsFor(nameof(Amount))]
public static class InvalidAmountOperationError {

    #region Statics members declarations

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    public static DomainError CurrencyMismatch(Amount amount1, Amount amount2) {
        return new DomainError(
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
                                            ErrorOrigin.Internal,
                                            "Verify whether all amounts involved in the operation were converted to a common currency before being used together."
                             )
                            .AndDiagnostic("Amounts expected to be expressed in the same currency were provided with different currencies.",
                                           ErrorOrigin.InternalOrExternal,
                                           "Check the currencies associated with each amount and confirm whether a common currency was expected for this operation."
                             )
                            .WithExamples(() => CurrencyMismatch(new Amount(127.33m, Currency.EUR), new Amount(57689.00m, Currency.USD)));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        // ReSharper disable MemberHidesStaticFromOuterClass
        public static readonly ErrorCode CurrencyMismatch = ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
        // ReSharper restore MemberHidesStaticFromOuterClass

        #endregion

    }

    #endregion

}
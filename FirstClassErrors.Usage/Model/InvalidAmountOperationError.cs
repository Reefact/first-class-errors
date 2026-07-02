#region Usings declarations

using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Provides factory methods for creating errors related to invalid operations on monetary amounts.
/// </summary>
[ProvidesErrorsFor(nameof(Amount),
                   Description = "Amount_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
public static class InvalidAmountOperationError {

    #region Statics members declarations

    [DocumentedBy(nameof(CurrencyMismatchDocumentation))]
    internal static DomainError CurrencyMismatch(Amount amount1, Amount amount2) {
        return new DomainError(
            Code.CurrencyMismatch,
            DocumentationFormatter.Format(UsageErrorMessages.Get("Amount_CurrencyMismatch_Message"), amount1, amount2),
            UsageErrorMessages.Get("Amount_CurrencyMismatch_ShortMessage")
        );
    }

    private static ErrorDocumentation CurrencyMismatchDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("Amount_CurrencyMismatch_Title"))
                            .WithDescription(UsageErrorMessages.Get("Amount_CurrencyMismatch_Description"))
                            .WithRule(UsageErrorMessages.Get("Amount_CurrencyMismatch_Rule"))
                            .WithDiagnostic(UsageErrorMessages.Get("Amount_CurrencyMismatch_Cause1"),
                                            ErrorOrigin.Internal,
                                            UsageErrorMessages.Get("Amount_CurrencyMismatch_Hint1")
                             )
                            .AndDiagnostic(UsageErrorMessages.Get("Amount_CurrencyMismatch_Cause2"),
                                           ErrorOrigin.InternalOrExternal,
                                           UsageErrorMessages.Get("Amount_CurrencyMismatch_Hint2")
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
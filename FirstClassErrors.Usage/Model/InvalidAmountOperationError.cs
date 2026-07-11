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
        return DomainError.Create(
                              Code.CurrencyMismatch,
                              DocumentationFormatter.Format("Failed to perform the monetary operation because the involved amounts are expressed in different currencies: {0} and {1}.", amount1, amount2))
                          .WithPublicMessage(
                              UsageErrorMessages.Get("Amount_CurrencyMismatch_ShortMessage"),
                              UsageErrorMessages.Get("Amount_CurrencyMismatch_DetailedMessage"));
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" members",
        Justification = "Each error-code constant deliberately mirrors the name of its factory method; both are always qualified (Code.X versus the X(...) call), so there is no real ambiguity.")]
    private static class Code {

        #region Statics members declarations

        // ReSharper disable MemberHidesStaticFromOuterClass
        public static readonly ErrorCode CurrencyMismatch = ErrorCode.Create("AMOUNT_CURRENCY_MISMATCH");
        // ReSharper restore MemberHidesStaticFromOuterClass

        #endregion

    }

    #endregion

}
#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="Currency" /> fails with when a request carries a code that is not a well-formed
///     three-letter currency code.
/// </summary>
[ProvidesErrorsFor(nameof(Currency),
                   Description = "Currency_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidCurrencyError {

    #region Statics members declarations

    /// <summary>The value carried by the request is not a well-formed currency code.</summary>
    [DocumentedBy(nameof(MalformedDocumentation))]
    internal static DomainError Malformed(string rawValue) {
        return DomainError.Create(
                              Code.Malformed,
                              DocumentationFormatter.Format("'{0}' is not a valid three-letter currency code.", rawValue))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("Currency_Malformed_ShortMessage"),
                              RequestBinderUsageMessages.Get("Currency_Malformed_DetailedMessage"));
    }

    private static ErrorDocumentation MalformedDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("Currency_Malformed_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("Currency_Malformed_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("Currency_Malformed_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("Currency_Malformed_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("Currency_Malformed_Hint1"))
                            .WithExamples(() => Malformed("EURO"));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode Malformed = ErrorCode.Create("CURRENCY_CODE_MALFORMED");

        #endregion

    }

    #endregion

}

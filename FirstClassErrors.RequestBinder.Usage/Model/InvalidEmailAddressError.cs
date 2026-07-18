#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error an <see cref="EmailAddress" /> fails with when a request carries a malformed address. It is the
///     <b>leaf</b> error a binder wraps in <c>REQUEST_ARGUMENT_INVALID</c> when the conversion of a bound property fails.
/// </summary>
[ProvidesErrorsFor(nameof(EmailAddress),
                   Description = "EmailAddress_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidEmailAddressError {

    #region Statics members declarations

    /// <summary>The value carried by the request is not a well-formed e-mail address.</summary>
    [DocumentedBy(nameof(MalformedDocumentation))]
    internal static DomainError Malformed(string rawValue) {
        return DomainError.Create(
                              Code.Malformed,
                              DocumentationFormatter.Format("'{0}' is not a valid e-mail address.", rawValue))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("EmailAddress_Malformed_ShortMessage"),
                              RequestBinderUsageMessages.Get("EmailAddress_Malformed_DetailedMessage"));
    }

    private static ErrorDocumentation MalformedDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("EmailAddress_Malformed_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("EmailAddress_Malformed_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("EmailAddress_Malformed_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("EmailAddress_Malformed_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("EmailAddress_Malformed_Hint1"))
                            .WithExamples(() => Malformed("not-an-email"));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode Malformed = ErrorCode.Create("EMAIL_ADDRESS_MALFORMED");

        #endregion

    }

    #endregion

}

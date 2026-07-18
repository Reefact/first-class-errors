#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="BookingDate" /> fails with when a request carries a value that is not an ISO
///     <c>yyyy-MM-dd</c> date.
/// </summary>
[ProvidesErrorsFor(nameof(BookingDate),
                   Description = "BookingDate_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidBookingDateError {

    #region Statics members declarations

    /// <summary>The value carried by the request is not a well-formed ISO date.</summary>
    [DocumentedBy(nameof(MalformedDocumentation))]
    internal static DomainError Malformed(string rawValue) {
        return DomainError.Create(
                              Code.Malformed,
                              DocumentationFormatter.Format("'{0}' is not a valid ISO (yyyy-MM-dd) date.", rawValue))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("BookingDate_Malformed_ShortMessage"),
                              RequestBinderUsageMessages.Get("BookingDate_Malformed_DetailedMessage"));
    }

    private static ErrorDocumentation MalformedDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("BookingDate_Malformed_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("BookingDate_Malformed_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("BookingDate_Malformed_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("BookingDate_Malformed_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("BookingDate_Malformed_Hint1"))
                            .WithExamples(() => Malformed("2026-13-40"));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode Malformed = ErrorCode.Create("BOOKING_DATE_MALFORMED");

        #endregion

    }

    #endregion

}

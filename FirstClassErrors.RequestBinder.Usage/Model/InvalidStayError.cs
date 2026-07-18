#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="Stay" /> fails with when its dates individually parse but violate the cross-field
///     rule (check-out not strictly after check-in). Returned by <see cref="Stay.Create" />, it surfaces from the
///     binder's <c>Create</c> terminal <b>as-is</b> — the factory owns the rule, so it is not grouped under the field
///     envelope.
/// </summary>
[ProvidesErrorsFor(nameof(Stay),
                   Description = "Stay_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidStayError {

    #region Statics members declarations

    /// <summary>The requested check-out date is on or before the check-in date.</summary>
    [DocumentedBy(nameof(CheckOutNotAfterCheckInDocumentation))]
    internal static DomainError CheckOutNotAfterCheckIn(BookingDate checkIn, BookingDate checkOut) {
        return DomainError.Create(
                              Code.CheckOutNotAfterCheckIn,
                              DocumentationFormatter.Format("Check-out {0} must be strictly after check-in {1}.", checkOut.Value, checkIn.Value))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_ShortMessage"),
                              RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_DetailedMessage"));
    }

    private static ErrorDocumentation CheckOutNotAfterCheckInDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("Stay_CheckOutNotAfterCheckIn_Hint1"))
                            .WithExamples(() => CheckOutNotAfterCheckIn(SampleDate("2026-08-14"), SampleDate("2026-08-10")));
    }

    private static BookingDate SampleDate(string iso) {
        return BookingDate.Parse(iso).GetResultOrThrow();
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode CheckOutNotAfterCheckIn = ErrorCode.Create("STAY_CHECKOUT_NOT_AFTER_CHECKIN");

        #endregion

    }

    #endregion

}

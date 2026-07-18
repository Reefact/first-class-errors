#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Binding;
using FirstClassErrors.RequestBinder.Usage.Model;
using FirstClassErrors.RequestBinder.Usage.Resources;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Errors;

/// <summary>
///     The primary-port (incoming) envelope errors of the booking endpoint. Each factory is the <c>FailWith(...)</c>
///     envelope a binder groups its collected field failures into — the command-level envelope for the whole request,
///     and one per nested scope (the stay, each guest). They are the coded, documented roots of a binding-failure tree.
/// </summary>
[ProvidesErrorsFor(nameof(BookingEndpoint),
                   Description = "BookingEndpoint_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class PlaceBookingError {

    #region Statics members declarations

    /// <summary>The top-level envelope grouping every field failure of a booking request.</summary>
    [DocumentedBy(nameof(CommandInvalidDocumentation))]
    internal static PrimaryPortError CommandInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   Code.CommandInvalid,
                                   "The booking command is invalid: one or more request arguments failed to bind.",
                                   violations)
                               .WithPublicMessage(
                                   RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_ShortMessage"),
                                   RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_DetailedMessage"));
    }

    /// <summary>The nested envelope grouping the failures of the stay sub-object.</summary>
    [DocumentedBy(nameof(StayInvalidDocumentation))]
    internal static PrimaryPortError StayInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   Code.StayInvalid,
                                   "The stay is invalid: one or more of its dates failed to bind.",
                                   violations)
                               .WithPublicMessage(
                                   RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_ShortMessage"),
                                   RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_DetailedMessage"));
    }

    /// <summary>The per-element envelope grouping the failures of a single guest of the guests list.</summary>
    [DocumentedBy(nameof(GuestInvalidDocumentation))]
    internal static PrimaryPortError GuestInvalid(PrimaryPortInnerErrors violations) {
        return PrimaryPortError.Create(
                                   Code.GuestInvalid,
                                   "The guest is invalid: one or more of its fields failed to bind.",
                                   violations)
                               .WithPublicMessage(
                                   RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_ShortMessage"),
                                   RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_DetailedMessage"));
    }

    private static ErrorDocumentation CommandInvalidDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("PlaceBooking_CommandInvalid_Hint1"))
                            .WithExamples(() => CommandInvalid(Violation(InvalidEmailAddressError.Malformed("not-an-email"))));
    }

    private static ErrorDocumentation StayInvalidDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("PlaceBooking_StayInvalid_Hint1"))
                            .WithExamples(() => StayInvalid(Violation(InvalidBookingDateError.Malformed("2026-13-40"))));
    }

    private static ErrorDocumentation GuestInvalidDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("PlaceBooking_GuestInvalid_Hint1"))
                            .WithExamples(() => GuestInvalid(Violation(InvalidEmailAddressError.Malformed("still-not-an-email"))));
    }

    /// <summary>Wraps a single representative leaf failure into an inner-errors collection for the documentation examples.</summary>
    private static PrimaryPortInnerErrors Violation(DomainError leaf) {
        PrimaryPortInnerErrors violations = new();
        violations.Add(leaf);

        return violations;
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode CommandInvalid = ErrorCode.Create("BOOKING_COMMAND_INVALID");
        public static readonly ErrorCode StayInvalid     = ErrorCode.Create("BOOKING_STAY_INVALID");
        public static readonly ErrorCode GuestInvalid    = ErrorCode.Create("BOOKING_GUEST_INVALID");

        #endregion

    }

    #endregion

}

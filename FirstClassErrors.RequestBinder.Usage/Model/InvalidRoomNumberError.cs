#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Resources;
using FirstClassErrors.RequestBinder.Usage.Utils;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The domain error a <see cref="RoomNumber" /> fails with when a request list carries a room number outside the
///     supported 1–999 range.
/// </summary>
[ProvidesErrorsFor(nameof(RoomNumber),
                   Description = "RoomNumber_Source",
                   DescriptionResourceType = typeof(RequestBinderUsageMessages))]
public static class InvalidRoomNumberError {

    #region Statics members declarations

    /// <summary>An element of the request's room-number list is outside the supported range.</summary>
    [DocumentedBy(nameof(OutOfRangeDocumentation))]
    internal static DomainError OutOfRange(int number) {
        return DomainError.Create(
                              Code.OutOfRange,
                              DocumentationFormatter.Format("Room number {0} is outside the supported range 1-999.", number))
                          .WithPublicMessage(
                              RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_ShortMessage"),
                              RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_DetailedMessage"));
    }

    private static ErrorDocumentation OutOfRangeDocumentation() {
        return DescribeError.WithTitle(RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_Title"))
                            .WithDescription(RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_Description"))
                            .WithRule(RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_Rule"))
                            .WithDiagnostic(RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_Cause1"),
                                            ErrorOrigin.External,
                                            RequestBinderUsageMessages.Get("RoomNumber_OutOfRange_Hint1"))
                            .WithExamples(() => OutOfRange(1000));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode OutOfRange = ErrorCode.Create("ROOM_NUMBER_OUT_OF_RANGE");

        #endregion

    }

    #endregion

}

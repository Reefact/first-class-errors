namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     The typed command a valid booking request binds into: a record of value objects, ready for the application core.
///     Every field is already validated by construction — the boundary is the only place untrusted input is turned into
///     these types.
/// </summary>
/// <param name="GuestEmail">The booking guest's e-mail address (required).</param>
/// <param name="Reference">The client-supplied booking reference, bound raw (required, presence-only).</param>
/// <param name="Currency">The billing currency; defaults when the request omits it.</param>
/// <param name="Nights">The number of nights (required value-type binding).</param>
/// <param name="MaxNights">An optional cap on the number of nights — <c>null</c> when the request omitted it.</param>
/// <param name="Stay">The validated stay (check-in / check-out), built through a cross-field factory.</param>
/// <param name="Tags">The booking tags (an empty list when the request omitted them).</param>
/// <param name="RoomNumbers">The requested room numbers (a list of value-type elements).</param>
/// <param name="Guests">The guests on the booking (a list of nested objects).</param>
public sealed record PlaceBookingCommand(
    EmailAddress             GuestEmail,
    string                   Reference,
    Currency                 Currency,
    NightCount               Nights,
    NightCount?              MaxNights,
    Stay                     Stay,
    IReadOnlyList<Tag>       Tags,
    IReadOnlyList<RoomNumber> RoomNumbers,
    IReadOnlyList<Guest>     Guests);

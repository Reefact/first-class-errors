namespace FirstClassErrors.RequestBinder.Usage.Boundary;

/// <summary>
///     The incoming request DTO — the "wire" shape a primary adapter (HTTP controller, message consumer, CLI) hands the
///     binder. Every property is <b>nullable</b>: absence is data the binder must detect, so a missing value arrives as
///     <c>null</c> rather than a silent default. Nothing here is validated; the binder turns it into a
///     <see cref="Model.PlaceBookingCommand" /> of value objects.
/// </summary>
/// <param name="GuestEmail">The guest e-mail address (required once bound).</param>
/// <param name="Reference">The booking reference (required, bound raw).</param>
/// <param name="Currency">The billing currency code (optional, falls back to a default).</param>
/// <param name="Nights">The number of nights — a nullable value type, bound through the value-type overload.</param>
/// <param name="MaxNights">An optional cap on the number of nights — bound through <c>AsOptionalValue</c>.</param>
/// <param name="Stay">The nested stay object (required complex property).</param>
/// <param name="Tags">The booking tags (a list of simple string properties).</param>
/// <param name="RoomNumbers">The requested room numbers (a list of nullable value-type properties).</param>
/// <param name="Guests">The guests (a list of nested complex properties).</param>
public sealed record BookingRequest(
    string?                   GuestEmail,
    string?                   Reference,
    string?                   Currency,
    int?                      Nights,
    int?                      MaxNights,
    StayDto?                  Stay,
    IReadOnlyList<string?>?   Tags,
    IReadOnlyList<int?>?      RoomNumbers,
    IReadOnlyList<GuestDto?>? Guests);

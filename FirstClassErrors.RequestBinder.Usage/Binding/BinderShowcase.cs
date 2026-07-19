#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Boundary;
using FirstClassErrors.RequestBinder.Usage.Errors;
using FirstClassErrors.RequestBinder.Usage.Model;
using FirstClassErrors.RequestBinder.Usage.Options;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Binding;

/// <summary>
///     Focused variations that round out the picture <see cref="BookingBinder" /> gives: the overloads and options the
///     canonical one-pass binder does not use. Each reuses the same <see cref="BookingRequest" /> DTO and envelope, so
///     the difference is only in the one binder feature it demonstrates.
/// </summary>
public static class BinderShowcase {

    #region Statics members declarations

    /// <summary>
    ///     The structural-error <b>definition</b> (code + public messages, kept together) this sample raises for a
    ///     missing argument when it overrides the binder defaults. Derived from the built-in default with
    ///     <c>WithCode</c>, so it keeps the default messages and only swaps the code; <c>WithMessage</c> would override
    ///     the messages too.
    /// </summary>
    public static readonly BinderErrorDefinition HotelApiArgumentRequired =
        RequestBindingError.DefaultArgumentRequired.WithCode(ErrorCode.Create("HOTELAPI_ARGUMENT_REQUIRED"));

    /// <summary>
    ///     The structural-error definition this sample raises for a present-but-invalid argument when it overrides the
    ///     binder defaults.
    /// </summary>
    public static readonly BinderErrorDefinition HotelApiArgumentInvalid =
        RequestBindingError.DefaultArgumentInvalid.WithCode(ErrorCode.Create("HOTELAPI_ARGUMENT_INVALID"));

    /// <summary>
    ///     A <b>required</b> list of simple properties (contrast <see cref="BookingBinder" />, which binds the tags as
    ///     optional): an absent list records <c>REQUEST_ARGUMENT_REQUIRED</c>, while a present-but-empty list is valid
    ///     and binds an empty list — a required list constrains presence, not element count.
    /// </summary>
    public static Outcome<IReadOnlyList<Tag>> BindTagsAsRequired(BookingRequest request) {
        RequestBinder                     binder = Bind.Request(PlaceBookingError.CommandInvalid);
        PropertySource<BookingRequest>    body   = binder.PropertiesOf(request);

        RequiredField<IReadOnlyList<Tag>> tags = body.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        return binder.New(s => s.Get(tags));
    }

    /// <summary>
    ///     An <b>optional</b> complex property and an <b>optional</b> list of complex properties: an absent stay yields
    ///     <c>null</c>, an absent guests list yields an empty list — both record nothing. Present-but-invalid content
    ///     still records under its (prefixed / indexed) path.
    /// </summary>
    public static Outcome<OptionalSections> BindOptionalSections(BookingRequest request) {
        RequestBinder                   binder = Bind.Request(PlaceBookingError.CommandInvalid);
        PropertySource<BookingRequest>  body   = binder.PropertiesOf(request);

        OptionalReferenceField<Stay>        stay   = body.ComplexProperty(r => r.Stay).FailWith(PlaceBookingError.StayInvalid).AsOptionalReference(BookingBinder.BindStay);
        RequiredField<IReadOnlyList<Guest>> guests = body.ListOfComplexProperties(r => r.Guests).FailWith(PlaceBookingError.GuestInvalid).AsOptional(BookingBinder.BindGuest);

        return binder.New(s => new OptionalSections(s.Get(stay), s.Get(guests)));
    }

    /// <summary>
    ///     Binds the guest e-mail with a custom <see cref="IArgumentNameProvider" /> (<see cref="SnakeCaseArgumentNames" />)
    ///     fixed once through <c>Bind.WithOptions</c>, so a failing <c>GuestEmail</c> is reported under the path
    ///     <c>guest_email</c> — the key a snake_case client actually sent — rather than the C# property name.
    /// </summary>
    public static Outcome<EmailAddress> BindGuestEmailWithSnakeCaseNames(BookingRequest request) {
        RequestBinder binder =
            Bind.WithOptions(new RequestBinderOptions(new SnakeCaseArgumentNames()))
                .Request(PlaceBookingError.CommandInvalid);
        PropertySource<BookingRequest> body = binder.PropertiesOf(request);

        RequiredField<EmailAddress> email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        return binder.New(s => s.Get(email));
    }

    /// <summary>
    ///     Binds the guest e-mail with custom <b>structural codes</b>: the binder raises
    ///     <c>HOTELAPI_ARGUMENT_REQUIRED</c> / <c>HOTELAPI_ARGUMENT_INVALID</c> instead of the defaults, so its
    ///     structural failures line up with the rest of the application's catalog. Consumers still branch
    ///     <b>symbolically</b> (see <see cref="IsMissingArgument" />), never on a message string.
    /// </summary>
    public static Outcome<EmailAddress> BindGuestEmailWithCustomStructuralCodes(BookingRequest request) {
        RequestBinderOptions options = new(
            new SnakeCaseArgumentNames(),
            HotelApiArgumentRequired,
            HotelApiArgumentInvalid);

        RequestBinder                  binder = Bind.WithOptions(options).Request(PlaceBookingError.CommandInvalid);
        PropertySource<BookingRequest> body   = binder.PropertiesOf(request);

        RequiredField<EmailAddress> email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        return binder.New(s => s.Get(email));
    }

    /// <summary>
    ///     The symbolic way to branch on a binder's structural failures: compare the error's <see cref="Error.Code" />
    ///     against the well-known code rather than parsing its message. Pass the default
    ///     (<see cref="RequestBindingError.DefaultArgumentRequiredCode" />), or your own configured code.
    /// </summary>
    public static bool IsMissingArgument(Error error, ErrorCode argumentRequiredCode) {
        return error.Code == argumentRequiredCode;
    }

    /// <summary>
    ///     The binder's programming-error guard, shown in isolation: selecting a <b>non-nullable</b> value-type property
    ///     (an <c>int</c>, not an <c>int?</c>) throws <see cref="ArgumentException" /> at <c>SimpleProperty(...)</c>,
    ///     because an absent value would be indistinguishable from its default. Declaring the DTO property nullable is
    ///     the fix. Returns <c>true</c> to confirm the guard tripped.
    /// </summary>
    public static bool NonNullableValueTypeGuardTrips() {
        RequestBinder                    binder = Bind.Request(PlaceBookingError.CommandInvalid);
        PropertySource<NonNullableProbe> probe  = binder.PropertiesOf(new NonNullableProbe(0));

        try {
            probe.SimpleProperty(p => p.Nights);

            return false;
        } catch (ArgumentException) {
            return true;
        }
    }

    #endregion

    #region Nested types declarations

    /// <summary>The pair produced by <see cref="BindOptionalSections" />: an optional stay and the guests list.</summary>
    /// <param name="Stay">The bound stay, or <c>null</c> when the request omitted it.</param>
    /// <param name="Guests">The bound guests (empty when the request omitted the list).</param>
    public sealed record OptionalSections(Stay? Stay, IReadOnlyList<Guest> Guests);

    /// <summary>A DTO with a non-nullable value-type property, used only by <see cref="NonNullableValueTypeGuardTrips" />.</summary>
    private sealed record NonNullableProbe(int Nights);

    #endregion

}

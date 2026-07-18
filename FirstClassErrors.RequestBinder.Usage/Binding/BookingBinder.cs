#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Boundary;
using FirstClassErrors.RequestBinder.Usage.Errors;
using FirstClassErrors.RequestBinder.Usage.Model;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Binding;

/// <summary>
///     The canonical request-binder example: turns a <see cref="BookingRequest" /> DTO into a
///     <see cref="PlaceBookingCommand" /> of value objects, collecting <b>every</b> failure into one coded envelope
///     instead of stopping at the first. One method, one pass, exercising every converter shape.
/// </summary>
public static class BookingBinder {

    #region Statics members declarations

    /// <summary>
    ///     Binds a full booking request. On success, a complete command of value objects; on failure, the
    ///     <see cref="PlaceBookingError.CommandInvalid" /> envelope grouping every field failure, in declaration order,
    ///     with each argument's full (indexed, prefixed) path. Raises no exception on the invalid-input path.
    /// </summary>
    public static Outcome<PlaceBookingCommand> BindBooking(BookingRequest request) {
        RequestBinder<BookingRequest> binder = Bind.PropertiesOf(request).FailWith(PlaceBookingError.CommandInvalid);

        // Scalars: required + converter, required + raw (presence only), optional + fallback.
        RequiredField<EmailAddress> email     = binder.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        RequiredField<string>       reference = binder.SimpleProperty(r => r.Reference).AsRequired();
        RequiredField<Currency>     currency  = binder.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");

        // Value-type scalars: a nullable value-type property bound over its underlying int; required, and optional
        // (a real Nullable<NightCount> when absent — never default(NightCount)).
        RequiredField<NightCount>      nights    = binder.SimpleProperty(r => r.Nights).AsRequired(NightCount.From);
        OptionalValueField<NightCount> maxNights = binder.SimpleProperty(r => r.MaxNights).AsOptionalValue(NightCount.From);

        // Complex (nested) property: a required sub-object bound by a nested binder under its own envelope.
        RequiredField<Stay> stay = binder.ComplexProperty(r => r.Stay).FailWith(PlaceBookingError.StayInvalid).AsRequired(BindStay);

        // Lists: of simple reference elements (optional), of value-type elements (required), of complex elements (required).
        RequiredField<IReadOnlyList<Tag>>        tags  = binder.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);
        RequiredField<IReadOnlyList<RoomNumber>> rooms = binder.ListOfSimpleProperties(r => r.RoomNumbers).AsRequired(RoomNumber.From);
        RequiredField<IReadOnlyList<Guest>>      guests = binder.ListOfComplexProperties(r => r.Guests).FailWith(PlaceBookingError.GuestInvalid).AsRequired(BindGuest);

        // Total assembler: runs once, only when no failure was recorded.
        return binder.New(s => new PlaceBookingCommand(
                              s.Get(email),
                              s.Get(reference),
                              s.Get(currency),
                              s.Get(nights),
                              s.Get(maxNights),
                              s.Get(stay),
                              s.Get(tags),
                              s.Get(rooms),
                              s.Get(guests)));
    }

    /// <summary>
    ///     Binds a nested stay, then assembles it through the <b>validating</b> <c>Create</c> terminal so the
    ///     cross-field rule (check-out strictly after check-in) — which no single field can check — is enforced and
    ///     flattened. Each date is reported under a <c>Stay.</c>-prefixed path. Shared with <see cref="BinderShowcase" />.
    /// </summary>
    internal static Outcome<Stay> BindStay(RequestBinder<StayDto> stay) {
        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return stay.Create(s => Stay.Create(s.Get(checkIn), s.Get(checkOut)));
    }

    /// <summary>
    ///     Binds a single guest: a required raw first name and an optional e-mail address, assembled by the total
    ///     <c>New</c> terminal. Each field is reported under an indexed path such as <c>Guests[1].FirstName</c>. Shared
    ///     with <see cref="BinderShowcase" />.
    /// </summary>
    internal static Outcome<Guest> BindGuest(RequestBinder<GuestDto> guest) {
        RequiredField<string>                firstName = guest.SimpleProperty(g => g.FirstName).AsRequired();
        OptionalReferenceField<EmailAddress> email     = guest.SimpleProperty(g => g.Email).AsOptionalReference(EmailAddress.Parse);

        return guest.New(s => new Guest(s.Get(firstName), s.Get(email)));
    }

    #endregion

}

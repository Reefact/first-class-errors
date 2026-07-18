#region Usings declarations

using FirstClassErrors.RequestBinder.Usage.Binding;
using FirstClassErrors.RequestBinder.Usage.Boundary;
using FirstClassErrors.RequestBinder.Usage.Model;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.UnitTests;

/// <summary>
///     Behavioural tests for the canonical <see cref="BookingBinder" />: they prove the examples are honest — a valid
///     request binds into the complete command, and an invalid one produces the full, ordered, coded error tree with no
///     exception.
/// </summary>
public sealed class BookingBinderTests {

    [Fact(DisplayName = "A fully valid request binds into the complete command.")]
    public void ValidRequestBinds() {
        Outcome<PlaceBookingCommand> outcome = BookingBinder.BindBooking(BookingRequests.Valid());

        Check.That(outcome.IsSuccess).IsTrue();
        PlaceBookingCommand command = outcome.GetResultOrThrow();

        Check.That(command.GuestEmail.Value).IsEqualTo("alice@example.org");
        Check.That(command.Reference).IsEqualTo("REF-42");
        Check.That(command.Currency.Code).IsEqualTo("EUR"); // fell back
        Check.That(command.Nights.Value).IsEqualTo(3);
        Check.That(command.MaxNights.HasValue).IsFalse(); // absent -> real null, not 0
        Check.That(command.Stay.CheckIn.Value).IsEqualTo(new DateOnly(2026, 8, 10));
        Check.That(command.Stay.CheckOut.Value).IsEqualTo(new DateOnly(2026, 8, 14));
        Check.That(command.Tags.Select(t => t.Value)).ContainsExactly("vip");
        Check.That(command.RoomNumbers.Select(r => r.Value)).ContainsExactly(101, 102);
        Check.That(command.Guests.Select(g => g.FirstName)).ContainsExactly("Alice", "Bob");
        Check.That(command.Guests[1].Email).IsNull(); // optional e-mail absent
    }

    [Fact(DisplayName = "A request failing at every level yields one envelope with the full ordered error tree, and no exception.")]
    public void InvalidRequestYieldsTheOrderedTree() {
        Outcome<PlaceBookingCommand> outcome = BookingBinder.BindBooking(BookingRequests.InvalidEverywhere());

        Check.That(outcome.IsFailure).IsTrue();
        Error envelope = outcome.Error!;
        Check.That(envelope.Code.ToString()).IsEqualTo("BOOKING_COMMAND_INVALID");

        // Children, in binding (declaration) order.
        Check.That(envelope.InnerErrors).HasSize(9);
        Check.That(envelope.InnerErrors.Select(e => e.Code.ToString())).ContainsExactly(
            "REQUEST_ARGUMENT_INVALID",  // GuestEmail
            "REQUEST_ARGUMENT_REQUIRED", // Reference
            "REQUEST_ARGUMENT_INVALID",  // Currency
            "REQUEST_ARGUMENT_REQUIRED", // Nights
            "REQUEST_ARGUMENT_INVALID",  // MaxNights
            "BOOKING_STAY_INVALID",      // Stay (nested envelope)
            "REQUEST_ARGUMENT_INVALID",  // Tags[1]
            "REQUEST_ARGUMENT_INVALID",  // RoomNumbers[1]
            "BOOKING_GUEST_INVALID");    // Guests[1] (nested envelope)
    }

    [Fact(DisplayName = "Every failing scalar carries its full argument path in context.")]
    public void FailingScalarsCarryTheirPaths() {
        Error envelope = BookingBinder.BindBooking(BookingRequests.InvalidEverywhere()).Error!;

        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[0])).IsEqualTo("GuestEmail");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[1])).IsEqualTo("Reference");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[2])).IsEqualTo("Currency");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[3])).IsEqualTo("Nights");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[4])).IsEqualTo("MaxNights");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[6])).IsEqualTo("Tags[1]");
        Check.That(BookingRequests.ArgumentPathOf(envelope.InnerErrors[7])).IsEqualTo("RoomNumbers[1]");
    }

    [Fact(DisplayName = "A nested stay envelope carries the prefixed path of its failing date.")]
    public void NestedStayEnvelopeCarriesPrefixedPaths() {
        Error envelope   = BookingBinder.BindBooking(BookingRequests.InvalidEverywhere()).Error!;
        Error stayInvalid = envelope.InnerErrors[5];

        Check.That(stayInvalid.Code.ToString()).IsEqualTo("BOOKING_STAY_INVALID");
        Check.That(stayInvalid.InnerErrors.Select(BookingRequests.ArgumentPathOf)).ContainsExactly("Stay.CheckIn");
    }

    [Fact(DisplayName = "A per-element guest envelope carries the indexed paths of its failing fields.")]
    public void GuestEnvelopeCarriesIndexedPaths() {
        Error envelope    = BookingBinder.BindBooking(BookingRequests.InvalidEverywhere()).Error!;
        Error guestInvalid = envelope.InnerErrors[8];

        Check.That(guestInvalid.Code.ToString()).IsEqualTo("BOOKING_GUEST_INVALID");
        Check.That(guestInvalid.InnerErrors.Select(BookingRequests.ArgumentPathOf))
             .ContainsExactly("Guests[1].FirstName", "Guests[1].Email");
    }

    [Fact(DisplayName = "The cross-field Create rule surfaces when both dates parse but check-out is not after check-in.")]
    public void CrossFieldRuleSurfaces() {
        BookingRequest request = BookingRequests.Valid() with {
            Stay = new StayDto("2026-08-14", "2026-08-10") // check-out before check-in
        };

        Error envelope = BookingBinder.BindBooking(request).Error!;

        // The Stay slot fails, and the leaf is the factory's own cross-field domain error (surfaced as-is).
        Error stayFailure = envelope.InnerErrors.Single();
        Check.That(FindCode(stayFailure, "STAY_CHECKOUT_NOT_AFTER_CHECKIN")).IsTrue();
    }

    private static bool FindCode(Error error, string code) {
        if (error.Code.ToString() == code) { return true; }

        return error.InnerErrors.Any(inner => FindCode(inner, code));
    }

}

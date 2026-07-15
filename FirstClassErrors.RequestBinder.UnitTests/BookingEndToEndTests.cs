#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     End-to-end scenario mirroring the reference example of the design spec (issue #126): a full booking command
///     bound in one pass, exercising every converter shape, then rejoining the Outcome pipeline.
/// </summary>
public sealed class BookingEndToEndTests {

    private static Outcome<Stay> BindStay(RequestBinder<StayDto> stay) {
        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return stay.Build(s => new Stay(s.Get(checkIn), s.Get(checkOut)));
    }

    private static Outcome<Guest> BindGuest(RequestBinder<GuestDto> guest) {
        RequiredField<string>                firstName = guest.SimpleProperty(g => g.FirstName).AsRequired();
        OptionalReferenceField<EmailAddress> email     = guest.SimpleProperty(g => g.Email).AsOptionalReference(EmailAddress.Parse);

        return guest.Build(s => new Guest(s.Get(firstName), s.Get(email)));
    }

    private static Outcome<BookingCommand> BindCommand(BookingRequest request) {
        var bind = Bind.PropertiesOf(request).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<EmailAddress>         email     = bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        RequiredField<string>               reference = bind.SimpleProperty(r => r.Reference).AsRequired();
        RequiredField<Currency>             currency  = bind.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        OptionalValueField<int>             maxNights = bind.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
        OptionalReferenceField<Stay>        stay      = bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsOptional(BindStay);
        RequiredField<IReadOnlyList<Tag>>   tags      = bind.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);
        RequiredField<IReadOnlyList<Guest>> guests    = bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(BindGuest);

        return bind.Build(s => new BookingCommand(
                              s.Get(email), s.Get(reference), s.Get(currency), s.Get(maxNights),
                              s.Get(stay), s.Get(tags), s.Get(guests)));
    }

    [Fact(DisplayName = "A fully valid request binds into the complete command and flows through Then/Finally.")]
    public void FullyValidRequestBindsAndRejoinsThePipeline() {
        BookingRequest request = new(
            "alice@example.org", "REF-42", null, "7",
            new StayDto("2026-08-10", "2026-08-14"),
            ["vip"],
            [new GuestDto("Alice", "alice@example.org"), new GuestDto("Bob", null)]);

        string confirmation = BindCommand(request)
                              .Then(command => Outcome<string>.Success($"{command.Reference}:{command.Currency.Code}:{command.MaxNights}:{command.Guests.Count}"))
                              .Finally(result => result, error => $"rejected:{error.Code}");

        Check.That(confirmation).IsEqualTo("REF-42:EUR:7:2");
    }

    [Fact(DisplayName = "A request failing at every level yields the full error tree of the spec, and no exception.")]
    public void InvalidRequestYieldsTheFullTree() {
        BookingRequest request = new(
            "not-an-email", null, "EURO", "0",
            new StayDto("not-a-date", "2026-08-14"),
            ["ok", "not ok"],
            [new GuestDto("Alice", null), new GuestDto(null, "still-not-an-email")]);

        Outcome<BookingCommand> outcome = BindCommand(request);

        Check.That(outcome.IsFailure).IsTrue();
        Error envelope = outcome.Error!;
        Check.That(envelope.Code.ToString()).IsEqualTo("TEST_BOOKING_COMMAND_INVALID");

        // Envelope children, in declaration order:
        //   GuestEmail invalid, Reference missing, Currency invalid, MaxNights invalid,
        //   Stay envelope (present but invalid), Tags[1] invalid, Guests[1] envelope.
        Check.That(envelope.InnerErrors).HasSize(7);
        Check.That(envelope.InnerErrors.Select(e => e.Code.ToString())).ContainsExactly(
            "REQUEST_ARGUMENT_INVALID",   // GuestEmail
            "REQUEST_ARGUMENT_REQUIRED",  // Reference
            "REQUEST_ARGUMENT_INVALID",   // Currency
            "REQUEST_ARGUMENT_INVALID",   // MaxNights
            "TEST_STAY_INVALID",          // Stay (nested envelope)
            "REQUEST_ARGUMENT_INVALID",   // Tags[1]
            "TEST_GUEST_INVALID");        // Guests[1] (nested envelope)

        Error stayEnvelope = envelope.InnerErrors[4];
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Stay.CheckIn");
        Check.That(stayEnvelope.InnerErrors[0].InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_DATE_INVALID");

        Error guestEnvelope = envelope.InnerErrors[6];
        Check.That(guestEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Guests[1].FirstName", "Guests[1].Email");
    }

}

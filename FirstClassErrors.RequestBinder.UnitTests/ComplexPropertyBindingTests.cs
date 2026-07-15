#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class ComplexPropertyBindingTests {

    private static Outcome<Stay> BindStay(RequestBinder<StayDto> stay) {
        RequiredProperty<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredProperty<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return stay.Build(() => new Stay(checkIn.Value, checkOut.Value));
    }

    private static BookingRequest RequestWith(StayDto? stay) {
        return new BookingRequest("alice@example.org", "REF-1", "EUR", null, stay, Tags: null, Guests: null);
    }

    [Fact(DisplayName = "A required complex property binds through its nested binder when every field is valid.")]
    public void RequiredComplexBinds() {
        var bind = Bind.PropertiesOf(RequestWith(new StayDto("2026-08-10", "2026-08-14"))).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredProperty<Stay> stay = bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<Stay> outcome = bind.Build(() => stay.Value);
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().CheckIn.Value).IsEqualTo(new DateOnly(2026, 8, 10));
    }

    [Fact(DisplayName = "A failed nested binding surfaces as its own envelope, whose inner paths are prefixed with the property name.")]
    public void NestedFailureSurfacesAsItsEnvelopeWithPrefixedPaths() {
        var bind = Bind.PropertiesOf(RequestWith(new StayDto("not-a-date", null))).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<string> outcome = bind.Build(() => "never");
        Check.That(outcome.IsFailure).IsTrue();

        Error stayEnvelope = outcome.Error!.InnerErrors.Single();
        Check.That(stayEnvelope.Code.ToString()).IsEqualTo("TEST_STAY_INVALID");

        // Both nested failures are collected, and their paths carry the "Stay." prefix.
        Check.That(stayEnvelope.InnerErrors).HasSize(2);
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("Stay.CheckIn", "Stay.CheckOut");
        Check.That(stayEnvelope.InnerErrors[0].Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(stayEnvelope.InnerErrors[1].Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    [Fact(DisplayName = "A required complex property that is missing records REQUEST_ARGUMENT_REQUIRED; its envelope is never invoked.")]
    public void RequiredComplexMissing() {
        var bind = Bind.PropertiesOf(RequestWith(stay: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<string> outcome = bind.Build(() => "never");
        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("Stay");
    }

    [Fact(DisplayName = "An optional complex property yields null when absent — recording nothing — and binds when present.")]
    public void OptionalComplex() {
        var absent = Bind.PropertiesOf(RequestWith(stay: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalReferenceProperty<Stay> none = absent.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsOptional(BindStay);
        Check.That(none.Value).IsNull();
        Check.That(absent.Build(() => "built").IsSuccess).IsTrue();

        var present = Bind.PropertiesOf(RequestWith(new StayDto("2026-08-10", "2026-08-14"))).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalReferenceProperty<Stay> some = present.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsOptional(BindStay);
        Check.That(some.Value!.CheckOut.Value).IsEqualTo(new DateOnly(2026, 8, 14));
    }

    [Fact(DisplayName = "An optional complex property that is present but invalid records its envelope.")]
    public void OptionalComplexPresentButInvalidRecords() {
        var bind = Bind.PropertiesOf(RequestWith(new StayDto(null, null))).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsOptional(BindStay);

        Outcome<string> outcome = bind.Build(() => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_STAY_INVALID");
    }

}

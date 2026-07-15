#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class RequestBinderTests {

    private static Outcome<Stay> BindStay(RequestBinder<StayDto> stay) {
        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return stay.Build(read => new Stay(read.Get(checkIn), read.Get(checkOut)));
    }

    [Fact(DisplayName = "Build assembles the command exactly once when every property bound.")]
    public void BuildAssemblesOnceOnSuccess() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", "REF-1", "EUR", null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<EmailAddress> email = bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        int assembled = 0;
        Outcome<string> outcome = bind.Build(read => {
            assembled++;

            return read.Get(email).Value;
        });

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(assembled).IsEqualTo(1);
    }

    [Fact(DisplayName = "Build never runs the assembler when a failure was recorded — field reads are safe by construction.")]
    public void BuildNeverAssemblesOnFailure() {
        var bind = Bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);
        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        bool assembled = false;
        Outcome<string> outcome = bind.Build(_ => {
            assembled = true;

            return "never";
        });

        Check.That(outcome.IsFailure).IsTrue();
        Check.That(assembled).IsFalse();
    }

    [Fact(DisplayName = "Every failing property is collected into the envelope, in declaration order — collect-all, not first-failure.")]
    public void CollectsEveryFailure() {
        var bind = Bind.PropertiesOf(new BookingRequest("nope", null, "EURO", null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        bind.SimpleProperty(r => r.Reference).AsRequired();
        bind.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");

        Outcome<string> outcome = bind.Build(_ => "never");
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_BOOKING_COMMAND_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(e => e.Code.ToString()))
             .ContainsExactly("REQUEST_ARGUMENT_INVALID", "REQUEST_ARGUMENT_REQUIRED", "REQUEST_ARGUMENT_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("GuestEmail", "Reference", "Currency");
    }

    [Fact(DisplayName = "A fully invalid request binds without a single exception being thrown.")]
    public void FullyInvalidRequestThrowsNothing() {
        Check.ThatCode(() => {
                 var bind = Bind.PropertiesOf(new BookingRequest("nope", null, "EURO", "-1", new StayDto(null, "bad"), ["a b", null], [null, new GuestDto(null, "x")]))
                                .FailWith(BookingEnvelopeError.CommandInvalid);

                 bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
                 bind.SimpleProperty(r => r.Reference).AsRequired();
                 bind.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
                 bind.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
                 bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);
                 bind.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);
                 bind.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired(g => {
                     RequiredField<string> firstName = g.SimpleProperty(x => x.FirstName).AsRequired();

                     return g.Build(read => new Guest(read.Get(firstName), null));
                 });

                 return bind.Build(_ => "never");
             })
             .DoesNotThrow();
    }

    [Fact(DisplayName = "A converter that throws is a bug: the exception propagates to the host, undisguised.")]
    public void ThrowingConverterPropagates() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail)
                                 .AsRequired<EmailAddress>(_ => throw new FormatException("converter bug")))
             .Throws<FormatException>();
    }

    [Fact(DisplayName = "A selector that is not a direct property access is a programming error and throws.")]
    public void InvalidSelectorThrows() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        // A method call on the property — not a direct member access.
        Check.ThatCode(() => bind.SimpleProperty(r => r.GuestEmail!.ToUpperInvariant()))
             .Throws<ArgumentException>();
        // A nested property access — the member's base is not the request parameter.
        Check.ThatCode(() => bind.SimpleProperty(r => r.Stay!.CheckIn))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A custom argument-name provider renames the paths — and nested binders inherit it.")]
    public void CustomNameProviderRenamesPaths() {
        var bind = Bind.PropertiesOf(new BookingRequest("a@b.c", "REF-1", null, null, new StayDto(null, null), null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid)
                       .WithOptions(new RequestBinderOptions(new SnakeCaseNameProvider()));

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Outcome<string> outcome = bind.Build(_ => "never");
        Error stayEnvelope = outcome.Error!.InnerErrors.Single(e => e.Code.ToString() == "TEST_STAY_INVALID");
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("stay.check_in", "stay.check_out");
    }

    [Fact(DisplayName = "Binding failures are non-transient: resubmitting the same request cannot succeed.")]
    public void MissingArgumentIsNonTransient() {
        var bind = Bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.Build(_ => "never");
        var required = (InfrastructureError)outcome.Error!.InnerErrors.Single();
        Check.That(required.Transience).IsEqualTo(Transience.NonTransient);
    }

    private sealed class SnakeCaseNameProvider : IArgumentNameProvider {

        public string GetArgumentNameFrom(PropertyInfo property) {
            return string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

    }

}

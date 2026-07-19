#region Usings declarations

using System.Reflection;

using FirstClassErrors.Testing;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class RequestBinderTests {

    private static Outcome<Stay> BindStay(RequestBinder binder, StayDto dto) {
        PropertySource<StayDto>    stay     = binder.PropertiesOf(dto);
        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return binder.New(s => new Stay(s.Get(checkIn), s.Get(checkOut)));
    }

    private static readonly ErrorCode CheckOutNotAfterCheckIn = ErrorCode.Create("TEST_CHECKOUT_NOT_AFTER_CHECKIN");

    // A validating factory: every field is already bound, yet a cross-field rule — check-out strictly after check-in —
    // can still reject an all-valid combination. That is exactly what Create (unlike New) can express.
    private static Outcome<Stay> AssembleStay(BookingDate checkIn, BookingDate checkOut) {
        return checkOut.Value > checkIn.Value
                   ? Outcome<Stay>.Success(new Stay(checkIn, checkOut))
                   : Outcome<Stay>.Failure(DomainError.Create(CheckOutNotAfterCheckIn, DiagnosticMessageFactory.Any())
                                                      .WithPublicMessage(ShortMessageFactory.Any()));
    }

    [Fact(DisplayName = "New assembles the command exactly once when every property bound.")]
    public void NewAssemblesOnceOnSuccess() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("a@b.c", "REF-1", "EUR", null, null, null, null));
        RequiredField<EmailAddress> email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        int assembled = 0;
        Outcome<string> outcome = bind.New(s => {
            assembled++;

            return s.Get(email).Value;
        });

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(assembled).IsEqualTo(1);
    }

    [Fact(DisplayName = "New never runs the assembler when a failure was recorded — field reads are safe by construction.")]
    public void NewNeverAssemblesOnFailure() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null));
        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        bool assembled = false;
        Outcome<string> outcome = bind.New(_ => {
            assembled = true;

            return "never";
        });

        Check.That(outcome.IsFailure).IsTrue();
        Check.That(assembled).IsFalse();
    }

    [Fact(DisplayName = "Create flattens the validating factory's success into Outcome<TCommand> — the command itself, not a nested Outcome.")]
    public void CreateFlattensFactorySuccess() {
        var bind = Bind.Request(BookingEnvelopeError.StayInvalid);
        var body = bind.PropertiesOf(new StayDto("2026-08-10", "2026-08-14"));
        RequiredField<BookingDate> checkIn  = body.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = body.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        Outcome<Stay> outcome = bind.Create(s => AssembleStay(s.Get(checkIn), s.Get(checkOut)));

        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow().CheckOut.Value).IsEqualTo(new DateOnly(2026, 8, 14));
    }

    [Fact(DisplayName = "Create returns the validating factory's own failure as-is — a cross-field rule surfaces undisguised, not wrapped in the binder envelope.")]
    public void CreateReturnsFactoryFailureAsIs() {
        var bind = Bind.Request(BookingEnvelopeError.StayInvalid);
        var body = bind.PropertiesOf(new StayDto("2026-08-14", "2026-08-10"));
        RequiredField<BookingDate> checkIn  = body.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = body.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        Outcome<Stay> outcome = bind.Create(s => AssembleStay(s.Get(checkIn), s.Get(checkOut)));

        Check.That(outcome.IsFailure).IsTrue();
        // The factory's error is returned directly: its own code, and NOT wrapped under the binder's TEST_STAY_INVALID envelope.
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_CHECKOUT_NOT_AFTER_CHECKIN");
        Check.That(outcome.Error).IsInstanceOf<DomainError>();
    }

    [Fact(DisplayName = "Create never calls the validating factory when a binding failed — it returns the envelope, factory untouched.")]
    public void CreateNeverCallsFactoryOnBindingFailure() {
        var bind = Bind.Request(BookingEnvelopeError.StayInvalid);
        var body = bind.PropertiesOf(new StayDto(null, "2026-08-14"));
        RequiredField<BookingDate> checkIn  = body.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = body.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        bool factoryCalled = false;
        Outcome<Stay> outcome = bind.Create(s => {
            factoryCalled = true;

            return AssembleStay(s.Get(checkIn), s.Get(checkOut));
        });

        Check.That(outcome.IsFailure).IsTrue();
        Check.That(factoryCalled).IsFalse();
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_STAY_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(e => e.Code.ToString())).ContainsExactly("REQUEST_ARGUMENT_REQUIRED");
    }

    [Fact(DisplayName = "Every failing property is collected into the envelope, in declaration order — collect-all, not first-failure.")]
    public void CollectsEveryFailure() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("nope", null, "EURO", null, null, null, null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        body.SimpleProperty(r => r.Reference).AsRequired();
        body.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_BOOKING_COMMAND_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(e => e.Code.ToString()))
             .ContainsExactly("REQUEST_ARGUMENT_INVALID", "REQUEST_ARGUMENT_REQUIRED", "REQUEST_ARGUMENT_INVALID");
        Check.That(outcome.Error!.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("GuestEmail", "Reference", "Currency");
    }

    [Fact(DisplayName = "A fully invalid request binds without a single exception being thrown.")]
    public void FullyInvalidRequestThrowsNothing() {
        Check.ThatCode(() => {
                 var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
                 var body = bind.PropertiesOf(new BookingRequest("nope", null, "EURO", "-1", new StayDto(null, "bad"), ["a b", null], [null, new GuestDto(null, "x")]));

                 body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
                 body.SimpleProperty(r => r.Reference).AsRequired();
                 body.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
                 body.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
                 body.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired<Stay>(BindStay);
                 body.ListOfSimpleProperties(r => r.Tags).AsOptional(Tag.Parse);
                 body.ListOfComplexProperties(r => r.Guests).FailWith(BookingEnvelopeError.GuestInvalid).AsRequired<Guest>((g, dto) => {
                     PropertySource<GuestDto> guestBody = g.PropertiesOf(dto);
                     RequiredField<string>    firstName = guestBody.SimpleProperty(x => x.FirstName).AsRequired();

                     return g.New(s => new Guest(s.Get(firstName), null));
                 });

                 return bind.New(_ => "never");
             })
             .DoesNotThrow();
    }

    [Fact(DisplayName = "A converter that throws is a bug: the exception propagates to the host, undisguised.")]
    public void ThrowingConverterPropagates() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, null));

        Check.ThatCode(() => body.SimpleProperty(r => r.GuestEmail)
                                 .AsRequired<EmailAddress>(_ => throw new FormatException("converter bug")))
             .Throws<FormatException>();
    }

    [Fact(DisplayName = "A selector that is not a direct property access is a programming error and throws.")]
    public void InvalidSelectorThrows() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("a@b.c", null, null, null, null, null, null));

        // A method call on the property — not a direct member access; the exception echoes the offending selector.
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => { body.SimpleProperty(r => r.GuestEmail!.ToUpperInvariant()); });
        Check.That(exception.Message).Contains("ToUpperInvariant");
        // A nested property access — the member's base is not the request parameter.
        Check.ThatCode(() => body.SimpleProperty(r => r.Stay!.CheckIn))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A custom argument-name provider renames the paths — and nested binders inherit it.")]
    public void CustomNameProviderRenamesPaths() {
        var bind = Bind.WithOptions(new RequestBinderOptions(new SnakeCaseNameProvider()))
                       .Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest("a@b.c", "REF-1", null, null, new StayDto(null, null), null, null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        body.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired<Stay>(BindStay);

        Outcome<string> outcome = bind.New(_ => "never");
        Error stayEnvelope = outcome.Error!.InnerErrors.Single(e => e.Code.ToString() == "TEST_STAY_INVALID");
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf))
             .ContainsExactly("stay.check_in", "stay.check_out");
    }

    [Fact(DisplayName = "A configured entry point is reusable: Bind.WithOptions(...) binds many requests under the same naming policy.")]
    public void ConfiguredEntryPointIsReusableAcrossRequests() {
        // Bind.WithOptions holds no per-request state, so one instance configures every request the same way — the
        // pattern an application sets once at startup and reuses per request, instead of reconfiguring each binder.
        ConfiguredBind bind = Bind.WithOptions(new RequestBinderOptions(new SnakeCaseNameProvider()));

        string? PathOfMissingEmail(BookingRequest request) {
            var b    = bind.Request(BookingEnvelopeError.CommandInvalid);
            var body = b.PropertiesOf(request);
            body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

            return BindingAssertions.ArgumentPathOf(b.New(_ => "never").Error!.InnerErrors.Single());
        }

        Check.That(PathOfMissingEmail(new BookingRequest(null, "REF-1", null, null, null, null, null))).IsEqualTo("guest_email");
        Check.That(PathOfMissingEmail(new BookingRequest(null, "REF-2", null, null, null, null, null))).IsEqualTo("guest_email");
    }

    [Fact(DisplayName = "Binding failures are non-transient: resubmitting the same request cannot succeed.")]
    public void MissingArgumentIsNonTransient() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
        var required = (InfrastructureError)outcome.Error!.InnerErrors.Single();
        Check.That(required.Transience).IsEqualTo(Transience.NonTransient);
    }

    private sealed class SnakeCaseNameProvider : IArgumentNameProvider {

        public string GetArgumentNameFrom(PropertyInfo property) {
            return string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

    }

}

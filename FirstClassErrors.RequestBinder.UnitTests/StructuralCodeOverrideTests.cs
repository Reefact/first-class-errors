#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class StructuralCodeOverrideTests {

    private static readonly ErrorCode AcmeRequired = ErrorCode.Create("ACME_ARGUMENT_REQUIRED");
    private static readonly ErrorCode AcmeInvalid  = ErrorCode.Create("ACME_ARGUMENT_INVALID");

    private static RequestBinderOptions CustomCodes() {
        return new RequestBinderOptions(RequestBinderOptions.Default.ArgumentNameProvider, AcmeRequired, AcmeInvalid);
    }

    private static BookingRequest Request(string? guestEmail = "a@b.c", StayDto? stay = null,
                                          IReadOnlyList<string?>? tags = null) {
        return new BookingRequest(guestEmail, "REF-1", null, null, stay, tags, null);
    }

    private static Outcome<Stay> BindStay(RequestBinder<StayDto> stay) {
        RequiredField<BookingDate> checkIn  = stay.SimpleProperty(s => s.CheckIn).AsRequired(BookingDate.Parse);
        RequiredField<BookingDate> checkOut = stay.SimpleProperty(s => s.CheckOut).AsRequired(BookingDate.Parse);

        return stay.New(s => new Stay(s.Get(checkIn), s.Get(checkOut)));
    }

    // ── Defaults are unchanged ────────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "The public default codes are exposed and are REQUEST_ARGUMENT_REQUIRED / REQUEST_ARGUMENT_INVALID.")]
    public void PublicDefaultCodesAreExposed() {
        Check.That(RequestBindingError.DefaultArgumentRequiredCode.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(RequestBindingError.DefaultArgumentInvalidCode.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
    }

    [Fact(DisplayName = "Options without custom codes keep the default structural codes.")]
    public void OptionsDefaultToTheStructuralCodes() {
        Check.That(RequestBinderOptions.Default.ArgumentRequiredCode == RequestBindingError.DefaultArgumentRequiredCode).IsTrue();
        Check.That(RequestBinderOptions.Default.ArgumentInvalidCode == RequestBindingError.DefaultArgumentInvalidCode).IsTrue();

        var namesOnly = new RequestBinderOptions(RequestBinderOptions.Default.ArgumentNameProvider);
        Check.That(namesOnly.ArgumentRequiredCode == RequestBindingError.DefaultArgumentRequiredCode).IsTrue();
        Check.That(namesOnly.ArgumentInvalidCode == RequestBindingError.DefaultArgumentInvalidCode).IsTrue();
    }

    [Fact(DisplayName = "By default, a missing required argument still records REQUEST_ARGUMENT_REQUIRED.")]
    public void DefaultCodeStillRaisedWhenNotOverridden() {
        var bind = Bind.PropertiesOf(Request(guestEmail: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Check.That(bind.New(_ => "x").Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    // ── Overridden codes flow through every structural path ───────────────────────────────────────────────

    [Fact(DisplayName = "A configured code replaces REQUEST_ARGUMENT_REQUIRED on a missing scalar, keeping the path.")]
    public void CustomRequiredCodeOnMissingScalar() {
        var bind = Bind.WithOptions(CustomCodes()).PropertiesOf(Request(guestEmail: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Error error = bind.New(_ => "x").Error!.InnerErrors.Single();
        Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(error)).IsEqualTo("GuestEmail");
    }

    [Fact(DisplayName = "A configured code replaces REQUEST_ARGUMENT_INVALID on an invalid scalar, still wrapping the converter's cause.")]
    public void CustomInvalidCodeOnInvalidScalar() {
        var bind = Bind.WithOptions(CustomCodes()).PropertiesOf(Request(guestEmail: "not-an-email")).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Error error = bind.New(_ => "x").Error!.InnerErrors.Single();
        Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_INVALID");
        Check.That(error.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
    }

    [Fact(DisplayName = "Configured codes flow through list elements: a null element uses the required code, an invalid one the invalid code.")]
    public void CustomCodesOnListElements() {
        var bind = Bind.WithOptions(CustomCodes())
                       .PropertiesOf(Request(tags: ["ok", null, "bad tag"]))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ListOfSimpleProperties(r => r.Tags).AsRequired(Tag.Parse);

        IReadOnlyList<Error> errors = bind.New(_ => "x").Error!.InnerErrors;
        Check.That(errors.Select(e => e.Code.ToString())).ContainsExactly("ACME_ARGUMENT_REQUIRED", "ACME_ARGUMENT_INVALID");
        Check.That(errors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Tags[1]", "Tags[2]");
    }

    [Fact(DisplayName = "A missing complex property records the configured required code under its path.")]
    public void CustomRequiredCodeOnMissingComplex() {
        var bind = Bind.WithOptions(CustomCodes()).PropertiesOf(Request(stay: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Error error = bind.New(_ => "x").Error!.InnerErrors.Single();
        Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(error)).IsEqualTo("Stay");
    }

    [Fact(DisplayName = "A nested binder inherits the configured codes: the nested envelope's inner failures use them under prefixed paths.")]
    public void NestedBinderInheritsCustomCodes() {
        var bind = Bind.WithOptions(CustomCodes())
                       .PropertiesOf(Request(stay: new StayDto("not-a-date", null)))
                       .FailWith(BookingEnvelopeError.CommandInvalid);

        bind.ComplexProperty(r => r.Stay).FailWith(BookingEnvelopeError.StayInvalid).AsRequired(BindStay);

        Error stayEnvelope = bind.New(_ => "x").Error!.InnerErrors.Single(e => e.Code.ToString() == "TEST_STAY_INVALID");
        // CheckIn "not-a-date" is invalid; CheckOut is missing — both under the inherited codes, both prefixed with "Stay.".
        Check.That(stayEnvelope.InnerErrors.Select(e => e.Code.ToString())).ContainsExactly("ACME_ARGUMENT_INVALID", "ACME_ARGUMENT_REQUIRED");
        Check.That(stayEnvelope.InnerErrors.Select(BindingAssertions.ArgumentPathOf)).ContainsExactly("Stay.CheckIn", "Stay.CheckOut");
    }

    // ── The #147 crux: branch symbolically, never on a string ─────────────────────────────────────────────

    [Fact(DisplayName = "A consumer branches on a binder failure via ErrorCode equality — the exposed default, or its own overridden code.")]
    public void ConsumerBranchesSymbolically() {
        var byDefault = Bind.PropertiesOf(Request(guestEmail: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        byDefault.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        Error defaulted = byDefault.New(_ => "x").Error!.InnerErrors.Single();
        Check.That(defaulted.Code == RequestBindingError.DefaultArgumentRequiredCode).IsTrue();

        var overridden = Bind.WithOptions(CustomCodes()).PropertiesOf(Request(guestEmail: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        overridden.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);
        Error owned = overridden.New(_ => "x").Error!.InnerErrors.Single();
        Check.That(owned.Code == AcmeRequired).IsTrue();
    }

}

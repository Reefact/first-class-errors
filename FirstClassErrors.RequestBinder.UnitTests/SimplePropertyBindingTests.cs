#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

public sealed class SimplePropertyBindingTests {

    private static BookingRequest Request(string? email    = "alice@example.org",
                                          string? currency = "EUR",
                                          string? nights   = "3") {
        return new BookingRequest(email, "REF-1", currency, nights, Stay: null, Tags: null, Guests: null);
    }

    [Fact(DisplayName = "A required property that is present and valid binds its value.")]
    public void RequiredPresentAndValidBinds() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request());

        RequiredField<EmailAddress> email = body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.New(s => s.Get(email).Value);
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("alice@example.org");
    }

    [Fact(DisplayName = "A required property that is missing fails the build with REQUEST_ARGUMENT_REQUIRED, carrying the argument path.")]
    public void RequiredMissingFails() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(email: null));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_BOOKING_COMMAND_INVALID");

        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("GuestEmail");
    }

    [Fact(DisplayName = "A required property that is present but invalid fails with REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void RequiredInvalidWrapsTheConverterError() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(email: "not-an-email"));

        body.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();

        Error invalid = outcome.Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("GuestEmail");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
    }

    [Fact(DisplayName = "A required property without conversion binds the raw value when present, and fails when missing.")]
    public void RequiredWithoutConversion() {
        var present     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var presentBody = present.PropertiesOf(Request());
        RequiredField<string> reference = presentBody.SimpleProperty(r => r.Reference).AsRequired();
        Check.That(present.New(s => s.Get(reference)).GetResultOrThrow()).IsEqualTo("REF-1");

        var missing     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var missingBody = missing.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null));
        missingBody.SimpleProperty(r => r.Reference).AsRequired();
        Outcome<string> outcome = missing.New(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    [Fact(DisplayName = "An optional property with a fallback converts the provided value when present, and the fallback when absent.")]
    public void OptionalWithFallback() {
        var provided     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var providedBody = provided.PropertiesOf(Request(currency: "USD"));
        RequiredField<Currency> providedCurrency = providedBody.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        Check.That(provided.New(s => s.Get(providedCurrency).Code).GetResultOrThrow()).IsEqualTo("USD");

        var absent     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var absentBody = absent.PropertiesOf(Request(currency: null));
        RequiredField<Currency> defaulted = absentBody.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        Check.That(absent.New(s => s.Get(defaulted).Code).GetResultOrThrow()).IsEqualTo("EUR");
    }

    [Fact(DisplayName = "An optional property that is present but invalid still records an error: optional never means malformed.")]
    public void OptionalPresentButInvalidRecords() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(currency: "EURO"));

        body.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");

        Outcome<string> outcome = bind.New(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
    }

    [Fact(DisplayName = "An optional fallback that does not convert is a developer bug and throws, naming the misconfigured argument.")]
    public void InvalidFallbackIsABug() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(currency: null));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => { body.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "NOT-A-CURRENCY"); });
        Check.That(exception.Message).Contains("Currency");
        // The exception must carry the converter's DIAGNOSTIC detail (the raw offending value), not the sanitized
        // public summary — so a developer can see WHY the configured fallback is rejected.
        Check.That(exception.Message).Contains("NOT-A-CURRENCY");
    }

    [Fact(DisplayName = "An optional reference property yields null when absent — recording nothing — and the value when present.")]
    public void OptionalReference() {
        var absent     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var absentBody = absent.PropertiesOf(Request(email: null));
        OptionalReferenceField<EmailAddress> none = absentBody.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        Check.That(absent.New(s => s.Get(none) is null).GetResultOrThrow()).IsTrue();

        var present     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var presentBody = present.PropertiesOf(Request());
        OptionalReferenceField<EmailAddress> some = presentBody.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        Check.That(present.New(s => s.Get(some)!.Value).GetResultOrThrow()).IsEqualTo("alice@example.org");
    }

    [Fact(DisplayName = "An optional reference property that is present but invalid records REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void OptionalReferencePresentButInvalidRecords() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(email: "nope"));

        body.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);

        Error invalid = bind.New(_ => "never").Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("GuestEmail");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
    }

    [Fact(DisplayName = "An optional value property yields a real null when absent — never default(T): an absent count is null, not 0.")]
    public void OptionalValueYieldsNullWhenAbsent() {
        var absent     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var absentBody = absent.PropertiesOf(Request(nights: null));
        OptionalValueField<int> none = absentBody.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
        // Project to a non-null bool: New's TCommand cannot itself be int? (Nullable<T> is not `notnull`);
        // a real consumer flows s.Get(none) straight into a command constructor argument instead.
        Outcome<bool> absentOutcome = absent.New(s => s.Get(none).HasValue);
        Check.That(absentOutcome.IsSuccess).IsTrue();
        Check.That(absentOutcome.GetResultOrThrow()).IsFalse();

        var present     = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var presentBody = present.PropertiesOf(Request(nights: "5"));
        OptionalValueField<int> some = presentBody.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
        Check.That(present.New(s => s.Get(some) ?? 0).GetResultOrThrow()).IsEqualTo(5);
    }

    [Fact(DisplayName = "An optional value property that is present but invalid records REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void OptionalValuePresentButInvalidRecords() {
        var bind = Bind.Request(BookingEnvelopeError.CommandInvalid);
        var body = bind.PropertiesOf(Request(nights: "-2"));

        body.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);

        Error invalid = bind.New(_ => "never").Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("MaxNights");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NOT_A_POSITIVE_NUMBER");
    }

}

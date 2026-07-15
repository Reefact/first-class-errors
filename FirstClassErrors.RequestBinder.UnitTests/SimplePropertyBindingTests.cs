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
        var bind = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);

        RequiredField<EmailAddress> email = bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.Build(s => s.Get(email).Value);
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("alice@example.org");
    }

    [Fact(DisplayName = "A required property that is missing fails the build with REQUEST_ARGUMENT_REQUIRED, carrying the argument path.")]
    public void RequiredMissingFails() {
        var bind = Bind.PropertiesOf(Request(email: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.Build(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.Code.ToString()).IsEqualTo("TEST_BOOKING_COMMAND_INVALID");

        Error required = outcome.Error!.InnerErrors.Single();
        Check.That(required.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(required)).IsEqualTo("GuestEmail");
    }

    [Fact(DisplayName = "A required property that is present but invalid fails with REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void RequiredInvalidWrapsTheConverterError() {
        var bind = Bind.PropertiesOf(Request(email: "not-an-email")).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        Outcome<string> outcome = bind.Build(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();

        Error invalid = outcome.Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("GuestEmail");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
    }

    [Fact(DisplayName = "A required property without conversion binds the raw value when present, and fails when missing.")]
    public void RequiredWithoutConversion() {
        var present = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<string> reference = present.SimpleProperty(r => r.Reference).AsRequired();
        Check.That(present.Build(s => s.Get(reference)).GetResultOrThrow()).IsEqualTo("REF-1");

        var missing = Bind.PropertiesOf(new BookingRequest(null, null, null, null, null, null, null)).FailWith(BookingEnvelopeError.CommandInvalid);
        missing.SimpleProperty(r => r.Reference).AsRequired();
        Outcome<string> outcome = missing.Build(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
    }

    [Fact(DisplayName = "An optional property with a fallback converts the provided value when present, and the fallback when absent.")]
    public void OptionalWithFallback() {
        var provided = Bind.PropertiesOf(Request(currency: "USD")).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<Currency> providedCurrency = provided.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        Check.That(provided.Build(s => s.Get(providedCurrency).Code).GetResultOrThrow()).IsEqualTo("USD");

        var absent = Bind.PropertiesOf(Request(currency: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        RequiredField<Currency> defaulted = absent.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");
        Check.That(absent.Build(s => s.Get(defaulted).Code).GetResultOrThrow()).IsEqualTo("EUR");
    }

    [Fact(DisplayName = "An optional property that is present but invalid still records an error: optional never means malformed.")]
    public void OptionalPresentButInvalidRecords() {
        var bind = Bind.PropertiesOf(Request(currency: "EURO")).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "EUR");

        Outcome<string> outcome = bind.Build(_ => "never");
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error!.InnerErrors.Single().Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
    }

    [Fact(DisplayName = "An optional fallback that does not convert is a developer bug and throws, naming the misconfigured argument.")]
    public void InvalidFallbackIsABug() {
        var bind = Bind.PropertiesOf(Request(currency: null)).FailWith(BookingEnvelopeError.CommandInvalid);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => { bind.SimpleProperty(r => r.Currency).AsOptional(Currency.Parse, "NOT-A-CURRENCY"); });
        Check.That(exception.Message).Contains("Currency");
        // The exception must carry the converter's DIAGNOSTIC detail (the raw offending value), not the sanitized
        // public summary — so a developer can see WHY the configured fallback is rejected.
        Check.That(exception.Message).Contains("NOT-A-CURRENCY");
    }

    [Fact(DisplayName = "An optional reference property yields null when absent — recording nothing — and the value when present.")]
    public void OptionalReference() {
        var absent = Bind.PropertiesOf(Request(email: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalReferenceField<EmailAddress> none = absent.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        Check.That(absent.Build(s => s.Get(none) is null).GetResultOrThrow()).IsTrue();

        var present = Bind.PropertiesOf(Request()).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalReferenceField<EmailAddress> some = present.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);
        Check.That(present.Build(s => s.Get(some)!.Value).GetResultOrThrow()).IsEqualTo("alice@example.org");
    }

    [Fact(DisplayName = "An optional reference property that is present but invalid records REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void OptionalReferencePresentButInvalidRecords() {
        var bind = Bind.PropertiesOf(Request(email: "nope")).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.GuestEmail).AsOptionalReference(EmailAddress.Parse);

        Error invalid = bind.Build(_ => "never").Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("GuestEmail");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_EMAIL_INVALID");
    }

    [Fact(DisplayName = "An optional value property yields a real null when absent — never default(T): an absent count is null, not 0.")]
    public void OptionalValueYieldsNullWhenAbsent() {
        var absent = Bind.PropertiesOf(Request(nights: null)).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalValueField<int> none = absent.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
        // Project to a non-null bool: Build's TCommand cannot itself be int? (Nullable<T> is not `notnull`);
        // a real consumer flows s.Get(none) straight into a command constructor argument instead.
        Outcome<bool> absentOutcome = absent.Build(s => s.Get(none).HasValue);
        Check.That(absentOutcome.IsSuccess).IsTrue();
        Check.That(absentOutcome.GetResultOrThrow()).IsFalse();

        var present = Bind.PropertiesOf(Request(nights: "5")).FailWith(BookingEnvelopeError.CommandInvalid);
        OptionalValueField<int> some = present.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);
        Check.That(present.Build(s => s.Get(some) ?? 0).GetResultOrThrow()).IsEqualTo(5);
    }

    [Fact(DisplayName = "An optional value property that is present but invalid records REQUEST_ARGUMENT_INVALID wrapping the converter's error.")]
    public void OptionalValuePresentButInvalidRecords() {
        var bind = Bind.PropertiesOf(Request(nights: "-2")).FailWith(BookingEnvelopeError.CommandInvalid);

        bind.SimpleProperty(r => r.MaxNights).AsOptionalValue(PositiveInt.Parse);

        Error invalid = bind.Build(_ => "never").Error!.InnerErrors.Single();
        Check.That(invalid.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_INVALID");
        Check.That(BindingAssertions.ArgumentPathOf(invalid)).IsEqualTo("MaxNights");
        Check.That(invalid.InnerErrors.Single().Code.ToString()).IsEqualTo("TEST_NOT_A_POSITIVE_NUMBER");
    }

}

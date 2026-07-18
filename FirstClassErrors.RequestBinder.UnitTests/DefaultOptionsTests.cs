#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     The application-wide <see cref="RequestBinderOptions.Default" />: it is what <see cref="Bind.PropertiesOf{TRequest}" />
///     binds with, configurable once at startup and frozen on first use. These tests inject it through the scoped,
///     parallel-safe test seam (<c>OverrideDefaultForTests</c>) so they never mutate the process default — the binder
///     suite keeps seeing the built-in default.
/// </summary>
public sealed class DefaultOptionsTests {

    private static BookingRequest MissingEmail() {
        return new BookingRequest(null, "R", null, null, null, null, null);
    }

    private static Error BindMissingEmail(RequestBinderEnvelopeStage<BookingRequest> start) {
        var bind = start.FailWith(BookingEnvelopeError.CommandInvalid);
        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        return bind.New(_ => "x").Error!.InnerErrors.Single();
    }

    // ── Bind.PropertiesOf binds with the configured default ───────────────────────────────────────────────

    [Fact(DisplayName = "Bind.PropertiesOf binds with the configured default options — naming and structural codes — without WithOptions.")]
    public void BindPropertiesOfUsesTheConfiguredDefault() {
        var configured = new RequestBinderOptions(new SnakeCaseNameProvider(),
                                                  ErrorCode.Create("ACME_ARGUMENT_REQUIRED"),
                                                  ErrorCode.Create("ACME_ARGUMENT_INVALID"));

        using (RequestBinderOptions.OverrideDefaultForTests(configured)) {
            Error error = BindMissingEmail(Bind.PropertiesOf(MissingEmail()));

            Check.That(error.Code.ToString()).IsEqualTo("ACME_ARGUMENT_REQUIRED");
            Check.That(BindingAssertions.ArgumentPathOf(error)).IsEqualTo("guest_email");
        }
    }

    [Fact(DisplayName = "Outside the configured scope, Bind.PropertiesOf falls back to the built-in default.")]
    public void OutsideScopeFallsBackToBuiltIn() {
        using (RequestBinderOptions.OverrideDefaultForTests(new RequestBinderOptions(new SnakeCaseNameProvider()))) { }

        Error error = BindMissingEmail(Bind.PropertiesOf(MissingEmail()));

        Check.That(error.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(BindingAssertions.ArgumentPathOf(error)).IsEqualTo("GuestEmail");
    }

    [Fact(DisplayName = "A per-call Bind.WithOptions overrides the configured default.")]
    public void WithOptionsWinsOverTheConfiguredDefault() {
        var appDefault = new RequestBinderOptions(new SnakeCaseNameProvider(),
                                                  ErrorCode.Create("DEFAULT_REQUIRED"),
                                                  ErrorCode.Create("DEFAULT_INVALID"));
        var perCall = new RequestBinderOptions(new SnakeCaseNameProvider(),
                                               ErrorCode.Create("PERCALL_REQUIRED"),
                                               ErrorCode.Create("PERCALL_INVALID"));

        using (RequestBinderOptions.OverrideDefaultForTests(appDefault)) {
            Error error = BindMissingEmail(Bind.WithOptions(perCall).PropertiesOf(MissingEmail()));

            Check.That(error.Code.ToString()).IsEqualTo("PERCALL_REQUIRED");
        }
    }

    // ── The default resolves to the built-in when unconfigured ────────────────────────────────────────────

    [Fact(DisplayName = "Unconfigured, RequestBinderOptions.Default is the built-in default (default structural codes).")]
    public void DefaultIsBuiltInWhenUnconfigured() {
        Check.That(RequestBinderOptions.Default.ArgumentRequiredCode == RequestBindingError.DefaultArgumentRequiredCode).IsTrue();
        Check.That(RequestBinderOptions.Default.ArgumentInvalidCode == RequestBindingError.DefaultArgumentInvalidCode).IsTrue();
    }

    // ── Setter contract: null-rejecting and frozen-after-first-use ────────────────────────────────────────

    [Fact(DisplayName = "Setting RequestBinderOptions.Default to null throws ArgumentNullException.")]
    public void SettingNullThrows() {
        Check.ThatCode(() => RequestBinderOptions.Default = null!).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Configuring RequestBinderOptions.Default after the first bind has read it throws — the default is frozen.")]
    public void ConfiguringAfterFirstUseThrows() {
        _ = RequestBinderOptions.Default; // reading it (as the first bind does) freezes it; idempotent

        Check.ThatCode(() => RequestBinderOptions.Default = new RequestBinderOptions(new SnakeCaseNameProvider()))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The test seam rejects a null options.")]
    public void OverrideForTestsRejectsNull() {
        Check.ThatCode(() => RequestBinderOptions.OverrideDefaultForTests(null!)).Throws<ArgumentNullException>();
    }

    private sealed class SnakeCaseNameProvider : IArgumentNameProvider {

        public string GetArgumentNameFrom(System.Reflection.PropertyInfo property) {
            return string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

    }

}

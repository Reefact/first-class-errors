#region Usings declarations

using System.Globalization;

using NFluent;

#endregion

namespace FirstClassErrors.RequestBinder.UnitTests;

/// <summary>
///     The binder's structural public messages are overridable and localizable through the same options seam as the
///     codes: a <see cref="BinderErrorDefinition" /> bundles a code with its message builder. These tests lock the
///     default rendering, the message-only override, the code-and-message override as one unit, the per-emission
///     localization, and the guarantee that custom options which never touch the definitions keep the shipped defaults.
/// </summary>
public sealed class StructuralMessageOverrideTests {

    private static BookingRequest MissingEmail() {
        return new BookingRequest(null, "REF-1", null, null, null, null, null);
    }

    private static Error BindMissingEmail(RequestBinderOptions? options = null) {
        RequestBinderEnvelopeStage<BookingRequest> start = options is null
                                                               ? Bind.PropertiesOf(MissingEmail())
                                                               : Bind.WithOptions(options).PropertiesOf(MissingEmail());

        var bind = start.FailWith(BookingEnvelopeError.CommandInvalid);
        bind.SimpleProperty(r => r.GuestEmail).AsRequired(EmailAddress.Parse);

        return bind.New(_ => "x").Error!.InnerErrors.Single();
    }

    // ── The default rendering is preserved (the "same output out of the box" guarantee) ───────────────────

    [Fact(DisplayName = "By default, a missing required argument carries the shipped English public messages.")]
    public void DefaultMessagesArePreserved() {
        Error error = BindMissingEmail();

        Check.That(error.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(error.ShortMessage).IsEqualTo("A required argument is missing.");
        Check.That(error.DetailedMessage).IsEqualTo("The argument 'GuestEmail' is required.");
    }

    // ── Overriding the messages, keeping the default code ─────────────────────────────────────────────────

    [Fact(DisplayName = "Overriding only the messages changes the public text while keeping the default code.")]
    public void MessageOverrideKeepsDefaultCode() {
        var options = new RequestBinderOptions(
            RequestBinderOptions.Default.ArgumentNameProvider,
            RequestBindingError.DefaultArgumentRequired.WithMessage(
                argumentPath => new BindingMessage("This field is mandatory.", $"Please provide '{argumentPath}'.")));

        Error error = BindMissingEmail(options);

        Check.That(error.Code == RequestBindingError.DefaultArgumentRequiredCode).IsTrue(); // code untouched
        Check.That(error.ShortMessage).IsEqualTo("This field is mandatory.");
        Check.That(error.DetailedMessage).IsEqualTo("Please provide 'GuestEmail'.");
    }

    // ── Overriding code and message together, as one coherent unit ────────────────────────────────────────

    [Fact(DisplayName = "A definition can carry a custom code and custom messages together; both flow to the raised error.")]
    public void CodeAndMessageOverrideTogether() {
        var options = new RequestBinderOptions(
            RequestBinderOptions.Default.ArgumentNameProvider,
            new BinderErrorDefinition(
                ErrorCode.Create("ACME_REQUIRED"),
                argumentPath => new BindingMessage("Champ obligatoire.", $"Le champ '{argumentPath}' est obligatoire.")));

        Error error = BindMissingEmail(options);

        Check.That(error.Code.ToString()).IsEqualTo("ACME_REQUIRED");
        Check.That(error.ShortMessage).IsEqualTo("Champ obligatoire.");
        Check.That(error.DetailedMessage).IsEqualTo("Le champ 'GuestEmail' est obligatoire.");
    }

    // ── Localization is per-emission: one options instance serves several languages ───────────────────────

    [Fact(DisplayName = "The message builder is evaluated per emission, so one options instance localizes by ambient culture.")]
    public void MessageIsLocalizedPerEmission() {
        var options = new RequestBinderOptions(
            RequestBinderOptions.Default.ArgumentNameProvider,
            RequestBindingError.DefaultArgumentRequired.WithMessage(Localized));

        CultureInfo original = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            Check.That(BindMissingEmail(options).ShortMessage).IsEqualTo("Un argument requis est manquant.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Check.That(BindMissingEmail(options).ShortMessage).IsEqualTo("A required argument is missing.");
        } finally {
            CultureInfo.CurrentUICulture = original;
        }
    }

    private static BindingMessage Localized(string argumentPath) {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                   ? new BindingMessage("Un argument requis est manquant.", $"L'argument '{argumentPath}' est requis.")
                   : new BindingMessage("A required argument is missing.", $"The argument '{argumentPath}' is required.");
    }

    // ── Custom options that never touch the definitions keep the shipped defaults ──────────────────────────

    [Fact(DisplayName = "Options built only for a naming policy keep the default code AND the default messages.")]
    public void CustomOptionsWithoutOverridesKeepDefaults() {
        var namingOnly = new RequestBinderOptions(RequestBinderOptions.Default.ArgumentNameProvider);

        Error error = BindMissingEmail(namingOnly);

        Check.That(error.Code.ToString()).IsEqualTo("REQUEST_ARGUMENT_REQUIRED");
        Check.That(error.ShortMessage).IsEqualTo("A required argument is missing.");
        Check.That(error.DetailedMessage).IsEqualTo("The argument 'GuestEmail' is required.");
    }

}

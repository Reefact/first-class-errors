#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorDescription))]
public sealed class ErrorDescriptionTests {

    [Fact(DisplayName = "An error description cannot be created with a null short message.")]
    public void AnErrorDescriptionCannotBeCreatedWithANullShortMessage() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(null!, DiagnosticMessageFactory.Any()))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An error description cannot be created with an empty or whitespace short message.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("        ")]
    public void AnErrorDescriptionCannotBeCreatedWithAnEmptyOrWhitespaceShortMessage(string value) {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(value, DiagnosticMessageFactory.Any()))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An error description cannot be created with a null diagnostic message.")]
    public void AnErrorDescriptionCannotBeCreatedWithANullDiagnosticMessage() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(ShortMessageFactory.Any(), null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An error description cannot be created with an empty or whitespace diagnostic message.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("        ")]
    public void AnErrorDescriptionCannotBeCreatedWithAnEmptyOrWhitespaceDiagnosticMessage(string value) {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(ShortMessageFactory.Any(), value))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An error description trims the short message.")]
    public void AnErrorDescriptionTrimsTheShortMessage() {
        // Exercise
        ErrorDescription description = new("  Short message  ", DiagnosticMessageFactory.Any());

        // Verify
        Check.That(description.ShortMessage).IsEqualTo("Short message");
    }

    [Fact(DisplayName = "An error description trims the diagnostic message.")]
    public void AnErrorDescriptionTrimsTheDiagnosticMessage() {
        // Exercise
        ErrorDescription description = new(ShortMessageFactory.Any(), "  Diagnostic message  ");

        // Verify
        Check.That(description.DiagnosticMessage).IsEqualTo("Diagnostic message");
    }

    [Fact(DisplayName = "An error description sets detailed message to null when not provided.")]
    public void AnErrorDescriptionSetsDetailedMessageToNullWhenNotProvided() {
        // Exercise
        ErrorDescription description = new(ShortMessageFactory.Any(), DiagnosticMessageFactory.Any());

        // Verify
        Check.That(description.DetailedMessage).IsNull();
    }

    [Theory(DisplayName = "An error description sets detailed message to null when it is empty or whitespace.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnErrorDescriptionSetsDetailedMessageToNullWhenItIsEmptyOrWhitespace(string value) {
        // Exercise
        ErrorDescription description = new(ShortMessageFactory.Any(), DiagnosticMessageFactory.Any(), value);

        // Verify
        Check.That(description.DetailedMessage).IsNull();
    }

    [Fact(DisplayName = "An error description trims the detailed message when provided.")]
    public void AnErrorDescriptionTrimsTheDetailedMessageWhenProvided() {
        // Exercise
        ErrorDescription description = new(ShortMessageFactory.Any(), DiagnosticMessageFactory.Any(), "  Detailed  ");

        // Verify
        Check.That(description.DetailedMessage).IsEqualTo("Detailed");
    }

    [Fact(DisplayName = "An error description preserves the short, diagnostic and detailed messages when valid.")]
    public void AnErrorDescriptionPreservesAllMessagesWhenValid() {
        // Exercise
        ErrorDescription description = new("Short", "Diagnostic", "Detailed");

        // Verify
        Check.That(description.ShortMessage).IsEqualTo("Short");
        Check.That(description.DiagnosticMessage).IsEqualTo("Diagnostic");
        Check.That(description.DetailedMessage).IsEqualTo("Detailed");
    }

}

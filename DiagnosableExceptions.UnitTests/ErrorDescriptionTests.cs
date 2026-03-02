#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(ErrorDescription))]
public sealed class ErrorDescriptionTests {

    [Fact(DisplayName = "An error description cannot be created with a null detailed message.")]
    public void AnErrorDescriptionCannotBeCreatedWithANullDetailedMessage() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An error description cannot be created with an empty or whitespace detailed message.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("        ")]
    public void AnErrorDescriptionCannotBeCreatedWithAnEmptyOrWhitespaceDetailedMessage(string value) {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDescription(value))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An error description trims the detailed message.")]
    public void AnErrorDescriptionTrimsTheDetailedMessage() {
        // Exercise
        ErrorDescription description = new("  Detailed message  ");

        // Verify
        Check.That(description.DetailedMessage).IsEqualTo("Detailed message");
    }

    [Fact(DisplayName = "An error description sets short message to null when not provided.")]
    public void AnErrorDescriptionSetsShortMessageToNullWhenNotProvided() {
        // Exercise
        ErrorDescription description = new("Detailed message");

        // Verify
        Check.That(description.ShortMessage).IsNull();
    }

    [Fact(DisplayName = "An error description sets short message to null when it is null.")]
    public void AnErrorDescriptionSetsShortMessageToNullWhenItIsNull() {
        // Exercise
        ErrorDescription description = new("Detailed message");

        // Verify
        Check.That(description.ShortMessage).IsNull();
    }

    [Theory(DisplayName = "An error description sets short message to null when it is empty or whitespace.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnErrorDescriptionSetsShortMessageToNullWhenItIsEmptyOrWhitespace(string value) {
        // Exercise
        ErrorDescription description = new("Detailed message", value);

        // Verify
        Check.That(description.ShortMessage).IsNull();
    }

    [Fact(DisplayName = "An error description trims the short message when provided.")]
    public void AnErrorDescriptionTrimsTheShortMessageWhenProvided() {
        // Exercise
        ErrorDescription description = new("Detailed message", "  Short  ");

        // Verify
        Check.That(description.ShortMessage).IsEqualTo("Short");
    }

    [Fact(DisplayName = "An error description preserves both detailed and short messages when valid.")]
    public void AnErrorDescriptionPreservesBothDetailedAndShortMessagesWhenValid() {
        // Exercise
        ErrorDescription description = new("Detailed", "Short");

        // Verify
        Check.That(description.DetailedMessage).IsEqualTo("Detailed");
        Check.That(description.ShortMessage).IsEqualTo("Short");
    }

}
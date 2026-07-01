#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorDocumentation))]
public sealed class ErrorDocumentationTests {

    [Fact(DisplayName = "ToString returns the documentation code when it is set.")]
    public void ToStringReturnsTheDocumentationCodeWhenItIsSet() {
        // Setup
        ErrorDocumentation documentation = new() { Code = "TEMPERATURE_BELOW_ABSOLUTE_ZERO" };

        // Exercise & verify
        Check.That(documentation.ToString()).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
    }

    [Fact(DisplayName = "ToString returns an empty string when the documentation code is not set.")]
    public void ToStringReturnsAnEmptyStringWhenTheDocumentationCodeIsNotSet() {
        // Setup
        ErrorDocumentation documentation = new();

        // Exercise & verify
        Check.That(documentation.ToString()).IsEqualTo(string.Empty);
    }

}

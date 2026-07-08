#region Usings declarations

using System.Globalization;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandHelpersTests {

    [Theory(DisplayName = "FirstNonEmpty returns the primary value when it is not blank.")]
    [InlineData("primary", "fallback", "primary")]
    [InlineData("primary", null, "primary")]
    public void FirstNonEmptyReturnsThePrimaryWhenPresent(string? primary, string? fallback, string expected) {
        // Exercise & verify
        Check.That(GenerateCommand.FirstNonEmpty(primary, fallback)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "FirstNonEmpty falls back when the primary is null, empty or whitespace.")]
    [InlineData(null, "fallback")]
    [InlineData("", "fallback")]
    [InlineData("   ", "fallback")]
    public void FirstNonEmptyFallsBackWhenThePrimaryIsBlank(string? primary, string fallback) {
        // Exercise & verify
        Check.That(GenerateCommand.FirstNonEmpty(primary, fallback)).IsEqualTo(fallback);
    }

    [Fact(DisplayName = "FirstNonEmpty returns null when both values are blank.")]
    public void FirstNonEmptyReturnsNullWhenBothAreBlank() {
        // Exercise & verify
        Check.That(GenerateCommand.FirstNonEmpty("  ", null)).IsNull();
    }

    [Theory(DisplayName = "NormalizeFormat lowercases, trims, and maps the 'md' alias to 'markdown'.")]
    [InlineData("md", "markdown")]
    [InlineData("  MD  ", "markdown")]
    [InlineData("JSON", "json")]
    [InlineData(" Html ", "html")]
    public void NormalizeFormatNormalizesAndMapsTheAlias(string input, string expected) {
        // Exercise & verify
        Check.That(GenerateCommand.NormalizeFormat(input)).IsEqualTo(expected);
    }

    [Fact(DisplayName = "ResolveCulture resolves a known culture name.")]
    public void ResolveCultureResolvesAKnownName() {
        // Exercise
        CultureInfo culture = GenerateCommand.ResolveCulture(" fr ");

        // Verify
        Check.That(culture.Name).IsEqualTo("fr");
    }

    [Fact(DisplayName = "ResolveCulture throws a clear error for an invalid culture name.")]
    public void ResolveCultureThrowsForAnInvalidName() {
        // "!!!" is not a well-formed culture name, so CultureInfo.GetCultureInfo raises CultureNotFoundException, which
        // ResolveCulture surfaces as an actionable InvalidOperationException. (A merely unknown-but-well-formed name is
        // leniently accepted by ICU as a custom culture and does not throw, so it cannot be used here.)
        Check.ThatCode(() => GenerateCommand.ResolveCulture("!!!"))
             .Throws<InvalidOperationException>()
             .WithMessage("Unknown language '!!!'. Use a culture name such as en, fr, es, de or sv.");
    }

}

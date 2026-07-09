#region Usings declarations

using System.Globalization;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.Cli.UnitTests;

[TestSubject(typeof(GenerateCommand))]
public sealed class GenerateCommandHelpersTests {

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

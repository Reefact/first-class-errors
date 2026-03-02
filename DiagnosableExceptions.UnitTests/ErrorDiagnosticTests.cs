#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(ErrorDiagnostic))]
public sealed class ErrorDiagnosticTests {

    [Fact(DisplayName = "An error diagnostic cannot be created with a null cause.")]
    public void AnErrorDiagnosticCannotBeCreatedWithANullCause() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDiagnostic(null!, ErrorCauseType.System, StringFactory.AnyAnalysisLead()))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "An error diagnostic cannot be created with a null analysis lead.")]
    public void AnErrorDiagnosticCannotBeCreatedWithANullAnalysisLead() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDiagnostic(StringFactory.AnyCause(), ErrorCauseType.Input, null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An error diagnostic cannot be created with an empty or whitespace cause.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public void AnErrorDiagnosticCannotBeCreatedWithAnEmptyOrWhitespaceCause(string value) {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDiagnostic(value, ErrorCauseType.System, StringFactory.AnyAnalysisLead()))
             .Throws<ArgumentException>();
    }

    [Theory(DisplayName = "An error diagnostic cannot be created with an empty or whitespace analysis lead.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public void AnErrorDiagnosticCannotBeCreatedWithAnEmptyOrWhitespaceAnalysisLead(string value) {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDiagnostic(StringFactory.AnyCause(), ErrorCauseType.System, value))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "An error diagnostic normalizes the cause by removing surrounding whitespace.")]
    public void AnErrorDiagnosticTrimsTheCause() {
        // Exercise
        ErrorDiagnostic diagnostic = new("  Invalid input.  ", ErrorCauseType.Input, StringFactory.AnyAnalysisLead());

        // Verify
        Check.That(diagnostic.Cause).IsEqualTo("Invalid input.");
    }

    [Fact(DisplayName = "An error diagnostic normalizes the analysis lead by removing surrounding whitespace.")]
    public void AnErrorDiagnosticTrimsTheAnalysisLead() {
        // Exercise
        ErrorDiagnostic diagnostic = new(StringFactory.AnyCause(), ErrorCauseType.System, "  Inspect payload  ");

        // Verify
        Check.That(diagnostic.AnalysisLead).IsEqualTo("Inspect payload");
    }

    [Fact(DisplayName = "An error diagnostic is defined by a cause, a cause type, and an analysis lead.")]
    public void AnErrorDiagnosticIsDefinedByACauseACauseTypeAndAnAnalysisLead() {
        // Exercise
        ErrorDiagnostic diagnostic = new("Invalid input.", ErrorCauseType.Input, "Check upstream system");

        // Verify
        Check.That(diagnostic.Cause).IsEqualTo("Invalid input.");
        Check.That(diagnostic.Type).IsEqualTo(ErrorCauseType.Input);
        Check.That(diagnostic.AnalysisLead).IsEqualTo("Check upstream system");
    }

    #region Nested types

    private static class StringFactory {

        #region Static members

        public static string AnyCause() {
            return "any cause";
        }

        public static string AnyAnalysisLead() {
            return "any analysis lead";
        }

        #endregion

    }

    #endregion

}
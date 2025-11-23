namespace Reefact.DiagnosableExceptions;

public class ___Temperature {

    #region Statics members declarations

    public static ___Temperature FromKelvin(decimal kelvin) {
        if (kelvin < 0) { throw TemperatureException.BelowAbsoluteZero(kelvin); }

        return new ___Temperature(kelvin);
    }

    #endregion

    #region Fields declarations

    private readonly decimal _kelvin;

    #endregion

    #region Constructors declarations

    private ___Temperature(decimal kelvin) {
        _kelvin = kelvin;
    }

    #endregion

    #region Nested types declarations

    [DocErrorCodePrefix("TEMPERATURE")]
    public class TemperatureException : DomainException {

        #region Statics members declarations

        [DocError(
            Title = "Below absolute zero",
            Explanation = "This error occurs when trying to initialize a temperature with a value that is below absolute zero.",
            DiagnosticType = typeof(___ValueObjectDiagnostic),
            DiagnosticMemberName = nameof(___ValueObjectDiagnostic.Diagnostic)
        )]
        public static TemperatureException BelowAbsoluteZero([DocExample(-300)] decimal invalidValue) {
            const string code = "BELOW_ABSOLUTE_ZERO";

            ErrorDescription description = new(
                $"Failed to initialize temperature with value {invalidValue}K.",
                "Value is too low.",
                "A temperature cannot be lower than absolute zero.");

            var details = new { Value = invalidValue };

            return new TemperatureException(code, description, details);
        }

        #endregion

        #region Constructors declarations

        /// <inheritdoc />
        private TemperatureException(string errorCode, ErrorDescription description, object? errorDetails = null) : base(errorCode, description, errorDetails) { }

        #endregion

    }

    #endregion

}
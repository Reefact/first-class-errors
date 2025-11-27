namespace Reefact.DiagnosableExceptions.Usage;

[ErrorCodePrefix("TEMPERATURE")]
public class TemperatureException : DomainException {

    #region Statics members declarations

    [ErrorDocumentation(
        Title = "Below absolute zero",
        Explanation = "This error occurs when trying to instantiate a temperature with a value that is below absolute zero.",
        BusinessRule = "Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.",
        DiagnosticType = typeof(ValueObjectDiagnostic),
        DiagnosticMemberName = nameof(ValueObjectDiagnostic.Diagnostic)
    )]
    [ErrorExample(-1, TemperatureUnit.Kelvin)]
    [ErrorExample(-280, TemperatureUnit.Celsius)]
    public static TemperatureException BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        const string code    = "BELOW_ABSOLUTE_ZERO";
        const string message = "Failed to instantiate temperature: value is too low.";
        ErrorContext context = new(1, new { Value = invalidValue, Unit = invalidValueUnit });

        return new TemperatureException(code, message, context);
    }

    #endregion

    #region Constructors declarations

    /// <inheritdoc />
    private TemperatureException(string errorCode, string message, ErrorContext? errorDetails = null) : base(errorCode, message, errorDetails) { }

    #endregion

}
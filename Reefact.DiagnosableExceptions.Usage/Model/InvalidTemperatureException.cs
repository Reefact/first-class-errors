namespace Reefact.DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents an exception that is thrown when an attempt is made to create a temperature with an invalid value.
/// </summary>
[ErrorFor(typeof(Temperature))]
public sealed class InvalidTemperatureException : DomainException {

    #region Static members

    /// <summary>
    ///     Creates an <see cref="InvalidTemperatureException" /> indicating that the temperature value is below absolute zero.
    /// </summary>
    /// <param name="invalidValue">The invalid temperature value that caused the exception.</param>
    /// <param name="invalidValueUnit">The unit of the invalid temperature value.</param>
    /// <returns>An instance of <see cref="InvalidTemperatureException" />.</returns>
    /// >
    [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
    internal static InvalidTemperatureException BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        return new InvalidTemperatureException(
            "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            new ErrorDescription {
                ShortMessage    = "Temperature below absolute zero.",
                DetailedMessage = DocumentationFormatter.Format("Failed to instantiate temperature: the value {0}{1} is below absolute zero.", invalidValue, invalidValueUnit)
            });
    }

    private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
        return Describe.Error(nameof(BelowAbsoluteZero))
                       .WithTitle("Temperature below absolute zero")
                       .WithExplanation("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                       .WithBusinessRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                       .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                       .WithExamples(
                            () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                            () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
    }

    #endregion

    #region Constructors & Destructor

    /// <inheritdoc />
    private InvalidTemperatureException(string errorCode, ErrorDescription errorDescription) : base(errorCode, errorDescription) { }

    #endregion

}
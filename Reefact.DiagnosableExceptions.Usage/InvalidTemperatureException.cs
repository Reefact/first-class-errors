namespace Reefact.DiagnosableExceptions.Usage;

/// <summary>
///     Represents an exception that is thrown when an attempt is made to create a temperature
///     with an invalid value.
/// </summary>
/// <remarks>
///     This exception is used to enforce business rules related to temperature values. For example,
///     it ensures that temperatures adhere to physical constraints, such as not being below absolute zero.
/// </remarks>
[ExceptionOf(typeof(Temperature))]
public class InvalidTemperatureException : DomainException {

    #region Statics members declarations

    /// <summary>
    ///     Creates a new instance of <see cref="InvalidTemperatureException" /> for a temperature value below absolute zero.
    /// </summary>
    /// <param name="invalidValue">The invalid temperature value that caused the exception.</param>
    /// <param name="invalidValueUnit">The unit of the invalid temperature value.</param>
    /// <returns>An instance of <see cref="InvalidTemperatureException" /> with detailed error information.</returns>
    /// <remarks>
    ///     This exception is thrown when attempting to instantiate a temperature with a value below absolute zero.
    ///     Absolute zero is the point where particles have the minimum possible energy, and temperatures cannot go below this
    ///     limit.
    /// </remarks>
    /// <example>
    ///     The following examples demonstrate invalid temperature values that would trigger this exception:
    ///     <code>
    /// throw InvalidTemperatureException.BelowAbsoluteZero(-1, TemperatureUnit.Kelvin);
    /// throw InvalidTemperatureException.BelowAbsoluteZero(-280, TemperatureUnit.Celsius);
    /// </code>
    /// </example>
    [ErrorDocumentation(
        Title = "Temperature below absolute zero",
        Explanation = "This error occurs when trying to instantiate a temperature with a value that is below absolute zero.",
        BusinessRule = "Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.",
        DiagnosticType = typeof(ValueObjectDiagnostic),
        DiagnosticMemberName = nameof(ValueObjectDiagnostic.Diagnostic)
    )]
    [ErrorExample(-1, TemperatureUnit.Kelvin)]
    [ErrorExample(-280, TemperatureUnit.Celsius)]
    internal static InvalidTemperatureException BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        const string code                   = "TEMPERATURE_BELOW_ABSOLUTE_ZERO";
        string       invalidValueUnitSymbol = invalidValueUnit.ToSymbol();
        ErrorContext context                = new(1, new { Value = invalidValue, Unit = invalidValueUnitSymbol });
        string       message                = $"Failed to instantiate temperature: the value {invalidValue}{invalidValueUnitSymbol} is below absolute zero.";

        return new InvalidTemperatureException(code, message, context);
    }

    #endregion

    #region Constructors declarations

    /// <inheritdoc />
    private InvalidTemperatureException(string errorCode, string message, ErrorContext? errorDetails = null) : base(errorCode, message, errorDetails) { }

    #endregion

}
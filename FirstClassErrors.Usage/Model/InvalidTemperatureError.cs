#region Usings declarations

using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Represents a domain error produced when an attempt is made to create a temperature with an invalid value.
/// </summary>
[ProvidesErrorsFor(nameof(Temperature),
                   Description = "Errors raised when constructing a Temperature value from an out-of-range input.")]
public static class InvalidTemperatureError {

    #region Statics members declarations

    /// <summary>
    ///     Creates a <see cref="InvalidTemperatureError" /> indicating that the temperature value is below absolute zero.
    /// </summary>
    /// <param name="invalidValue">The invalid temperature value that caused the error.</param>
    /// <param name="invalidValueUnit">The unit of the invalid temperature value.</param>
    /// <returns>An instance of <see cref="InvalidTemperatureError" />.</returns>
    [DocumentedBy(nameof(BelowAbsoluteZeroDocumentation))]
    internal static DomainError BelowAbsoluteZero(decimal invalidValue, TemperatureUnit invalidValueUnit) {
        return DomainError.Create(
                               Code.TemperatureBelowAbsoluteZero,
                               DocumentationFormatter.Format("Failed to instantiate temperature: the value {0} {1} is below absolute zero.", invalidValue, invalidValueUnit))
                          .WithPublicMessage(
                               "Temperature is invalid.",
                               DocumentationFormatter.Format("The temperature {0} {1} is below absolute zero.", invalidValue, invalidValueUnit));
    }

    private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
        return DescribeError.WithTitle("Temperature below absolute zero")
                            .WithDescription("This error occurs when trying to instantiate a temperature with a value that is below absolute zero.")
                            .WithRule("Temperature cannot go below absolute zero because absolute zero is the point where particles have minimum possible energy.")
                            .WithDiagnostics(ValueObjectDiagnostic.Diagnostic)
                            .WithExamples(
                                 () => BelowAbsoluteZero(-1, TemperatureUnit.Kelvin),
                                 () => BelowAbsoluteZero(-280, TemperatureUnit.Celsius));
    }

    #endregion

    #region Nested types declarations

    private static class Code {

        #region Statics members declarations

        public static readonly ErrorCode TemperatureBelowAbsoluteZero = ErrorCode.Create("TEMPERATURE_BELOW_ABSOLUTE_ZERO");

        #endregion

    }

    #endregion

}
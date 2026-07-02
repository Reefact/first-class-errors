#region Usings declarations

using FirstClassErrors.Usage.Resources;
using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Represents a domain error produced when an attempt is made to create a temperature with an invalid value.
/// </summary>
[ProvidesErrorsFor(nameof(Temperature),
                   Description = "Temperature_Source",
                   DescriptionResourceType = typeof(UsageErrorMessages))]
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
        return new DomainError(
            Code.TemperatureBelowAbsoluteZero,
            DocumentationFormatter.Format(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Message"), invalidValue, invalidValueUnit),
            UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_ShortMessage"));
    }

    private static ErrorDocumentation BelowAbsoluteZeroDocumentation() {
        return DescribeError.WithTitle(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Title"))
                            .WithDescription(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Description"))
                            .WithRule(UsageErrorMessages.Get("Temperature_BelowAbsoluteZero_Rule"))
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
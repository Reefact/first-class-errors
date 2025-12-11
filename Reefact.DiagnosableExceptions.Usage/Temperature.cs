#region Usings declarations

using System.Diagnostics;

#endregion

namespace Reefact.DiagnosableExceptions.Usage;

/// <summary>
///     Represents an immutable temperature value.
/// </summary>
/// <remarks>
///     Use the static factory methods <see cref="FromKelvin(decimal)" />,
///     <see cref="TryFromKelvin(decimal)" />, <see cref="FromCelsius(decimal)" />,
///     or <see cref="TryFromCelsius(decimal)" /> to create instances.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class Temperature {

    private const decimal AbsoluteZeroInKelvin  = 0;
    private const decimal CelsiusToKelvinOffset = 273.15m;

    #region Statics members declarations

    /// <summary>
    ///     Represents the absolute zero temperature, the lowest possible temperature where particles have minimal energy.
    /// </summary>
    public static readonly Temperature AbsoluteZero = FromKelvin(AbsoluteZeroInKelvin);

    /// <summary>
    ///     Creates a <see cref="Temperature" /> from a kelvin value.
    /// </summary>
    /// <param name="kelvin">Temperature in kelvin.</param>
    /// <returns>A new <see cref="Temperature" /> representing the specified kelvin value.</returns>
    /// <exception cref="Reefact.DiagnosableExceptions.Usage.InvalidTemperatureException">
    ///     Thrown when <paramref name="kelvin" /> is lower than absolute zero.
    /// </exception>
    public static Temperature FromKelvin(decimal kelvin) {
        if (IsLowerThanAbsoluteZero(kelvin)) { throw InvalidTemperatureException.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin); }

        return new Temperature(kelvin);
    }

    /// <summary>
    ///     Attempts to create a <see cref="Temperature" /> from a kelvin value.
    /// </summary>
    /// <param name="kelvin">Temperature in kelvin.</param>
    /// <returns>
    ///     A <c>TryOutcome&lt;Temperature&gt;</c> that is successful when <paramref name="kelvin" />
    ///     is not below absolute zero; otherwise a failure containing the corresponding
    ///     <see cref="InvalidTemperatureException" />.
    /// </returns>
    public static TryOutcome<Temperature> TryFromKelvin(decimal kelvin) {
        if (IsLowerThanAbsoluteZero(kelvin)) { return TryOutcome<Temperature>.Failure(InvalidTemperatureException.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin)); }

        return TryOutcome<Temperature>.Success(new Temperature(kelvin));
    }

    /// <summary>
    ///     Creates a <see cref="Temperature" /> from a celsius value.
    /// </summary>
    /// <param name="celsius">Temperature in degrees Celsius.</param>
    /// <returns>A new <see cref="Temperature" /> representing the specified Celsius value.</returns>
    /// <exception cref="Reefact.DiagnosableExceptions.Usage.InvalidTemperatureException">
    ///     Thrown when the converted kelvin value is lower than absolute zero.
    /// </exception>
    public static Temperature FromCelsius(decimal celsius) {
        decimal kelvin = celsius + CelsiusToKelvinOffset;
        if (IsLowerThanAbsoluteZero(kelvin)) { throw InvalidTemperatureException.BelowAbsoluteZero(celsius, TemperatureUnit.Celsius); }

        return new Temperature(kelvin);
    }

    /// <summary>
    ///     Attempts to create a <see cref="Temperature" /> from a Celsius value.
    /// </summary>
    /// <param name="celsius">Temperature in degrees Celsius.</param>
    /// <returns>
    ///     A <c>TryOutcome&lt;Temperature&gt;</c> that is successful when the Celsius value
    ///     is not below absolute zero; otherwise a failure containing the corresponding
    ///     <see cref="InvalidTemperatureException" />.
    /// </returns>
    public static TryOutcome<Temperature> TryFromCelsius(decimal celsius) {
        decimal kelvin = celsius + CelsiusToKelvinOffset;
        if (IsLowerThanAbsoluteZero(kelvin)) { return TryOutcome<Temperature>.Failure(InvalidTemperatureException.BelowAbsoluteZero(celsius, TemperatureUnit.Celsius)); }

        return TryOutcome<Temperature>.Success(new Temperature(kelvin));
    }

    private static bool IsLowerThanAbsoluteZero(decimal kelvin) {
        return kelvin < AbsoluteZeroInKelvin;
    }

    #endregion

    #region Fields declarations

    private readonly decimal _kelvin;

    #endregion

    #region Constructors declarations

    private Temperature(decimal kelvin) {
        _kelvin = kelvin;
    }

    #endregion

    /// <summary>
    ///     Returns the temperature value expressed in kelvin.
    /// </summary>
    /// <returns>The temperature in kelvin.</returns>
    public decimal ToKelvin() {
        return _kelvin;
    }

    /// <summary>
    ///     Returns the temperature value expressed in degrees Celsius.
    /// </summary>
    /// <returns>The temperature in degrees Celsius.</returns>
    public decimal ToCelsius() {
        decimal celsius = _kelvin - CelsiusToKelvinOffset;

        return celsius;
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{_kelvin} K";
    }

}
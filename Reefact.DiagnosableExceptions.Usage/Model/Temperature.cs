#region Usings declarations

using System.Diagnostics;

#endregion

namespace Reefact.DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents a temperature.
/// </summary>
/// <remarks>
///     Represents a minimal and intentionally simplified model used only to illustrate
///     how documentation attributes are applied in a concrete domain scenario.
///     This type is not intended to be a full or production-ready Value Object implementation.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class Temperature : IEquatable<Temperature>, IComparable<Temperature>, IComparable {

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
    /// <exception cref="InvalidTemperatureException">
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
    /// <exception cref="InvalidTemperatureException">
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

    public static bool operator ==(Temperature? left, Temperature? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Temperature? left, Temperature? right) {
        return !Equals(left, right);
    }

    public static bool operator <(Temperature? left, Temperature? right) {
        return Comparer<Temperature>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(Temperature? left, Temperature? right) {
        return Comparer<Temperature>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(Temperature? left, Temperature? right) {
        return Comparer<Temperature>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(Temperature? left, Temperature? right) {
        return Comparer<Temperature>.Default.Compare(left, right) >= 0;
    }

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

    /// <inheritdoc />
    public bool Equals(Temperature? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return _kelvin == other._kelvin;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is Temperature other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return _kelvin.GetHashCode();
    }

    /// <inheritdoc />
    public int CompareTo(Temperature? other) {
        if (ReferenceEquals(this, other)) { return 0; }
        if (other is null) { return 1; }

        return _kelvin.CompareTo(other._kelvin);
    }

    /// <inheritdoc />
    public int CompareTo(object? obj) {
        if (obj is null) { return 1; }
        if (ReferenceEquals(this, obj)) { return 0; }

        return obj is Temperature other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Temperature)}");
    }

}
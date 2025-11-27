#region Usings declarations

using System.Diagnostics;

#endregion

namespace Reefact.DiagnosableExceptions.Usage;

[DebuggerDisplay("{ToString()}")]
public sealed class Temperature {

    private const decimal AbsoluteZeroInKelvin  = 0;
    private const decimal CelsiusToKelvinOffset = 273.15m;

    #region Statics members declarations

    public static Temperature FromKelvin(decimal kelvin) {
        if (IsLowerThanAbsoluteZero(kelvin)) { throw TemperatureException.BelowAbsoluteZero(kelvin, TemperatureUnit.Kelvin); }

        return new Temperature(kelvin);
    }

    public static Temperature FromCelsius(decimal celsius) {
        decimal kelvin = celsius + CelsiusToKelvinOffset;
        if (IsLowerThanAbsoluteZero(kelvin)) { throw TemperatureException.BelowAbsoluteZero(celsius, TemperatureUnit.Celsius); }

        return new Temperature(kelvin);
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

    public decimal ToKelvin() {
        return _kelvin;
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{_kelvin} K";
    }

}
namespace Reefact.DiagnosableExceptions.Usage;

public static class TemperatureUnitExtensions {

    #region Statics members declarations

    public static string ToSymbol(this TemperatureUnit unit) {
        return unit switch {
            TemperatureUnit.Kelvin  => "K",
            TemperatureUnit.Celsius => "°C",
            _                       => unit.ToString()
        };
    }

    #endregion

}
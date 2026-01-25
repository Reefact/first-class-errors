namespace Reefact.DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents a currency.
/// </summary>
/// <remarks>
///     Represents a minimal and intentionally simplified model used only to illustrate
///     how documentation attributes are applied in a concrete domain scenario.
///     This type is not intended to be a full or production-ready Value Object implementation.
/// </remarks>
public sealed class Currency : IEquatable<Currency> {

    #region Static members

    public static readonly Currency EUR = new("EUR");
    public static readonly Currency USD = new("USD");

    #endregion

    #region Fields

    private readonly string _code;

    #endregion

    #region Constructors & Destructor

    private Currency(string code) {
        _code = code;
    }

    #endregion

    public bool Equals(Currency? other) {
        return other is not null && _code == other._code;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as Currency);
    }

    public override int GetHashCode() {
        return _code.GetHashCode();
    }

    public override string ToString() {
        return _code;
    }

}
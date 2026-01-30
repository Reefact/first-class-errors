#region Usings declarations

using DiagnosableExceptions.Usage.Utils;

#endregion

namespace DiagnosableExceptions.Usage.Model;

/// <summary>
///     Represents a monetary amount.
/// </summary>
/// <remarks>
///     Represents a minimal and intentionally simplified model used only to illustrate
///     how documentation attributes are applied in a concrete domain scenario.
///     This type is not intended to be a full or production-ready Value Object implementation.
/// </remarks>
public sealed class Amount : IEquatable<Amount>, IComparable<Amount> {

    #region Constructors & Destructor

    public Amount(decimal value, Currency currency) {
        ArgumentNullException.ThrowIfNull(currency);

        Value    = value;
        Currency = currency;
    }

    #endregion

    public decimal  Value    { get; }
    public Currency Currency { get; }

    public Amount Add(Amount other) {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return new Amount(Value + other.Value, Currency);
    }

    public Amount Subtract(Amount other) {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return new Amount(Value - other.Value, Currency);
    }

    public bool IsGreaterThan(Amount other) {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return Value > other.Value;
    }

    public bool IsLessThan(Amount other) {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return Value < other.Value;
    }

    /// <inheritdoc />
    public bool Equals(Amount? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return Value == other.Value && Currency.Equals(other.Currency);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is Amount other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return HashCode.Combine(Value, Currency);
    }

    public int CompareTo(Amount? other) {
        if (other is null) { return 1; }

        EnsureSameCurrency(other);

        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public override string ToString() {
        return DocumentationFormatter.Format("{0} {1}", Value, Currency);
    }

    private void EnsureSameCurrency(Amount other) {
        if (!Currency.Equals(other.Currency)) { throw InvalidAmountOperationException.CurrencyMismatch(this, other); }
    }

}
#region Usings declarations

using System.Diagnostics;

using FirstClassErrors.Usage.Utils;

#endregion

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Represents a monetary amount.
/// </summary>
/// <remarks>
///     Represents a minimal and intentionally simplified model used only to illustrate
///     how documentation attributes are applied in a concrete domain scenario.
///     This type is not intended to be a full or production-ready Value Object implementation.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public sealed class Amount : IEquatable<Amount>, IComparable<Amount> {

    #region Constructors declarations

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

    // A type that is IComparable should also carry the comparison operators (S1210). Ordering mismatched currencies is
    // undefined, so the ordering operators surface it the same way CompareTo/IsLessThan do — by throwing.
    public static bool operator ==(Amount? left, Amount? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Amount? left, Amount? right) {
        return !Equals(left, right);
    }

    public static bool operator <(Amount? left, Amount? right) {
        return Compare(left, right) < 0;
    }

    public static bool operator >(Amount? left, Amount? right) {
        return Compare(left, right) > 0;
    }

    public static bool operator <=(Amount? left, Amount? right) {
        return Compare(left, right) <= 0;
    }

    public static bool operator >=(Amount? left, Amount? right) {
        return Compare(left, right) >= 0;
    }

    private static int Compare(Amount? left, Amount? right) {
        if (ReferenceEquals(left, right)) { return 0; }
        if (left is null) { return -1; }

        return left.CompareTo(right);
    }

    /// <inheritdoc />
    public override string ToString() {
        return DocumentationFormatter.Format("{0} {1}", Value, Currency);
    }

    private void EnsureSameCurrency(Amount other) {
        if (!Currency.Equals(other.Currency)) { throw InvalidAmountOperationError.CurrencyMismatch(this, other).ToException(); }
    }

}
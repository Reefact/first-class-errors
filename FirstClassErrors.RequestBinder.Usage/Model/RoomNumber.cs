namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A hotel room number in the range 1–999 — a second <b>value-type</b> value object over <see cref="int" />. It
///     exists to show binding a <b>list of value-type properties</b> (<c>IReadOnlyList&lt;int?&gt;</c>): each element is
///     converted through <see cref="From" /> over the underlying <see cref="int" />, and a <c>null</c> element is
///     reported under its indexed path.
/// </summary>
/// <remarks>
///     See <see cref="NightCount" /> for the note on why a consumer may model a value object as a <c>struct</c> while
///     the library keeps its own as classes.
/// </remarks>
public readonly struct RoomNumber : IEquatable<RoomNumber> {

    #region Constants

    private const int Lowest  = 1;
    private const int Highest = 999;

    #endregion

    #region Properties declarations

    /// <summary>The validated room number (always within 1–999 when obtained through <see cref="From" />).</summary>
    public int Value { get; private init; }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Creates a <see cref="RoomNumber" /> from <paramref name="number" />, or fails with a documented
    ///     <see cref="InvalidRoomNumberError" /> when it is outside the 1–999 range.
    /// </summary>
    public static Outcome<RoomNumber> From(int number) {
        if (number is < Lowest or > Highest) {
            return Outcome<RoomNumber>.Failure(InvalidRoomNumberError.OutOfRange(number));
        }

        return Outcome<RoomNumber>.Success(new RoomNumber { Value = number });
    }

    #endregion

    /// <inheritdoc />
    public bool Equals(RoomNumber other) {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is RoomNumber other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return Value;
    }

    /// <inheritdoc />
    public override string ToString() {
        return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

}

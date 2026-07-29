#region Usings declarations

using System.Globalization;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A calendar date on a booking (check-in / check-out) — a reference-type value object parsed from an ISO
///     <c>yyyy-MM-dd</c> request string. Bound inside the nested <c>Stay</c> binder.
/// </summary>
public sealed class BookingDate : IEquatable<BookingDate>, IComparable<BookingDate> {

    #region Constructors declarations

    private BookingDate(DateOnly value) {
        Value = value;
    }

    #endregion

    /// <summary>The validated calendar date.</summary>
    public DateOnly Value { get; }

    #region Statics members declarations

    /// <summary>
    ///     Parses <paramref name="raw" /> (an ISO <c>yyyy-MM-dd</c> date) into a <see cref="BookingDate" />, or fails
    ///     with a documented <see cref="InvalidBookingDateError" />.
    /// </summary>
    public static Outcome<BookingDate> Parse(string raw) {
        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)) {
            return Outcome<BookingDate>.Failure(InvalidBookingDateError.Malformed(raw));
        }

        return Outcome<BookingDate>.Success(new BookingDate(parsed));
    }

    #endregion

    // A reference-type value object gets `==` from the language, comparing REFERENCES, while Equals compares
    // values — the two would disagree silently on two instances of the same date. Declaring the operators is
    // what keeps them saying the same thing. The ordering operators follow CompareTo, including its verdict
    // that a null sorts before any date.

    /// <summary>
    ///     Determines whether two <see cref="BookingDate" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="BookingDate" /> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(BookingDate? left, BookingDate? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="BookingDate" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="BookingDate" /> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(BookingDate? left, BookingDate? right) {
        return !Equals(left, right);
    }

    /// <summary>Determines whether <paramref name="left" /> falls strictly before <paramref name="right" />.</summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> is earlier; otherwise, <c>false</c>.</returns>
    public static bool operator <(BookingDate? left, BookingDate? right) {
        return Compare(left, right) < 0;
    }

    /// <summary>Determines whether <paramref name="left" /> falls before <paramref name="right" /> or on the same day.</summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> is earlier or equal; otherwise, <c>false</c>.</returns>
    public static bool operator <=(BookingDate? left, BookingDate? right) {
        return Compare(left, right) <= 0;
    }

    /// <summary>Determines whether <paramref name="left" /> falls strictly after <paramref name="right" />.</summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> is later; otherwise, <c>false</c>.</returns>
    public static bool operator >(BookingDate? left, BookingDate? right) {
        return Compare(left, right) > 0;
    }

    /// <summary>Determines whether <paramref name="left" /> falls after <paramref name="right" /> or on the same day.</summary>
    /// <param name="left">The first <see cref="BookingDate" /> instance to compare.</param>
    /// <param name="right">The second <see cref="BookingDate" /> instance to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> is later or equal; otherwise, <c>false</c>.</returns>
    public static bool operator >=(BookingDate? left, BookingDate? right) {
        return Compare(left, right) >= 0;
    }

    // The one place the ordering operators agree on how a null compares, so all four cannot drift apart.
    private static int Compare(BookingDate? left, BookingDate? right) {
        if (ReferenceEquals(left, right)) { return 0; }
        if (left is null) { return -1; }

        return left.CompareTo(right);
    }

    /// <inheritdoc />
    public int CompareTo(BookingDate? other) {
        if (other is null) { return 1; }

        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public bool Equals(BookingDate? other) {
        if (other is null) { return false; }
        if (ReferenceEquals(this, other)) { return true; }

        return Value.Equals(other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is BookingDate other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return Value.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString() {
        return Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

}

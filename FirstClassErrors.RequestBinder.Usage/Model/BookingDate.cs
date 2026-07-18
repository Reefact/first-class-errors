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
        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", out DateOnly parsed)) {
            return Outcome<BookingDate>.Failure(InvalidBookingDateError.Malformed(raw));
        }

        return Outcome<BookingDate>.Success(new BookingDate(parsed));
    }

    #endregion

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
        return Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

}

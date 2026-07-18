namespace FirstClassErrors.RequestBinder.Usage.Model;

/// <summary>
///     A validated stay: a check-in and a check-out date, where check-out is strictly after check-in. It is built by a
///     <b>validating factory</b> (<see cref="Create" />) that enforces the cross-field rule no single field could check
///     on its own — the case the binder's <c>Create</c> terminal exists for.
/// </summary>
public sealed record Stay {

    #region Constructors declarations

    private Stay(BookingDate checkIn, BookingDate checkOut) {
        CheckIn  = checkIn;
        CheckOut = checkOut;
    }

    #endregion

    /// <summary>The (inclusive) check-in date.</summary>
    public BookingDate CheckIn { get; }

    /// <summary>The (exclusive) check-out date, strictly after <see cref="CheckIn" />.</summary>
    public BookingDate CheckOut { get; }

    #region Statics members declarations

    /// <summary>
    ///     Builds a <see cref="Stay" /> from two already-parsed dates, or fails with a documented
    ///     <see cref="InvalidStayError" /> when check-out is not strictly after check-in. This is the
    ///     <c>Command.Create(...)</c> the binder's <c>Create</c> terminal flattens, so the cross-field failure surfaces
    ///     directly instead of nesting a second <see cref="Outcome{T}" />.
    /// </summary>
    public static Outcome<Stay> Create(BookingDate checkIn, BookingDate checkOut) {
        if (checkOut.Value <= checkIn.Value) {
            return Outcome<Stay>.Failure(InvalidStayError.CheckOutNotAfterCheckIn(checkIn, checkOut));
        }

        return Outcome<Stay>.Success(new Stay(checkIn, checkOut));
    }

    #endregion

}

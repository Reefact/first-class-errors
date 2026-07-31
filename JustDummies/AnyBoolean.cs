namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="bool" /> values. <c>True()</c> and <c>False()</c> pin the value —
///     mostly useful for symmetry when a test sweeps cases — and contradictory pins fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides, like every other generator.
/// </summary>
public sealed class AnyBoolean : IAny<bool>, IHasRandomSource, ICardinalityHint<bool> {

    /// <summary>How many values <see cref="bool" /> has: <c>false</c> and <c>true</c>, and nothing else.</summary>
    private const int BooleanValueCount = 2;

    /// <summary>How many values a pin leaves producible — the one it fixed.</summary>
    private const int PinnedCardinality = 1;

    #region Statics members declarations

    internal static AnyBoolean Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyBoolean(source, null, null);
    }

    private static string V(bool value) {
        return value ? "true" : "false";
    }

    #endregion

    #region Fields declarations

    private readonly bool?        _pinned;
    private readonly ConstraintCall? _pinnedConstraint;
    private readonly RandomSource _source;

    #endregion

    private AnyBoolean(RandomSource source, bool? pinned, ConstraintCall? pinnedConstraint) {
        _source           = source;
        _pinned           = pinned;
        _pinnedConstraint = pinnedConstraint;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Two distinct values unless a pin has already fixed one of them.
    long? ICardinalityHint<bool>.DistinctCardinality => _pinned is null ? BooleanValueCount : PinnedCardinality;

    // A pin narrows the domain to that single value; unpinned, both booleans are producible.
    bool ICardinalityHint<bool>.Contains(bool value) => _pinned is not bool pinned || pinned == value;

    /// <summary>Pins the value to <c>true</c>.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBoolean True() {
        return Pin(true, ConstraintCall.Of(nameof(True)));
    }

    /// <summary>Pins the value to <c>false</c>.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBoolean False() {
        return Pin(false, ConstraintCall.Of(nameof(False)));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — which, for a boolean, pins it to the
    ///     opposite. The name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBoolean DifferentFrom(bool value) {
        return Pin(!value, ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public bool Generate() {
        return _pinned ?? _source.Current.Next(BooleanValueCount) == 0;
    }

    private AnyBoolean Pin(bool value, ConstraintCall applying) {
        if (_pinnedConstraint is not null && _pinned != value) {
            throw ConflictingAnyConstraintException.AlreadyPinned(applying, _pinnedConstraint, V(_pinned!.Value));
        }

        return new AnyBoolean(_source, value, applying);
    }

}

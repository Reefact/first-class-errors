namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="bool" /> values. <c>True()</c> and <c>False()</c> pin the value —
///     mostly useful for symmetry when a test sweeps cases — and contradictory pins fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides, like every other generator.
/// </summary>
public sealed class AnyBool : IAny<bool>, IHasRandomSource, ICardinalityHint, IDomainMembership<bool> {

    #region Statics members declarations

    internal static AnyBool Create(RandomSource source) {
        return new AnyBool(source, null, null);
    }

    private static string V(bool value) {
        return value ? "true" : "false";
    }

    #endregion

    #region Fields declarations

    private readonly bool?        _pinned;
    private readonly string?      _pinnedConstraint;
    private readonly RandomSource _source;

    #endregion

    private AnyBool(RandomSource source, bool? pinned, string? pinnedConstraint) {
        _source           = source;
        _pinned           = pinned;
        _pinnedConstraint = pinnedConstraint;
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Two distinct values unless a pin has already fixed one of them.
    long? ICardinalityHint.DistinctCardinality => _pinned is null ? 2 : 1;

    // A pin narrows the domain to that single value; unpinned, both booleans are producible.
    bool IDomainMembership<bool>.Contains(bool value) => _pinned is not bool pinned || pinned == value;

    /// <summary>Pins the value to <c>true</c>.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBool True() {
        return Pin(true, "True()");
    }

    /// <summary>Pins the value to <c>false</c>.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBool False() {
        return Pin(false, "False()");
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — which, for a boolean, pins it to the
    ///     opposite. The name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyBool DifferentFrom(bool value) {
        return Pin(!value, $"DifferentFrom({V(value)})");
    }

    /// <inheritdoc />
    public bool Generate() {
        return _pinned ?? _source.Current.Random.Next(2) == 0;
    }

    private AnyBool Pin(bool value, string applying) {
        if (_pinnedConstraint is not null && _pinned != value) {
            throw new ConflictingAnyConstraintException($"Cannot apply {applying} because {_pinnedConstraint} already pins the value to {V(_pinned!.Value)}.");
        }

        return new AnyBool(_source, value, applying);
    }

}

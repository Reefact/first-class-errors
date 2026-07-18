namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="bool" /> values. <c>True()</c> and <c>False()</c> pin the value —
///     mostly useful for symmetry when a test sweeps cases — and contradictory pins fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> naming both sides, like every other generator.
/// </summary>
public sealed class AnyBool : IAny<bool>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyBool" /> can be used wherever a <see cref="bool" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator bool(AnyBool generator) {
        return generator.Generate();
    }

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

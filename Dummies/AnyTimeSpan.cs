#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="TimeSpan" /> values — the same contract as
///     <see cref="AnyInt32" />: constraints express what the surrounding code requires of the value, never what the
///     test asserts; contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" />
///     naming both sides; instances are immutable recipes, and each value is built to satisfy the constraints in one
///     draw. Unconstrained, it draws from the full <see cref="TimeSpan" /> range, negative durations included.
/// </summary>
public sealed class AnyTimeSpan : IAny<TimeSpan>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyTimeSpan" /> can be used wherever a <see cref="TimeSpan" /> is
    ///     expected. Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator TimeSpan(AnyTimeSpan generator) {
        return generator.Generate();
    }

    internal static AnyTimeSpan Create(RandomSource source) {
        return new AnyTimeSpan(source, OrdinalIntervalSpec.Unconstrained("TimeSpan", ordinal => V(Val(ordinal)), Ord(TimeSpan.MinValue), Ord(TimeSpan.MaxValue)));
    }

    private static ulong Ord(TimeSpan value) {
        return OrdinalMapping.FromInt64(value.Ticks);
    }

    private static TimeSpan Val(ulong ordinal) {
        return new TimeSpan(OrdinalMapping.ToInt64(ordinal));
    }

    private static string V(TimeSpan value) {
        return value.ToString("c", CultureInfo.InvariantCulture);
    }

    private static string Join(TimeSpan[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private AnyTimeSpan(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires a duration strictly greater than <see cref="TimeSpan.Zero" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan Positive() {
        return new AnyTimeSpan(_source, _spec.WithMinimum(OrdinalMapping.FromInt64(1L), "Positive()"));
    }

    /// <summary>Requires a duration strictly less than <see cref="TimeSpan.Zero" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan Negative() {
        return new AnyTimeSpan(_source, _spec.WithMaximum(OrdinalMapping.FromInt64(-1L), "Negative()"));
    }

    /// <summary>Pins the duration to exactly <see cref="TimeSpan.Zero" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan Zero() {
        return new AnyTimeSpan(_source, _spec.WithMinimum(Ord(TimeSpan.Zero), "Zero()").WithMaximum(Ord(TimeSpan.Zero), "Zero()"));
    }

    /// <summary>Requires a duration different from <see cref="TimeSpan.Zero" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan NonZero() {
        return new AnyTimeSpan(_source, _spec.WithExcluded([Ord(TimeSpan.Zero)], "NonZero()"));
    }

    /// <summary>Requires a duration strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan GreaterThan(TimeSpan value) {
        return new AnyTimeSpan(_source, _spec.WithMinimumAbove(Ord(value), $"GreaterThan({V(value)})"));
    }

    /// <summary>Requires a duration greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan GreaterThanOrEqualTo(TimeSpan value) {
        return new AnyTimeSpan(_source, _spec.WithMinimum(Ord(value), $"GreaterThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a duration strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan LessThan(TimeSpan value) {
        return new AnyTimeSpan(_source, _spec.WithMaximumBelow(Ord(value), $"LessThan({V(value)})"));
    }

    /// <summary>Requires a duration less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan LessThanOrEqualTo(TimeSpan value) {
        return new AnyTimeSpan(_source, _spec.WithMaximum(Ord(value), $"LessThanOrEqualTo({V(value)})"));
    }

    /// <summary>Requires a duration within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan Between(TimeSpan minimum, TimeSpan maximum) {
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        string constraint = $"Between({V(minimum)}, {V(maximum)})";

        return new AnyTimeSpan(_source, _spec.WithMinimum(Ord(minimum), constraint).WithMaximum(Ord(maximum), constraint));
    }

    /// <summary>Requires the duration to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan OneOf(params TimeSpan[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyTimeSpan(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the duration to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan Except(params TimeSpan[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyTimeSpan(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the duration to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated duration must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeSpan DifferentFrom(TimeSpan value) {
        return new AnyTimeSpan(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public TimeSpan Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current.Random));
    }

}

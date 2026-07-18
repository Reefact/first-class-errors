#if NET8_0_OR_GREATER
#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="TimeOnly" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes, and each value is built to satisfy the constraints in one draw.
///     Available on the net8.0 target only, like the type itself. There is deliberately no clock-relative
///     constraint: a reproducible test pins its reference time of days explicitly with <see cref="After" /> and
///     <see cref="Before" />.
/// </summary>
public sealed class AnyTimeOnly : IAny<TimeOnly>, IHasRandomSource {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyTimeOnly" /> can be used wherever a <see cref="TimeOnly" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator TimeOnly(AnyTimeOnly generator) {
        return generator.Generate();
    }

    internal static AnyTimeOnly Create(RandomSource source) {
        return new AnyTimeOnly(source, OrdinalIntervalSpec.Unconstrained("TimeOnly", ordinal => V(Val(ordinal)), Ord(TimeOnly.MinValue), Ord(TimeOnly.MaxValue)));
    }

    private static ulong Ord(TimeOnly value) {
        return (ulong)value.Ticks;
    }

    private static TimeOnly Val(ulong ordinal) {
        return new TimeOnly((long)ordinal);
    }

    private static string V(TimeOnly value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join(TimeOnly[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private AnyTimeOnly(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Requires a time of day strictly after <paramref name="time" />.</summary>
    /// <param name="time">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly After(TimeOnly time) {
        return new AnyTimeOnly(_source, _spec.WithMinimumAbove(Ord(time), $"After({V(time)})"));
    }

    /// <summary>Requires a time of day at or after <paramref name="time" />.</summary>
    /// <param name="time">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly AfterOrEqualTo(TimeOnly time) {
        return new AnyTimeOnly(_source, _spec.WithMinimum(Ord(time), $"AfterOrEqualTo({V(time)})"));
    }

    /// <summary>Requires a time of day strictly before <paramref name="time" />.</summary>
    /// <param name="time">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Before(TimeOnly time) {
        return new AnyTimeOnly(_source, _spec.WithMaximumBelow(Ord(time), $"Before({V(time)})"));
    }

    /// <summary>Requires a time of day at or before <paramref name="time" />.</summary>
    /// <param name="time">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly BeforeOrEqualTo(TimeOnly time) {
        return new AnyTimeOnly(_source, _spec.WithMaximum(Ord(time), $"BeforeOrEqualTo({V(time)})"));
    }

    /// <summary>Requires a time of day within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Between(TimeOnly start, TimeOnly end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        string constraint = $"Between({V(start)}, {V(end)})";

        return new AnyTimeOnly(_source, _spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint));
    }

    /// <summary>Requires the time of day to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly OneOf(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyTimeOnly(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the time of day to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Except(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyTimeOnly(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the time of day to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated time of day must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly DifferentFrom(TimeOnly value) {
        return new AnyTimeOnly(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public TimeOnly Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current.Random));
    }

}
#endif

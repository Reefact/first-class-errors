#if NET8_0_OR_GREATER
#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="DateOnly" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes, and each value is built to satisfy the constraints in one draw.
///     Available on the net8.0 target only, like the type itself. There is deliberately no clock-relative
///     constraint: a reproducible test pins its reference dates explicitly with <see cref="After" /> and
///     <see cref="Before" />.
/// </summary>
public sealed class AnyDateOnly : IAny<DateOnly>, IHasRandomSource, ICardinalityHint {

    #region Statics members declarations

    /// <summary>
    ///     Generates the value — an <see cref="AnyDateOnly" /> can be used wherever a <see cref="DateOnly" /> is expected.
    ///     Each conversion draws a fresh value.
    /// </summary>
    /// <param name="generator">The generator to draw from.</param>
    /// <returns>An arbitrary value satisfying the generator's constraints.</returns>
    public static implicit operator DateOnly(AnyDateOnly generator) {
        return generator.Generate();
    }

    internal static AnyDateOnly Create(RandomSource source) {
        return new AnyDateOnly(source, OrdinalIntervalSpec.Unconstrained("DateOnly", ordinal => V(Val(ordinal)), Ord(DateOnly.MinValue), Ord(DateOnly.MaxValue)));
    }

    private static ulong Ord(DateOnly value) {
        return (ulong)value.DayNumber;
    }

    private static DateOnly Val(ulong ordinal) {
        return DateOnly.FromDayNumber((int)ordinal);
    }

    private static string V(DateOnly value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join(DateOnly[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private AnyDateOnly(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint.DistinctCardinality => _spec.Cardinality;

    /// <summary>Requires a date strictly after <paramref name="date" />.</summary>
    /// <param name="date">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly After(DateOnly date) {
        return new AnyDateOnly(_source, _spec.WithMinimumAbove(Ord(date), $"After({V(date)})"));
    }

    /// <summary>Requires a date at or after <paramref name="date" />.</summary>
    /// <param name="date">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly AfterOrEqualTo(DateOnly date) {
        return new AnyDateOnly(_source, _spec.WithMinimum(Ord(date), $"AfterOrEqualTo({V(date)})"));
    }

    /// <summary>Requires a date strictly before <paramref name="date" />.</summary>
    /// <param name="date">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly Before(DateOnly date) {
        return new AnyDateOnly(_source, _spec.WithMaximumBelow(Ord(date), $"Before({V(date)})"));
    }

    /// <summary>Requires a date at or before <paramref name="date" />.</summary>
    /// <param name="date">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly BeforeOrEqualTo(DateOnly date) {
        return new AnyDateOnly(_source, _spec.WithMaximum(Ord(date), $"BeforeOrEqualTo({V(date)})"));
    }

    /// <summary>Requires a date within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly Between(DateOnly start, DateOnly end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        string constraint = $"Between({V(start)}, {V(end)})";

        return new AnyDateOnly(_source, _spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint));
    }

    /// <summary>Requires the date to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly OneOf(params DateOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyDateOnly(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"));
    }

    /// <summary>Requires the date to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly Except(params DateOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyDateOnly(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the date to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated date must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateOnly DifferentFrom(DateOnly value) {
        return new AnyDateOnly(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public DateOnly Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current.Random));
    }

}
#endif

#region Usings declarations

using System.Globalization;

#endregion

namespace Dummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="DateTime" /> values — the same contract as
///     <see cref="AnyInt32" />: constraints express what the surrounding code requires of the value, never what the
///     test asserts; contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" />
///     naming both sides; instances are immutable recipes, and each value is built to satisfy the constraints in one
///     draw.
/// </summary>
/// <remarks>
///     Generated values carry <see cref="DateTimeKind.Utc" />; constraints compare by <see cref="DateTime.Ticks" />,
///     ignoring the <see cref="DateTime.Kind" /> of the supplied bounds — exactly as <see cref="DateTime" />'s own
///     comparison operators do. Values supplied to <see cref="OneOf" /> are returned as given, Kind included. There is deliberately no clock-relative constraint (no "in the past/future"): a
///     reproducible test pins its reference instants explicitly with <see cref="After" /> and <see cref="Before" />.
/// </remarks>
public sealed class AnyDateTime : IAny<DateTime>, IHasRandomSource, ICardinalityHint<DateTime> {

    #region Statics members declarations

    internal static AnyDateTime Create(RandomSource source) {
        return new AnyDateTime(source, OrdinalIntervalSpec.Unconstrained("DateTime", ordinal => V(Val(ordinal)), Ord(DateTime.MinValue), Ord(DateTime.MaxValue)));
    }

    private static ulong Ord(DateTime value) {
        return (ulong)value.Ticks;
    }

    private static DateTime Val(ulong ordinal) {
        return new DateTime((long)ordinal, DateTimeKind.Utc);
    }

    private static string V(DateTime value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join(DateTime[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyDictionary<ulong, DateTime>? _allowedOriginals;
    private readonly RandomSource                          _source;
    private readonly OrdinalIntervalSpec                   _spec;

    #endregion

    private AnyDateTime(RandomSource source, OrdinalIntervalSpec spec, IReadOnlyDictionary<ulong, DateTime>? allowedOriginals = null) {
        _source           = source;
        _spec             = spec;
        _allowedOriginals = allowedOriginals;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<DateTime>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<DateTime>.Contains(DateTime value) => _spec.Contains(Ord(value));

    /// <summary>Requires an instant strictly after <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime After(DateTime instant) {
        return new AnyDateTime(_source, _spec.WithMinimumAbove(Ord(instant), $"After({V(instant)})"), _allowedOriginals);
    }

    /// <summary>Requires an instant at or after <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime AfterOrEqualTo(DateTime instant) {
        return new AnyDateTime(_source, _spec.WithMinimum(Ord(instant), $"AfterOrEqualTo({V(instant)})"), _allowedOriginals);
    }

    /// <summary>Requires an instant strictly before <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime Before(DateTime instant) {
        return new AnyDateTime(_source, _spec.WithMaximumBelow(Ord(instant), $"Before({V(instant)})"), _allowedOriginals);
    }

    /// <summary>Requires an instant at or before <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime BeforeOrEqualTo(DateTime instant) {
        return new AnyDateTime(_source, _spec.WithMaximum(Ord(instant), $"BeforeOrEqualTo({V(instant)})"), _allowedOriginals);
    }

    /// <summary>Requires an instant within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime Between(DateTime start, DateTime end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        string constraint = $"Between({V(start)}, {V(end)})";

        return new AnyDateTime(_source, _spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint), _allowedOriginals);
    }

    /// <summary>Requires the instant to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime OneOf(params DateTime[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        // Remember the supplied values by instant, so generation returns them as given: the ordinal space
        // only carries the ticks, and rebuilding from it would silently normalize the Kind to Utc.
        Dictionary<ulong, DateTime> originals = new();
        foreach (DateTime value in values) {
            if (!originals.ContainsKey(Ord(value))) { originals.Add(Ord(value), value); }
        }

        return new AnyDateTime(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), $"OneOf({Join(values)})"), originals);
    }

    /// <summary>Requires the instant to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime Except(params DateTime[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new AnyDateTime(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"), _allowedOriginals);
    }

    /// <summary>
    ///     Requires the instant to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated instant must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTime DifferentFrom(DateTime value) {
        return new AnyDateTime(_source, _spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"), _allowedOriginals);
    }

    /// <inheritdoc />
    public DateTime Generate() {
        ulong ordinal = _spec.GenerateOrdinal(_source.Current.Random);
        if (_allowedOriginals is not null && _allowedOriginals.TryGetValue(ordinal, out DateTime original)) { return original; }

        return Val(ordinal);
    }

}

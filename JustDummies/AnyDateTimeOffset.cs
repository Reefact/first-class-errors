#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="DateTimeOffset" /> values — the same contract as
///     <see cref="AnyInt32" />: constraints express what the surrounding code requires of the value, never what the
///     test asserts; contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" />
///     naming both sides; instances are immutable recipes, and each value is built to satisfy the constraints in one
///     draw.
/// </summary>
/// <remarks>
///     Constraints compare by <see cref="DateTimeOffset.UtcTicks" /> — the instant, not the local rendering — exactly
///     as <see cref="DateTimeOffset" />'s own comparison operators do. Unconstrained, generated values carry offset
///     <see cref="TimeSpan.Zero" /> (UTC); <see cref="WithOffset" /> / <see cref="WithOffsetBetween" /> opt the offset
///     dimension into a fixed or bounded whole-minute value so offset-sensitive code can be exercised. Values supplied
///     to <see cref="OneOf" /> are returned as given, offset included. There is deliberately no clock-relative
///     constraint (no "in the past/future"): a reproducible test pins its reference instants explicitly with
///     <see cref="After" /> and <see cref="Before" />.
/// </remarks>
public sealed class AnyDateTimeOffset : IAny<DateTimeOffset>, IHasRandomSource, ICardinalityHint<DateTimeOffset> {

    // DateTimeOffset admits an offset in whole minutes within ±14:00.
    private const int MaxOffsetMinutes = 14 * 60;

    #region Statics members declarations

    internal static AnyDateTimeOffset Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyDateTimeOffset(source, OrdinalIntervalSpec.Unconstrained("DateTimeOffset", ordinal => V(Val(ordinal)), Ord(DateTimeOffset.MinValue), Ord(DateTimeOffset.MaxValue)), null, false, 0, 0);
    }

    private static ulong Ord(DateTimeOffset value) {
        return (ulong)value.UtcTicks;
    }

    private static DateTimeOffset Val(ulong ordinal) {
        return new DateTimeOffset((long)ordinal, TimeSpan.Zero);
    }

    private static string V(DateTimeOffset value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Render(TimeSpan offset) {
        return offset.ToString("c", CultureInfo.InvariantCulture);
    }

    private static string Join(DateTimeOffset[] values) {
        return string.Join(", ", values.Select(V));
    }

    /// <summary>Validates a supplied offset (whole minutes, within ±14:00) and returns it in whole minutes.</summary>
    private static int ValidateOffset(TimeSpan offset, string parameterName) {
        if (offset.Ticks % TimeSpan.TicksPerMinute != 0) { throw new ArgumentException("The offset must be a whole number of minutes.", parameterName); }
        if (offset < TimeSpan.FromMinutes(-MaxOffsetMinutes) || offset > TimeSpan.FromMinutes(MaxOffsetMinutes)) {
            throw new ArgumentOutOfRangeException(parameterName, offset, "The offset must be within ±14:00.");
        }

        return (int)(offset.Ticks / TimeSpan.TicksPerMinute);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyDictionary<ulong, DateTimeOffset>? _allowedOriginals;
    private readonly bool                                        _offsetDeclared;
    private readonly int                                         _offsetMaxMinutes;
    private readonly int                                         _offsetMinMinutes;
    private readonly RandomSource                                _source;
    private readonly OrdinalIntervalSpec                         _spec;

    #endregion

    private AnyDateTimeOffset(RandomSource source, OrdinalIntervalSpec spec, IReadOnlyDictionary<ulong, DateTimeOffset>? allowedOriginals,
                             bool offsetDeclared, int offsetMinMinutes, int offsetMaxMinutes) {
        _source           = source;
        _spec             = spec;
        _allowedOriginals = allowedOriginals;
        _offsetDeclared   = offsetDeclared;
        _offsetMinMinutes = offsetMinMinutes;
        _offsetMaxMinutes = offsetMaxMinutes;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<DateTimeOffset>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<DateTimeOffset>.Contains(DateTimeOffset value) => _spec.Contains(Ord(value));

    /// <summary>Requires an instant strictly after <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset After(DateTimeOffset instant) {
        return With(_spec.WithMinimumAbove(Ord(instant), $"After({V(instant)})"));
    }

    /// <summary>Requires an instant at or after <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset AfterOrEqualTo(DateTimeOffset instant) {
        return With(_spec.WithMinimum(Ord(instant), $"AfterOrEqualTo({V(instant)})"));
    }

    /// <summary>Requires an instant strictly before <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Before(DateTimeOffset instant) {
        return With(_spec.WithMaximumBelow(Ord(instant), $"Before({V(instant)})"));
    }

    /// <summary>Requires an instant at or before <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset BeforeOrEqualTo(DateTimeOffset instant) {
        return With(_spec.WithMaximum(Ord(instant), $"BeforeOrEqualTo({V(instant)})"));
    }

    /// <summary>Requires an instant within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Between(DateTimeOffset start, DateTimeOffset end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        string constraint = $"Between({V(start)}, {V(end)})";

        return With(_spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint));
    }

    /// <summary>
    ///     Requires the instant to fall on a lattice of <paramref name="granularity" /> from
    ///     <see cref="DateTimeOffset.MinValue" /> — a round instant (a whole second, a quarter-hour, a whole day),
    ///     built on the grid rather than snapped after the fact, so tick-precision values never surprise a
    ///     serialization round-trip. Declared once per generator.
    /// </summary>
    /// <param name="granularity">The lattice step; must be strictly positive. A granularity of one tick adds no constraint.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="granularity" /> is not strictly positive.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithGranularity(TimeSpan granularity) {
        if (granularity <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "The granularity must be strictly positive."); }

        string rendered = granularity.ToString("c", CultureInfo.InvariantCulture);

        return With(_spec.WithStep((ulong)granularity.Ticks, Ord(DateTimeOffset.MinValue), $"WithGranularity({rendered})"));
    }

    /// <summary>
    ///     Pins the offset dimension to <paramref name="offset" /> — every generated value carries exactly that offset,
    ///     rather than the default <see cref="TimeSpan.Zero" />. The instant is tightened so the value stays valid at
    ///     the domain edges. Declared once per generator.
    /// </summary>
    /// <param name="offset">The offset to pin; a whole number of minutes, within ±14:00.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="offset" /> is not a whole number of minutes.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset" /> is outside ±14:00.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithOffset(TimeSpan offset) {
        int minutes = ValidateOffset(offset, nameof(offset));

        return WithOffsetRange(minutes, minutes, $"WithOffset({Render(offset)})");
    }

    /// <summary>
    ///     Draws the offset dimension from the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />] —
    ///     a bounded, whole-minute offset — so a test can exercise offset-sensitive logic while staying valid. The
    ///     instant is tightened so every offset in the range stays valid. Declared once per generator.
    /// </summary>
    /// <param name="minimum">The inclusive lower offset; a whole number of minutes, within ±14:00.</param>
    /// <param name="maximum">The inclusive upper offset; a whole number of minutes, within ±14:00.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when an offset is not a whole number of minutes, or <paramref name="minimum" /> is after <paramref name="maximum" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an offset is outside ±14:00.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithOffsetBetween(TimeSpan minimum, TimeSpan maximum) {
        int min = ValidateOffset(minimum, nameof(minimum));
        int max = ValidateOffset(maximum, nameof(maximum));
        if (min > max) { throw new ArgumentException($"The minimum offset ({Render(minimum)}) must be at or before the maximum ({Render(maximum)}).", nameof(minimum)); }

        return WithOffsetRange(min, max, $"WithOffsetBetween({Render(minimum)}, {Render(maximum)})");
    }

    /// <summary>
    ///     Requires the instant to be one of the supplied values — returned as given, offset included. Declared once
    ///     per generator.
    /// </summary>
    /// <param name="values">The allowed values; duplicates (same instant) are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with LINQ expressions",
                                                     Justification =
                                                         "The condition reads the collection the body mutates. Where is lazily evaluated, so lifting the filter out would run each " +
                                                         "predicate against a snapshot taken before the additions it is meant to see, and let duplicates through.")]
    public AnyDateTimeOffset OneOf(params DateTimeOffset[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        string applying = $"OneOf({Join(values)})";
        // A pooled value is returned as given, offset included — rebuilding it from the ordinal would normalize the
        // offset to UTC. So when an offset constraint is already in force, the pool is FILTERED by it rather than
        // the constraint being ignored: a value carrying a different offset is one this generator must not produce.
        DateTimeOffset[] admitted = _offsetDeclared ? values.Where(SatisfiesDeclaredOffset).ToArray() : values;
        if (admitted.Length == 0) { throw OffsetExcludesEveryPooledValue(applying, _offsetMinMinutes, _offsetMaxMinutes); }

        // Remember the supplied values by instant, so generation returns them as given: the ordinal space
        // only carries the instant, and rebuilding from it would silently normalize the offset to UTC.
        Dictionary<ulong, DateTimeOffset> originals = [];
        foreach (DateTimeOffset value in admitted) {
            if (!originals.ContainsKey(Ord(value))) { originals.Add(Ord(value), value); }
        }

        return new AnyDateTimeOffset(_source, _spec.WithAllowed(admitted.Select(Ord).ToArray(), applying), originals, _offsetDeclared, _offsetMinMinutes, _offsetMaxMinutes);
    }

    /// <summary>Requires the instant to be none of the supplied values (compared by instant).</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Except(params DateTimeOffset[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return With(_spec.WithExcluded(values.Select(Ord).ToArray(), $"Except({Join(values)})"));
    }

    /// <summary>
    ///     Requires the instant to differ from <paramref name="value" /> (compared by instant) — typically an existing
    ///     value the test already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent
    ///     at the call site.
    /// </summary>
    /// <param name="value">The value the generated instant must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset DifferentFrom(DateTimeOffset value) {
        return With(_spec.WithExcluded([Ord(value)], $"DifferentFrom({V(value)})"));
    }

    /// <inheritdoc />
    public DateTimeOffset Generate() {
        SeededRandom random  = _source.Current;
        ulong        ordinal = _spec.GenerateOrdinal(random);
        if (_allowedOriginals is not null && _allowedOriginals.TryGetValue(ordinal, out DateTimeOffset original)) { return original; }
        if (!_offsetDeclared) { return Val(ordinal); }

        int minutes = _offsetMinMinutes == _offsetMaxMinutes
                          ? _offsetMinMinutes
                          : _offsetMinMinutes + random.Next(_offsetMaxMinutes - _offsetMinMinutes + 1);
        TimeSpan offset = TimeSpan.FromMinutes(minutes);

        // The instant domain was tightened when the offset was declared, so the local ticks stay valid here.
        return new DateTimeOffset((long)ordinal + offset.Ticks, offset);
    }

    /// <summary>Carries the offset state forward onto a new spec — every instant constraint routes through here.</summary>
    private AnyDateTimeOffset With(OrdinalIntervalSpec spec) {
        return new AnyDateTimeOffset(_source, spec, _allowedOriginals, _offsetDeclared, _offsetMinMinutes, _offsetMaxMinutes);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out",
                                                     Justification =
                                                         "The flagged lines are prose, not disabled code: the heuristic reads an equation, a bracketed range or a semicolon inside an " +
                                                         "explanatory sentence as a statement. These comments carry the reasoning this codebase asks every comment to carry, so the " +
                                                         "finding is recorded rather than the comment deleted.")]
    private AnyDateTimeOffset WithOffsetRange(int minMinutes, int maxMinutes, string applying) {
        if (_offsetDeclared) {
            if (_offsetMinMinutes == minMinutes && _offsetMaxMinutes == maxMinutes) { return this; }

            throw ConflictingAnyConstraintException.AlreadyDefined(applying, "an offset constraint");
        }

        // Tighten the instant so local ticks = UtcTicks + offset stay in [0, MaxTicks] for every offset in the range;
        // the offset can then be drawn independently, never producing an out-of-range DateTimeOffset.
        long  minOffsetTicks = minMinutes * TimeSpan.TicksPerMinute;
        long  maxOffsetTicks = maxMinutes * TimeSpan.TicksPerMinute;
        ulong lowerUtc       = (ulong)Math.Max(0L, -minOffsetTicks);
        ulong upperUtc       = (ulong)(DateTimeOffset.MaxValue.UtcTicks - Math.Max(0L, maxOffsetTicks));

        OrdinalIntervalSpec spec = _spec.WithMinimum(lowerUtc, applying).WithMaximum(upperUtc, applying);

        // The mirror of OneOf's filter, so the two orders reach the same verdict: an offset declared AFTER a pool
        // narrows that pool to the values it admits, and contradicts when it admits none.
        if (_allowedOriginals is not null) {
            Dictionary<ulong, DateTimeOffset> admitted = _allowedOriginals
                                                         .Where(entry => SatisfiesOffset(entry.Value, minMinutes, maxMinutes))
                                                         .ToDictionary(entry => entry.Key, entry => entry.Value);
            if (admitted.Count == 0) { throw OffsetExcludesEveryPooledValue(applying, minMinutes, maxMinutes); }

            return new AnyDateTimeOffset(_source, spec.NarrowingAllowed(admitted.Keys.ToArray(), applying), admitted, true, minMinutes, maxMinutes);
        }

        return new AnyDateTimeOffset(_source, spec, _allowedOriginals, true, minMinutes, maxMinutes);
    }

    /// <summary>Whether <paramref name="value" /> carries an offset the declared offset dimension admits.</summary>
    private bool SatisfiesDeclaredOffset(DateTimeOffset value) {
        return SatisfiesOffset(value, _offsetMinMinutes, _offsetMaxMinutes);
    }

    private static bool SatisfiesOffset(DateTimeOffset value, int minMinutes, int maxMinutes) {
        double minutes = value.Offset.TotalMinutes;

        return minutes >= minMinutes && minutes <= maxMinutes;
    }

    // The range is passed in rather than read off the fields: when an offset is declared AFTER a pool, the fields
    // still hold the previous (undeclared) state at the point the contradiction is detected.
    private static ConflictingAnyConstraintException OffsetExcludesEveryPooledValue(string applying, int minMinutes, int maxMinutes) {
        string admitted = minMinutes == maxMinutes
                              ? Render(TimeSpan.FromMinutes(minMinutes))
                              : $"{Render(TimeSpan.FromMinutes(minMinutes))} to {Render(TimeSpan.FromMinutes(maxMinutes))}";

        return new ConflictingAnyConstraintException($"Cannot apply {applying} because no pooled value carries an offset it admits ({admitted}).");
    }

}

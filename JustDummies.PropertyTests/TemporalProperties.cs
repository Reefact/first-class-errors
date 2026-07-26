#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the three temporal generators — <see cref="AnyDateTime" />,
///     <see cref="AnyTimeSpan" /> and <see cref="AnyDateTimeOffset" />, including its offset dimension. Where the
///     example-based suite pins one anchor instant and a handful of hand-picked windows, these draw the instants,
///     the durations and the offsets themselves, over the whole ten-thousand-year domain and the whole ±14:00
///     offset range, so a bound that overflows at the edge of the domain, or an offset that quietly pins itself to
///     one end of its range, is found and shrunk to its minimal counter-example.
/// </summary>
/// <remarks>
///     Two traps shape almost every property here. The naming differs by type — <see cref="AnyDateTime" /> and
///     <see cref="AnyDateTimeOffset" /> say <c>After</c>/<c>Before</c> while <see cref="AnyTimeSpan" /> says
///     <c>GreaterThan</c>/<c>LessThan</c> — and legality is <b>value-dependent</b>: <c>Positive()</c> on an interval
///     that lies below zero, <c>After</c> at the very top of the domain, or a <c>WithOffset</c> that no longer fits
///     the instant window already declared are conflicts rather than narrowings. The properties therefore decide
///     the expectation from the drawn value instead of assuming the call shape settles it.
///     <para>
///         Instants are built from a drawn tick count rather than from FsCheck's own <see cref="DateTime" />
///         arbitrary, so this file owns its domain and the distance it keeps from the edges that overflow. Bounds
///         are compared the way the library compares them — by ticks for <see cref="DateTime" /> (kind ignored) and
///         by <see cref="DateTimeOffset.UtcTicks" /> for <see cref="DateTimeOffset" /> (rendering ignored).
///     </para>
/// </remarks>
[TestSubject(typeof(AnyDateTimeOffset))]
public sealed class TemporalProperties {

    #region Statics members declarations

    /// <summary><see cref="DateTimeOffset" /> admits an offset in whole minutes within ±14:00; both offset constraints mirror that domain.</summary>
    private const int MaxOffsetMinutes = 14 * 60;

    /// <summary>The narrowest offset range the variation property accepts, so the range always holds enough offsets for "it varies" to mean something.</summary>
    private const int SpreadMinutes = 60;

    /// <summary>Draws taken when a property reasons over a batch rather than over each value in isolation.</summary>
    private const int VariationDraws = 24;

    /// <summary>The widest window below the top of the domain the offset-tightening property opens — deliberately wider than ±14:00, so both sides of the legality line are drawn.</summary>
    private const int CeilingWindowMinutes = 15 * 60;

    private static readonly DateTime       AnchorInstant = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset AnchorMoment  = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The number of ticks in the instant domain, shared by <see cref="DateTime" /> and <see cref="DateTimeOffset" /> — their maxima carry the same tick count.</summary>
    private static readonly ulong DomainTicks = (ulong)DateTime.MaxValue.Ticks + 1UL;

    /// <summary>
    ///     A 64-bit word drawn over its whole range. FsCheck's own numeric generators are size-bounded and rarely
    ///     leave a hundred of zero, which for a tick count would mean "always within a microsecond of year one"; the
    ///     word is therefore assembled from three narrow draws, each spanning far less than a 32-bit range so the
    ///     span itself never has to be counted in 64 bits.
    /// </summary>
    private static Gen<long> Bits64() {
        return from high in Gen.Choose(-(1 << 21), (1 << 21) - 1)
               from middle in Gen.Choose(0, (1 << 21) - 1)
               from low in Gen.Choose(0, (1 << 21) - 1)
               // Added rather than or-ed: the three fields occupy disjoint bit ranges, so the sum is the same
               // word, without or-ing a sign-extended operand (CS0675) to carry `high`'s sign into bit 63.
               select ((long)high << 42) + ((long)middle << 21) + low;
    }

    /// <summary>
    ///     Arbitrary instants over the whole <see cref="DateTime" /> domain, its edges included and its
    ///     <see cref="DateTimeKind" /> drawn rather than fixed: constraints compare by <see cref="DateTime.Ticks" />
    ///     and ignore the kind of the bounds they are handed, exactly as <see cref="DateTime" />'s own operators do.
    /// </summary>
    private static Gen<DateTime> Instants() {
        Gen<DateTime> anywhere = from bits in Bits64()
                                 from kind in Gen.Choose(0, 2)
                                 select new DateTime((long)((ulong)bits % DomainTicks), (DateTimeKind)kind);

        return Generators.WithEdges(anywhere, DateTime.MinValue, DateTime.MinValue.AddTicks(1), AnchorInstant,
                                    DateTime.MaxValue.AddTicks(-1), DateTime.MaxValue);
    }

    /// <summary>Arbitrary durations over the whole <see cref="TimeSpan" /> domain, biased towards zero and towards the edges an off-by-one hides behind.</summary>
    private static Gen<TimeSpan> Durations() {
        Gen<TimeSpan> anywhere = Bits64().Select(ticks => TimeSpan.FromTicks(ticks));

        return Generators.WithEdges(anywhere, TimeSpan.MinValue, TimeSpan.FromTicks(long.MinValue + 1), TimeSpan.FromTicks(-1),
                                    TimeSpan.Zero, TimeSpan.FromTicks(1), TimeSpan.FromTicks(long.MaxValue - 1), TimeSpan.MaxValue);
    }

    /// <summary>
    ///     Arbitrary instants over the whole <see cref="DateTimeOffset" /> domain, expressed in UTC: a bound built at
    ///     the edge of the domain with a non-zero offset would overflow on construction, and the constraints compare
    ///     by <see cref="DateTimeOffset.UtcTicks" /> anyway, so the offset of a bound carries no information.
    /// </summary>
    private static Gen<DateTimeOffset> Moments() {
        Gen<DateTimeOffset> anywhere = Bits64().Select(bits => new DateTimeOffset((long)((ulong)bits % DomainTicks), TimeSpan.Zero));

        return Generators.WithEdges(anywhere, DateTimeOffset.MinValue, DateTimeOffset.MinValue.AddTicks(1), AnchorMoment,
                                    DateTimeOffset.MaxValue.AddTicks(-1), DateTimeOffset.MaxValue);
    }

    /// <summary>A legal offset in whole minutes: anywhere within ±14:00, biased towards the ends of the range and towards UTC.</summary>
    private static Gen<int> OffsetMinutes() {
        return Generators.WithEdges(Gen.Choose(-MaxOffsetMinutes, MaxOffsetMinutes), -MaxOffsetMinutes, -1, 0, 1, MaxOffsetMinutes);
    }

    /// <summary>
    ///     Two legal whole-minute offsets that are strictly apart. This is the shape every "declared once" conflict
    ///     needs: re-declaring the <i>same</i> offset range is idempotent by design, so a property quantifying over
    ///     two independent draws would expect a conflict that never comes.
    /// </summary>
    private static Gen<(int Low, int High)> DistinctOffsetMinutes() {
        return from low in Gen.Choose(-MaxOffsetMinutes, MaxOffsetMinutes - 1)
               from high in Gen.Choose(low + 1, MaxOffsetMinutes)
               select (Low: low, High: high);
    }

    /// <summary>Turns a whole number of minutes into an offset without going through the double-based factories, so no rounding can creep between the draw and the call.</summary>
    private static TimeSpan Minutes(int minutes) {
        return TimeSpan.FromTicks(minutes * TimeSpan.TicksPerMinute);
    }

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="action" /> throws an <see cref="ArgumentException" /> — that
    ///     exact type — naming <paramref name="parameterName" />. The exactness is the point:
    ///     <see cref="ArgumentOutOfRangeException" /> derives from <see cref="ArgumentException" />, so a mere
    ///     assignability check could not tell a malformed offset from an out-of-range one, and the offset dimension
    ///     distinguishes the two deliberately.
    /// </summary>
    private static bool ThrowsArgumentExceptionNaming(Action action, string parameterName) {
        try {
            action();

            return false;
        } catch (ArgumentException exception) {
            return exception.GetType() == typeof(ArgumentException) && exception.ParamName == parameterName;
        }
    }

    #endregion

    [Fact(DisplayName = "DateTime: Between contains — every draw falls within the declared inclusive instants.")]
    public void DateTimeBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Instants()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.DateTime().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTime: Between with equal instants pins the value, for every instant.")]
    public void DateTimeBetweenWithEqualBoundsPins() {
        Prop.ForAll(Instants().ToArbitrary(),
                    instant => Expect.EveryDraw(Any.DateTime().Between(instant, instant), drawn => drawn.Ticks == instant.Ticks))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTime: After and Before are exclusive, and conflict at the edge of the domain.")]
    public void DateTimeAfterAndBeforeAreExclusive() {
        Prop.ForAll(Instants().ToArbitrary(),
                    instant => {
                        // No instant lies after DateTime.MaxValue, and none before DateTime.MinValue: there the
                        // exclusive bound empties the domain, and the library owes a conflict at the fluent call
                        // rather than a failure at Generate().
                        bool after = instant == DateTime.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTime().After(instant))
                                         : Expect.EveryDraw(Any.DateTime().After(instant), value => value > instant);
                        bool before = instant == DateTime.MinValue
                                          ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTime().Before(instant))
                                          : Expect.EveryDraw(Any.DateTime().Before(instant), value => value < instant);

                        return after && before;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTime: AfterOrEqualTo and BeforeOrEqualTo are inclusive, right up to the edge of the domain.")]
    public void DateTimeInclusiveBoundsAreInclusive() {
        Prop.ForAll(Instants().ToArbitrary(),
                    instant => Expect.EveryDraw(Any.DateTime().AfterOrEqualTo(instant), value => value >= instant)
                               && Expect.EveryDraw(Any.DateTime().BeforeOrEqualTo(instant), value => value <= instant))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTime: every generated value carries Utc kind, whatever kind the bounds carry.")]
    public void DateTimeGeneratedValuesCarryUtcKind() {
        Prop.ForAll(Instants().ToArbitrary(),
                    instant => Expect.EveryDraw(Any.DateTime(), value => value.Kind == DateTimeKind.Utc)
                               && Expect.EveryDraw(Any.DateTime().AfterOrEqualTo(instant), value => value.Kind == DateTimeKind.Utc)
                               && Expect.EveryDraw(Any.DateTime().BeforeOrEqualTo(instant), value => value.Kind == DateTimeKind.Utc))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTime: crossed Between instants are an argument error naming the start, never a silent swap.")]
    public void DateTimeCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Instants()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || ThrowsArgumentExceptionNaming(() => Any.DateTime().Between(bounds.Max, bounds.Min), "start"))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: Between contains — every draw falls within the declared inclusive durations.")]
    public void TimeSpanBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Durations()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.TimeSpan().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: GreaterThan and LessThan are exclusive, and conflict at the edge of the domain.")]
    public void TimeSpanExclusiveBoundsAreExclusive() {
        Prop.ForAll(Durations().ToArbitrary(),
                    duration => {
                        // The duration surface names its bounds GreaterThan/LessThan where the instant surface says
                        // After/Before; the invariant underneath is the same one.
                        bool greater = duration == TimeSpan.MaxValue
                                           ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.TimeSpan().GreaterThan(duration))
                                           : Expect.EveryDraw(Any.TimeSpan().GreaterThan(duration), value => value > duration);
                        bool less = duration == TimeSpan.MinValue
                                        ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.TimeSpan().LessThan(duration))
                                        : Expect.EveryDraw(Any.TimeSpan().LessThan(duration), value => value < duration);

                        return greater && less;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: GreaterThanOrEqualTo and LessThanOrEqualTo are inclusive, right up to the edge of the domain.")]
    public void TimeSpanInclusiveBoundsAreInclusive() {
        Prop.ForAll(Durations().ToArbitrary(),
                    duration => Expect.EveryDraw(Any.TimeSpan().GreaterThanOrEqualTo(duration), value => value >= duration)
                                && Expect.EveryDraw(Any.TimeSpan().LessThanOrEqualTo(duration), value => value <= duration))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: Positive and Negative are strict about zero, and conflict with an interval lying on the wrong side of it.")]
    public void TimeSpanPositiveAndNegativeAreStrictAboutZero() {
        Prop.ForAll(Generators.OrderedPair(Durations()).ToArbitrary(),
                    bounds => {
                        AnyTimeSpan interval = Any.TimeSpan().Between(bounds.Min, bounds.Max);

                        // Value-dependent legality: the very same call narrows an interval that still reaches past
                        // zero and empties one that does not, so the expectation is read off the drawn bounds. An
                        // interval touching zero from one side only is exactly the corner an example would miss.
                        bool positive = bounds.Max > TimeSpan.Zero
                                            ? Expect.EveryDraw(interval.Positive(),
                                                               value => value > TimeSpan.Zero && value >= bounds.Min && value <= bounds.Max)
                                            : Expect.Throws<ConflictingAnyConstraintException>(() => interval.Positive());
                        bool negative = bounds.Min < TimeSpan.Zero
                                            ? Expect.EveryDraw(interval.Negative(),
                                                               value => value < TimeSpan.Zero && value >= bounds.Min && value <= bounds.Max)
                                            : Expect.Throws<ConflictingAnyConstraintException>(() => interval.Negative());

                        return positive && negative;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: Zero pins any interval that holds zero, and conflicts with every interval that does not.")]
    public void TimeSpanZeroPinsTheIntervalsThatHoldIt() {
        Prop.ForAll(Generators.OrderedPair(Durations()).ToArbitrary(),
                    bounds => {
                        AnyTimeSpan interval = Any.TimeSpan().Between(bounds.Min, bounds.Max);

                        if (bounds.Min <= TimeSpan.Zero && bounds.Max >= TimeSpan.Zero) {
                            return Expect.EveryDraw(interval.Zero(), value => value == TimeSpan.Zero);
                        }

                        return Expect.Throws<ConflictingAnyConstraintException>(() => interval.Zero());
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: NonZero removes zero from any interval, and empties the one interval holding nothing else.")]
    public void TimeSpanNonZeroExcludesZero() {
        Prop.ForAll(Generators.OrderedPair(Durations()).ToArbitrary(),
                    bounds => {
                        AnyTimeSpan interval = Any.TimeSpan().Between(bounds.Min, bounds.Max);

                        // Excluding zero from the interval pinned to zero leaves nothing to draw: that is a conflict
                        // at the fluent call, the duration counterpart of excluding the single value of a pinned
                        // integer interval.
                        if (bounds.Min == TimeSpan.Zero && bounds.Max == TimeSpan.Zero) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => interval.NonZero());
                        }

                        return Expect.EveryDraw(interval.NonZero(),
                                                value => value != TimeSpan.Zero && value >= bounds.Min && value <= bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeSpan: crossed Between durations are an argument error naming the minimum, never a silent swap.")]
    public void TimeSpanCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Durations()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || ThrowsArgumentExceptionNaming(() => Any.TimeSpan().Between(bounds.Max, bounds.Min), "minimum"))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTimeOffset: Between contains — every draw falls within the declared inclusive instants.")]
    public void DateTimeOffsetBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Moments()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.DateTimeOffset().Between(bounds.Min, bounds.Max),
                                               value => value.UtcTicks >= bounds.Min.UtcTicks && value.UtcTicks <= bounds.Max.UtcTicks))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTimeOffset: After and Before are exclusive, and conflict at the edge of the domain.")]
    public void DateTimeOffsetAfterAndBeforeAreExclusive() {
        Prop.ForAll(Moments().ToArbitrary(),
                    instant => {
                        bool after = instant == DateTimeOffset.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTimeOffset().After(instant))
                                         : Expect.EveryDraw(Any.DateTimeOffset().After(instant), value => value.UtcTicks > instant.UtcTicks);
                        bool before = instant == DateTimeOffset.MinValue
                                          ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTimeOffset().Before(instant))
                                          : Expect.EveryDraw(Any.DateTimeOffset().Before(instant), value => value.UtcTicks < instant.UtcTicks);

                        return after && before;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTimeOffset: AfterOrEqualTo and BeforeOrEqualTo are inclusive, right up to the edge of the domain.")]
    public void DateTimeOffsetInclusiveBoundsAreInclusive() {
        Prop.ForAll(Moments().ToArbitrary(),
                    instant => Expect.EveryDraw(Any.DateTimeOffset().AfterOrEqualTo(instant), value => value.UtcTicks >= instant.UtcTicks)
                               && Expect.EveryDraw(Any.DateTimeOffset().BeforeOrEqualTo(instant), value => value.UtcTicks <= instant.UtcTicks))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTimeOffset: with no offset constraint every draw carries the UTC offset, whatever the instant window.")]
    public void DateTimeOffsetDefaultsToTheUtcOffset() {
        Prop.ForAll(Generators.OrderedPair(Moments()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.DateTimeOffset(), value => value.Offset == TimeSpan.Zero)
                              && Expect.EveryDraw(Any.DateTimeOffset().Between(bounds.Min, bounds.Max), value => value.Offset == TimeSpan.Zero))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateTimeOffset: crossed Between instants are an argument error naming the start, never a silent swap.")]
    public void DateTimeOffsetCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Moments()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || ThrowsArgumentExceptionNaming(() => Any.DateTimeOffset().Between(bounds.Max, bounds.Min), "start"))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithOffset: every draw carries exactly the pinned offset, for every legal offset.")]
    public void WithOffsetPinsTheOffset() {
        Prop.ForAll(OffsetMinutes().ToArbitrary(),
                    minutes => {
                        TimeSpan offset = Minutes(minutes);

                        // Reaching the assertion at all is half the property: the local ticks are the UTC ticks
                        // shifted by the offset, so without the instant range the library tightens on declaration,
                        // the extreme offsets would overflow inside Generate() long before the offset is compared.
                        return Expect.EveryDraw(Any.DateTimeOffset().WithOffset(offset),
                                                value => value.Offset == offset
                                                         && value.UtcTicks + offset.Ticks >= DateTime.MinValue.Ticks
                                                         && value.UtcTicks + offset.Ticks <= DateTime.MaxValue.Ticks);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithOffset: an instant window with no room left for the offset conflicts; one with room keeps both.")]
    public void WithOffsetTightensTheInstantWindow() {
        Prop.ForAll((from floorMinutes in Gen.Choose(1, CeilingWindowMinutes)
                     from offsetMinutes in OffsetMinutes()
                     select (floorMinutes, offsetMinutes)).ToArbitrary(),
                    testCase => {
                        DateTimeOffset    floor    = DateTimeOffset.MaxValue.AddTicks(-testCase.floorMinutes * TimeSpan.TicksPerMinute);
                        TimeSpan          offset   = Minutes(testCase.offsetMinutes);
                        AnyDateTimeOffset windowed = Any.DateTimeOffset().After(floor);

                        // Value-dependent legality again: the last hours of the domain can host a +02:00 offset but
                        // not a +14:00 one, and host every negative offset whatever the window — the window must be
                        // wider than the shift the offset applies to the local ticks.
                        if (testCase.floorMinutes <= testCase.offsetMinutes) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => windowed.WithOffset(offset));
                        }

                        return Expect.EveryDraw(windowed.WithOffset(offset),
                                                value => value.Offset == offset && value.UtcTicks > floor.UtcTicks);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithOffsetBetween: every draw's offset lies within the inclusive range, in whole minutes.")]
    public void WithOffsetBetweenStaysWithinItsRange() {
        Prop.ForAll(Generators.OrderedPair(OffsetMinutes()).ToArbitrary(),
                    bounds => {
                        TimeSpan minimum = Minutes(bounds.Min);
                        TimeSpan maximum = Minutes(bounds.Max);

                        // The degenerate range is kept: a range collapsed onto one offset must pin it, not reject it.
                        return Expect.EveryDraw(Any.DateTimeOffset().WithOffsetBetween(minimum, maximum),
                                                value => value.Offset >= minimum
                                                         && value.Offset <= maximum
                                                         && value.Offset.Ticks % TimeSpan.TicksPerMinute == 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithOffsetBetween: over enough draws the offset really varies — it is drawn, not pinned to an end of the range.")]
    public void WithOffsetBetweenVariesTheOffset() {
        Prop.ForAll((from low in Gen.Choose(-MaxOffsetMinutes, MaxOffsetMinutes - SpreadMinutes)
                     from high in Gen.Choose(low + SpreadMinutes, MaxOffsetMinutes)
                     select (low, high)).ToArbitrary(),
                    bounds => {
                        TimeSpan minimum = Minutes(bounds.low);
                        TimeSpan maximum = Minutes(bounds.high);

                        // The range is drawn at least an hour wide, so it always offers more than sixty offsets: a
                        // generator that quietly pinned the offset to one end — the failure a single-draw assertion
                        // cannot see, since one end of the range satisfies the bounds perfectly — surfaces here.
                        List<DateTimeOffset> draws = Expect.Draws(Any.DateTimeOffset().WithOffsetBetween(minimum, maximum), VariationDraws);

                        return draws.Select(value => value.Offset).Distinct().Count() > 1
                               && draws.All(value => value.Offset >= minimum && value.Offset <= maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Offsets: an offset carrying a sub-minute remainder is an argument error, wherever the remainder falls.")]
    public void SubMinuteOffsetsAreAnArgumentError() {
        Prop.ForAll((from minutes in Gen.Choose(-(MaxOffsetMinutes - 1), MaxOffsetMinutes - 1)
                     from remainder in Gen.Choose(1, (int)TimeSpan.TicksPerMinute - 1)
                     select TimeSpan.FromTicks(minutes * TimeSpan.TicksPerMinute + remainder)).ToArbitrary(),
                    offset =>
                        // The drawn offsets stay inside ±14:00, so it is the whole-minute rule that fires and not
                        // the range rule — argument validation runs in that order, and the two are told apart by
                        // the exact exception type.
                        ThrowsArgumentExceptionNaming(() => Any.DateTimeOffset().WithOffset(offset), "offset")
                        && ThrowsArgumentExceptionNaming(() => Any.DateTimeOffset().WithOffsetBetween(offset, TimeSpan.Zero), "minimum")
                        && ThrowsArgumentExceptionNaming(() => Any.DateTimeOffset().WithOffsetBetween(TimeSpan.Zero, offset), "maximum"))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Offsets: a whole-minute offset beyond ±14:00 is out of range, however far beyond it lands.")]
    public void OffsetsBeyondFourteenHoursAreOutOfRange() {
        Prop.ForAll((from magnitude in Gen.Choose(MaxOffsetMinutes + 1, 10 * MaxOffsetMinutes)
                     from mirrored in Gen.Choose(0, 1)
                     select Minutes(mirrored == 0 ? magnitude : -magnitude)).ToArbitrary(),
                    // Whole minutes by construction, so the whole-minute rule passes and the range rule is the one
                    // under test.
                    offset => Expect.Throws<ArgumentOutOfRangeException>(() => Any.DateTimeOffset().WithOffset(offset))
                              && Expect.Throws<ArgumentOutOfRangeException>(() => Any.DateTimeOffset().WithOffsetBetween(offset, offset)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithOffsetBetween: a crossed offset range is an argument error naming the minimum, never a silent swap.")]
    public void CrossedOffsetRangeIsAnArgumentError() {
        Prop.ForAll(DistinctOffsetMinutes().ToArbitrary(),
                    bounds => ThrowsArgumentExceptionNaming(
                        () => Any.DateTimeOffset().WithOffsetBetween(Minutes(bounds.High), Minutes(bounds.Low)), "minimum"))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Offsets: the offset dimension is declared once — a second, different offset constraint conflicts whichever form it takes.")]
    public void TheOffsetDimensionIsDeclaredOnce() {
        Prop.ForAll(DistinctOffsetMinutes().ToArbitrary(),
                    bounds => {
                        TimeSpan low  = Minutes(bounds.Low);
                        TimeSpan high = Minutes(bounds.High);

                        return Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTimeOffset().WithOffset(low).WithOffset(high))
                               && Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTimeOffset().WithOffset(low).WithOffsetBetween(low, high))
                               && Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTimeOffset().WithOffsetBetween(low, high).WithOffset(high))
                               // Re-declaring the very same range is idempotent, not a conflict: the dimension is
                               // declared once, which is not the same thing as called once.
                               && Expect.EveryDraw(Any.DateTimeOffset().WithOffset(low).WithOffset(low), value => value.Offset == low);
                    })
            .QuickCheckThrowOnFailure();
    }

}

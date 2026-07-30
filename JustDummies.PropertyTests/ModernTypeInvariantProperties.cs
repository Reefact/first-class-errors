#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the five generators the netstandard2.0 asset cannot carry —
///     <see cref="AnyInt128" />, <see cref="AnyUInt128" />, <see cref="AnyHalf" />, <see cref="AnyDateOnly" /> and
///     <see cref="AnyTimeOnly" />. Where the example-based suite pins one anchor date, one anchor time and a handful
///     of tiny hand-picked intervals (<c>Between(1, 3)</c>, <c>Between(1f, 2f)</c>), these draw the bounds, the
///     lattice steps and the seeds themselves, over the whole 128-bit domain, the whole ten-thousand-year day-number
///     range and the whole day of ticks, so a bound that overflows or truncates for one interval in a million is
///     found and shrunk to its minimal counter-example rather than missed.
/// </summary>
/// <remarks>
///     The file is excluded from the .NET Framework 4.7.2 floor by the project file, because the types themselves
///     are: its invariants are net8-and-later by construction, and the rest of the suite proves the netstandard2.0
///     contract on the floor.
///     <para>
///         Three traps shape the properties here. The 128-bit generators ride an <b>ordinal</b> mapping (a sign-bit
///         flip for <see cref="Int128" />, the identity for <see cref="UInt128" />), so the domain edges are exactly
///         where the mapping could fold — the values are therefore assembled from two drawn 64-bit words, edges
///         included, instead of from FsCheck's size-bounded numerics, which would never leave the neighbourhood of
///         zero. <see cref="Half" /> carries about three decimal digits and tops out around 65504, so bounds are
///         drawn as small whole numbers and the intervals stay deliberately coarse: the point is the interval
///         algebra, not the rounding. And legality is <b>value-dependent</b>: an exclusive bound at the edge of a
///         domain, or a ceiling on the wrong side of zero for <c>Positive()</c>, is a conflict rather than a
///         narrowing, so the properties decide the expectation from the drawn value instead of assuming the call
///         shape settles it.
///     </para>
/// </remarks>
[TestSubject(typeof(AnyInt128))]
public sealed class ModernTypeInvariantProperties {

    #region Statics members declarations

    /// <summary>The widest interval the exclusion property opens — small enough that excluding one value stays a visible event.</summary>
    private const int MaxWindow = 40;

    /// <summary>The <see cref="TimeOnly" /> domain split into blocks of a billion ticks: <c>863 * 1_000_000_000 + 999_999_999</c> is exactly <see cref="TimeOnly.MaxValue" />'s tick count.</summary>
    private const long TicksPerBlock = 1_000_000_000L;

    /// <summary>The number of whole billion-tick blocks in a day — see <see cref="TicksPerBlock" />.</summary>
    private const int TickBlocks = 863;

    /// <summary>The coarse magnitude the <see cref="Half" /> bounds stay within: every whole number up to 2048 is exactly representable, so a drawn bound survives the cast unrounded.</summary>
    private const int MaxHalfMagnitude = 1024;

    /// <summary>
    ///     A 64-bit word drawn over its whole range, assembled from three narrow draws so no single draw has to span
    ///     more than a 32-bit range. FsCheck's own numeric generators are size-bounded and cluster around zero, which
    ///     for the halves of a 128-bit value would mean "always a small number in a huge domain" — precisely the part
    ///     of the domain an ordinal mapping cannot get wrong.
    /// </summary>
    private static Gen<ulong> Word64() {
        return from high in Gen.Choose(0, (1 << 22) - 1)
               from middle in Gen.Choose(0, (1 << 21) - 1)
               from low in Gen.Choose(0, (1 << 21) - 1)
               // Added rather than or-ed: the three fields occupy disjoint bit ranges, so the sum is the same
               // word, without or-ing an operand the compiler sees as sign-extended (CS0675).
               select ((ulong)high << 42) + ((ulong)middle << 21) + (ulong)low;
    }

    /// <summary>Arbitrary <see cref="Int128" />s over the whole domain, biased towards the ends of the range and towards the sign change at zero.</summary>
    private static Gen<Int128> Int128Values() {
        Gen<Int128> anywhere = from upper in Word64()
                               from lower in Word64()
                               select new Int128(upper, lower);

        return Generators.WithEdges(anywhere, Int128.MinValue, Int128.MinValue + Int128.One, Int128.NegativeOne,
                                    Int128.Zero, Int128.One, Int128.MaxValue - Int128.One, Int128.MaxValue);
    }

    /// <summary>Arbitrary <see cref="UInt128" />s over the whole domain, biased towards the ends of the range — where an unsigned floor at zero hides its off-by-one.</summary>
    private static Gen<UInt128> UInt128Values() {
        Gen<UInt128> anywhere = from upper in Word64()
                                from lower in Word64()
                                select new UInt128(upper, lower);

        return Generators.WithEdges(anywhere, UInt128.MinValue, UInt128.One, UInt128.MaxValue - UInt128.One, UInt128.MaxValue);
    }

    /// <summary>
    ///     Arbitrary <see cref="Half" /> bounds: whole numbers within a coarse magnitude, plus the edges of the type.
    ///     Coarse is deliberate — <see cref="Half" /> carries about three decimal digits, so a bound drawn with more
    ///     precision than that would be testing the cast rather than the interval algebra.
    /// </summary>
    private static Gen<Half> Halves() {
        Gen<Half> anywhere = Gen.Choose(-MaxHalfMagnitude, MaxHalfMagnitude).Select(value => (Half)value);

        return Generators.WithEdges(anywhere, Half.MinValue, Half.NegativeOne, Half.Zero, Half.Epsilon, Half.One, Half.MaxValue);
    }

    /// <summary>Arbitrary dates over the whole <see cref="DateOnly" /> domain, its edges included: the day number is the ordinal the generator works in.</summary>
    private static Gen<DateOnly> Dates() {
        Gen<DateOnly> anywhere = Gen.Choose(DateOnly.MinValue.DayNumber, DateOnly.MaxValue.DayNumber).Select(dayNumber => DateOnly.FromDayNumber(dayNumber));

        return Generators.WithEdges(anywhere, DateOnly.MinValue, DateOnly.MinValue.AddDays(1),
                                    DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue);
    }

    /// <summary>
    ///     Arbitrary times of day over the whole <see cref="TimeOnly" /> domain, drawn as a tick count so the
    ///     sub-second end of the range is reached as often as the hours — a granularity property is only worth
    ///     anything when the values it constrains are tick-precise to begin with.
    /// </summary>
    private static Gen<TimeOnly> Times() {
        Gen<TimeOnly> anywhere = from block in Gen.Choose(0, TickBlocks)
                                 from offset in Gen.Choose(0, (int)TicksPerBlock - 1)
                                 select new TimeOnly(block * TicksPerBlock + offset);

        return Generators.WithEdges(anywhere, TimeOnly.MinValue, new TimeOnly(1),
                                    new TimeOnly(TimeOnly.MaxValue.Ticks - 1), TimeOnly.MaxValue);
    }

    /// <summary>
    ///     Arbitrary lattice steps for <see cref="AnyTimeOnly.WithGranularity" />, spanning the units a caller
    ///     actually asks for — a tick, a millisecond, a second, a minute, an hour — and deliberately reaching below
    ///     zero: a non-positive granularity is an argument error, and that half of the contract deserves the same
    ///     quantification as the lattice itself.
    /// </summary>
    private static Gen<TimeSpan> Granularities() {
        Gen<long> units = Gen.Elements(1L, TimeSpan.TicksPerMillisecond, TimeSpan.TicksPerSecond, TimeSpan.TicksPerMinute, TimeSpan.TicksPerHour);

        return from unit in units
               from count in Gen.Choose(-4, 24)
               select TimeSpan.FromTicks(unit * count);
    }

    #endregion

    [Fact(DisplayName = "Int128: Between contains — every draw falls within the declared inclusive bounds.")]
    public void Int128BetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Int128Values()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Int128().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: Between with equal bounds pins the value, for every value.")]
    public void Int128BetweenWithEqualBoundsPins() {
        Prop.ForAll(Int128Values().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Int128().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: crossed Between arguments are an argument error, never a silent swap.")]
    public void Int128CrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Int128Values()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Int128().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: GreaterThan is strict below Int128.MaxValue, and conflicts at it.")]
    public void Int128GreaterThanIsStrictAndConflictsAtTheCeiling() {
        Prop.ForAll(Int128Values().ToArbitrary(),
                    bound => bound == Int128.MaxValue
                                 ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int128().GreaterThan(bound))
                                 : Expect.EveryDraw(Any.Int128().GreaterThan(bound), value => value > bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: LessThan is strict above Int128.MinValue, and conflicts at it.")]
    public void Int128LessThanIsStrictAndConflictsAtTheFloor() {
        Prop.ForAll(Int128Values().ToArbitrary(),
                    bound => bound == Int128.MinValue
                                 ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int128().LessThan(bound))
                                 : Expect.EveryDraw(Any.Int128().LessThan(bound), value => value < bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: every exclusion-caused conflict message makes only true claims, over the whole combination space.")]
    public void Int128ConflictMessagesAreTruthful() {
        // The 128-bit sibling of the ordinal engine; the shared oracle lives in ConflictMessageTruthfulnessProperties.
        ConflictMessageTruthfulnessProperties.CheckEngine(BuildInt128, supportsLattice: true);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed",
                                                     Justification =
                                                         "The assignment is dead and the CALL is not. These builders exist to provoke the declaration-time conflict, " +
                                                         "so what matters is that Except() runs; nothing reads the spec afterwards because the verdict is the exception " +
                                                         "or its absence. Dropping `spec =` from the last line alone would break the uniform chain that makes the " +
                                                         "sequence of constraints readable.")]
    private static string? BuildInt128(bool hasBetween, int lo, int hi, int step, int[] allow, int[] excl) {
        try {
            AnyInt128 spec = Any.Int128();
            if (hasBetween) { spec = spec.Between(lo, hi); }
            if (step > 1)   { spec = spec.MultipleOf(step); }
            if (allow.Length > 0) { spec = spec.OneOf(allow.Select(value => (Int128)value).ToArray()); }
            if (excl.Length  > 0) { spec = spec.Except(excl.Select(value => (Int128)value).ToArray()); }

            return null;
        } catch (ConflictingAnyConstraintException exception) { return exception.Message; }
    }

    [Fact(DisplayName = "Int128: Positive and Negative meet a bound on their own side of zero, and conflict with one on the other.")]
    public void Int128SignConstraintsMeetABoundOrConflict() {
        Prop.ForAll(Int128Values().ToArbitrary(),
                    bound => {
                        // Positive() has already pinned the minimum to one, so a ceiling at or below zero leaves the
                        // interval empty — and the library owes a conflict at the fluent call, not a failure at
                        // Generate(). The mirror image holds for Negative() and a floor at or above zero.
                        bool positive = bound <= Int128.Zero
                                            ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int128().Positive().LessThanOrEqualTo(bound))
                                            : Expect.EveryDraw(Any.Int128().Positive().LessThanOrEqualTo(bound),
                                                               value => value > Int128.Zero && value <= bound);
                        bool negative = bound >= Int128.Zero
                                            ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int128().Negative().GreaterThanOrEqualTo(bound))
                                            : Expect.EveryDraw(Any.Int128().Negative().GreaterThanOrEqualTo(bound),
                                                               value => value < Int128.Zero && value >= bound);

                        return positive && negative;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: MultipleOf puts every draw on the lattice, and rejects a step that is not strictly positive.")]
    public void Int128MultipleOfPutsEveryDrawOnTheGrid() {
        Prop.ForAll(Int128Values().ToArbitrary(),
                    step => step <= Int128.Zero
                                ? Expect.Throws<ArgumentOutOfRangeException>(() => Any.Int128().MultipleOf(step))
                                : Expect.EveryDraw(Any.Int128().MultipleOf(step), value => value % step == Int128.Zero))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int128: Except removes the value from the interval, whatever the interval and the value.")]
    public void Int128ExceptRemovesTheValueFromTheInterval() {
        // The window is anchored on a drawn 64-bit value and widened by at most MaxWindow, so it reaches far into
        // the 128-bit domain while its top can never overflow past Int128.MaxValue.
        Gen<(Int128 Start, int Span, int Offset)> windows = from start in Generators.Int64()
                                                            from span in Gen.Choose(0, MaxWindow)
                                                            from offset in Gen.Choose(0, span)
                                                            select ((Int128)start, span, offset);

        Prop.ForAll(windows.ToArbitrary(),
                    window => {
                        Int128 minimum  = window.Start;
                        Int128 maximum  = minimum + window.Span;
                        Int128 excluded = minimum + window.Offset;

                        // Excluding the single value of a pinned interval empties it: that is a conflict, not a draw.
                        if (window.Span == 0) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Int128().Between(minimum, maximum).Except(excluded));
                        }

                        return Expect.EveryDraw(Any.Int128().Between(minimum, maximum).Except(excluded),
                                                value => value != excluded && value >= minimum && value <= maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt128: Between contains — every draw falls within the declared inclusive bounds.")]
    public void UInt128BetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(UInt128Values()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.UInt128().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt128: Between with equal bounds pins the value, for every value.")]
    public void UInt128BetweenWithEqualBoundsPins() {
        Prop.ForAll(UInt128Values().ToArbitrary(),
                    value => Expect.EveryDraw(Any.UInt128().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt128: the exclusive bounds are strict, and conflict at the ends of an unsigned domain.")]
    public void UInt128ExclusiveBoundsAreStrictAndConflictAtTheDomainEdges() {
        Prop.ForAll(UInt128Values().ToArbitrary(),
                    bound => {
                        // Nothing lies above UInt128.MaxValue, and — the unsigned specificity — nothing below zero:
                        // there the exclusive bound empties the domain rather than narrowing it.
                        bool above = bound == UInt128.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.UInt128().GreaterThan(bound))
                                         : Expect.EveryDraw(Any.UInt128().GreaterThan(bound), value => value > bound);
                        bool below = bound == UInt128.MinValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.UInt128().LessThan(bound))
                                         : Expect.EveryDraw(Any.UInt128().LessThan(bound), value => value < bound);

                        return above && below;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt128: MultipleOf puts every draw on the lattice, and rejects a step of zero.")]
    public void UInt128MultipleOfPutsEveryDrawOnTheGrid() {
        Prop.ForAll(UInt128Values().ToArbitrary(),
                    step => step == UInt128.Zero
                                ? Expect.Throws<ArgumentOutOfRangeException>(() => Any.UInt128().MultipleOf(step))
                                : Expect.EveryDraw(Any.UInt128().MultipleOf(step), value => value % step == UInt128.Zero))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt128: OneOf draws only from the supplied pool, whatever the pool.")]
    public void UInt128OneOfStaysWithinItsPool() {
        Gen<UInt128[]> pools = Gen.NonEmptyListOf(UInt128Values()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.UInt128().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Half: every draw is finite — NaN and the infinities are never generated, whatever the seed.")]
    public void HalfDrawsAreAlwaysFiniteWhateverTheSeed() {
        // Quantifying over the seed is what an example cannot do: the guarantee is about every sequence the
        // generator can ever produce, not about the one the ambient context happens to produce today.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => Expect.EveryDraw(Any.WithSeed(seed).Half(),
                                             value => !Half.IsNaN(value) && !Half.IsInfinity(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Half: Between contains — every draw falls within the declared inclusive bounds.")]
    public void HalfBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Halves()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Half().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Half: crossed Between arguments are an argument error, not a conflict — argument validation comes first.")]
    public void HalfCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Halves()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Half().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Half: Positive and Negative meet a bound on their own side of zero, and conflict with one on the other.")]
    public void HalfSignConstraintsMeetABoundOrConflict() {
        Prop.ForAll(Halves().ToArbitrary(),
                    bound => {
                        // Positive() pins the minimum to the smallest representable half above zero, so no positive
                        // bound can ever fall below it: the legality line sits exactly at zero, on both sides.
                        bool positive = bound <= Half.Zero
                                            ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Half().Positive().LessThanOrEqualTo(bound))
                                            : Expect.EveryDraw(Any.Half().Positive().LessThanOrEqualTo(bound),
                                                               value => value > Half.Zero && value <= bound);
                        bool negative = bound >= Half.Zero
                                            ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Half().Negative().GreaterThanOrEqualTo(bound))
                                            : Expect.EveryDraw(Any.Half().Negative().GreaterThanOrEqualTo(bound),
                                                               value => value < Half.Zero && value >= bound);

                        return positive && negative;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Half: Zero pins the value, and only excluding zero itself empties it.")]
    public void HalfZeroPinsUnlessTheExclusionEmptiesIt() {
        Prop.ForAll(Halves().ToArbitrary(),
                    excluded => excluded == Half.Zero
                                    ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Half().Zero().DifferentFrom(excluded))
                                    : Expect.EveryDraw(Any.Half().Zero().DifferentFrom(excluded), value => value == Half.Zero))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateOnly: Between contains — every draw falls within the declared inclusive dates.")]
    public void DateOnlyBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Dates()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.DateOnly().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateOnly: Between with equal dates pins the value, for every date.")]
    public void DateOnlyBetweenWithEqualBoundsPins() {
        Prop.ForAll(Dates().ToArbitrary(),
                    date => Expect.EveryDraw(Any.DateOnly().Between(date, date), drawn => drawn == date))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateOnly: After and Before are exclusive, and conflict at the edges of the domain.")]
    public void DateOnlyAfterAndBeforeAreExclusive() {
        Prop.ForAll(Dates().ToArbitrary(),
                    date => {
                        // No date lies after DateOnly.MaxValue, and none before DateOnly.MinValue: there the
                        // exclusive bound empties the domain, and the library owes a conflict at the fluent call.
                        bool after = date == DateOnly.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(date))
                                         : Expect.EveryDraw(Any.DateOnly().After(date), value => value > date);
                        bool before = date == DateOnly.MinValue
                                          ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().Before(date))
                                          : Expect.EveryDraw(Any.DateOnly().Before(date), value => value < date);

                        return after && before;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateOnly: AfterOrEqualTo and BeforeOrEqualTo keep their own bound, for every date.")]
    public void DateOnlyInclusiveBoundsKeepTheirEdge() {
        Prop.ForAll(Dates().ToArbitrary(),
                    date => {
                        // A half-bounded draw only shows the bound is respected — an exclusive reading would pass
                        // that too. Closing the interval on the very same date is what proves it inclusive: read
                        // exclusively, those two constraints would leave nothing to draw.
                        bool lower  = Expect.EveryDraw(Any.DateOnly().AfterOrEqualTo(date), value => value >= date);
                        bool upper  = Expect.EveryDraw(Any.DateOnly().BeforeOrEqualTo(date), value => value <= date);
                        bool closed = Expect.EveryDraw(Any.DateOnly().AfterOrEqualTo(date).BeforeOrEqualTo(date), value => value == date);

                        return lower && upper && closed;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DateOnly: crossed Between arguments are an argument error, never a silent swap.")]
    public void DateOnlyCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Dates()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.DateOnly().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeOnly: Between contains — every draw falls within the declared inclusive times of day.")]
    public void TimeOnlyBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Times()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.TimeOnly().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeOnly: After and Before are exclusive, AfterOrEqualTo and BeforeOrEqualTo inclusive, at every time of day.")]
    public void TimeOnlyBoundsCarryTheirInclusivity() {
        Prop.ForAll(Times().ToArbitrary(),
                    time => {
                        // A time of day does not wrap: nothing lies after the last tick of the day, nor before
                        // midnight, so both exclusive bounds empty the domain at their own end of it.
                        bool after = time == TimeOnly.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().After(time))
                                         : Expect.EveryDraw(Any.TimeOnly().After(time), value => value > time);
                        bool before = time == TimeOnly.MinValue
                                          ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().Before(time))
                                          : Expect.EveryDraw(Any.TimeOnly().Before(time), value => value < time);
                        // Closing the interval on the very same time is what proves the other pair inclusive: read
                        // exclusively, those two constraints would leave nothing to draw.
                        bool closed = Expect.EveryDraw(Any.TimeOnly().AfterOrEqualTo(time).BeforeOrEqualTo(time), value => value == time);

                        return after && before && closed;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "TimeOnly: WithGranularity puts every draw on the lattice anchored at midnight, and rejects a non-positive step.")]
    public void TimeOnlyWithGranularityPutsEveryDrawOnTheLattice() {
        // The anchor is TimeOnly.MinValue, not the drawn value: the lattice belongs to the domain, so a granularity
        // yields the same grid whatever else has been declared — and the value is built on it, never snapped onto it.
        Prop.ForAll(Granularities().ToArbitrary(),
                    granularity => granularity <= TimeSpan.Zero
                                       ? Expect.Throws<ArgumentOutOfRangeException>(() => Any.TimeOnly().WithGranularity(granularity))
                                       : Expect.EveryDraw(Any.TimeOnly().WithGranularity(granularity),
                                                          value => (value.Ticks - TimeOnly.MinValue.Ticks) % granularity.Ticks == 0L))
            .QuickCheckThrowOnFailure();
    }

}

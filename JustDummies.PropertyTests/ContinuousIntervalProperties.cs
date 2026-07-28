#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the continuous interval algebra — <see cref="AnyDouble" />, <see cref="AnySingle" />
///     and <see cref="AnyDecimal" />. Where the example-based suite pins a couple of hand-picked ranges
///     (<c>Between(1, 2)</c>), these quantify over the whole bound space: the finite ends of each domain, the
///     off-by-one representable neighbours around them, degenerate pinned intervals, and the non-finite arguments the
///     binary floating-point generators must refuse rather than propagate.
/// </summary>
/// <remarks>
///     The three types share one contract but not one engine — <c>double</c> and <c>float</c> ride a binary
///     next-representable ladder, <c>decimal</c> expresses exclusive bounds as an inclusive bound plus a point
///     exclusion — so each invariant is stated once per type rather than once for a representative. The last two
///     properties are the reachability guard for issue #206, generalized: the historical defect was not that a draw
///     left its range but that half of every range was silently unreachable, which only a property over the interval
///     itself, not over a fixed one, can rule out across the constraint space.
/// </remarks>
[TestSubject(typeof(AnyDouble))]
public sealed class ContinuousIntervalProperties {

    #region Statics members declarations

    /// <summary>
    ///     Draws per reachability case: large enough that a uniform sampler covers both halves of a range with
    ///     overwhelming probability, small enough that a hundred FsCheck cases stay cheap.
    /// </summary>
    private const int ReachabilityDrawCount = 300;

    /// <summary>The magnitude an arbitrary number stays within unless the declared bounds leave no room (ADR-0052).</summary>
    private const double OrdinaryMagnitude = 1_000_000d;

    /// <summary>
    ///     Finiteness, spelled the way the .NET Framework 4.7.2 floor leg understands: <c>double.IsFinite</c> arrived
    ///     with .NET Core 3.0, and this suite is built against the support floor too.
    /// </summary>
    private static bool IsFinite(double value) {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    ///     The interval a generator actually draws from: the declared one clipped to the ordinary magnitude window,
    ///     or the declared one untouched when that clip would leave nothing (ADR-0052).
    /// </summary>
    /// <remarks>
    ///     Mirrored here so the two reachability properties can name the range they expect covered. The split of
    ///     responsibility is deliberate: those properties own <i>the sampler covers the range it draws from</i> —
    ///     issue #206 was a bit-level defect in assembling the fraction, magnitude-independent, and a fraction stuck
    ///     below one half still fails them with this helper in place. That the range is the <i>right</i> one is owned
    ///     by the windowing properties, which assert it against the API rather than against a mirror.
    /// </remarks>
    private static (double Min, double Max) DrawnFrom(double min, double max) {
        double lower = Math.Max(min, -OrdinaryMagnitude);
        double upper = Math.Min(max, OrdinaryMagnitude);

        return lower > upper ? (min, max) : (lower, upper);
    }

    /// <summary>The <see cref="decimal" /> counterpart of <see cref="DrawnFrom(double,double)" />.</summary>
    private static (decimal Min, decimal Max) DrawnFrom(decimal min, decimal max) {
        decimal lower = Math.Max(min, -(decimal)OrdinaryMagnitude);
        decimal upper = Math.Min(max, (decimal)OrdinaryMagnitude);

        return lower > upper ? (min, max) : (lower, upper);
    }

    /// <summary>Arbitrary finite <see cref="float" />s — the <c>Generators.Double()</c> recipe on the narrow type.</summary>
    private static Gen<float> Singles() {
        return Generators.WithEdges(ArbMap.Default.GeneratorFor<float>().Where(value => !float.IsNaN(value) && !float.IsInfinity(value)),
                                    float.MinValue, -1f, 0f, 1f, float.MaxValue);
    }

    /// <summary>
    ///     Arbitrary <see cref="decimal" />s of moderate magnitude, with a few decimal places. Used where a constraint
    ///     adds a point exclusion (<c>GreaterThan</c>, <c>LessThan</c>): <see cref="decimal" /> has no
    ///     next-representable ladder, so the engine steps a colliding draw by <c>1E-28</c> — an increment that vanishes
    ///     in rounding near <see cref="decimal.MaxValue" /> and fails the generation loudly. That documented
    ///     extreme-magnitude behaviour is not the invariant under test, so the bounds stay well inside it.
    /// </summary>
    private static Gen<decimal> ModerateDecimals() {
        return Gen.Choose(-1_000_000_000, 1_000_000_000).Select(value => value / 1000m);
    }

    /// <summary>The three values a floating-point generator must refuse as a bound instead of quietly carrying.</summary>
    private static Gen<double> NonFiniteDoubles() {
        return Gen.Elements(new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity });
    }

    /// <summary>The <see cref="float" /> counterpart of <see cref="NonFiniteDoubles" />.</summary>
    private static Gen<float> NonFiniteSingles() {
        return Gen.Elements(new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity });
    }

    /// <summary>
    ///     Seeded, comfortably wide <see cref="double" /> intervals at three magnitudes. Deliberately kept away from the
    ///     domain edges: reachability asks whether the sampler covers a range, and a midpoint taken over the full domain
    ///     cannot be formed without the arithmetic itself becoming the subject.
    /// </summary>
    private static Gen<(int Seed, double Min, double Max)> DoubleIntervals() {
        return from seed in Generators.Seed()
               from low in Gen.Choose(-1_000_000, 1_000_000)
               from width in Gen.Choose(1, 1_000_000)
               from unit in Gen.Elements(new[] { 0.0001d, 1d, 1000d })
               select (Seed: seed, Min: low * unit, Max: (low + width) * unit);
    }

    /// <summary>The <see cref="decimal" /> counterpart of <see cref="DoubleIntervals" />.</summary>
    private static Gen<(int Seed, decimal Min, decimal Max)> DecimalIntervals() {
        return from seed in Generators.Seed()
               from low in Gen.Choose(-1_000_000, 1_000_000)
               from width in Gen.Choose(1, 1_000_000)
               from unit in Gen.Elements(new[] { 0.0001m, 1m, 1000m })
               select (Seed: seed, Min: low * unit, Max: (low + width) * unit);
    }

    #endregion

    [Fact(DisplayName = "Unconstrained double and float draws are finite, whatever the seed.")]
    public void UnconstrainedDrawsAreAlwaysFinite() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext any = Any.WithSeed(seed);

                        return Expect.EveryDraw(any.Double(), value => !double.IsNaN(value) && !double.IsInfinity(value))
                               && Expect.EveryDraw(any.Single(), value => !float.IsNaN(value) && !float.IsInfinity(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: Between contains — every draw falls within the declared inclusive bounds.")]
    public void DoubleBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Generators.Double()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Double().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: Between contains — the quantized draw never escapes the bounds it was narrowed to.")]
    public void SingleBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Singles()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Single().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: Between contains, across the whole decimal range.")]
    public void DecimalBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Generators.Decimal()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Decimal().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: Between with equal bounds pins the value, for every value.")]
    public void DoubleBetweenWithEqualBoundsPins() {
        Prop.ForAll(Generators.Double().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Double().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: Between with equal bounds pins the value, for every value.")]
    public void SingleBetweenWithEqualBoundsPins() {
        Prop.ForAll(Singles().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Single().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: Between with equal bounds pins the value, for every value.")]
    public void DecimalBetweenWithEqualBoundsPins() {
        Prop.ForAll(Generators.Decimal().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Decimal().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: GreaterThanOrEqualTo and LessThanOrEqualTo are inclusive — the bound itself stays legal.")]
    public void DoubleInclusiveBoundsAreInclusive() {
        Prop.ForAll(Generators.Double().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Double().GreaterThanOrEqualTo(bound), value => value >= bound)
                             && Expect.EveryDraw(Any.Double().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: GreaterThanOrEqualTo and LessThanOrEqualTo are inclusive — the bound itself stays legal.")]
    public void SingleInclusiveBoundsAreInclusive() {
        Prop.ForAll(Singles().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Single().GreaterThanOrEqualTo(bound), value => value >= bound)
                             && Expect.EveryDraw(Any.Single().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: GreaterThanOrEqualTo and LessThanOrEqualTo are inclusive — the bound itself stays legal.")]
    public void DecimalInclusiveBoundsAreInclusive() {
        Prop.ForAll(Generators.Decimal().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Decimal().GreaterThanOrEqualTo(bound), value => value >= bound)
                             && Expect.EveryDraw(Any.Decimal().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: GreaterThan and LessThan are strict, and conflict at the finite ends of the domain.")]
    public void DoubleStrictBoundsAreStrictAndConflictAtTheDomainEnds() {
        Prop.ForAll(Generators.Double().ToArbitrary(),
                    bound => {
                        // Nothing representable lies above double.MaxValue or below double.MinValue, so the exclusive
                        // bound has no value left to name: a conflict at declaration, not an empty draw at generation.
                        bool above = bound == double.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(bound))
                                         : Expect.EveryDraw(Any.Double().GreaterThan(bound), value => value > bound);
                        bool below = bound == double.MinValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Double().LessThan(bound))
                                         : Expect.EveryDraw(Any.Double().LessThan(bound), value => value < bound);

                        return above && below;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: GreaterThan and LessThan are strict on the float ladder, and conflict at its ends.")]
    public void SingleStrictBoundsAreStrictAndConflictAtTheDomainEnds() {
        Prop.ForAll(Singles().ToArbitrary(),
                    bound => {
                        // The step is taken on the float ladder, not the double one: a sub-ulp double step would
                        // re-quantize onto the same float and stall the strictness this asserts.
                        bool above = bound == float.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Single().GreaterThan(bound))
                                         : Expect.EveryDraw(Any.Single().GreaterThan(bound), value => value > bound);
                        bool below = bound == float.MinValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Single().LessThan(bound))
                                         : Expect.EveryDraw(Any.Single().LessThan(bound), value => value < bound);

                        return above && below;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: GreaterThan and LessThan are strict — the inclusive bound plus a point exclusion.")]
    public void DecimalStrictBoundsAreStrict() {
        Prop.ForAll(ModerateDecimals().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Decimal().GreaterThan(bound), value => value > bound)
                             && Expect.EveryDraw(Any.Decimal().LessThan(bound), value => value < bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Positive and Negative are strict, Zero pins and NonZero excludes — for all three types, whatever the seed.")]
    public void SignConstraintsHoldForEverySeed() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext any = Any.WithSeed(seed);

                        return Expect.EveryDraw(any.Double().Positive(), value => value > 0d)
                               && Expect.EveryDraw(any.Double().Negative(), value => value < 0d)
                               && Expect.EveryDraw(any.Double().Zero(), value => value == 0d)
                               && Expect.EveryDraw(any.Double().NonZero(), value => value != 0d)
                               && Expect.EveryDraw(any.Single().Positive(), value => value > 0f)
                               && Expect.EveryDraw(any.Single().Negative(), value => value < 0f)
                               && Expect.EveryDraw(any.Single().Zero(), value => value == 0f)
                               && Expect.EveryDraw(any.Single().NonZero(), value => value != 0f)
                               && Expect.EveryDraw(any.Decimal().Positive(), value => value > 0m)
                               && Expect.EveryDraw(any.Decimal().Negative(), value => value < 0m)
                               && Expect.EveryDraw(any.Decimal().Zero(), value => value == 0m)
                               && Expect.EveryDraw(any.Decimal().NonZero(), value => value != 0m);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: NaN and the infinities are rejected as argument errors by every entry point taking a bound.")]
    public void DoubleRejectsNonFiniteArguments() {
        Prop.ForAll((from finite in Generators.Double()
                     from nonFinite in NonFiniteDoubles()
                     select (finite, nonFinite)).ToArbitrary(),
                    testCase => Expect.Throws<ArgumentException>(() => Any.Double().GreaterThan(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().GreaterThanOrEqualTo(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().LessThan(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().LessThanOrEqualTo(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().Between(testCase.nonFinite, testCase.finite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().Between(testCase.finite, testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().OneOf(testCase.finite, testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().Except(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Double().DifferentFrom(testCase.nonFinite)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: NaN and the infinities are rejected as argument errors by every entry point taking a bound.")]
    public void SingleRejectsNonFiniteArguments() {
        Prop.ForAll((from finite in Singles()
                     from nonFinite in NonFiniteSingles()
                     select (finite, nonFinite)).ToArbitrary(),
                    testCase => Expect.Throws<ArgumentException>(() => Any.Single().GreaterThan(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().GreaterThanOrEqualTo(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().LessThan(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().LessThanOrEqualTo(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().Between(testCase.nonFinite, testCase.finite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().Between(testCase.finite, testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().OneOf(testCase.finite, testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().Except(testCase.nonFinite))
                                && Expect.Throws<ArgumentException>(() => Any.Single().DifferentFrom(testCase.nonFinite)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: crossed Between arguments are an argument error, never a silent swap.")]
    public void DoubleCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Generators.Double()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Double().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: crossed Between arguments are an argument error, never a silent swap.")]
    public void SingleCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Singles()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Single().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: crossed Between arguments are an argument error, never a silent swap.")]
    public void DecimalCrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Generators.Decimal()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Decimal().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: OneOf draws only from the supplied pool, whatever the pool.")]
    public void DoubleOneOfStaysWithinItsPool() {
        Gen<double[]> pools = Gen.NonEmptyListOf(Generators.Double()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Double().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Single: OneOf draws only from the supplied pool, whatever the pool.")]
    public void SingleOneOfStaysWithinItsPool() {
        Gen<float[]> pools = Gen.NonEmptyListOf(Singles()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Single().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: OneOf draws only from the supplied pool, whatever the pool.")]
    public void DecimalOneOfStaysWithinItsPool() {
        Gen<decimal[]> pools = Gen.NonEmptyListOf(Generators.Decimal()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Decimal().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An unconstrained draw is an ordinary number, whatever the seed — arithmetic on it stays finite.")]
    public void UnconstrainedDrawsAreOrdinary() {
        // ADR-0052. Stated as arithmetic rather than as a magnitude on purpose: what a dummy owes its test is that
        // using it does not sabotage the test. Before this, a sixth of Positive() doubles overflowed to Infinity on
        // a single multiplication, and the decimal equivalent threw OverflowException.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext any = Any.WithSeed(seed);

                        return Expect.EveryDraw(any.Double(), value => IsFinite(value * 1.2d))
                            && Expect.EveryDraw(any.Double().Positive(), value => IsFinite(value * 1.2d) && value > 0d)
                            && Expect.EveryDraw(any.Single(), value => IsFinite(value * 1.2f))
                            && Expect.EveryDraw(any.Decimal(), value => Expect.DoesNotThrow(() => _ = value * 1.2m));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An interval that merely permits large values still yields ordinary ones, for every upper bound.")]
    public void PermittingALargeValueIsNotRequestingOne() {
        // The heart of ADR-0052: a bound is a permission, not a request. Any.Double().LessThan(huge) says what the
        // value may not exceed, so widening that bound must not enlarge the draw — exactly as a size maximum does
        // not enlarge a string under ADR-0050.
        Prop.ForAll(Gen.Elements(1e7d, 1e50d, 1e200d, 1e308d, double.MaxValue).ToArbitrary(),
                    permitted => Expect.EveryDraw(Any.Double().Between(0d, permitted), value => value <= OrdinaryMagnitude)
                              && Expect.EveryDraw(Any.Double().LessThan(permitted), value => Math.Abs(value) <= OrdinaryMagnitude))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "An interval lying wholly beyond the ordinary window is drawn from as declared, for every such interval.")]
    public void AnIntervalBeyondTheWindowIsHonouredAsDeclared() {
        // The other half of the rule, and the one that keeps it from being a silent cap: a caller who names a
        // magnitude gets that magnitude. Without this the window would not clip the draw, it would break the bound.
        Prop.ForAll(Generators.OrderedPair(Gen.Elements(1e7d, 1e20d, 1e100d, 1e250d, 1e307d)).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                           || Expect.EveryDraw(Any.Double().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Decimal: repeated draws over an arbitrary interval straddle its midpoint — neither half is unreachable.")]
    public void DecimalBetweenReachesBothHalves() {
        // Issue #206, generalized from its fixed 0..100 regression: the fraction was assembled from three
        // non-negative Random.Next() draws, so each limb's top bit stayed zero, the fraction never crossed ~0.5,
        // and every value of every range landed in its lower half. Membership held throughout — only reachability
        // caught it, and only a property over the interval itself proves it for intervals nobody thought to pin.
        Prop.ForAll(DecimalIntervals().ToArbitrary(),
                    interval => {
                        (decimal Min, decimal Max) drawn = DrawnFrom(interval.Min, interval.Max);
                        decimal midpoint = drawn.Min / 2m + drawn.Max / 2m;

                        List<decimal> values = Expect.Draws(Any.WithSeed(interval.Seed).Decimal().Between(interval.Min, interval.Max),
                                                            ReachabilityDrawCount);

                        return values.Min() < midpoint && values.Max() > midpoint;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Double: repeated draws over an arbitrary interval straddle its midpoint — neither half is unreachable.")]
    public void DoubleBetweenReachesBothHalves() {
        // The binary engine samples as midpoint ± half rather than by interpolation, so it fails differently from
        // the decimal one — which is exactly why the guard is stated per engine instead of once for a representative.
        Prop.ForAll(DoubleIntervals().ToArbitrary(),
                    interval => {
                        (double Min, double Max) drawn = DrawnFrom(interval.Min, interval.Max);
                        double midpoint = drawn.Min / 2d + drawn.Max / 2d;

                        List<double> values = Expect.Draws(Any.WithSeed(interval.Seed).Double().Between(interval.Min, interval.Max),
                                                           ReachabilityDrawCount);

                        return values.Min() < midpoint && values.Max() > midpoint;
                    })
            .QuickCheckThrowOnFailure();
    }

}

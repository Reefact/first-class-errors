#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the three lattice constraints — <c>MultipleOf</c> on the integers,
///     <see cref="AnyDecimal.WithScale" /> on <see cref="decimal" />, and <c>WithGranularity</c> on the temporals.
///     Where the example-based suite pins a handful of hand-picked steps (<c>MultipleOf(100)</c>,
///     <c>WithScale(2)</c>, <c>WithGranularity(15 minutes)</c>) and can only prove the grid right for those, these
///     quantify over the whole step space: the step, the interval it must live inside, and the allow-list it must
///     intersect are all drawn by FsCheck, so a grid that drifts off its anchor, empties without saying so, or
///     silently escapes its declared range is found and shrunk to its minimal counter-example.
/// </summary>
/// <remarks>
///     Two rules shape almost every property here. A lattice is <b>declared once</b>, but the second declaration is
///     only a conflict when it really is a second lattice — a step of one (and a granularity of one tick) is a
///     no-op, and re-declaring the same step is idempotent — so the properties branch on the drawn value rather
///     than assuming the call shape decides. And <c>WithScale</c> is a <b>value</b> lattice, not a representation
///     contract: the drawn value lies on the <c>10^-scale</c> grid but is not padded with trailing zeros, so it is
///     checked with <c>Math.Round(value, scale)</c> and never through <c>decimal.GetBits</c> or <c>ToString()</c>.
/// </remarks>
[TestSubject(typeof(AnyDecimal))]
public sealed class LatticeConstraintProperties {

    #region Statics members declarations

    /// <summary>
    ///     A strictly positive lattice step, kept modest so the constrained domain never thins out to nothing and
    ///     the draw stays cheap — the invariant under test is about the grid, not about arithmetic at 2^31.
    /// </summary>
    private static Gen<int> Steps() {
        return Gen.Choose(1, 1000);
    }

    /// <summary>
    ///     A strictly positive granularity: fine tick-level steps mixed with the round durations real code asks for.
    ///     All stay far below the width of every temporal domain, so the lattice is never empty on its own.
    /// </summary>
    private static Gen<TimeSpan> Granularities() {
        TimeSpan[] realistic = [
            TimeSpan.FromTicks(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1),
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromDays(1)
        ];

        return Gen.OneOf(Gen.Choose(1, 10000).Select(ticks => TimeSpan.FromTicks(ticks)), Gen.Elements(realistic));
    }

    /// <summary>
    ///     A granularity drawn from a deliberately tiny pool, so two independent draws collide often enough to
    ///     exercise the idempotent re-declaration alongside the conflicting one.
    /// </summary>
    private static Gen<TimeSpan> CollidingGranularities() {
        TimeSpan[] pool = [TimeSpan.FromTicks(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)];

        return Gen.Elements(pool);
    }

    /// <summary>
    ///     Whether the inclusive interval [<paramref name="minimum" />, <paramref name="maximum" />] holds at least
    ///     one multiple of <paramref name="step" />. The oracle strides down from the top of the interval in integer
    ///     arithmetic rather than reusing the library's own lattice walk, so it cannot inherit the very off-by-one it
    ///     is meant to catch.
    /// </summary>
    private static bool ContainsMultiple(int minimum, int maximum, int step) {
        int remainder = maximum % step;                                              // C# gives the remainder the sign of the dividend
        int largest   = maximum - (remainder < 0 ? remainder + step : remainder);    // the largest multiple at or below the maximum

        return largest >= minimum;
    }

    /// <summary>
    ///     One step of the <c>10^-scale</c> grid, built by exact <see cref="decimal" /> division so the oracle stays
    ///     free of the binary rounding a <c>double</c> power would smuggle in.
    /// </summary>
    private static decimal GridStep(int scale) {
        decimal step = 1m;
        for (int i = 0; i < scale; i++) { step /= 10m; }

        return step;
    }

    #endregion

    [Fact(DisplayName = "MultipleOf: every draw of every integer width lands on the grid, for every step.")]
    public void MultipleOfLandsOnTheGrid() {
        Prop.ForAll((from step in Steps()
                     from narrowStep in Gen.Choose(1, byte.MaxValue)
                     select (step, narrowStep)).ToArbitrary(),
                    testCase => {
                        int  signedStep   = testCase.step;
                        uint unsignedStep = (uint)testCase.step;
                        byte byteStep     = (byte)testCase.narrowStep;

                        // The signed widths and the unsigned ones map onto the shared ordinal engine differently;
                        // the grid is anchored at zero for all of them, so the invariant reads the same everywhere.
                        return Expect.EveryDraw(Any.Int32().MultipleOf(signedStep), value => value % signedStep == 0)
                               && Expect.EveryDraw(Any.Int64().MultipleOf(signedStep), value => value % signedStep == 0)
                               && Expect.EveryDraw(Any.UInt32().MultipleOf(unsignedStep), value => value % unsignedStep == 0)
                               && Expect.EveryDraw(Any.Byte().MultipleOf(byteStep), value => value % byteStep == 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "MultipleOf: an interval keeps the grid inside it, and an interval holding no grid point conflicts.")]
    public void MultipleOfComposesWithAnInterval() {
        Prop.ForAll((from bounds in Generators.OrderedPair(Gen.Choose(-2000, 2000))
                     from step in Steps()
                     select (bounds, step)).ToArbitrary(),
                    testCase => {
                        int minimum  = testCase.bounds.Min;
                        int maximum  = testCase.bounds.Max;
                        int gridStep = testCase.step;

                        // An interval narrower than the step can fall entirely between two grid points — the whole
                        // point of drawing the bounds and the step independently. The lattice is then empty, and the
                        // library owes the caller a conflict at the fluent call, not a failure at Generate().
                        if (!ContainsMultiple(minimum, maximum, gridStep)) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(minimum, maximum).MultipleOf(gridStep));
                        }

                        return Expect.EveryDraw(Any.Int32().Between(minimum, maximum).MultipleOf(gridStep),
                                                value => value % gridStep == 0 && value >= minimum && value <= maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "MultipleOf: an allow-list is filtered to its grid points, and an allow-list missing them conflicts.")]
    public void MultipleOfFiltersAnAllowList() {
        Gen<int[]> pools = Gen.NonEmptyListOf(Gen.Choose(-200, 200)).Select(values => values.Distinct().ToArray());

        Prop.ForAll((from pool in pools
                     from step in Gen.Choose(1, 20)
                     select (pool, step)).ToArbitrary(),
                    testCase => {
                        int[] survivors = testCase.pool.Where(value => value % testCase.step == 0).ToArray();

                        // Nothing in the pool on the grid means the intersection is empty: eager conflict, again at
                        // the call. The example-based suite can only pin one pool; this quantifies over all of them.
                        if (survivors.Length == 0) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Int32().OneOf(testCase.pool).MultipleOf(testCase.step));
                        }

                        return Expect.EveryDraw(Any.Int32().OneOf(testCase.pool).MultipleOf(testCase.step),
                                                value => survivors.Contains(value));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "MultipleOf: a step that is not strictly positive is an argument error, for every such step.")]
    public void NonPositiveMultipleOfIsAnArgumentError() {
        Prop.ForAll(Gen.Choose(-1000, 0).ToArbitrary(),
                    step => Expect.Throws<ArgumentOutOfRangeException>(() => Any.Int32().MultipleOf(step))
                            && Expect.Throws<ArgumentOutOfRangeException>(() => Any.Int64().MultipleOf(step)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "MultipleOf: a second, genuinely different step conflicts; a no-op or a repeat does not.")]
    public void MultipleOfIsDeclaredOnce() {
        Prop.ForAll((from first in Gen.Choose(1, 6)
                     from second in Gen.Choose(1, 6)
                     select (first, second)).ToArbitrary(),
                    testCase => {
                        int firstStep  = testCase.first;
                        int secondStep = testCase.second;

                        // A step of one constrains nothing, and the same step twice is idempotent — neither is a
                        // second lattice. Only a real second lattice conflicts, so the verdict comes from the drawn
                        // values rather than from the call shape.
                        if (firstStep != secondStep && firstStep != 1 && secondStep != 1) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int32().MultipleOf(firstStep).MultipleOf(secondStep));
                        }

                        // In every accepted case exactly one of the two steps survives: the coarser one.
                        int surviving = Math.Max(firstStep, secondStep);

                        return Expect.EveryDraw(Any.Int32().MultipleOf(firstStep).MultipleOf(secondStep), value => value % surviving == 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithScale: every draw lies on the 10^-scale value grid, for every supported scale.")]
    public void WithScaleLandsOnTheDecimalGrid() {
        Prop.ForAll(Gen.Choose(0, 28).ToArbitrary(),
                    scale => Expect.EveryDraw(Any.Decimal().WithScale(scale),
                                              // A value lattice: the value is expressible in `scale` decimals. Its
                                              // rendered form is deliberately not asserted — the library pads nothing.
                                              value => Math.Round(value, scale, MidpointRounding.ToEven) == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithScale: a grid-aligned interval keeps every draw both in range and on the grid.")]
    public void WithScaleComposesWithABoundedInterval() {
        Prop.ForAll((from scale in Gen.Choose(0, 28)
                     from start in Gen.Choose(-1000, 1000)
                     from width in Gen.Choose(0, 1000)
                     select (scale, start, width)).ToArbitrary(),
                    testCase => {
                        // Bounds placed ON the grid and only a few grid points apart: the window is genuinely narrow,
                        // so the draw has to land on one of a handful of points instead of anywhere in a vast range.
                        // A zero width pins the interval to a single grid point — the degenerate corner kept on purpose.
                        decimal step    = GridStep(testCase.scale);
                        decimal minimum = testCase.start * step;
                        decimal maximum = (testCase.start + testCase.width) * step;

                        return Expect.EveryDraw(Any.Decimal().Between(minimum, maximum).WithScale(testCase.scale),
                                                value => Math.Round(value, testCase.scale, MidpointRounding.ToEven) == value
                                                         && value >= minimum
                                                         && value <= maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithScale: an interval lying strictly inside one grid cell conflicts at the call.")]
    public void WithScaleOnAnIntervalWithoutGridPointConflicts() {
        Prop.ForAll((from scale in Gen.Choose(0, 27)
                     from cell in Gen.Choose(-100, 100)
                     select (scale, cell)).ToArbitrary(),
                    testCase => {
                        // A window from a tenth to nine tenths of the way through one grid cell: it holds no value
                        // expressible in `scale` decimals, whichever cell and whichever scale were drawn. The scale
                        // stops at 27 so that a tenth of a step is still a representable decimal.
                        decimal step  = GridStep(testCase.scale);
                        decimal finer = GridStep(testCase.scale + 1);
                        decimal lower = testCase.cell * step + finer;
                        decimal upper = testCase.cell * step + 9m * finer;

                        return Expect.Throws<ConflictingAnyConstraintException>(
                            () => Any.Decimal().Between(lower, upper).WithScale(testCase.scale));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithScale: a scale outside [0, 28] is an argument error, for every such scale.")]
    public void WithScaleOutsideTheSupportedRangeIsAnArgumentError() {
        Prop.ForAll(Gen.OneOf(Gen.Choose(-1000, -1), Gen.Choose(29, 1000)).ToArbitrary(),
                    scale => Expect.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().WithScale(scale)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithScale: a second, different scale conflicts; the same scale again does not.")]
    public void WithScaleIsDeclaredOnce() {
        Prop.ForAll((from first in Gen.Choose(0, 6)
                     from second in Gen.Choose(0, 6)
                     select (first, second)).ToArbitrary(),
                    testCase => {
                        // Unlike MultipleOf, no scale is a no-op: scale zero is the integer grid, a constraint in its
                        // own right. Only re-declaring the very same scale is idempotent.
                        if (testCase.first != testCase.second) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Decimal().WithScale(testCase.first).WithScale(testCase.second));
                        }

                        return Expect.EveryDraw(Any.Decimal().WithScale(testCase.first).WithScale(testCase.second),
                                                value => Math.Round(value, testCase.first, MidpointRounding.ToEven) == value);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithGranularity: every draw sits on the grid anchored at its own type's origin.")]
    public void WithGranularityLandsOnTheAnchoredGrid() {
        Prop.ForAll(Granularities().ToArbitrary(),
                    granularity => {
                        long step = granularity.Ticks;

                        // The anchor is part of the contract and differs per type — TimeSpan.Zero for a duration,
                        // MinValue for an instant — so it is written out rather than folded into a bare modulo:
                        // an anchor drifting onto the wrong origin is exactly what this property exists to catch.
                        // AnyTimeSpan is the sharpest of the three, since its unconstrained domain is signed and a
                        // misplaced anchor shows up on the negative side.
                        return Expect.EveryDraw(Any.TimeSpan().WithGranularity(granularity),
                                                value => (value.Ticks - TimeSpan.Zero.Ticks) % step == 0)
                               && Expect.EveryDraw(Any.DateTime().WithGranularity(granularity),
                                                   value => (value.Ticks - DateTime.MinValue.Ticks) % step == 0)
                               // The DateTimeOffset lattice lives on the instant, which is what its own ordering
                               // compares; unconstrained, the offset is TimeSpan.Zero and the two tick counts agree.
                               && Expect.EveryDraw(Any.DateTimeOffset().WithGranularity(granularity),
                                                   value => (value.UtcTicks - DateTimeOffset.MinValue.UtcTicks) % step == 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithGranularity: a range keeps the grid inside it, on all three temporal types.")]
    public void WithGranularityComposesWithARange() {
        Prop.ForAll((from granularity in Granularities()
                     from signedStart in Gen.Choose(-1000, 1000)
                     from unsignedStart in Gen.Choose(0, 1000)
                     from width in Gen.Choose(0, 1000)
                     select (granularity, signedStart, unsignedStart, width)).ToArbitrary(),
                    testCase => {
                        // Bounds a whole number of granularities from each anchor, a few grid points apart: the
                        // window is narrow enough that a draw off the grid, or one step outside the range, shows up.
                        // The instant types start at or after their own minimum; the duration one straddles zero.
                        long           step         = testCase.granularity.Ticks;
                        TimeSpan       durationFrom = TimeSpan.FromTicks(testCase.signedStart * step);
                        TimeSpan       durationTo   = TimeSpan.FromTicks((testCase.signedStart + testCase.width) * step);
                        DateTime       instantFrom  = new(testCase.unsignedStart * step, DateTimeKind.Utc);
                        DateTime       instantTo    = new((testCase.unsignedStart + testCase.width) * step, DateTimeKind.Utc);
                        DateTimeOffset offsetFrom   = new(instantFrom.Ticks, TimeSpan.Zero);
                        DateTimeOffset offsetTo     = new(instantTo.Ticks, TimeSpan.Zero);

                        return Expect.EveryDraw(Any.TimeSpan().Between(durationFrom, durationTo).WithGranularity(testCase.granularity),
                                                value => value.Ticks % step == 0 && value >= durationFrom && value <= durationTo)
                               && Expect.EveryDraw(Any.DateTime().Between(instantFrom, instantTo).WithGranularity(testCase.granularity),
                                                   value => value.Ticks % step == 0 && value >= instantFrom && value <= instantTo)
                               && Expect.EveryDraw(Any.DateTimeOffset().Between(offsetFrom, offsetTo).WithGranularity(testCase.granularity),
                                                   value => value.UtcTicks % step == 0 && value >= offsetFrom && value <= offsetTo);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithGranularity: a granularity that is not strictly positive is an argument error.")]
    public void NonPositiveGranularityIsAnArgumentError() {
        Prop.ForAll(Gen.Choose(-10000, 0).Select(ticks => TimeSpan.FromTicks(ticks)).ToArbitrary(),
                    granularity => Expect.Throws<ArgumentOutOfRangeException>(() => Any.TimeSpan().WithGranularity(granularity))
                                   && Expect.Throws<ArgumentOutOfRangeException>(() => Any.DateTime().WithGranularity(granularity))
                                   && Expect.Throws<ArgumentOutOfRangeException>(() => Any.DateTimeOffset().WithGranularity(granularity)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithGranularity: a second, genuinely different granularity conflicts; a no-op or a repeat does not.")]
    public void WithGranularityIsDeclaredOnce() {
        Prop.ForAll((from first in CollidingGranularities()
                     from second in CollidingGranularities()
                     select (first, second)).ToArbitrary(),
                    testCase => {
                        TimeSpan declared   = testCase.first;
                        TimeSpan redeclared = testCase.second;

                        // One tick constrains nothing, and the same granularity twice is idempotent — the same rule
                        // as MultipleOf, since both ride the one lattice the interval engine carries.
                        if (declared != redeclared && declared.Ticks != 1 && redeclared.Ticks != 1) {
                            return Expect.Throws<ConflictingAnyConstraintException>(() => Any.TimeSpan().WithGranularity(declared).WithGranularity(redeclared))
                                   && Expect.Throws<ConflictingAnyConstraintException>(() => Any.DateTime().WithGranularity(declared).WithGranularity(redeclared));
                        }

                        long surviving = Math.Max(declared.Ticks, redeclared.Ticks);

                        return Expect.EveryDraw(Any.TimeSpan().WithGranularity(declared).WithGranularity(redeclared), value => value.Ticks % surviving == 0)
                               && Expect.EveryDraw(Any.DateTime().WithGranularity(declared).WithGranularity(redeclared), value => value.Ticks % surviving == 0);
                    })
            .QuickCheckThrowOnFailure();
    }

}

#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Behaviour of the lattice constraints — <c>MultipleOf</c> on the integers, <c>WithScale</c> on
///     <see cref="decimal" />, and <c>WithGranularity</c> on the temporals: values are built directly on the grid in
///     one draw, the grid composes with bounds/exclusions/allow-lists, and an empty grid conflicts eagerly.
/// </summary>
public sealed class AnyLatticeConstraintTests {

    private const int SampleCount = 200;

    #region MultipleOf

    [Fact(DisplayName = "MultipleOf: every drawn value is a multiple of the step.")]
    public void MultipleOfAlwaysDivisible() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int32().MultipleOf(100).Generate() % 100).IsEqualTo(0);
            Check.That(Any.Int64().Positive().MultipleOf(12L).Generate() % 12L).IsEqualTo(0L);
            Check.That(Any.Byte().MultipleOf(5).Generate()               % 5).IsEqualTo(0);
        }
    }

    [Fact(DisplayName = "MultipleOf: draws on the grid within the declared range and reaches both ends.")]
    public void MultipleOfHonoursRangeAndReachesBounds() {
        HashSet<int> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            int value = Any.Int32().Between(0, 1000).MultipleOf(100).Generate();
            Check.That(value % 100).IsEqualTo(0);
            Check.That(value >= 0 && value <= 1000).IsTrue();
            seen.Add(value);
        }

        Check.That(seen.Contains(0)).IsTrue();
        Check.That(seen.Contains(1000)).IsTrue();
    }

    [Fact(DisplayName = "MultipleOf: negative multiples are drawn on the grid too.")]
    public void MultipleOfHandlesNegativeGrid() {
        for (int i = 0; i < SampleCount; i++) {
            int value = Any.Int32().Between(-100, -1).MultipleOf(10).Generate();
            Check.That(value % 10).IsEqualTo(0);
            Check.That(value >= -100 && value <= -10).IsTrue();
        }
    }

    [Fact(DisplayName = "MultipleOf: composes with Except, never yielding an excluded grid point.")]
    public void MultipleOfComposesWithExcept() {
        HashSet<int> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            int value = Any.Int32().Between(0, 30).MultipleOf(10).Except(10, 20).Generate();
            Check.That(value % 10).IsEqualTo(0);
            Check.That(value != 10 && value != 20).IsTrue();
            seen.Add(value);
        }

        Check.That(seen.SetEquals([0, 30])).IsTrue();
    }

    [Fact(DisplayName = "MultipleOf: filters a OneOf allow-list to its members on the grid.")]
    public void MultipleOfFiltersAllowList() {
        for (int i = 0; i < SampleCount; i++) {
            int value = Any.Int32().OneOf(5, 10, 15, 20).MultipleOf(10).Generate();
            Check.That(value == 10 || value == 20).IsTrue();
        }
    }

    [Fact(DisplayName = "MultipleOf: one is a no-op; zero and negatives are rejected.")]
    public void MultipleOfArguments() {
        for (int i = 0; i < SampleCount; i++) { Any.Int32().MultipleOf(1).Generate(); } // no-op: any value

        Check.ThatCode(() => Any.Int32().MultipleOf(0)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.Int32().MultipleOf(-5)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.Byte().MultipleOf(0)).Throws<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "MultipleOf: an empty grid inside the range conflicts eagerly, naming the step.")]
    public void MultipleOfEmptyGridConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().Between(1, 9).MultipleOf(10));
        Check.That(conflict.Message).Contains("MultipleOf(10)");

        // The same emptiness, whichever order the two constraints arrive in.
        Check.ThatCode(() => Any.Int32().MultipleOf(10).Between(1, 9)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "MultipleOf: a second, different step is rejected as already declared.")]
    public void MultipleOfDeclaredOnce() {
        Check.ThatCode(() => Any.Int32().MultipleOf(4).MultipleOf(6)).Throws<ConflictingAnyConstraintException>();
        // The same step twice is idempotent, not a conflict.
        Check.That(Any.Int32().Between(0, 100).MultipleOf(10).MultipleOf(10).Generate() % 10).IsEqualTo(0);
    }

    [Fact(DisplayName = "MultipleOf: a distinct collection sees the grid cardinality.")]
    public void MultipleOfFeedsCardinality() {
        HashSet<int> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Int32().Between(0, 20).MultipleOf(10).Generate()); }

        // Exactly the three grid points are reachable — the cardinality hint a distinct collection relies on.
        Check.That(seen.SetEquals([0, 10, 20])).IsTrue();
    }

    #endregion

    #region WithScale

    [Fact(DisplayName = "WithScale: every drawn value lies on the 10^-scale grid.")]
    public void WithScaleStaysOnGrid() {
        for (int i = 0; i < SampleCount; i++) {
            decimal amount = Any.Decimal().Between(0m, 1000m).WithScale(2).Generate();
            Check.That(amount).IsEqualTo(Math.Round(amount, 2, MidpointRounding.ToEven));
            Check.That(amount >= 0m && amount <= 1000m).IsTrue();

            decimal whole = Any.Decimal().WithScale(0).Generate();
            Check.That(whole).IsEqualTo(Math.Round(whole, 0, MidpointRounding.ToEven));
        }
    }

    [Fact(DisplayName = "WithScale: reaches both ends of a narrow grid.")]
    public void WithScaleReachesBounds() {
        HashSet<decimal> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            decimal value = Any.Decimal().Between(0m, 1m).WithScale(1).Generate();
            Check.That(value).IsEqualTo(Math.Round(value, 1, MidpointRounding.ToEven));
            seen.Add(value);
        }

        Check.That(seen.Contains(0m)).IsTrue();
        Check.That(seen.Contains(1m)).IsTrue();
    }

    [Fact(DisplayName = "WithScale: composes with Except on the grid.")]
    public void WithScaleComposesWithExcept() {
        for (int i = 0; i < SampleCount; i++) {
            decimal value = Any.Decimal().Between(0m, 1m).WithScale(1).Except(0.5m).Generate();
            Check.That(value).IsEqualTo(Math.Round(value, 1, MidpointRounding.ToEven));
            Check.That(value).IsNotEqualTo(0.5m);
        }
    }

    [Fact(DisplayName = "WithScale: a scale outside [0, 28] is rejected.")]
    public void WithScaleArguments() {
        Check.ThatCode(() => Any.Decimal().WithScale(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.Decimal().WithScale(29)).Throws<ArgumentOutOfRangeException>();
        Any.Decimal().WithScale(0).Generate();
        Any.Decimal().WithScale(28).Generate();
    }

    [Fact(DisplayName = "WithScale: a range containing no grid point conflicts eagerly.")]
    public void WithScaleEmptyGridConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(0.001m, 0.009m).WithScale(2));
        Check.That(conflict.Message).Contains("WithScale(2)");
    }

    #endregion

    #region WithGranularity

    [Fact(DisplayName = "WithGranularity: every drawn instant/duration lands on the grid.")]
    public void WithGranularityStaysOnGrid() {
        long quarterHour = TimeSpan.FromMinutes(15).Ticks;
        long oneSecond   = TimeSpan.FromSeconds(1).Ticks;
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.DateTime().WithGranularity(TimeSpan.FromMinutes(15)).Generate().Ticks % quarterHour).IsEqualTo(0L);
            Check.That(Any.TimeSpan().WithGranularity(TimeSpan.FromSeconds(1)).Generate().Ticks       % oneSecond).IsEqualTo(0L);
            Check.That(Any.DateTimeOffset().WithGranularity(TimeSpan.FromSeconds(1)).Generate().UtcTicks % oneSecond).IsEqualTo(0L);
        }
    }

    [Fact(DisplayName = "WithGranularity: composes with a range and stays aligned within it.")]
    public void WithGranularityHonoursRange() {
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime end   = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        long     day   = TimeSpan.FromDays(1).Ticks;
        for (int i = 0; i < SampleCount; i++) {
            DateTime value = Any.DateTime().Between(start, end).WithGranularity(TimeSpan.FromDays(1)).Generate();
            Check.That(value.Ticks % day).IsEqualTo(0L);
            Check.That(value >= start && value <= end).IsTrue();
        }
    }

    [Fact(DisplayName = "WithGranularity: a non-positive granularity is rejected.")]
    public void WithGranularityArguments() {
        Check.ThatCode(() => Any.DateTime().WithGranularity(TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.TimeSpan().WithGranularity(TimeSpan.FromTicks(-1))).Throws<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "WithGranularity: a window with no aligned instant conflicts eagerly.")]
    public void WithGranularityEmptyGridConflicts() {
        DateTime start = new(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc);
        DateTime end   = new(2026, 1, 1, 0, 0, 2, DateTimeKind.Utc);

        Check.ThatCode(() => Any.DateTime().Between(start, end).WithGranularity(TimeSpan.FromDays(1)))
             .Throws<ConflictingAnyConstraintException>();
    }

    #endregion

#if NET8_0_OR_GREATER
    #region Modern types (net8.0)

    [Fact(DisplayName = "MultipleOf: the 128-bit integers draw on the grid too.")]
    public void MultipleOfWideIntegers() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int128().Positive().MultipleOf((Int128)1000).Generate() % 1000 == (Int128)0).IsTrue();
            Check.That(Any.UInt128().MultipleOf((UInt128)7).Generate()             % 7 == (UInt128)0).IsTrue();
        }

        Check.ThatCode(() => Any.Int128().Between((Int128)1, (Int128)9).MultipleOf((Int128)10)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "WithGranularity: TimeOnly aligns to the grid.")]
    public void WithGranularityTimeOnly() {
        long oneSecond = TimeSpan.FromSeconds(1).Ticks;
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.TimeOnly().WithGranularity(TimeSpan.FromSeconds(1)).Generate().Ticks % oneSecond).IsEqualTo(0L);
        }
    }

    #endregion
#endif

}

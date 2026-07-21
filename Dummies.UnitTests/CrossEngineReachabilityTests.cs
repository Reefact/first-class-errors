#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

/// <summary>
///     A single scenario battery run against <b>every</b> interval-backed builder through a small per-type adapter,
///     so an engine-level regression cannot hide in the one type-facade a hand-written test happens not to cover.
///     Adding a builder means adding one row to <see cref="CrossEngineReachabilityTests.AllCases" />, not a new file.
/// </summary>
/// <remarks>
///     <para>
///         The suite closes the reachability blind spot the 2026-07-20 Dummies architecture audit named (§9.2/§9.3,
///         issue #213): the existing tests assert <i>membership</i> (a generated value satisfies its constraints) but
///         never <i>reachability</i> (the whole declared domain is actually generable). Two shipped defects survived
///         that gap — <see cref="AnyDecimal" /> never reaching the upper half of a range (#206) and the
///         <c>AnySingle</c>/<c>AnyHalf</c> exclusion nudge stalling on satisfiable specs (#207). Both are guarded here
///         structurally, across every engine at once, in addition to their dedicated regressions
///         (<see cref="AnyContinuousTests.DecimalBetweenReachesBothHalves" /> and
///         <see cref="ContinuousExclusionNudgeTests" />, kept as focused, commented guards).
///     </para>
///     <para>
///         Every scenario is deterministic: one fixed seed, a fixed draw count large enough that a correct uniform
///         generator reaches the asserted region with overwhelming probability while a stuck one (half a range
///         unreachable) fails every time. No randomness leaks in, so the suite is a stable CI guard, never a flaky one.
///     </para>
/// </remarks>
public sealed class CrossEngineReachabilityTests {

    #region Statics members declarations

    // Three consecutive representable values of each floating type: a tight domain where excluding one endpoint
    // must still generate by nudging along the type's own ladder — the exact shape that stalled #207 on the
    // quantized types. Computed once via the type-aware bit step so the values are genuinely adjacent.
    private static readonly double DLo = 1.0d;
    private static readonly double DMid = Math.BitIncrement(1.0d);
    private static readonly double DHi = Math.BitIncrement(Math.BitIncrement(1.0d));

    private static readonly float FLo = 1.0f;
    private static readonly float FMid = MathF.BitIncrement(1.0f);
    private static readonly float FHi = MathF.BitIncrement(MathF.BitIncrement(1.0f));

    private static readonly Half HLo = (Half)1;
    private static readonly Half HMid = NextHalf((Half)1);
    private static readonly Half HHi = NextHalf(NextHalf((Half)1));

    // Temporal anchors — the low end of each narrow (few-ordinal) domain and of each wide range.
    private static readonly DateTime       DtAnchor  = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset DtoAnchor = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly       DoAnchor  = new(2000, 1, 1);
    private static readonly TimeOnly       ToAnchor  = new(9, 0, 0);

    private static readonly Int128  WideInt128  = (Int128)ulong.MaxValue * 10;
    private static readonly UInt128 WideUInt128 = (UInt128)ulong.MaxValue * 10;

    /// <summary>
    ///     One adapter per interval-backed builder — every discrete integer, the two 128-bit integers, the three
    ///     binary-floating types, <see cref="decimal" />, and the five temporal types. The non-interval scalars
    ///     (bool, guid, string, enum, char) are deliberately absent: they expose no user-bounded ordered range, so
    ///     "both halves / both endpoints" is undefined for them.
    /// </summary>
    private static readonly ReachabilityCase[] AllCases = {
        Case<sbyte>("SByte", true,
                    c => c.SByte(),
                    (c, lo, hi) => c.SByte().Between(lo, hi),
                    (c, lo, hi, x) => c.SByte().Between(lo, hi).DifferentFrom(x),
                    (c, allow, except) => c.SByte().OneOf(allow).Except(except),
                    v => v, sbyte.MinValue, sbyte.MaxValue, -100, 100, 1, 2, 3),
        Case<short>("Int16", true,
                    c => c.Int16(),
                    (c, lo, hi) => c.Int16().Between(lo, hi),
                    (c, lo, hi, x) => c.Int16().Between(lo, hi).DifferentFrom(x),
                    (c, allow, except) => c.Int16().OneOf(allow).Except(except),
                    v => v, short.MinValue, short.MaxValue, -30000, 30000, 1, 2, 3),
        Case<int>("Int32", true,
                  c => c.Int32(),
                  (c, lo, hi) => c.Int32().Between(lo, hi),
                  (c, lo, hi, x) => c.Int32().Between(lo, hi).DifferentFrom(x),
                  (c, allow, except) => c.Int32().OneOf(allow).Except(except),
                  v => v, int.MinValue, int.MaxValue, -1_000_000, 1_000_000, 1, 2, 3),
        Case<long>("Int64", true,
                   c => c.Int64(),
                   (c, lo, hi) => c.Int64().Between(lo, hi),
                   (c, lo, hi, x) => c.Int64().Between(lo, hi).DifferentFrom(x),
                   (c, allow, except) => c.Int64().OneOf(allow).Except(except),
                   v => v, long.MinValue, long.MaxValue, -1_000_000_000_000L, 1_000_000_000_000L, 1L, 2L, 3L),
        Case<byte>("Byte", true,
                   c => c.Byte(),
                   (c, lo, hi) => c.Byte().Between(lo, hi),
                   (c, lo, hi, x) => c.Byte().Between(lo, hi).DifferentFrom(x),
                   (c, allow, except) => c.Byte().OneOf(allow).Except(except),
                   v => v, byte.MinValue, byte.MaxValue, 10, 240, 100, 101, 102),
        Case<ushort>("UInt16", true,
                     c => c.UInt16(),
                     (c, lo, hi) => c.UInt16().Between(lo, hi),
                     (c, lo, hi, x) => c.UInt16().Between(lo, hi).DifferentFrom(x),
                     (c, allow, except) => c.UInt16().OneOf(allow).Except(except),
                     v => v, ushort.MinValue, ushort.MaxValue, 100, 60000, 1000, 1001, 1002),
        Case<uint>("UInt32", true,
                   c => c.UInt32(),
                   (c, lo, hi) => c.UInt32().Between(lo, hi),
                   (c, lo, hi, x) => c.UInt32().Between(lo, hi).DifferentFrom(x),
                   (c, allow, except) => c.UInt32().OneOf(allow).Except(except),
                   v => v, uint.MinValue, uint.MaxValue, 1000u, 4_000_000_000u, 100_000u, 100_001u, 100_002u),
        Case<ulong>("UInt64", true,
                    c => c.UInt64(),
                    (c, lo, hi) => c.UInt64().Between(lo, hi),
                    (c, lo, hi, x) => c.UInt64().Between(lo, hi).DifferentFrom(x),
                    (c, allow, except) => c.UInt64().OneOf(allow).Except(except),
                    v => v, ulong.MinValue, ulong.MaxValue, 1000ul, 10_000_000_000_000_000_000ul, 1_000_000ul, 1_000_001ul, 1_000_002ul),
        Case<Int128>("Int128", true,
                     c => c.Int128(),
                     (c, lo, hi) => c.Int128().Between(lo, hi),
                     (c, lo, hi, x) => c.Int128().Between(lo, hi).DifferentFrom(x),
                     (c, allow, except) => c.Int128().OneOf(allow).Except(except),
                     v => (double)v, Int128.MinValue, Int128.MaxValue, -WideInt128, WideInt128, Int128.Zero, Int128.One, (Int128)2),
        Case<UInt128>("UInt128", true,
                      c => c.UInt128(),
                      (c, lo, hi) => c.UInt128().Between(lo, hi),
                      (c, lo, hi, x) => c.UInt128().Between(lo, hi).DifferentFrom(x),
                      (c, allow, except) => c.UInt128().OneOf(allow).Except(except),
                      v => (double)v, UInt128.MinValue, UInt128.MaxValue, UInt128.Zero, WideUInt128, UInt128.Zero, UInt128.One, (UInt128)2),
        Case<double>("Double", false,
                     c => c.Double(),
                     (c, lo, hi) => c.Double().Between(lo, hi),
                     (c, lo, hi, x) => c.Double().Between(lo, hi).DifferentFrom(x),
                     (c, allow, except) => c.Double().OneOf(allow).Except(except),
                     v => v, double.MinValue, double.MaxValue, -1_000_000d, 1_000_000d, DLo, DMid, DHi),
        Case<float>("Single", false,
                    c => c.Single(),
                    (c, lo, hi) => c.Single().Between(lo, hi),
                    (c, lo, hi, x) => c.Single().Between(lo, hi).DifferentFrom(x),
                    (c, allow, except) => c.Single().OneOf(allow).Except(except),
                    v => v, float.MinValue, float.MaxValue, -100_000f, 100_000f, FLo, FMid, FHi),
        Case<Half>("Half", false,
                   c => c.Half(),
                   (c, lo, hi) => c.Half().Between(lo, hi),
                   (c, lo, hi, x) => c.Half().Between(lo, hi).DifferentFrom(x),
                   (c, allow, except) => c.Half().OneOf(allow).Except(except),
                   v => (double)v, Half.MinValue, Half.MaxValue, (Half)(-1000), (Half)1000, HLo, HMid, HHi),
        Case<decimal>("Decimal", false,
                      c => c.Decimal(),
                      (c, lo, hi) => c.Decimal().Between(lo, hi),
                      (c, lo, hi, x) => c.Decimal().Between(lo, hi).DifferentFrom(x),
                      (c, allow, except) => c.Decimal().OneOf(allow).Except(except),
                      v => (double)v, decimal.MinValue, decimal.MaxValue, 0m, 1_000_000m, 1m, 2m, 3m),
        Case<DateTime>("DateTime", true,
                       c => c.DateTime(),
                       (c, lo, hi) => c.DateTime().Between(lo, hi),
                       (c, lo, hi, x) => c.DateTime().Between(lo, hi).DifferentFrom(x),
                       (c, allow, except) => c.DateTime().OneOf(allow).Except(except),
                       v => v.Ticks, DateTime.MinValue, DateTime.MaxValue,
                       DtAnchor, new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc), DtAnchor, DtAnchor.AddTicks(1), DtAnchor.AddTicks(2)),
        Case<DateTimeOffset>("DateTimeOffset", true,
                             c => c.DateTimeOffset(),
                             (c, lo, hi) => c.DateTimeOffset().Between(lo, hi),
                             (c, lo, hi, x) => c.DateTimeOffset().Between(lo, hi).DifferentFrom(x),
                             (c, allow, except) => c.DateTimeOffset().OneOf(allow).Except(except),
                             v => v.UtcTicks, DateTimeOffset.MinValue, DateTimeOffset.MaxValue,
                             DtoAnchor, new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero), DtoAnchor, DtoAnchor.AddTicks(1), DtoAnchor.AddTicks(2)),
        Case<DateOnly>("DateOnly", true,
                       c => c.DateOnly(),
                       (c, lo, hi) => c.DateOnly().Between(lo, hi),
                       (c, lo, hi, x) => c.DateOnly().Between(lo, hi).DifferentFrom(x),
                       (c, allow, except) => c.DateOnly().OneOf(allow).Except(except),
                       v => v.DayNumber, DateOnly.MinValue, DateOnly.MaxValue,
                       DoAnchor, new DateOnly(2100, 1, 1), DoAnchor, DoAnchor.AddDays(1), DoAnchor.AddDays(2)),
        Case<TimeOnly>("TimeOnly", true,
                       c => c.TimeOnly(),
                       (c, lo, hi) => c.TimeOnly().Between(lo, hi),
                       (c, lo, hi, x) => c.TimeOnly().Between(lo, hi).DifferentFrom(x),
                       (c, allow, except) => c.TimeOnly().OneOf(allow).Except(except),
                       v => v.Ticks, TimeOnly.MinValue, TimeOnly.MaxValue,
                       new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), ToAnchor, ToAnchor.Add(TimeSpan.FromTicks(1)), ToAnchor.Add(TimeSpan.FromTicks(2))),
        Case<TimeSpan>("TimeSpan", true,
                       c => c.TimeSpan(),
                       (c, lo, hi) => c.TimeSpan().Between(lo, hi),
                       (c, lo, hi, x) => c.TimeSpan().Between(lo, hi).DifferentFrom(x),
                       (c, allow, except) => c.TimeSpan().OneOf(allow).Except(except),
                       v => v.Ticks, TimeSpan.MinValue, TimeSpan.MaxValue,
                       TimeSpan.FromHours(-1), TimeSpan.FromHours(1), TimeSpan.Zero, TimeSpan.FromTicks(1), TimeSpan.FromTicks(2)),
    };

    private static readonly IReadOnlyDictionary<string, ReachabilityCase> ByName = AllCases.ToDictionary(one => one.Name);

    /// <summary>The builder names, one theory row each — a serializable key so every builder is an isolated test.</summary>
    public static TheoryData<string> CaseNames() {
        TheoryData<string> data = new();
        foreach (ReachabilityCase one in AllCases) { data.Add(one.Name); }

        return data;
    }

    private static IntervalCase<T> Case<T>(string                          name,       bool exact,
                                           Func<AnyContext, IAny<T>>        full,
                                           Func<AnyContext, T, T, IAny<T>>  between,
                                           Func<AnyContext, T, T, T, IAny<T>> betweenDifferentFrom,
                                           Func<AnyContext, T[], T[], IAny<T>> oneOfExcept,
                                           Func<T, double>                 scale,
                                           T domainMin, T domainMax, T wideLo, T wideHi, T na, T nb, T nc) {
        return new IntervalCase<T>(name, exact, full, between, betweenDifferentFrom, oneOfExcept, scale,
                                   domainMin, domainMax, wideLo, wideHi, na, nb, nc);
    }

    private static Half NextHalf(Half value) {
        return BitConverter.Int16BitsToHalf((short)(BitConverter.HalfToInt16Bits(value) + 1));
    }

    #endregion

    [Theory(DisplayName = "Full range: an unconstrained generator reaches both halves of its domain.")]
    [MemberData(nameof(CaseNames))]
    public void FullRangeReachesBothHalvesOfTheDomain(string builder) {
        ByName[builder].FullRangeReachesBothHalvesOfTheDomain();
    }

    [Theory(DisplayName = "Between: a wide range reaches both halves — neither half is silently unreachable.")]
    [MemberData(nameof(CaseNames))]
    public void WideBetweenReachesBothHalves(string builder) {
        ByName[builder].WideBetweenReachesBothHalves();
    }

    [Theory(DisplayName = "Between: both inclusive bounds are reachable (exactly for discrete types, to within 1% for continuous ones).")]
    [MemberData(nameof(CaseNames))]
    public void BothInclusiveBoundsAreReachable(string builder) {
        ByName[builder].BothInclusiveBoundsAreReachable();
    }

    [Theory(DisplayName = "Exclusion: a point excluded from a narrow range still generates and is never returned.")]
    [MemberData(nameof(CaseNames))]
    public void NarrowExclusionStillGenerates(string builder) {
        ByName[builder].NarrowExclusionStillGenerates();
    }

    [Theory(DisplayName = "OneOf then Except: only the surviving allowed values are generated.")]
    [MemberData(nameof(CaseNames))]
    public void OneOfThenExceptYieldsOnlyTheSurvivors(string builder) {
        ByName[builder].OneOfThenExceptYieldsOnlyTheSurvivors();
    }

    [Theory(DisplayName = "Conflict: contradictory constraints throw naming both sides.")]
    [MemberData(nameof(CaseNames))]
    public void ContradictoryConstraintsNameBothSides(string builder) {
        ByName[builder].ContradictoryConstraintsNameBothSides();
    }

}

/// <summary>One interval-backed builder's participation in the shared scenario battery — the per-type adapter.</summary>
internal abstract class ReachabilityCase {

    protected ReachabilityCase(string name) {
        Name = name;
    }

    public string Name { get; }

    public sealed override string ToString() {
        return Name;
    }

    public abstract void FullRangeReachesBothHalvesOfTheDomain();

    public abstract void WideBetweenReachesBothHalves();

    public abstract void BothInclusiveBoundsAreReachable();

    public abstract void NarrowExclusionStillGenerates();

    public abstract void OneOfThenExceptYieldsOnlyTheSurvivors();

    public abstract void ContradictoryConstraintsNameBothSides();

}

/// <summary>
///     The generic adapter carrying a builder's value type <typeparamref name="T" />, its wide and narrow test
///     domains, whether its endpoints are exactly reachable, a monotone projection onto <see cref="double" /> for
///     half/bound detection, and the four uniformly-named chains the scenarios drive (<c>Between</c>,
///     <c>Between + DifferentFrom</c>, <c>OneOf + Except</c>, and the unconstrained builder).
/// </summary>
internal sealed class IntervalCase<T> : ReachabilityCase {

    private const int Seed                = 20260721;
    private const int DistributionSamples = 4000;
    private const int SetSamples          = 200;

    #region Fields declarations

    private readonly Func<AnyContext, T, T, T, IAny<T>>   _betweenDifferentFrom;
    private readonly Func<AnyContext, T, T, IAny<T>>      _between;
    private readonly T                                    _domainMax;
    private readonly T                                    _domainMin;
    private readonly bool                                 _endpointsExact;
    private readonly Func<AnyContext, IAny<T>>            _full;
    private readonly T                                    _na;
    private readonly T                                    _nb;
    private readonly T                                    _nc;
    private readonly Func<AnyContext, T[], T[], IAny<T>>  _oneOfExcept;
    private readonly Func<T, double>                      _scale;
    private readonly T                                    _wideHi;
    private readonly T                                    _wideLo;

    #endregion

    public IntervalCase(string                             name,       bool endpointsExact,
                        Func<AnyContext, IAny<T>>          full,
                        Func<AnyContext, T, T, IAny<T>>    between,
                        Func<AnyContext, T, T, T, IAny<T>> betweenDifferentFrom,
                        Func<AnyContext, T[], T[], IAny<T>> oneOfExcept,
                        Func<T, double>                    scale,
                        T domainMin, T domainMax, T wideLo, T wideHi, T na, T nb, T nc)
        : base(name) {
        _endpointsExact       = endpointsExact;
        _full                 = full;
        _between              = between;
        _betweenDifferentFrom = betweenDifferentFrom;
        _oneOfExcept          = oneOfExcept;
        _scale                = scale;
        _domainMin            = domainMin;
        _domainMax            = domainMax;
        _wideLo               = wideLo;
        _wideHi               = wideHi;
        _na                   = na;
        _nb                   = nb;
        _nc                   = nc;
    }

    public override void FullRangeReachesBothHalvesOfTheDomain() {
        (double min, double max, _) = Sample(_full(Any.WithSeed(Seed)), DistributionSamples);
        double mid = _scale(_domainMin) / 2 + _scale(_domainMax) / 2;

        Check.That(min).IsStrictlyLessThan(mid);    // the lower half of the domain is reached
        Check.That(max).IsStrictlyGreaterThan(mid); // and so is the upper half
    }

    public override void WideBetweenReachesBothHalves() {
        (double min, double max, _) = Sample(_between(Any.WithSeed(Seed), _wideLo, _wideHi), DistributionSamples);
        double lo  = _scale(_wideLo);
        double hi  = _scale(_wideHi);
        double mid = lo / 2 + hi / 2;

        Check.That(min).IsGreaterOrEqualThan(lo);   // draws stay in range
        Check.That(max).IsLessOrEqualThan(hi);
        Check.That(min).IsStrictlyLessThan(mid);    // the lower half is reached
        Check.That(max).IsStrictlyGreaterThan(mid); // the upper half is reached — the #206 class of defect
    }

    public override void BothInclusiveBoundsAreReachable() {
        if (_endpointsExact) {
            // Discrete types: a narrow three-value range must hand back both of its exact endpoints.
            (_, _, HashSet<T> seen) = Sample(_between(Any.WithSeed(Seed), _na, _nc), DistributionSamples);
            Check.That(seen.Contains(_na)).IsTrue();
            Check.That(seen.Contains(_nc)).IsTrue();

            return;
        }

        // Continuous types: exact endpoints are a measure-zero target, so a draw must instead come within 1% of
        // each inclusive bound of a wide range. A generator stuck below the midpoint (#206) never gets near the top.
        (double min, double max, _) = Sample(_between(Any.WithSeed(Seed), _wideLo, _wideHi), DistributionSamples);
        double lo    = _scale(_wideLo);
        double hi    = _scale(_wideHi);
        double range = hi - lo;

        Check.That(max).IsStrictlyGreaterThan(hi - range * 0.01d);
        Check.That(min).IsStrictlyLessThan(lo + range * 0.01d);
    }

    public override void NarrowExclusionStillGenerates() {
        // Between(na, nc).DifferentFrom(na): on the quantized floating types this drives the type-aware nudge that
        // stalled in #207; on the discrete types it drives the ordinal exclusion. Either way it must generate.
        IAny<T>                 generator = _betweenDifferentFrom(Any.WithSeed(Seed), _na, _nc, _na);
        EqualityComparer<T>     equals    = EqualityComparer<T>.Default;
        double                  lo        = _scale(_na);
        double                  hi        = _scale(_nc);

        for (int i = 0; i < SetSamples; i++) {
            T value = generator.Generate();
            Check.That(equals.Equals(value, _na)).IsFalse();
            Check.That(_scale(value)).IsGreaterOrEqualThan(lo);
            Check.That(_scale(value)).IsLessOrEqualThan(hi);
        }
    }

    public override void OneOfThenExceptYieldsOnlyTheSurvivors() {
        IAny<T>             generator = _oneOfExcept(Any.WithSeed(Seed), [_na, _nb, _nc], [_nb]);
        EqualityComparer<T> equals    = EqualityComparer<T>.Default;

        for (int i = 0; i < SetSamples; i++) {
            T value = generator.Generate();
            Check.That(equals.Equals(value, _nb)).IsFalse();
            Check.That(equals.Equals(value, _na) || equals.Equals(value, _nc)).IsTrue();
        }
    }

    public override void ContradictoryConstraintsNameBothSides() {
        // OneOf(na).Except(na) empties the allow-list; the message must name both the allow-list and the exclusion.
        // Asserting the method-name tokens keeps this independent of how each type renders its values.
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => _oneOfExcept(Any.WithSeed(Seed), [_na], [_na]));

        Check.That(conflict.Message).Contains("OneOf(");
        Check.That(conflict.Message).Contains("Except(");
    }

    private (double Min, double Max, HashSet<T> Seen) Sample(IAny<T> generator, int count) {
        double     min  = double.PositiveInfinity;
        double     max  = double.NegativeInfinity;
        HashSet<T> seen = new();

        for (int i = 0; i < count; i++) {
            T      value  = generator.Generate();
            double scaled = _scale(value);
            seen.Add(value);
            if (scaled < min) { min = scaled; }
            if (scaled > max) { max = scaled; }
        }

        return (min, max, seen);
    }

}

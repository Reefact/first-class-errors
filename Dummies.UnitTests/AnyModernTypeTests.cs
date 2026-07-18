#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnyModernTypeTests {

    private const int SampleCount = 200;

    private static readonly DateOnly AnchorDate = new(2026, 1, 1);
    private static readonly TimeOnly AnchorTime = new(12, 0, 0);

    [Fact(DisplayName = "DateOnly: Between is inclusive and reached; After/Before are exclusive; conflicts surface.")]
    public void DateOnlyBehaves() {
        HashSet<DateOnly> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            DateOnly value = Any.DateOnly().Between(AnchorDate, AnchorDate.AddDays(2)).Generate();
            seen.Add(value);
            Check.That(value >= AnchorDate && value <= AnchorDate.AddDays(2)).IsTrue();
            Check.That(Any.DateOnly().After(AnchorDate).Before(AnchorDate.AddDays(2)).Generate()).IsEqualTo(AnchorDate.AddDays(1));
        }
        Check.That(seen.Contains(AnchorDate)).IsTrue();
        Check.That(seen.Contains(AnchorDate.AddDays(2))).IsTrue();

        Check.ThatCode(() => Any.DateOnly().After(DateOnly.MaxValue)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.DateOnly().Between(AnchorDate.AddDays(1), AnchorDate)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DateOnly: OneOf/Except/DifferentFrom behave.")]
    public void DateOnlySets() {
        DateOnly[] allowed = [AnchorDate, AnchorDate.AddDays(7)];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Any.DateOnly().OneOf(allowed).Generate())).IsTrue();
            Check.That(Any.DateOnly().OneOf(allowed).Except(AnchorDate).Generate()).IsEqualTo(AnchorDate.AddDays(7));
            Check.That(Any.DateOnly().OneOf(allowed).DifferentFrom(AnchorDate.AddDays(7)).Generate()).IsEqualTo(AnchorDate);
        }
    }

    [Fact(DisplayName = "TimeOnly: bounds behave and the exclusive window pins the middle tick.")]
    public void TimeOnlyBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            TimeOnly value = Any.TimeOnly().Between(AnchorTime, AnchorTime.Add(TimeSpan.FromMinutes(5))).Generate();
            Check.That(value >= AnchorTime && value <= AnchorTime.Add(TimeSpan.FromMinutes(5))).IsTrue();

            TimeOnly middle = Any.TimeOnly().After(AnchorTime).Before(new TimeOnly(AnchorTime.Ticks + 2)).Generate();
            Check.That(middle.Ticks).IsEqualTo(AnchorTime.Ticks + 1);
        }

        Check.ThatCode(() => Any.TimeOnly().After(TimeOnly.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Int128: signs, pins, full-width variety, extremes and conflicts.")]
    public void Int128Behaves() {
        HashSet<Int128> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Any.Int128().Generate());
            Check.That(Any.Int128().Positive().Generate() > 0).IsTrue();
            Check.That(Any.Int128().Negative().Generate() < 0).IsTrue();

            Int128 bounded = Any.Int128().Between(1, 3).Generate();
            Check.That(bounded >= 1 && bounded <= 3).IsTrue();
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Any.Int128().Zero().Generate() == 0).IsTrue();
        Check.ThatCode(() => Any.Int128().GreaterThan(Int128.MaxValue)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Int128().Positive().Negative()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "UInt128: bounds, exclusivity and full-width variety.")]
    public void UInt128Behaves() {
        HashSet<UInt128> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Any.UInt128().Generate());

            UInt128 bounded = Any.UInt128().Between(1, 3).Generate();
            Check.That(bounded >= 1 && bounded <= 3).IsTrue();
            Check.That(Any.UInt128().GreaterThan(5).LessThanOrEqualTo(6).Generate() == 6).IsTrue();
        }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.That(Any.UInt128().Zero().Generate() == 0).IsTrue();
        Check.ThatCode(() => Any.UInt128().GreaterThan(UInt128.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Half: finite draws, strict Positive, pinned Zero, contained bounds, argument checks.")]
    public void HalfBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Half value = Any.Half().Generate();
            Check.That(Half.IsNaN(value) || Half.IsInfinity(value)).IsFalse();
            Check.That(Any.Half().Positive().Generate() > Half.Zero).IsTrue();

            Half bounded = Any.Half().Between((Half)1f, (Half)2f).Generate();
            Check.That(bounded >= (Half)1f && bounded <= (Half)2f).IsTrue();
        }

        Check.That(Any.Half().Zero().Generate() == Half.Zero).IsTrue();
        Check.ThatCode(() => Any.Half().GreaterThan(Half.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Half().GreaterThan(Half.MaxValue)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Half().Positive().Negative()).Throws<ConflictingAnyConstraintException>();
    }

}

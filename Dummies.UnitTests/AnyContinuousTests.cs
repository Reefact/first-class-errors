#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnyContinuousTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "Double: unconstrained draws are always finite.")]
    public void DoubleIsFinite() {
        for (int i = 0; i < SampleCount; i++) {
            double value = Any.Double().Generate();
            Check.That(double.IsNaN(value) || double.IsInfinity(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "Double: sign constraints are strict, Zero pins, NonZero excludes.")]
    public void DoubleSignFamily() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Double().Positive().Generate()).IsStrictlyGreaterThan(0d);
            Check.That(Any.Double().Negative().Generate()).IsStrictlyLessThan(0d);
            Check.That(Any.Double().NonZero().Generate()).IsNotEqualTo(0d);
        }
        Check.That(Any.Double().Zero().Generate()).IsEqualTo(0d);
        Check.ThatCode(() => Any.Double().Zero().NonZero()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Double().Positive().Negative()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Double: Between contains, GreaterThan is strict, and conflicts name both sides.")]
    public void DoubleBounds() {
        for (int i = 0; i < SampleCount; i++) {
            double bounded = Any.Double().Between(1d, 2d).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1d);
            Check.That(bounded).IsLessOrEqualThan(2d);
            Check.That(Any.Double().GreaterThan(1d).LessThanOrEqualTo(2d).Generate()).IsStrictlyGreaterThan(1d);
        }

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Double().GreaterThan(100d).LessThan(10d));
        Check.That(conflict.Message).Contains("LessThan(10)");
        Check.That(conflict.Message).Contains("GreaterThan(100)");
        Check.ThatCode(() => Any.Double().GreaterThan(double.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Double: non-finite arguments are rejected as argument errors.")]
    public void DoubleRejectsNonFinite() {
        Check.ThatCode(() => Any.Double().GreaterThan(double.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Double().LessThan(double.PositiveInfinity)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Double().Between(double.NegativeInfinity, 0d)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Double().OneOf(1d, double.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Double().Between(10d, 1d)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Double: OneOf stays within and Except/DifferentFrom never yield the excluded value.")]
    public void DoubleSets() {
        double[] allowed = [1.5d, 2.5d];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Any.Double().OneOf(allowed).Generate())).IsTrue();
            Check.That(Any.Double().OneOf(allowed).Except(1.5d).Generate()).IsEqualTo(2.5d);
            Check.That(Any.Double().OneOf(allowed).DifferentFrom(2.5d).Generate()).IsEqualTo(1.5d);
        }
    }

    [Fact(DisplayName = "Single: finite draws, strict signs, bounds contained, NaN rejected.")]
    public void SingleBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            float value = Any.Single().Generate();
            Check.That(float.IsNaN(value) || float.IsInfinity(value)).IsFalse();
            Check.That(Any.Single().Positive().Generate()).IsStrictlyGreaterThan(0f);

            float bounded = Any.Single().Between(1f, 2f).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1f);
            Check.That(bounded).IsLessOrEqualThan(2f);
        }

        Check.That(Any.Single().Zero().Generate()).IsEqualTo(0f);
        Check.ThatCode(() => Any.Single().GreaterThan(float.NaN)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Single().GreaterThan(float.MaxValue)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Single().Positive().Negative()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Decimal: strict signs, pinned zero, contained bounds, and strict GreaterThan via exclusion.")]
    public void DecimalBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Decimal().Positive().Generate()).IsStrictlyGreaterThan(0m);
            Check.That(Any.Decimal().Negative().Generate()).IsStrictlyLessThan(0m);

            decimal bounded = Any.Decimal().Between(1m, 2m).Generate();
            Check.That(bounded).IsGreaterOrEqualThan(1m);
            Check.That(bounded).IsLessOrEqualThan(2m);
            Check.That(Any.Decimal().Between(1m, 2m).GreaterThan(1m).Generate()).IsStrictlyGreaterThan(1m);
        }

        Check.That(Any.Decimal().Zero().Generate()).IsEqualTo(0m);
        Check.ThatCode(() => Any.Decimal().Zero().NonZero()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Decimal().Between(10m, 1m)).Throws<ArgumentException>();

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().GreaterThan(100m).LessThan(10m));
        Check.That(conflict.Message).Contains("LessThan(10)");
        Check.That(conflict.Message).Contains("GreaterThan(100)");
    }

    [Fact(DisplayName = "Continuous generators convert implicitly to their value type.")]
    public void ImplicitConversions() {
        double  d = Any.Double().Between(1d, 2d);
        float   f = Any.Single().Between(1f, 2f);
        decimal m = Any.Decimal().Between(1m, 2m);

        Check.That(d).IsGreaterOrEqualThan(1d);
        Check.That(f).IsGreaterOrEqualThan(1f);
        Check.That(m).IsGreaterOrEqualThan(1m);
    }

}

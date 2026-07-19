#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnySignedIntegerTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "SByte: Positive and Negative are strict, and contradict each other.")]
    public void SByteSignConstraints() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That((sbyte)Any.SByte().Positive().Generate()).IsStrictlyGreaterThan((sbyte)0);
            Check.That((sbyte)Any.SByte().Negative().Generate()).IsStrictlyLessThan((sbyte)0);
        }

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SByte().Positive().Negative());
        Check.That(conflict.Message).Contains("Negative()");
        Check.That(conflict.Message).Contains("Positive()");
    }

    [Fact(DisplayName = "SByte: Between is inclusive and reaches both bounds; extremes are generable.")]
    public void SByteBounds() {
        HashSet<sbyte> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.SByte().Between(-1, 1).Generate()); }
        Check.That(seen.Contains(-1)).IsTrue();
        Check.That(seen.Contains(1)).IsTrue();

        Check.That(Any.SByte().LessThanOrEqualTo(sbyte.MinValue).Generate()).IsEqualTo(sbyte.MinValue);
        Check.That(Any.SByte().GreaterThanOrEqualTo(sbyte.MaxValue).Generate()).IsEqualTo(sbyte.MaxValue);
        Check.ThatCode(() => Any.SByte().GreaterThan(sbyte.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Int16: Zero pins, NonZero excludes, and the pair conflicts.")]
    public void Int16ZeroFamily() {
        Check.That(Any.Int16().Zero().Generate()).IsEqualTo((short)0);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int16().Between(-1, 1).NonZero().Generate()).IsNotEqualTo((short)0);
        }
        Check.ThatCode(() => Any.Int16().Zero().NonZero()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Int16: GreaterThan and LessThan are exclusive bounds.")]
    public void Int16ExclusiveBounds() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int16().GreaterThan(10).LessThanOrEqualTo(12).Generate()).IsGreaterOrEqualThan((short)11);
            Check.That(Any.Int16().LessThan(10).GreaterThanOrEqualTo(8).Generate()).IsLessOrEqualThan((short)9);
        }
    }

    [Fact(DisplayName = "Int64: full-range generation works and crossed bounds conflict naming both sides.")]
    public void Int64RangeAndConflicts() {
        HashSet<long> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Int64().Generate()); }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int64().GreaterThan(100L).LessThan(10L));
        Check.That(conflict.Message).Contains("LessThan(10)");
        Check.That(conflict.Message).Contains("GreaterThan(100)");
    }

    [Fact(DisplayName = "Int64: OneOf stays within the supplied values and Except never yields an excluded one.")]
    public void Int64OneOfAndExcept() {
        long[] allowed = [1L, 5L, 9L];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Any.Int64().OneOf(allowed).Generate())).IsTrue();
            Check.That(Any.Int64().Between(1L, 3L).Except(2L).Generate()).IsNotEqualTo(2L);
            Check.That(Any.Int64().Between(7L, 8L).DifferentFrom(7L).Generate()).IsEqualTo(8L);
        }
    }

    [Fact(DisplayName = "Int64: extremes are generable and arguments are validated.")]
    public void Int64ExtremesAndArguments() {
        Check.That(Any.Int64().LessThanOrEqualTo(long.MinValue).Generate()).IsEqualTo(long.MinValue);
        Check.That(Any.Int64().GreaterThanOrEqualTo(long.MaxValue).Generate()).IsEqualTo(long.MaxValue);
        Check.ThatCode(() => Any.Int64().GreaterThan(long.MaxValue)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Int64().Between(10L, 1L)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Int64().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Int64().Except(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Signed integers convert implicitly to their value type.")]
    public void ImplicitConversions() {
        sbyte small = Any.SByte().Positive().Generate();
        short mid   = Any.Int16().Negative().Generate();
        long  wide  = Any.Int64().Between(1L, 10L).Generate();

        Check.That((int)small).IsStrictlyGreaterThan(0);
        Check.That((int)mid).IsStrictlyLessThan(0);
        Check.That(wide).IsGreaterOrEqualThan(1L);
    }

}

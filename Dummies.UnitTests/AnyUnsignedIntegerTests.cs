#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

public sealed class AnyUnsignedIntegerTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "Byte: Between is inclusive and reaches both bounds; extremes are generable.")]
    public void ByteBounds() {
        HashSet<byte> seen = new();
        for (int i = 0; i < SampleCount; i++) {
            byte value = Any.Byte().Between(1, 3).Generate();
            seen.Add(value);
            Check.That((int)value).IsGreaterOrEqualThan(1);
            Check.That((int)value).IsLessOrEqualThan(3);
        }
        Check.That(seen.Contains(1)).IsTrue();
        Check.That(seen.Contains(3)).IsTrue();

        Check.That(Any.Byte().LessThanOrEqualTo(0).Generate()).IsEqualTo((byte)0);
        Check.That(Any.Byte().GreaterThanOrEqualTo(byte.MaxValue).Generate()).IsEqualTo(byte.MaxValue);
    }

    [Fact(DisplayName = "Byte: Zero pins, NonZero excludes, the pair conflicts, and GreaterThan(max) conflicts.")]
    public void ByteZeroAndConflicts() {
        Check.That(Any.Byte().Zero().Generate()).IsEqualTo((byte)0);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Byte().Between(0, 1).NonZero().Generate()).IsEqualTo((byte)1);
        }
        Check.ThatCode(() => Any.Byte().Zero().NonZero()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.Byte().GreaterThan(byte.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "UInt16 and UInt32: exclusive bounds behave and crossed bounds conflict.")]
    public void MidWidthExclusiveBounds() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That((int)Any.UInt16().GreaterThan(10).LessThanOrEqualTo(12).Generate()).IsGreaterOrEqualThan(11);
            Check.That(Any.UInt32().LessThan(10u).GreaterThanOrEqualTo(8u).Generate()).IsLessOrEqualThan(9u);
        }

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.UInt32().GreaterThan(100u).LessThan(10u));
        Check.That(conflict.Message).Contains("LessThan(10)");
        Check.That(conflict.Message).Contains("GreaterThan(100)");
    }

    [Fact(DisplayName = "UInt64: the full-width sampling path yields varied values and honors exclusions.")]
    public void UInt64FullWidth() {
        HashSet<ulong> seen = new();
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.UInt64().Generate()); }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.UInt64().Between(0UL, 2UL).Except(1UL).Generate()).IsNotEqualTo(1UL);
        }
    }

    [Fact(DisplayName = "UInt64: extremes are generable and OneOf/Except behave.")]
    public void UInt64ExtremesAndSets() {
        Check.That(Any.UInt64().GreaterThanOrEqualTo(ulong.MaxValue).Generate()).IsEqualTo(ulong.MaxValue);
        Check.ThatCode(() => Any.UInt64().GreaterThan(ulong.MaxValue)).Throws<ConflictingAnyConstraintException>();

        ulong[] allowed = [1UL, 5UL];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Any.UInt64().OneOf(allowed).Generate())).IsTrue();
            Check.That(Any.UInt64().Between(7UL, 8UL).DifferentFrom(7UL).Generate()).IsEqualTo(8UL);
        }
        Check.ThatCode(() => Any.UInt64().Between(10UL, 1UL)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Unsigned integers convert implicitly to their value type.")]
    public void ImplicitConversions() {
        byte   tiny = Any.Byte().Between(1, 10).Generate();
        ushort mid  = Any.UInt16().NonZero().Generate();
        uint   wide = Any.UInt32().Between(1u, 10u).Generate();
        ulong  huge = Any.UInt64().Between(1UL, 10UL).Generate();

        Check.That((int)tiny).IsGreaterOrEqualThan(1);
        Check.That((int)mid).IsStrictlyGreaterThan(0);
        Check.That(wide).IsGreaterOrEqualThan(1u);
        Check.That(huge).IsGreaterOrEqualThan(1UL);
    }

}

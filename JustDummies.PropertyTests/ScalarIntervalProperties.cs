#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the interval algebra of every integer width <b>other than</b>
///     <see cref="AnyInt32" />: <see cref="AnySByte" />, <see cref="AnyByte" />, <see cref="AnyInt16" />,
///     <see cref="AnyUInt16" />, <see cref="AnyUInt32" />, <see cref="AnyInt64" /> and <see cref="AnyUInt64" />.
///     They all ride the same ordinal interval engine, but each one supplies its own domain edges and its own
///     signed-or-unsigned mapping into ordinal space — and that mapping is exactly where an off-by-one at a domain
///     edge, or an overflow in the interval arithmetic, hides. Quantifying over the whole bound space of a width
///     reaches those corners; the hand-picked intervals of the example-based suite cannot.
/// </summary>
/// <remarks>
///     The invariants are deliberately spread across the widths rather than repeated seven times over: each one is
///     proven on at least one signed and one unsigned width, with the narrowest pair (<c>sbyte</c>/<c>byte</c>,
///     where almost every bound is an edge) and the widest pair (<c>long</c>/<c>ulong</c>, where the interval
///     arithmetic runs out of room) getting the fullest treatment.
/// </remarks>
[TestSubject(typeof(AnyInt64))]
public sealed class ScalarIntervalProperties {

    #region Statics members declarations

    // One generator per width, each built on the shared Generators.WithEdges so FsCheck's size-bounded draws —
    // which cluster around zero — are mixed with the domain edges an off-by-one hides behind. `long` needs no
    // local generator: Generators.Int64() is already part of the shared support.

    /// <summary>Arbitrary <see cref="sbyte" />s, biased towards the ends of the range.</summary>
    private static Gen<sbyte> SByte() {
        return Generators.WithEdges<sbyte>(ArbMap.Default.GeneratorFor<sbyte>(),
                                           sbyte.MinValue, sbyte.MinValue + 1, -1, 0, 1, sbyte.MaxValue - 1, sbyte.MaxValue);
    }

    /// <summary>Arbitrary <see cref="byte" />s, biased towards the ends of the range and the sign-bit boundary.</summary>
    private static Gen<byte> Byte() {
        return Generators.WithEdges<byte>(ArbMap.Default.GeneratorFor<byte>(),
                                          byte.MinValue, 1, 127, 128, byte.MaxValue - 1, byte.MaxValue);
    }

    /// <summary>Arbitrary <see cref="short" />s, biased towards the ends of the range.</summary>
    private static Gen<short> Int16() {
        return Generators.WithEdges<short>(ArbMap.Default.GeneratorFor<short>(),
                                           short.MinValue, short.MinValue + 1, -1, 0, 1, short.MaxValue - 1, short.MaxValue);
    }

    /// <summary>Arbitrary <see cref="ushort" />s, biased towards the ends of the range and the sign-bit boundary.</summary>
    private static Gen<ushort> UInt16() {
        return Generators.WithEdges<ushort>(ArbMap.Default.GeneratorFor<ushort>(),
                                            ushort.MinValue, 1, 32767, 32768, ushort.MaxValue - 1, ushort.MaxValue);
    }

    /// <summary>Arbitrary <see cref="uint" />s, biased towards the ends of the range and the sign-bit boundary.</summary>
    private static Gen<uint> UInt32() {
        return Generators.WithEdges<uint>(ArbMap.Default.GeneratorFor<uint>(),
                                          uint.MinValue, 1u, 0x8000_0000u, uint.MaxValue - 1u, uint.MaxValue);
    }

    /// <summary>
    ///     Arbitrary <see cref="ulong" />s, biased towards the ends of the range and towards 2^63 — the point a
    ///     signed reinterpretation of the ordinal space would fold in two.
    /// </summary>
    private static Gen<ulong> UInt64() {
        return Generators.WithEdges<ulong>(ArbMap.Default.GeneratorFor<ulong>(),
                                           ulong.MinValue, 1UL, 0x8000_0000_0000_0000UL, ulong.MaxValue - 1UL, ulong.MaxValue);
    }

    #endregion

    [Fact(DisplayName = "SByte: Between contains — every draw falls within the declared inclusive bounds.")]
    public void SByteBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(SByte()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.SByte().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "SByte: Between with equal bounds pins the value, for every value.")]
    public void SByteBetweenWithEqualBoundsPins() {
        Prop.ForAll(SByte().ToArbitrary(),
                    value => Expect.EveryDraw(Any.SByte().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "SByte: the inclusive bounds admit their own bound, on both sides.")]
    public void SByteInclusiveBoundsAdmitTheirOwnBound() {
        Prop.ForAll(SByte().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.SByte().GreaterThanOrEqualTo(bound), value => value >= bound)
                             && Expect.EveryDraw(Any.SByte().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "SByte: GreaterThan and LessThan are strict, and conflict at the domain edge they cannot clear.")]
    public void SByteStrictBoundsAreStrictAndConflictAtTheEdges() {
        Prop.ForAll(SByte().ToArbitrary(),
                    bound => {
                        // There is no sbyte above sbyte.MaxValue nor below sbyte.MinValue: asking for one is a
                        // conflict declared at the call, never an interval that generates and then disappoints.
                        bool above = bound == sbyte.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.SByte().GreaterThan(bound))
                                         : Expect.EveryDraw(Any.SByte().GreaterThan(bound), value => value > bound);
                        bool below = bound == sbyte.MinValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.SByte().LessThan(bound))
                                         : Expect.EveryDraw(Any.SByte().LessThan(bound), value => value < bound);

                        return above && below;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "SByte: Between never yields a value excluded by a subsequent Except.")]
    public void SByteExceptRemovesTheValueFromTheInterval() {
        Gen<((sbyte Min, sbyte Max) Bounds, sbyte Excluded)> cases =
            from bounds in Generators.OrderedPair(SByte())
            from offset in Gen.Choose(0, bounds.Max - bounds.Min)
            select (Bounds: bounds, Excluded: (sbyte)(bounds.Min + offset));

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // Excluding the single value of a pinned interval empties it: that is a conflict, not a draw.
                        if (testCase.Bounds.Min == testCase.Bounds.Max) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.SByte().Between(testCase.Bounds.Min, testCase.Bounds.Max).Except(testCase.Excluded));
                        }

                        return Expect.EveryDraw(Any.SByte().Between(testCase.Bounds.Min, testCase.Bounds.Max).Except(testCase.Excluded),
                                                value => value != testCase.Excluded
                                                         && value >= testCase.Bounds.Min
                                                         && value <= testCase.Bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Byte: Between contains — every draw falls within the declared inclusive bounds.")]
    public void ByteBetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Byte()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Byte().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Byte: Between with equal bounds pins the value, for every value.")]
    public void ByteBetweenWithEqualBoundsPins() {
        Prop.ForAll(Byte().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Byte().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Byte: the inclusive bounds admit their own bound, on both sides.")]
    public void ByteInclusiveBoundsAdmitTheirOwnBound() {
        Prop.ForAll(Byte().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Byte().GreaterThanOrEqualTo(bound), value => value >= bound)
                             && Expect.EveryDraw(Any.Byte().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Byte: GreaterThan and LessThan are strict, and conflict at the domain edges — zero being the floor.")]
    public void ByteStrictBoundsAreStrictAndConflictAtTheEdges() {
        Prop.ForAll(Byte().ToArbitrary(),
                    bound => {
                        // The unsigned floor is zero, not a negative sentinel: LessThan(0) has nothing left to offer
                        // and must conflict rather than wrap around to byte.MaxValue.
                        bool above = bound == byte.MaxValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Byte().GreaterThan(bound))
                                         : Expect.EveryDraw(Any.Byte().GreaterThan(bound), value => value > bound);
                        bool below = bound == byte.MinValue
                                         ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Byte().LessThan(bound))
                                         : Expect.EveryDraw(Any.Byte().LessThan(bound), value => value < bound);

                        return above && below;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Byte: Between never yields a value excluded by a subsequent Except.")]
    public void ByteExceptRemovesTheValueFromTheInterval() {
        Gen<((byte Min, byte Max) Bounds, byte Excluded)> cases =
            from bounds in Generators.OrderedPair(Byte())
            from offset in Gen.Choose(0, bounds.Max - bounds.Min)
            select (Bounds: bounds, Excluded: (byte)(bounds.Min + offset));

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        // Excluding the single value of a pinned interval empties it: that is a conflict, not a draw.
                        if (testCase.Bounds.Min == testCase.Bounds.Max) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Byte().Between(testCase.Bounds.Min, testCase.Bounds.Max).Except(testCase.Excluded));
                        }

                        return Expect.EveryDraw(Any.Byte().Between(testCase.Bounds.Min, testCase.Bounds.Max).Except(testCase.Excluded),
                                                value => value != testCase.Excluded
                                                         && value >= testCase.Bounds.Min
                                                         && value <= testCase.Bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int16: a generator is an immutable recipe — constraining it never narrows the original.")]
    public void Int16ConstrainingNeverMutatesTheOriginal() {
        Prop.ForAll(Generators.OrderedPair(Int16()).ToArbitrary(),
                    bounds => {
                        AnyInt16 original = Any.Int16().Between(bounds.Min, bounds.Max);
                        AnyInt16 narrowed = original.GreaterThanOrEqualTo(bounds.Max);

                        return !ReferenceEquals(original, narrowed)
                               && Expect.EveryDraw(original, value => value >= bounds.Min && value <= bounds.Max)
                               && Expect.EveryDraw(narrowed, value => value == bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt16: a generator is an immutable recipe — constraining it never narrows the original.")]
    public void UInt16ConstrainingNeverMutatesTheOriginal() {
        Prop.ForAll(Generators.OrderedPair(UInt16()).ToArbitrary(),
                    bounds => {
                        AnyUInt16 original = Any.UInt16().Between(bounds.Min, bounds.Max);
                        AnyUInt16 narrowed = original.GreaterThanOrEqualTo(bounds.Max);

                        return !ReferenceEquals(original, narrowed)
                               && Expect.EveryDraw(original, value => value >= bounds.Min && value <= bounds.Max)
                               && Expect.EveryDraw(narrowed, value => value == bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt32: crossed bounds are rejected — as Between arguments an argument error, as two constraints a conflict.")]
    public void UInt32CrossedBoundsAreRejected() {
        Prop.ForAll(Generators.OrderedPair(UInt32()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              // Argument validation precedes conflict checking: a swapped pair passed to a single
                              // call is an argument error, whereas the same emptiness spread over two calls is a
                              // constraint conflict. The two must not collapse into one another.
                              || (Expect.Throws<ArgumentException>(() => Any.UInt32().Between(bounds.Max, bounds.Min))
                                  && Expect.Throws<ConflictingAnyConstraintException>(
                                      () => Any.UInt32().GreaterThan(bounds.Max).LessThan(bounds.Min))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt32: OneOf draws only from the supplied pool, whatever the pool.")]
    public void UInt32OneOfStaysWithinItsPool() {
        Gen<uint[]> pools = Gen.NonEmptyListOf(UInt32()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.UInt32().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int64: Between contains — every draw falls within the bounds, across the whole 64-bit space.")]
    public void Int64BetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int64()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Int64().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int64: crossed bounds are rejected — as Between arguments an argument error, as two constraints a conflict.")]
    public void Int64CrossedBoundsAreRejected() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int64()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || (Expect.Throws<ArgumentException>(() => Any.Int64().Between(bounds.Max, bounds.Min))
                                  && Expect.Throws<ConflictingAnyConstraintException>(
                                      () => Any.Int64().GreaterThan(bounds.Max).LessThan(bounds.Min))))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Int64: OneOf draws only from the supplied pool, whatever the pool.")]
    public void Int64OneOfStaysWithinItsPool() {
        Gen<long[]> pools = Gen.NonEmptyListOf(Generators.Int64()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Int64().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt64: Between contains — every draw falls within the bounds, up to the full unsigned range.")]
    public void UInt64BetweenContainsEveryDraw() {
        // The unsigned 64-bit domain is the one interval whose own size does not fit its own width: an interval
        // spanning it cannot be sampled by "draw an index in [0, count)". Only quantified bounds reach that case.
        Prop.ForAll(Generators.OrderedPair(UInt64()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.UInt64().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UInt64: Except never yields the excluded value, even on the unbounded full-width path.")]
    public void UInt64ExceptHoldsOnTheFullWidthPath() {
        // No interval is declared, so the specification still spans the whole domain and the exclusion has to be
        // honoured by the full-width sampling path rather than by index arithmetic over a bounded range.
        Prop.ForAll(UInt64().ToArbitrary(),
                    excluded => Expect.EveryDraw(Any.UInt64().Except(excluded), value => value != excluded))
            .QuickCheckThrowOnFailure();
    }

}

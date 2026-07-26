#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyInt32" />'s interval algebra. Where the example-based suite pins a few
///     hand-picked intervals, these quantify over the whole bound space — <c>[int.MinValue, int.MaxValue]</c>,
///     degenerate intervals, and the off-by-one edges around them — so a bound that overflows or truncates for one
///     interval in a million is found and shrunk to its minimal counter-example rather than missed.
/// </summary>
[TestSubject(typeof(AnyInt32))]
public sealed class Int32IntervalProperties {

    [Fact(DisplayName = "Between contains: every draw falls within the declared inclusive bounds.")]
    public void BetweenContainsEveryDraw() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => Expect.EveryDraw(Any.Int32().Between(bounds.Min, bounds.Max),
                                               value => value >= bounds.Min && value <= bounds.Max))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Between with equal bounds pins the value, for every value.")]
    public void BetweenWithEqualBoundsPins() {
        Prop.ForAll(Generators.Int32().ToArbitrary(),
                    value => Expect.EveryDraw(Any.Int32().Between(value, value), drawn => drawn == value))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "GreaterThanOrEqualTo is inclusive: every draw is at least the bound.")]
    public void GreaterThanOrEqualToIsInclusive() {
        Prop.ForAll(Generators.Int32().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Int32().GreaterThanOrEqualTo(bound), value => value >= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "LessThanOrEqualTo is inclusive: every draw is at most the bound.")]
    public void LessThanOrEqualToIsInclusive() {
        Prop.ForAll(Generators.Int32().ToArbitrary(),
                    bound => Expect.EveryDraw(Any.Int32().LessThanOrEqualTo(bound), value => value <= bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "GreaterThan is strict below int.MaxValue, and conflicts at it.")]
    public void GreaterThanIsStrictAndConflictsAtTheCeiling() {
        Prop.ForAll(Generators.Int32().ToArbitrary(),
                    bound => bound == int.MaxValue
                                 ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int32().GreaterThan(bound))
                                 : Expect.EveryDraw(Any.Int32().GreaterThan(bound), value => value > bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "LessThan is strict above int.MinValue, and conflicts at it.")]
    public void LessThanIsStrictAndConflictsAtTheFloor() {
        Prop.ForAll(Generators.Int32().ToArbitrary(),
                    bound => bound == int.MinValue
                                 ? Expect.Throws<ConflictingAnyConstraintException>(() => Any.Int32().LessThan(bound))
                                 : Expect.EveryDraw(Any.Int32().LessThan(bound), value => value < bound))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Between never yields a value excluded by a subsequent Except.")]
    public void ExceptRemovesTheValueFromTheInterval() {
        Gen<(int Min, int Max)> intervals = Generators.OrderedPair(Generators.Count(40));

        Prop.ForAll((from bounds in intervals
                     from excluded in Gen.Choose(bounds.Min, bounds.Max)
                     select (bounds, excluded)).ToArbitrary(),
                    testCase => {
                        // Excluding the single value of a pinned interval empties it: that is a conflict, not a draw.
                        if (testCase.bounds.Min == testCase.bounds.Max) {
                            return Expect.Throws<ConflictingAnyConstraintException>(
                                () => Any.Int32().Between(testCase.bounds.Min, testCase.bounds.Max).Except(testCase.excluded));
                        }

                        return Expect.EveryDraw(Any.Int32().Between(testCase.bounds.Min, testCase.bounds.Max).Except(testCase.excluded),
                                                value => value != testCase.excluded
                                                         && value >= testCase.bounds.Min
                                                         && value <= testCase.bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OneOf draws only from the supplied pool, whatever the pool.")]
    public void OneOfStaysWithinItsPool() {
        Gen<int[]> pools = Gen.NonEmptyListOf(Generators.Int32()).Select(values => values.Distinct().ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Int32().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Crossed Between arguments are an argument error, never a silent swap.")]
    public void CrossedBoundsAreAnArgumentError() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ArgumentException>(() => Any.Int32().Between(bounds.Max, bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Bounds that cannot both hold conflict, for every crossed pair.")]
    public void ImpossibleBoundPairsConflict() {
        Prop.ForAll(Generators.OrderedPair(Generators.Int32()).ToArbitrary(),
                    bounds => bounds.Min == bounds.Max
                              || Expect.Throws<ConflictingAnyConstraintException>(
                                  () => Any.Int32().GreaterThan(bounds.Max).LessThan(bounds.Min)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A generator is an immutable recipe: constraining it never narrows the original.")]
    public void ConstrainingNeverMutatesTheOriginal() {
        Prop.ForAll(Generators.OrderedPair(Generators.Count(60)).ToArbitrary(),
                    bounds => {
                        AnyInt32 original = Any.Int32().Between(bounds.Min, bounds.Max);
                        AnyInt32 narrowed = original.GreaterThanOrEqualTo(bounds.Max);

                        return !ReferenceEquals(original, narrowed)
                               && Expect.EveryDraw(original, value => value >= bounds.Min && value <= bounds.Max)
                               && Expect.EveryDraw(narrowed, value => value == bounds.Max);
                    })
            .QuickCheckThrowOnFailure();
    }

}

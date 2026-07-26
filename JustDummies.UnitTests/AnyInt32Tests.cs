#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The example-based half of <see cref="AnyInt32" />'s contract: what a conflict message must name, which
///     arguments are rejected outright, that the named domain extremes are generable, and that a bounded range
///     is actually reached. The invariants that hold for <i>every</i> bound — containment, strictness,
///     inclusiveness, exclusion, immutability — are quantified over generated bounds in
///     <c>JustDummies.PropertyTests</c> instead, and are deliberately not restated here (ADR-0040).
/// </summary>
[TestSubject(typeof(AnyInt32))]
public sealed class AnyInt32Tests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<int> Samples(IAny<int> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "An unconstrained Int32 generates without failing.")]
    public void UnconstrainedGenerates() {
        Check.ThatCode(() => Any.Int32().Generate()).DoesNotThrow();
    }

    [Fact(DisplayName = "Positive yields values strictly greater than zero.")]
    public void PositiveIsStrictlyPositive() {
        foreach (int value in Samples(Any.Int32().Positive())) {
            Check.That(value).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "Negative yields values strictly less than zero.")]
    public void NegativeIsStrictlyNegative() {
        foreach (int value in Samples(Any.Int32().Negative())) {
            Check.That(value).IsStrictlyLessThan(0);
        }
    }

    [Fact(DisplayName = "Zero yields exactly zero.")]
    public void ZeroIsZero() {
        Check.That(Any.Int32().Zero().Generate()).IsEqualTo(0);
    }

    [Fact(DisplayName = "NonZero never yields zero.")]
    public void NonZeroIsNeverZero() {
        foreach (int value in Samples(Any.Int32().NonZero().Between(-2, 2))) {
            Check.That(value).IsNotEqualTo(0);
        }
    }

    [Fact(DisplayName = "Between eventually reaches both inclusive bounds.")]
    public void BetweenReachesItsBounds() {
        HashSet<int> seen = new(Samples(Any.Int32().Between(1, 3)));

        Check.That(seen.Contains(1)).IsTrue();
        Check.That(seen.Contains(3)).IsTrue();
    }

    [Fact(DisplayName = "The extreme bounds of the Int32 range are generable.")]
    public void ExtremeBoundsAreGenerable() {
        Check.That(Any.Int32().LessThanOrEqualTo(int.MinValue).Generate()).IsEqualTo(int.MinValue);
        Check.That(Any.Int32().GreaterThanOrEqualTo(int.MaxValue).Generate()).IsEqualTo(int.MaxValue);
    }

    [Fact(DisplayName = "DifferentFrom never yields the excluded value.")]
    public void DifferentFromNeverYieldsTheValue() {
        foreach (int value in Samples(Any.Int32().Between(7, 8).DifferentFrom(7))) {
            Check.That(value).IsEqualTo(8);
        }
    }

    [Fact(DisplayName = "Positive then Negative conflicts, naming both constraints.")]
    public void PositiveThenNegativeConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().Positive().Negative());

        Check.That(conflict.Message).Contains("Negative()");
        Check.That(conflict.Message).Contains("Positive()");
    }

    [Fact(DisplayName = "GreaterThan then an impossible LessThan conflicts, naming both constraints.")]
    public void CrossedBoundsConflict() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().GreaterThan(100).LessThan(10));

        Check.That(conflict.Message).Contains("LessThan(10)");
        Check.That(conflict.Message).Contains("GreaterThan(100)");
    }

    [Fact(DisplayName = "Zero then NonZero conflicts: the pinned value is excluded.")]
    public void ZeroThenNonZeroConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().Zero().NonZero());

        Check.That(conflict.Message).Contains("NonZero()");
        Check.That(conflict.Message).Contains("Zero()");
    }

    [Fact(DisplayName = "GreaterThan int.MaxValue conflicts: no Int32 satisfies it.")]
    public void GreaterThanMaxValueConflicts() {
        Check.ThatCode(() => Any.Int32().GreaterThan(int.MaxValue)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "OneOf then a bound excluding every allowed value conflicts.")]
    public void OneOfEmptiedByABoundConflicts() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().OneOf(1, 2).GreaterThan(5));

        Check.That(conflict.Message).Contains("GreaterThan(5)");
        Check.That(conflict.Message).Contains("OneOf(1, 2)");
    }

    [Fact(DisplayName = "A second OneOf conflicts: the allow-list is declared once.")]
    public void SecondOneOfConflicts() {
        Check.ThatCode(() => Any.Int32().OneOf(1, 2).OneOf(3, 4)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Except exhausting the whole interval conflicts.")]
    public void ExceptExhaustingTheIntervalConflicts() {
        Check.ThatCode(() => Any.Int32().Between(1, 2).Except(1, 2)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Except exhausting the allow-list conflicts.")]
    public void ExceptExhaustingTheAllowListConflicts() {
        Check.ThatCode(() => Any.Int32().OneOf(1, 2).Except(1).Except(2)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "OneOf and Except reject null or empty value lists.")]
    public void OneOfAndExceptRejectNullOrEmpty() {
        Check.ThatCode(() => Any.Int32().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Int32().OneOf(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Int32().Except()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.Int32().Except(null!)).Throws<ArgumentNullException>();
    }

}

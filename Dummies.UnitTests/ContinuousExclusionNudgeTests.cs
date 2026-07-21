#region Usings declarations

using NFluent;

#endregion

namespace Dummies.UnitTests;

/// <summary>
///     Regression coverage for issue #207. On the narrow (quantized) floating-point types an exclusion inside a tight
///     range must be honoured by the type-aware nudge — ascending or descending — instead of stalling on a sub-ulp
///     double step and exhausting the budget on a satisfiable specification. The identical scenarios on
///     <see cref="Any.Double" /> guard the shared engine from the other side.
/// </summary>
public sealed class ContinuousExclusionNudgeTests {

    private const int SeedCount = 500;

    [Fact(DisplayName = "Half: an exclusion on the lower bound of a two-value range yields the surviving value for every seed.")]
    public void HalfExclusionOnLowerBound() {
        Half min      = (Half)1f;
        Half max      = (Half)1.001f;   // rounds to 1.0009765625: the next representable Half above 1.0
        Half survivor = max;

        for (int seed = 0; seed < SeedCount; seed++) {
            Half value = Any.WithSeed(seed).Half().Between(min, max).DifferentFrom(min).Generate();
            Check.That(value == survivor).IsTrue();
        }
    }

    [Fact(DisplayName = "Half: an exclusion on the upper bound descends to the surviving lower value for every seed.")]
    public void HalfExclusionOnUpperBound() {
        Half min = (Half)1f;
        Half max = (Half)1.001f;

        for (int seed = 0; seed < SeedCount; seed++) {
            Half value = Any.WithSeed(seed).Half().Between(min, max).DifferentFrom(max).Generate();
            Check.That(value == min).IsTrue();
        }
    }

    [Fact(DisplayName = "Single: an exclusion inside a narrow range never yields the excluded value, either bound, for any seed.")]
    public void SingleExclusionInsideNarrowRange() {
        float min = 1f;
        float max = MathF.BitIncrement(MathF.BitIncrement(1f));   // 1 + 2 ulp: three representable floats in range

        for (int seed = 0; seed < SeedCount; seed++) {
            float lower = Any.WithSeed(seed).Single().Between(min, max).DifferentFrom(min).Generate();
            Check.That(lower).IsStrictlyGreaterThan(min);
            Check.That(lower).IsLessOrEqualThan(max);

            float upper = Any.WithSeed(seed).Single().Between(min, max).DifferentFrom(max).Generate();
            Check.That(upper).IsStrictlyLessThan(max);
            Check.That(upper).IsGreaterOrEqualThan(min);
        }
    }

    [Fact(DisplayName = "Double: an exclusion inside a narrow range never yields the excluded value, either bound, for any seed.")]
    public void DoubleExclusionInsideNarrowRange() {
        double min = 1d;
        double max = Math.BitIncrement(Math.BitIncrement(1d));   // 1 + 2 ulp: three representable doubles in range

        for (int seed = 0; seed < SeedCount; seed++) {
            double lower = Any.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            Check.That(lower).IsStrictlyGreaterThan(min);
            Check.That(lower).IsLessOrEqualThan(max);

            double upper = Any.WithSeed(seed).Double().Between(min, max).DifferentFrom(max).Generate();
            Check.That(upper).IsStrictlyLessThan(max);
            Check.That(upper).IsGreaterOrEqualThan(min);
        }
    }

    [Fact(DisplayName = "A range whose every representable value is excluded fails with a seeded AnyGenerationException whose replay hint points at Any.WithSeed, not the inapplicable Any.Reproducibly.")]
    public void ExhaustedRangeThrowsSeededGenerationException() {
        Half min = (Half)1f;
        Half max = (Half)1.001f;   // exactly two representable Half values in [min, max]

        AnyGenerationException thrown = Assert.Throws<AnyGenerationException>(
            () => Any.WithSeed(207).Half().Between(min, max).Except(min, max).Generate());

        Check.That(thrown.Seed).IsEqualTo(207);
        Check.That(thrown.Message).Contains("207");
        // The draw came from Any.WithSeed(207) — a fixed context that replays by itself — so the hint must name it,
        // not the ambient Any.Reproducibly(...) instruction, which would not reproduce this run.
        Check.That(thrown.Message).Contains("Any.WithSeed(207)");
        Check.That(thrown.Message).Not.Contains("Any.Reproducibly(");
    }

    [Fact(DisplayName = "The nudge stays reproducible: the same seed yields the same value across runs.")]
    public void NudgeIsReproducibleForAGivenSeed() {
        double min = 1d;
        double max = Math.BitIncrement(Math.BitIncrement(1d));

        for (int seed = 0; seed < 50; seed++) {
            double first  = Any.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            double second = Any.WithSeed(seed).Double().Between(min, max).DifferentFrom(min).Generate();
            Check.That(second).IsEqualTo(first);
        }
    }

}

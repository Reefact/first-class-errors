#region Usings declarations

using System.Collections.Concurrent;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Drawing from one seeded source on several threads at once. The ambient source flows with the execution
///     context, so it reaches every thread a test spawns; a context from <see cref="Any.WithSeed" /> is shared by
///     whoever holds it. Both therefore hand the same source to concurrent callers, and the source must survive it.
/// </summary>
/// <remarks>
///     <para>
///         Regression for issue #310. The defect was not a loss of quality but a collapse: an unsynchronized
///         <see cref="Random" /> whose two internal indices converge under contention returns zero for ever, so every
///         generator settles on the minimum of its declared range — <c>0</c>, <c>""</c>, <see cref="Guid.Empty" />,
///         <see cref="int.MinValue" /> — and stays there for the rest of the scope. Those are exactly the values most
///         likely to make an assertion pass for the wrong reason, and nothing throws.
///     </para>
///     <para>
///         These tests are statistical in the direction that matters least: once the draw is serialized, corruption is
///         impossible rather than unlikely, so they pass deterministically. Before the fix they failed with very high
///         probability but not certainty — the usual bargain for a concurrency regression. The parallelism is
///         deliberately oversubscribed relative to the core count to make the contention reliable.
///     </para>
/// </remarks>
[TestSubject(typeof(Any))]
public sealed class ConcurrentDrawTests {

    #region Statics members declarations

    private const int Threads         = 8;
    private const int DrawsPerThread  = 10_000;
    private const int TotalDraws      = Threads * DrawsPerThread;

    /// <summary>Runs <paramref name="draw" /> on every thread at once and collects everything it produced.</summary>
    private static List<T> Storm<T>(Func<T> draw) {
        ConcurrentBag<T> drawn = new();
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads },
                     _ => {
                         for (int index = 0; index < DrawsPerThread; index++) { drawn.Add(draw()); }
                     });

        return drawn.ToList();
    }

    /// <summary>How many times the most frequent value came up — the collapse signal, not a distribution measure.</summary>
    private static int MostFrequent<T>(IEnumerable<T> values) {
        return values.GroupBy(value => value).Max(group => group.Count());
    }

    #endregion

    [Fact(DisplayName = "Concurrent draws from an ambient seed scope never collapse onto one value.")]
    public void ConcurrentAmbientDrawsDoNotCollapse() {
        List<int> drawn = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.Int32().Generate()));

        // A tenth of the draws sharing one value cannot happen by chance over the full Int32 range; it is the
        // signature of a source that stopped generating. Far below what the defect produced (62% to 91%).
        Check.WithCustomMessage($"{MostFrequent(drawn)} of {TotalDraws} draws returned the same value; the shared Random collapsed.")
             .That(MostFrequent(drawn))
             .IsStrictlyLessThan(TotalDraws / 10);
    }

    [Fact(DisplayName = "A seeded source is still usable for sequential draws taken after a concurrent burst.")]
    public void ASeededSourceSurvivesAConcurrentBurst() {
        List<int> afterwards = [];

        Any.Reproducibly(310, () => {
            Storm(() => Any.Int32().Generate());

            // The heart of the regression: the defect was permanent. Once the indices had converged, every later
            // draw on that source returned int.MinValue — including these, taken on one thread with no contention.
            afterwards = Enumerable.Range(0, 20).Select(_ => Any.Int32().Generate()).ToList();
        });

        Check.WithCustomMessage($"Sequential draws after the burst were all {afterwards[0]}; the source stayed dead.")
             .That(afterwards.Distinct().Count())
             .IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "A concurrent burst does not poison the other generators of the same source.")]
    public void AConcurrentBurstDoesNotPoisonSiblingGenerators() {
        string text  = string.Empty;
        Guid   guid  = Guid.Empty;

        Any.Reproducibly(310, () => {
            Storm(() => Any.Int32().Generate());

            // The corruption lives in the source, not in the generator that triggered it, so unrelated generators
            // resolved from it afterwards collapsed too — a string to "" and a Guid to Guid.Empty.
            text = Any.String().NonEmpty().Generate();
            guid = Any.Guid().Generate();
        });

        Check.WithCustomMessage("A non-empty string generator returned an empty string after a concurrent burst.")
             .That(text).IsNotEmpty();
        Check.WithCustomMessage("The Guid generator returned Guid.Empty after a concurrent burst.")
             .That(guid).IsNotEqualTo(Guid.Empty);
    }

    [Fact(DisplayName = "Concurrent draws never collapse a bounded generator onto its lower bound.")]
    public void ConcurrentBoundedDrawsDoNotCollapseOntoTheirLowerBound() {
        List<int> drawn = [];

        // A bounded range makes the failure mode legible: a dead source does not return a random value inside the
        // interval, it returns the interval's minimum, which reads like a plausible dummy.
        Any.Reproducibly(310, () => drawn = Storm(() => Any.Int32().Between(1_000, 9_999).Generate()));

        int atTheBound = drawn.Count(value => value == 1_000);

        Check.WithCustomMessage($"{atTheBound} of {TotalDraws} draws returned the lower bound 1000.")
             .That(atTheBound)
             .IsStrictlyLessThan(TotalDraws / 10);
    }

    [Fact(DisplayName = "A context shared across threads stays usable after concurrent draws.")]
    public void ASharedContextSurvivesConcurrentDraws() {
        AnyContext context = Any.WithSeed(310);

        List<int> drawn      = Storm(() => context.Int32().Generate());
        List<int> afterwards = Enumerable.Range(0, 20).Select(_ => context.Int32().Generate()).ToList();

        Check.WithCustomMessage($"{MostFrequent(drawn)} of {TotalDraws} draws from a shared context returned the same value.")
             .That(MostFrequent(drawn))
             .IsStrictlyLessThan(TotalDraws / 10);
        Check.WithCustomMessage($"Sequential draws from the shared context after the burst were all {afterwards[0]}.")
             .That(afterwards.Distinct().Count())
             .IsStrictlyGreaterThan(1);
    }

}

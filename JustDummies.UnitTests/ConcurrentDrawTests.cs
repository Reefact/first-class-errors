#region Usings declarations

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

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

    #region Composed draw paths

    // The lock lives at one choke point — SeededRandom — but every generator reaches it through a different path.
    // The scalar cases above prove the choke point itself; these prove the paths with the most moving parts still
    // route through it: the derived generators (As, Combine) that wrap a draw, the collection engine's fill and
    // dedup-draw loops, and the regex context. Each collapses in its own way if the source dies, so each asserts
    // the shape of its own non-collapse — and each was confirmed red before the fix by stripping the lock (#310).
    //
    // Paths whose per-draw work is a single light sample — OrNull's null/value coin, a bare Any.Double() — are
    // deliberately absent. Corruption is reliably provoked only by NextBytes-heavy draws (an eight-byte ordinal
    // fill, a regex drawing one choice per character); a lone Next(2) or NextDouble() never builds enough
    // contention to corrupt the source within a bounded run, so a lock-stripped mutant does not make such a test
    // fail. A test that cannot go red on the broken code would only manufacture false confidence, and the choke
    // point those paths share is already pinned by the cases here. (Measured: OrNull and Double each missed 5/5
    // lock-stripped runs, while every case below tripped 5/5.)

    [Fact(DisplayName = "Concurrent draws through Combine never collapse either operand.")]
    public void ConcurrentCombineDrawsDoNotCollapse() {
        List<(int First, int Second)> drawn = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.Combine(Any.Int32(), Any.Int32(), (first, second) => (first, second)).Generate()));

        Check.WithCustomMessage($"Combine's first operand collapsed: {MostFrequent(drawn.Select(pair => pair.First))} of {TotalDraws} identical.")
             .That(MostFrequent(drawn.Select(pair => pair.First)))
             .IsStrictlyLessThan(TotalDraws / 10);
        Check.WithCustomMessage($"Combine's second operand collapsed: {MostFrequent(drawn.Select(pair => pair.Second))} of {TotalDraws} identical.")
             .That(MostFrequent(drawn.Select(pair => pair.Second)))
             .IsStrictlyLessThan(TotalDraws / 10);
    }

    [Fact(DisplayName = "Concurrent draws through As keep the underlying draw healthy.")]
    public void ConcurrentAsDrawsStayHealthy() {
        // A pure projection carries no shared state, so any degeneration here is the library's own serialized draw
        // collapsing, not a user-side race — the latter is the caller's responsibility, per the IAny<T> contract.
        List<long> drawn = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.Int32().As(value => (long)value * 2).Generate()));

        Check.WithCustomMessage($"{MostFrequent(drawn)} of {TotalDraws} As-projected values were identical; the underlying draw collapsed.")
             .That(MostFrequent(drawn))
             .IsStrictlyLessThan(TotalDraws / 10);
    }

    [Fact(DisplayName = "Concurrent draws through a list generator keep the right size and never collapse the elements.")]
    public void ConcurrentListDrawsDoNotCollapse() {
        List<List<int>> drawn = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.ListOf(Any.Int32()).WithCount(4).Generate()));

        List<int> elements = drawn.SelectMany(list => list).ToList();

        Check.WithCustomMessage("A fixed-count list came back the wrong size under concurrency.")
             .That(drawn.All(list => list.Count == 4)).IsTrue();
        Check.WithCustomMessage($"{MostFrequent(elements)} of {elements.Count} list elements were identical; the element draw collapsed.")
             .That(MostFrequent(elements))
             .IsStrictlyLessThan(elements.Count / 10);
    }

    [Fact(DisplayName = "Concurrent draws through a distinct set generator stay valid.")]
    public void ConcurrentSetDrawsStayValid() {
        // The distinct path runs a bounded dedup-draw against a fresh HashSet per generation — the path most exposed
        // to a dead source, which cannot supply the fresh values it needs and fails loudly rather than collapsing.
        List<HashSet<int>> drawn = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.SetOf(Any.Int32().Between(0, 100_000)).WithCount(5).Generate()));

        Check.WithCustomMessage("A distinct set came back the wrong size under concurrency.")
             .That(drawn.All(set => set.Count == 5)).IsTrue();

        List<int> elements = drawn.SelectMany(set => set).ToList();
        Check.WithCustomMessage($"{MostFrequent(elements)} of {elements.Count} set elements were identical; the element draw collapsed.")
             .That(MostFrequent(elements))
             .IsStrictlyLessThan(elements.Count / 5);
    }

    [Fact(DisplayName = "Concurrent draws through a pattern generator match and never collapse.")]
    public void ConcurrentPatternDrawsStayValid() {
        Regex        pattern = new("^[A-Z]{3}-[0-9]{4}$");
        List<string> drawn   = [];

        Any.Reproducibly(310, () => drawn = Storm(() => Any.StringMatching("^[A-Z]{3}-[0-9]{4}$").Generate()));

        Check.WithCustomMessage($"A pattern draw did not match under concurrency, e.g. \"{drawn.FirstOrDefault(value => !pattern.IsMatch(value))}\".")
             .That(drawn.All(value => pattern.IsMatch(value))).IsTrue();
        // A dead regex context still matches (it picks the first choice every time — "AAA-0000"), so matching alone
        // would not catch the collapse; the non-collapse assertion is what pins it.
        Check.WithCustomMessage($"{MostFrequent(drawn)} of {TotalDraws} pattern draws were identical; generation collapsed.")
             .That(MostFrequent(drawn))
             .IsStrictlyLessThan(TotalDraws / 10);
    }

    #endregion

}

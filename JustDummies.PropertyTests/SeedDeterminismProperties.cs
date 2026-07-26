#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the seeding surface: the isolated <see cref="AnyContext" /> handed out by
///     <see cref="Any.WithSeed" />, the ambient <c>Any.UseSeed(...)</c> scope, and the <c>Any.Reproducibly(...)</c>
///     runners. The example-based suite pins three hand-picked seeds — 12345, 777, 31415 — and can therefore only
///     prove reproducibility for those three numbers; these quantify over the seed itself, including the values a
///     hand-written test would never pick (zero, <c>int.MinValue</c>, <c>int.MaxValue</c>), so a seed that fails to
///     pin a run is found and shrunk to its minimal counter-example.
/// </summary>
/// <remarks>
///     <para>
///         Reproducibility is a claim about a whole run rather than about a single draw, so every property here
///         compares a <b>batch</b>: a mixed sequence of scalar, collection, nullable and pattern draws joined into
///         one string. The comparison fails as soon as any one draw shifts, which makes the batch both a sharper
///         probe than a single value and the reason the distinctness property at the end can rest on entropy rather
///         than on luck.
///     </para>
///     <para>
///         <see cref="AnyContext" /> deliberately mirrors only the scalar factories, so the collection parts of a
///         context batch go through the static combinators over a context-derived element generator. That is not a
///         workaround but part of what is under test: a collection draws from its element generator's random source,
///         never from the ambient one.
///     </para>
///     <para>
///         Failure diagnostics are asserted by <b>type</b> and, for the reported seed, by the seed being quotable
///         from the message — never by the sentence around it, whose wording is a diagnostic concern rather than a
///         seeding one.
///     </para>
/// </remarks>
[TestSubject(typeof(AnyContext))]
public sealed class SeedDeterminismProperties {

    #region Statics members declarations

    /// <summary>
    ///     Draws a mixed batch from <paramref name="any" /> and joins it into a single string: every scalar family
    ///     the context exposes, plus a list, a set, a nullable and a pattern. Mirrors the <c>Batch()</c> helper of
    ///     the example-based suite minus the .NET 8+ types — this file also compiles on the .NET Framework floor,
    ///     where <c>Int128</c> and <c>Half</c> do not exist.
    /// </summary>
    private static string ContextBatch(AnyContext any) {
        int      full    = any.Int32().Generate();
        int      bounded = any.Int32().Between(1, 1000).Generate();
        string   free    = any.String().Generate();
        string   capped  = any.String().NonEmpty().WithMaxLength(50).Generate();
        string   shaped  = any.String().StartingWith("ORD-").WithLength(12).Generate();
        long     wide    = any.Int64().Generate();
        double   real    = any.Double().Between(0d, 1000d).Generate();
        decimal  exact   = any.Decimal().Between(0m, 1000m).Generate();
        bool     flag    = any.Boolean().Generate();
        Guid     id      = any.Guid().Generate();
        char     letter  = any.Char().Generate();
        TimeSpan span    = any.TimeSpan().Generate();
        DateTime instant = any.DateTime().Generate();
        // A context carries no collection factories of its own, and does not need any: the static combinators take
        // the element generator's random source, so these two collections draw from the context all the same.
        List<int>    list  = Any.ListOf(any.Int32().Between(0, 9)).WithCount(4).Generate();
        HashSet<int> set   = Any.SetOf(any.Int32().Between(0, 99)).WithCount(3).Generate();
        int?         maybe = any.Int32().Between(0, 9).OrNull().Generate();
        string       coded = any.StringMatching(@"[A-Z]{3}-\d{4}").Generate();

        return string.Join("|", full, bounded, free, capped, shaped,
                           wide, real, exact, flag, id, letter,
                           span.Ticks, instant.Ticks,
                           string.Join("-", list), string.Join("-", set.OrderBy(value => value)),
                           maybe?.ToString() ?? "null", coded);
    }

    /// <summary>
    ///     The same batch drawn from the static entry points, for the mechanisms that pin the <b>ambient</b> context
    ///     instead of handing out a context object — <c>Any.UseSeed(...)</c> and <c>Any.Reproducibly(...)</c>. It is
    ///     a second method rather than one parameterized over both because <see cref="AnyContext" /> and the static
    ///     <see cref="Any" /> share a surface, not a type.
    /// </summary>
    private static string AmbientBatch() {
        int      full    = Any.Int32().Generate();
        int      bounded = Any.Int32().Between(1, 1000).Generate();
        string   free    = Any.String().Generate();
        string   capped  = Any.String().NonEmpty().WithMaxLength(50).Generate();
        string   shaped  = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        long     wide    = Any.Int64().Generate();
        double   real    = Any.Double().Between(0d, 1000d).Generate();
        decimal  exact   = Any.Decimal().Between(0m, 1000m).Generate();
        bool     flag    = Any.Boolean().Generate();
        Guid     id      = Any.Guid().Generate();
        char     letter  = Any.Char().Generate();
        TimeSpan span    = Any.TimeSpan().Generate();
        DateTime instant = Any.DateTime().Generate();

        List<int>    list  = Any.ListOf(Any.Int32().Between(0, 9)).WithCount(4).Generate();
        HashSet<int> set   = Any.SetOf(Any.Int32().Between(0, 99)).WithCount(3).Generate();
        int?         maybe = Any.Int32().Between(0, 9).OrNull().Generate();
        string       coded = Any.StringMatching(@"[A-Z]{3}-\d{4}").Generate();

        return string.Join("|", full, bounded, free, capped, shaped,
                           wide, real, exact, flag, id, letter,
                           span.Ticks, instant.Ticks,
                           string.Join("-", list), string.Join("-", set.OrderBy(value => value)),
                           maybe?.ToString() ?? "null", coded);
    }

    /// <summary>
    ///     A blank replay snippet: the empty string, and runs of the whitespace characters <c>Trim()</c> removes.
    ///     The example-based suite pins <c>""</c> and three spaces; what the guard rejects is blankness, not those
    ///     two spellings.
    /// </summary>
    private static Gen<string> BlankSnippet() {
        return from length in Gen.Choose(0, 6)
               from whitespace in Gen.Elements(new[] { ' ', '\t', '\n', '\r', '\v', '\f' })
               select new string(whitespace, length);
    }

    /// <summary>
    ///     Two seeds guaranteed to differ, drawn from the non-negative half of the seed space. The restriction is
    ///     deliberate and is about the BCL rather than about JustDummies: <c>new Random(seed)</c> derives its state
    ///     from the seed's absolute value, so <c>s</c> and <c>-s</c> are by design the very same generator. Quantifying
    ///     a distinctness claim over the whole <see cref="int" /> range would therefore fail for a reason that has
    ///     nothing to do with the library under test.
    /// </summary>
    private static Gen<(int First, int Second)> DifferentSeeds() {
        // Built from a base and a strictly positive delta rather than filtered for inequality, so no case is ever
        // rejected and the halved bounds keep the sum inside int.
        return from first in Gen.Choose(0, int.MaxValue / 2)
               from delta in Gen.Choose(1, int.MaxValue / 2)
               select (first, first + delta);
    }

    /// <summary>
    ///     Returns <c>true</c> when <paramref name="action" /> throws <typeparamref name="TException" /> itself
    ///     rather than a derived type. <see cref="Expect.Throws{TException}" /> accepts a subclass, which would let
    ///     an <see cref="ArgumentNullException" /> satisfy the blank-snippet property whose whole point is that the
    ///     two rejections are told apart.
    /// </summary>
    private static bool ThrowsExactly<TException>(Action action)
        where TException : Exception {
        try {
            action();

            return false;
        } catch (Exception exception) {
            return exception.GetType() == typeof(TException);
        }
    }

    #endregion

    [Fact(DisplayName = "Two contexts created with the same seed replay the same batch, for every seed.")]
    public void SameSeedContextsReplayTheSameBatch() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => ContextBatch(Any.WithSeed(seed)) == ContextBatch(Any.WithSeed(seed)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A context reports back the seed it was created with, for every seed.")]
    public void ContextReportsItsSeed() {
        // The round-trip is what makes a reported seed replayable at all: a context that silently normalized its
        // seed would hand the reader a number that reproduces nothing.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => Any.WithSeed(seed).Seed == seed)
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A context is isolated: ambient draws interleaved with its own never shift its sequence.")]
    public void ContextIsIsolatedFromAmbientDraws() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        AnyContext quiet       = Any.WithSeed(seed);
                        string     firstQuiet  = ContextBatch(quiet);
                        string     secondQuiet = ContextBatch(quiet);

                        // The same context again, this time with ambient draws before its first batch and between
                        // the two. A context owns its generator, so nothing the static entry points draw may
                        // advance it — not even by one value, which the second batch is there to catch.
                        AnyContext noisy = Any.WithSeed(seed);
                        Any.Guid().Generate();
                        string firstNoisy = ContextBatch(noisy);
                        Any.String().Generate();
                        Any.Int32().Generate();
                        string secondNoisy = ContextBatch(noisy);

                        return firstNoisy == firstQuiet && secondNoisy == secondQuiet;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Reproducibly replays the same batch for the same seed, for every seed.")]
    public void ReproduciblyReplaysTheSameBatch() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        // None of the four Reproducibly overloads returns a value, so the batch leaves the body
                        // through a captured local.
                        string first  = string.Empty;
                        string second = string.Empty;

                        Any.Reproducibly(seed, () => { first = AmbientBatch(); });
                        Any.Reproducibly(seed, () => { second = AmbientBatch(); });

                        return second == first;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UseSeed pins the ambient context, so the same seed replays the same batch.")]
    public void UseSeedPinsTheAmbientContext() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        string first;
                        string second;

                        using (Any.UseSeed(seed)) { first = AmbientBatch(); }
                        using (Any.UseSeed(seed)) { second = AmbientBatch(); }

                        return second == first;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UseSeed and Reproducibly pin the same sequence, for every seed.")]
    public void UseSeedAgreesWithReproducibly() {
        // The scope form exists for a caller that cannot wrap what it pins in a delegate; it must be the same
        // mechanism, not a second one that happens to agree on the seeds an example picked.
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        string fromScope;
                        string fromRunner = string.Empty;

                        using (Any.UseSeed(seed)) { fromScope = AmbientBatch(); }
                        Any.Reproducibly(seed, () => { fromRunner = AmbientBatch(); });

                        return fromScope == fromRunner;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UseSeed nests: an inner scope neither consumes nor resets the outer one, for every seed pair.")]
    public void UseSeedScopesNest() {
        Gen<(int Outer, int Inner)> seedPairs = from outer in Generators.Seed()
                                                from inner in Generators.Seed()
                                                select (outer, inner);

        Prop.ForAll(seedPairs.ToArbitrary(),
                    pair => {
                        string first;
                        string second;
                        using (Any.UseSeed(pair.Outer)) {
                            first  = AmbientBatch();
                            second = AmbientBatch();
                        }

                        // The same outer scope, interrupted by an inner one. The inner scope draws from its own
                        // generator, so the outer sequence must resume exactly where it was interrupted — including
                        // when the two seeds happen to be equal, where the inner scope still installs a generator
                        // of its own rather than sharing the outer one.
                        string restoredFirst;
                        string restoredSecond;
                        using (Any.UseSeed(pair.Outer)) {
                            restoredFirst = AmbientBatch();
                            using (Any.UseSeed(pair.Inner)) { AmbientBatch(); }
                            restoredSecond = AmbientBatch();
                        }

                        return restoredFirst == first && restoredSecond == second;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Disposing a UseSeed scope twice is harmless and cannot unpin a later scope.")]
    public void DisposingAScopeTwiceIsHarmless() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        IDisposable stale = Any.UseSeed(seed);
                        stale.Dispose();

                        string expected;
                        using (Any.UseSeed(seed)) { expected = AmbientBatch(); }

                        string actual;
                        using (Any.UseSeed(seed)) {
                            // The second dispose of an already-closed handle must do nothing at all. Were it to run
                            // its restore again it would reinstate its own predecessor over the scope open right
                            // now, and the batch below would no longer be pinned — which is what makes this a
                            // stronger claim than merely "it does not throw".
                            stale.Dispose();
                            actual = AmbientBatch();
                        }

                        return actual == expected;
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Reproducibly reports the seed and rethrows the original exception instance, for every seed.")]
    public void ReproduciblyReportsTheSeedAndRethrows() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => {
                        // An empty sentinel doubles as the never-reported case: it can contain no seed either way.
                        string                    reported = string.Empty;
                        InvalidOperationException boom     = new("boom");
                        // Explicitly typed: a throw-expression lambda converts to both Action and Func<Task>, so an
                        // inline one would make the overload ambiguous.
                        Action     failing = () => throw boom;
                        Exception? caught  = null;

                        try {
                            Any.Reproducibly(seed, failing, message => reported = message);
                        } catch (Exception exception) {
                            caught = exception;
                        }

                        // The exception must come back as the very instance the body threw — wrapping it would cost
                        // the test its real message — and the seed must be quotable from the report. What sentence
                        // carries it is a diagnostic concern, deliberately not asserted here.
                        return ReferenceEquals(caught, boom) && reported.Contains(seed.ToString());
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UseSeed rejects a null replay snippet, for every seed.")]
    public void UseSeedRejectsANullSnippet() {
        Prop.ForAll(Generators.Seed().ToArbitrary(),
                    seed => Expect.Throws<ArgumentNullException>(() => Any.UseSeed(seed, null!)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "UseSeed rejects a blank replay snippet, whatever the blank spelling.")]
    public void UseSeedRejectsABlankSnippet() {
        Gen<(int Seed, string Snippet)> cases = from seed in Generators.Seed()
                                                from snippet in BlankSnippet()
                                                select (seed, snippet);

        // Exactly ArgumentException, not merely something assignable to it: a blank snippet is not a null one, and
        // the two guards must stay distinguishable.
        Prop.ForAll(cases.ToArbitrary(),
                    testCase => ThrowsExactly<ArgumentException>(() => Any.UseSeed(testCase.Seed, testCase.Snippet)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Different seeds produce different batches — a statistical guard, not a theorem.")]
    public void DifferentSeedsProduceDifferentBatches() {
        // Nothing forbids two different seeds from landing on the same values, so this claim is probabilistic by
        // nature. It is made robust by the batch rather than by weakening the assertion: a Guid, two full-range
        // integers, three strings, a list, a set and a pattern would all have to coincide at once. What the property
        // really watches for is the failure mode that would make them coincide systematically — a seed that ends up
        // ignored, normalized away, or shared between contexts.
        Prop.ForAll(DifferentSeeds().ToArbitrary(),
                    seeds => ContextBatch(Any.WithSeed(seeds.First)) != ContextBatch(Any.WithSeed(seeds.Second)))
            .QuickCheckThrowOnFailure();
    }

}

#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace Dummies.UnitTests;

[TestSubject(typeof(Any))]
public sealed class SeedReproducibilityTests {

    #region Statics members declarations

    private static string Batch() {
        // Explicitly typed locals: string.Join(params object[]) would otherwise box the generators and
        // call their ToString() instead of triggering the implicit conversions.
        int      full     = Any.Int32();
        int      bounded  = Any.Int32().Between(1, 1000);
        string   free     = Any.String();
        string   capped   = Any.String().NonEmpty().WithMaxLength(50);
        string   shaped   = Any.String().StartingWith("ORD-").WithLength(12);
        long     wide     = Any.Int64();
        ulong    unsigned = Any.UInt64();
        double   real     = Any.Double().Between(0d, 1000d);
        decimal  exact    = Any.Decimal().Between(0m, 1000m);
        bool     flag     = Any.Bool();
        Guid     id       = Any.Guid();
        char     letter   = Any.Char();
        TimeSpan span     = Any.TimeSpan();
        DateTime instant  = Any.DateTime();
        Int128   huge     = Any.Int128();
        Half     tiny     = Any.Half();
        List<int>    list = Any.ListOf(Any.Int32().Between(0, 9)).WithCount(4);
        HashSet<int> set  = Any.SetOf(Any.Int32().Between(0, 99)).WithCount(3);

        return string.Join("|", full, bounded, free, capped, shaped,
                           wide, unsigned, real, exact, flag, id, letter,
                           span.Ticks, instant.Ticks, huge, tiny,
                           string.Join("-", list), string.Join("-", set.OrderBy(value => value)));
    }

    #endregion

    [Fact(DisplayName = "Two contexts created with the same seed yield the same values.")]
    public void SameSeedContextsAgree() {
        AnyContext any1 = Any.WithSeed(12345);
        AnyContext any2 = Any.WithSeed(12345);

        string value1 = any1.String().NonEmpty();
        string value2 = any2.String().NonEmpty();

        Check.That(value2).IsEqualTo(value1);
    }

    [Fact(DisplayName = "Two contexts with the same seed agree across a mixed sequence of draws.")]
    public void SameSeedContextsAgreeAcrossASequence() {
        AnyContext any1 = Any.WithSeed(777);
        AnyContext any2 = Any.WithSeed(777);

        string sequence1 = $"{any1.Int32().Positive().Generate()}|{any1.String().WithLength(8).Generate()}|{any1.Int32().Between(0, 9).Generate()}";
        string sequence2 = $"{any2.Int32().Positive().Generate()}|{any2.String().WithLength(8).Generate()}|{any2.Int32().Between(0, 9).Generate()}";

        Check.That(sequence2).IsEqualTo(sequence1);
    }

    [Fact(DisplayName = "Contexts with different seeds diverge.")]
    public void DifferentSeedContextsDiverge() {
        string sequence1 = string.Join("|", Enumerable.Range(0, 8).Select(_ => Any.WithSeed(1).String().WithLength(12).Generate()));
        string sequence2 = string.Join("|", Enumerable.Range(0, 8).Select(_ => Any.WithSeed(2).String().WithLength(12).Generate()));

        Check.That(sequence2).IsNotEqualTo(sequence1);
    }

    [Fact(DisplayName = "A context is isolated from the ambient source: interleaved ambient draws do not shift it.")]
    public void ContextIsIsolatedFromAmbientDraws() {
        AnyContext quiet = Any.WithSeed(31415);
        string undisturbed = quiet.String().WithLength(10);

        AnyContext interleaved = Any.WithSeed(31415);
        Any.String().Generate();
        Any.Int32().Generate();
        string disturbed = interleaved.String().WithLength(10);

        Check.That(disturbed).IsEqualTo(undisturbed);
    }

    [Fact(DisplayName = "Reproducibly with a given seed replays the same sequence of values.")]
    public void ReproduciblyWithASeedIsDeterministic() {
        string first  = string.Empty;
        string second = string.Empty;

        Any.Reproducibly(1234, () => { first = Batch(); });
        Any.Reproducibly(1234, () => { second = Batch(); });

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Reproducibly with different seeds produces different sequences.")]
    public void DifferentSeedsDiffer() {
        string fromOne = string.Empty;
        string fromTwo = string.Empty;

        Any.Reproducibly(1, () => { fromOne = Batch(); });
        Any.Reproducibly(2, () => { fromTwo = Batch(); });

        Check.That(fromTwo).IsNotEqualTo(fromOne);
    }

    [Fact(DisplayName = "Reproducibly reports the seed and rethrows the original exception on failure.")]
    public void ReproduciblyReportsTheSeedAndRethrows() {
        string?                   reported = null;
        InvalidOperationException boom     = new("boom");
        Action                    failing  = () => throw boom;

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => Any.Reproducibly(4242, failing, message => reported = message));

        Check.That(ReferenceEquals(thrown, boom)).IsTrue();
        Check.That(reported).IsNotNull();
        Check.That(reported!).Contains("4242");
        Check.That(reported!).Contains("Any.Reproducibly(");
    }

    [Fact(DisplayName = "Reproducibly does not report when the body succeeds.")]
    public void ReproduciblyIsSilentOnSuccess() {
        bool reported = false;

        Any.Reproducibly(() => { Any.String().NonEmpty().Generate(); }, _ => reported = true);

        Check.That(reported).IsFalse();
    }

    [Fact(DisplayName = "Reproducibly without a seed reports a replayable seed on failure.")]
    public void ReproduciblyWithoutSeedStillReportsAReplayableSeed() {
        string? reported = null;
        Action  failing  = () => throw new InvalidOperationException("x");

        Assert.Throws<InvalidOperationException>(
            () => Any.Reproducibly(failing, message => reported = message));

        Check.That(reported).IsNotNull();
        Check.That(reported!).Contains("Any.Reproducibly(");
    }

    [Fact(DisplayName = "The async Reproducibly reports the seed and rethrows on failure.")]
    public async Task AsyncReproduciblyReportsTheSeedAndRethrows() {
        string? reported = null;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Any.Reproducibly(7, async () => {
                await Task.Yield();

                throw new InvalidOperationException("boom");
            }, message => reported = message));

        Check.That(reported).IsNotNull();
        Check.That(reported!).Contains("7");
    }

    [Fact(DisplayName = "The async Reproducibly with a given seed replays the same sequence of values.")]
    public async Task AsyncReproduciblyWithASeedIsDeterministic() {
        string first  = string.Empty;
        string second = string.Empty;

        await Any.Reproducibly(4321, async () => {
            await Task.Yield();
            first = Batch();
        });
        await Any.Reproducibly(4321, async () => {
            await Task.Yield();
            second = Batch();
        });

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Reproducibly requires a body.")]
    public void ReproduciblyRequiresABody() {
        Check.ThatCode(() => Any.Reproducibly((Action)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Reproducibly((Func<Task>)null!)).Throws<ArgumentNullException>();
    }

}

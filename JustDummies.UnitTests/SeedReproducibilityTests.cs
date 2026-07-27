#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The example-based half of the reproducibility contract: what the failure report must say, that a
///     successful run stays silent, the asynchronous overloads, and the null-argument guards. That two runs
///     under the same seed agree — and that different seeds diverge — holds for <i>every</i> seed and is
///     quantified in <c>JustDummies.PropertyTests</c> instead of pinned to 12345, 777 and 31415 (ADR-0040).
/// </summary>
[TestSubject(typeof(Any))]
public sealed class SeedReproducibilityTests {

    #region Statics members declarations

    private static string Batch() {
        // Explicitly typed locals: string.Join(params object[]) would otherwise box the generators and
        // call their ToString() instead of triggering the implicit conversions.
        int      full     = Any.Int32().Generate();
        int      bounded  = Any.Int32().Between(1, 1000).Generate();
        string   free     = Any.String().Generate();
        string   capped   = Any.String().NonEmpty().WithMaxLength(50).Generate();
        string   shaped   = Any.String().StartingWith("ORD-").WithLength(12).Generate();
        long     wide     = Any.Int64().Generate();
        ulong    unsigned = Any.UInt64().Generate();
        double   real     = Any.Double().Between(0d, 1000d).Generate();
        decimal  exact    = Any.Decimal().Between(0m, 1000m).Generate();
        bool     flag     = Any.Boolean().Generate();
        Guid     id       = Any.Guid().Generate();
        char     letter   = Any.Char().Generate();
        TimeSpan span     = Any.TimeSpan().Generate();
        DateTime instant  = Any.DateTime().Generate();
#if NET8_0_OR_GREATER
        Int128   huge     = Any.Int128().Generate();
        Half     tiny     = Any.Half().Generate();
#endif
        List<int>    list = Any.ListOf(Any.Int32().Between(0, 9)).WithCount(4).Generate();
        HashSet<int> set  = Any.SetOf(Any.Int32().Between(0, 99)).WithCount(3).Generate();
        int?         maybe = Any.Int32().Between(0, 9).OrNull().Generate();
        string       coded = Any.StringMatching(@"[A-Z]{3}-\d{4}").Generate();

        return string.Join("|", full, bounded, free, capped, shaped,
                           wide, unsigned, real, exact, flag, id, letter,
                           span.Ticks, instant.Ticks,
#if NET8_0_OR_GREATER
                           huge, tiny,
#endif
                           string.Join("-", list), string.Join("-", set.OrderBy(value => value)),
                           maybe?.ToString() ?? "null", coded);
    }

    #endregion

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

    [Fact(DisplayName = "The async ReproduciblyAsync reports the seed and rethrows on failure.")]
    public async Task AsyncReproduciblyReportsTheSeedAndRethrows() {
        string? reported = null;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Any.ReproduciblyAsync(7, async () => {
                await Task.Yield();

                throw new InvalidOperationException("boom");
            }, message => reported = message));

        Check.That(reported).IsNotNull();
        Check.That(reported!).Contains("7");
    }

    [Fact(DisplayName = "The async ReproduciblyAsync with a given seed replays the same sequence of values.")]
    public async Task AsyncReproduciblyWithASeedIsDeterministic() {
        string first  = string.Empty;
        string second = string.Empty;

        await Any.ReproduciblyAsync(4321, async () => {
            await Task.Yield();
            first = Batch();
        });
        await Any.ReproduciblyAsync(4321, async () => {
            await Task.Yield();
            second = Batch();
        });

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Reproducibly and ReproduciblyAsync require a body.")]
    public void ReproduciblyRequiresABody() {
        Check.ThatCode(() => Any.Reproducibly((Action)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.ReproduciblyAsync((Func<Task>)null!)).Throws<ArgumentNullException>();
    }

}

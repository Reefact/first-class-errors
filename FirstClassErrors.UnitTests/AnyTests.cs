#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Any))]
public sealed class AnyTests {

    #region Statics members declarations

    private static string Batch() {
        return string.Join("|",
                           Any.Int(),
                           Any.Bool(),
                           Any.Guid(),
                           Any.Instant().UtcTicks,
                           Any.String(),
                           Any.ErrorCode(),
                           Any.DiagnosticMessage(),
                           Any.ShortMessage(),
                           Any.DetailedMessage(),
                           Any.Transience(),
                           Any.ErrorOrigin(),
                           Any.InteractionDirection());
    }

    #endregion

    [Fact(DisplayName = "Reproducibly with a given seed replays the same sequence of values.")]
    public void ReproduciblyWithASeedIsDeterministic() {
        string first  = string.Empty;
        string second = string.Empty;

        Any.Reproducibly(1234, () => { first = Batch(); });
        Any.Reproducibly(1234, () => { second = Batch(); });

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Different seeds produce different sequences.")]
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
    }

    [Fact(DisplayName = "Reproducibly does not report when the body succeeds.")]
    public void ReproduciblyIsSilentOnSuccess() {
        bool reported = false;

        Any.Reproducibly(() => { Any.ErrorCode(); }, _ => reported = true);

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

    [Fact(DisplayName = "ErrorCode returns a non-blank code shaped like ANY_CODE_*.")]
    public void ErrorCodeIsNonBlankAndRecognizable() {
        string code = Any.ErrorCode().ToString();

        Check.That(string.IsNullOrWhiteSpace(code)).IsFalse();
        Check.That(code).StartsWith("ANY_CODE_");
    }

    [Fact(DisplayName = "Instant is a UTC instant (zero offset).")]
    public void InstantIsUtc() {
        Check.That(Any.Instant().Offset).IsEqualTo(TimeSpan.Zero);
    }

    [Fact(DisplayName = "Transience never returns the Unknown sentinel.")]
    public void TransienceExcludesUnknown() {
        for (int i = 0; i < 200; i++) {
            Check.That(Any.Transience()).IsNotEqualTo(Transience.Unknown);
        }
    }

    [Fact(DisplayName = "InteractionDirection never returns the Unknown sentinel.")]
    public void InteractionDirectionExcludesUnknown() {
        for (int i = 0; i < 200; i++) {
            Check.That(Any.InteractionDirection()).IsNotEqualTo(InteractionDirection.Unknown);
        }
    }

    [Fact(DisplayName = "Enum returns a defined member of the requested enum.")]
    public void EnumReturnsADefinedMember() {
        Check.That(System.Enum.IsDefined(typeof(Transience), Any.Enum<Transience>())).IsTrue();
    }

    [Fact(DisplayName = "The string and message helpers are never blank.")]
    public void TextValuesAreNeverBlank() {
        Check.That(string.IsNullOrWhiteSpace(Any.String())).IsFalse();
        Check.That(string.IsNullOrWhiteSpace(Any.DiagnosticMessage())).IsFalse();
        Check.That(string.IsNullOrWhiteSpace(Any.ShortMessage())).IsFalse();
        Check.That(string.IsNullOrWhiteSpace(Any.DetailedMessage())).IsFalse();
    }

}

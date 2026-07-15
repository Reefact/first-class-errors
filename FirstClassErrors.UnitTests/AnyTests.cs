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

    [Fact(DisplayName = "UseSeed makes the whole sequence of Any values reproducible.")]
    public void UseSeedIsReproducible() {
        string first;
        string second;

        using (Any.UseSeed(1234)) { first = Batch(); }
        using (Any.UseSeed(1234)) { second = Batch(); }

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "Different seeds produce different sequences.")]
    public void DifferentSeedsDiffer() {
        string fromOne;
        string fromTwo;

        using (Any.UseSeed(1)) { fromOne = Batch(); }
        using (Any.UseSeed(2)) { fromTwo = Batch(); }

        Check.That(fromTwo).IsNotEqualTo(fromOne);
    }

    [Fact(DisplayName = "A nested UseSeed scope leaves the outer sequence undisturbed.")]
    public void NestedScopeDoesNotDisturbTheOuterSequence() {
        int outerFirst;
        int outerSecond;
        using (Any.UseSeed(7)) {
            outerFirst = Any.Int();
            using (Any.UseSeed(99)) {
                Any.Int();
                Any.Int();
            }

            outerSecond = Any.Int();
        }

        int expectedFirst;
        int expectedSecond;
        using (Any.UseSeed(7)) {
            expectedFirst  = Any.Int();
            expectedSecond = Any.Int();
        }

        Check.That(outerFirst).IsEqualTo(expectedFirst);
        Check.That(outerSecond).IsEqualTo(expectedSecond);
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
        using (Any.UseSeed(0)) {
            for (int i = 0; i < 200; i++) {
                Check.That(Any.Transience()).IsNotEqualTo(Transience.Unknown);
            }
        }
    }

    [Fact(DisplayName = "InteractionDirection never returns the Unknown sentinel.")]
    public void InteractionDirectionExcludesUnknown() {
        using (Any.UseSeed(0)) {
            for (int i = 0; i < 200; i++) {
                Check.That(Any.InteractionDirection()).IsNotEqualTo(InteractionDirection.Unknown);
            }
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

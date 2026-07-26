#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The scope form of reproducibility: a handle a caller opens and disposes itself, for a test-framework adapter
///     that observes a test through before/after hooks and therefore has no delegate to wrap (ADR-0035). The seed
///     behaviour must match <c>Any.Reproducibly</c>; what the scope adds is the replay snippet a
///     generation-failure diagnostic names, so a run pinned from outside the test body never advertises a call the
///     test does not contain.
/// </summary>
[TestSubject(typeof(Any))]
public sealed class AmbientSeedScopeTests {

    #region Statics members declarations

    private static (int, string) Batch() {
        return (Any.Int32().Generate(), Any.String().NonEmpty().Generate());
    }

    #endregion

    [Fact(DisplayName = "UseSeed pins the ambient context, so the same seed yields the same values.")]
    public void UseSeedPinsTheAmbientContext() {
        (int, string) first;
        (int, string) second;

        using (Any.UseSeed(1234)) { first = Batch(); }
        using (Any.UseSeed(1234)) { second = Batch(); }

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "UseSeed with different seeds produces different sequences.")]
    public void DifferentSeedsDiffer() {
        (int, string) fromOne;
        (int, string) fromTwo;

        using (Any.UseSeed(1)) { fromOne = Batch(); }
        using (Any.UseSeed(2)) { fromTwo = Batch(); }

        Check.That(fromTwo).IsNotEqualTo(fromOne);
    }

    [Fact(DisplayName = "UseSeed pins the same sequence as Reproducibly for the same seed.")]
    public void UseSeedMatchesReproducibly() {
        (int, string) fromScope;
        (int, string) fromRunner = default;

        using (Any.UseSeed(4242)) { fromScope = Batch(); }
        Any.Reproducibly(4242, () => { fromRunner = Batch(); });

        Check.That(fromScope).IsEqualTo(fromRunner);
    }

    [Fact(DisplayName = "Disposing the scope restores the previous context with its draw sequence intact.")]
    public void DisposingRestoresThePreviousContext() {
        (int, string) first;
        (int, string) second;
        (int, string) restoredFirst;
        (int, string) restoredSecond;

        using (Any.UseSeed(7)) {
            first  = Batch();
            second = Batch();
        }

        using (Any.UseSeed(7)) {
            restoredFirst = Batch();
            // A nested scope draws from its own generator; the outer one must neither be consumed nor reset by
            // it, so the outer sequence resumes exactly where it was interrupted.
            using (Any.UseSeed(99)) { Batch(); }
            restoredSecond = Batch();
        }

        Check.That(restoredFirst).IsEqualTo(first);
        Check.That(restoredSecond).IsEqualTo(second);
    }

    [Fact(DisplayName = "Nested scopes pin the inner seed while they are open.")]
    public void NestedScopesPinTheInnerSeed() {
        (int, string) standalone;
        (int, string) nested;

        using (Any.UseSeed(555)) { standalone = Batch(); }

        using (Any.UseSeed(111)) {
            using (Any.UseSeed(555)) { nested = Batch(); }
        }

        Check.That(nested).IsEqualTo(standalone);
    }

    [Fact(DisplayName = "Disposing the scope twice is harmless.")]
    public void DisposingTwiceIsHarmless() {
        IDisposable scope = Any.UseSeed(31);

        scope.Dispose();

        Check.ThatCode(() => scope.Dispose()).DoesNotThrow();
    }

    [Fact(DisplayName = "The scope does not leak across parallel execution contexts.")]
    public async Task TheScopeDoesNotLeakAcrossExecutionContexts() {
        (int, string) inside;
        (int, string) outside = default;

        using (Any.UseSeed(2026)) {
            inside = Batch();

            // A task started inside the scope inherits it, so run the probe on a context that never saw it.
            await Task.Run(() => { outside = Batch(); }, TestContext.Current.CancellationToken);
        }

        // The probe drew from a context whose scope was never entered, so it cannot have replayed the pinned
        // sequence. (An unseeded draw could coincide, but not across both components of the batch.)
        Check.That(outside).IsNotEqualTo(inside);
    }

    [Fact(DisplayName = "Without a replay snippet, a generation failure names Any.Reproducibly.")]
    public void WithoutAnInstructionTheFailureNamesTheDelegateRunner() {
        AnyGenerationException caught;

        using (Any.UseSeed(1234)) {
            caught = Assert.Throws<AnyGenerationException>(
                () => Any.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Seed).IsEqualTo(1234);
        Check.That(caught.Message).Contains("The arbitrary values were seeded with 1234");
        Check.That(caught.Message).Contains("Any.Reproducibly(1234, ...)");
    }

    [Fact(DisplayName = "With a replay snippet, a generation failure names it instead of Any.Reproducibly.")]
    public void WithAnInstructionTheFailureNamesIt() {
        AnyGenerationException caught;

        using (Any.UseSeed(1234, "[Reproducible(Seed = 1234)]")) {
            caught = Assert.Throws<AnyGenerationException>(
                () => Any.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("The arbitrary values were seeded with 1234");
        Check.That(caught.Message).Contains("[Reproducible(Seed = 1234)]");
        // The whole point: the reader is never pointed at a call their test does not contain.
        Check.That(caught.Message).Not.Contains("Any.Reproducibly");
    }

    [Fact(DisplayName = "The replay snippet also reaches the partial-replay guidance.")]
    public void TheInstructionReachesThePartialReplayGuidance() {
        AnyGenerationException caught;

        using (Any.UseSeed(777, "[Reproducible(Seed = 777)]")) {
            IAny<int> foreign = new ForeignAny();
            caught = Assert.Throws<AnyGenerationException>(
                () => Any.Combine<int, int, int>(Any.Int32(), foreign, (_, _) => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("not reproducible from this seed alone");
        Check.That(caught.Message).Contains("[Reproducible(Seed = 777)]");
        Check.That(caught.Message).Not.Contains("Any.Reproducibly");
    }

    [Fact(DisplayName = "The replay snippet is scoped: it does not outlive the scope that supplied it.")]
    public void TheInstructionDoesNotOutliveItsScope() {
        using (Any.UseSeed(1, "[Reproducible(Seed = 1)]")) { }

        AnyGenerationException caught;
        using (Any.UseSeed(2)) {
            caught = Assert.Throws<AnyGenerationException>(
                () => Any.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("Any.Reproducibly(2, ...)");
        Check.That(caught.Message).Not.Contains("[Reproducible");
    }

    [Fact(DisplayName = "UseSeed rejects a null replay snippet.")]
    public void UseSeedRejectsANullInstruction() {
        Check.ThatCode(() => Any.UseSeed(1, null!)).Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "UseSeed rejects a blank replay snippet.")]
    [InlineData("")]
    [InlineData("   ")]
    public void UseSeedRejectsABlankInstruction(string instruction) {
        Check.ThatCode(() => Any.UseSeed(1, instruction)).Throws<ArgumentException>();
    }

    #region Nested types

    /// <summary>A generator carrying no random source, so a derivation over it cannot promise a full replay.</summary>
    private sealed class ForeignAny : IAny<int> {

        public int Generate() {
            return 42;
        }

    }

    #endregion

}

#region Usings declarations

using System.Collections.Concurrent;
using System.Reflection;

using JetBrains.Annotations;

using NFluent;

using Xunit.v3;

#endregion

namespace Dummies.Xunit.UnitTests;

/// <summary>
///     The adapter is exercised through the real xUnit pipeline wherever a behaviour is observable from inside a
///     test: pinning, per-case seeds and scope closing all show up in the values a decorated test draws. Two things
///     cannot be observed that way — the outcome-dependent report, which needs a test that has already finished, and
///     the framework contract the adapter reads — so each gets its own guard: the reporting rule is decided by a seam
///     proved directly, and the contract is asserted from an after-hook, where a violation fails the test carrying it.
/// </summary>
[TestSubject(typeof(ReproducibleAttribute))]
public sealed class ReproducibleAttributeTests {

    #region Statics members declarations

    private static readonly ConcurrentDictionary<int, (int, string)> DrawnByCase = new();

    internal static (int, string) Batch() {
        return (Any.Int32().Generate(), Any.String().NonEmpty().Generate());
    }

    #endregion

    [Fact(DisplayName = "A pinned seed yields the values that seed produces.")]
    [Reproducible(Seed = 1234)]
    public void APinnedSeedYieldsThatSeedsValues() {
        (int, string) drawn = Batch();

        (int, string) expected;
        // The attribute pinned 1234 for this test; an explicit scope over the same seed must agree, which is
        // only true if the attribute really pinned the ambient source the static Any entry points draw from.
        using (Any.UseSeed(1234)) { expected = Batch(); }

        Check.That(drawn).IsEqualTo(expected);
    }

    [Fact(DisplayName = "A pinned seed of zero is honoured, not treated as unset.")]
    [Reproducible(Seed = 0)]
    public void APinnedSeedOfZeroIsHonoured() {
        (int, string) drawn = Batch();

        (int, string) expected;
        using (Any.UseSeed(0)) { expected = Batch(); }

        Check.That(drawn).IsEqualTo(expected);
    }

    [Theory(DisplayName = "Each theory case draws its own seed, not one shared with its siblings.")]
    [Reproducible]
    [InlineData(1)]
    [InlineData(2)]
    public void EachTheoryCaseDrawsItsOwnSeed(int which) {
        DrawnByCase[which] = Batch();

        // Both cases run the same code under the same attribute instance. Sharing one seed would make their
        // values identical; a seed drawn per case makes them differ. The check runs on whichever case lands
        // second, so it does not depend on the order the two are executed in.
        if (DrawnByCase.Count == 2) {
            Check.That(DrawnByCase[1]).IsNotEqualTo(DrawnByCase[2]);
        }
    }

    [Fact(DisplayName = "The scope stays open for the whole test and restores after a nested one.")]
    [Reproducible(Seed = 99)]
    public void TheAttributeSeedSurvivesANestedScope() {
        (int, string) first = Batch();

        using (Any.UseSeed(11)) { Batch(); }

        (int, string) afterNesting = Batch();

        (int, string) expectedFirst;
        (int, string) expectedSecond;
        using (Any.UseSeed(99)) {
            expectedFirst  = Batch();
            expectedSecond = Batch();
        }

        Check.That(first).IsEqualTo(expectedFirst);
        Check.That(afterNesting).IsEqualTo(expectedSecond);
    }

    [Fact(DisplayName = "A generation failure names the attribute, not the delegate runner.")]
    [Reproducible(Seed = 2026)]
    public void AGenerationFailureNamesTheAttribute() {
        AnyGenerationException caught = Assert.Throws<AnyGenerationException>(
            () => Any.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());

        Check.That(caught.Seed).IsEqualTo(2026);
        Check.That(caught.Message).Contains("[Reproducible(Seed = 2026)]");
        // The whole point of the replay instruction: this test contains no Any.Reproducibly call, so naming
        // one would send the reader to a call that is not there.
        Check.That(caught.Message).Not.Contains("Any.Reproducibly");
    }

    [Fact(DisplayName = "A failing test is told its seed and how to replay it.")]
    public void AFailingTestIsToldItsSeed() {
        string? report = ReproducibleAttribute.ReportFor(failed: true, seed: 1234);

        Check.That(report).IsNotNull();
        Check.That(report).Contains("seeded with 1234");
        Check.That(report).Contains("[Reproducible(Seed = 1234)]");
        Check.That(report).Not.Contains("Any.Reproducibly");
    }

    [Fact(DisplayName = "A passing test is told nothing.")]
    public void APassingTestIsToldNothing() {
        Check.That(ReproducibleAttribute.ReportFor(failed: false, seed: 1234)).IsNull();
    }

    [Fact(DisplayName = "xUnit still exposes a finished test's outcome to an after-hook.")]
    [OutcomeContract]
    public void TheFrameworkStillExposesTheOutcome() {
        // The assertion lives in OutcomeContractAttribute.After: by the time it runs, this test has finished,
        // which is the only moment the outcome exists. If a future xUnit stops populating it -- the contract
        // the whole "report only on failure" rule rests on -- this test fails.
        Check.That(true).IsTrue();
    }

    #region Nested types

    /// <summary>
    ///     Asserts, from the one place where a finished test's outcome exists, that the framework still reports it.
    ///     Throwing here fails the test that carries the attribute, which is exactly the signal wanted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class OutcomeContractAttribute : BeforeAfterTestAttribute {

        public override void After(MethodInfo methodUnderTest, IXunitTest test) {
            TestResultState? state = TestContext.Current.TestState;

            Check.WithCustomMessage("xUnit no longer exposes a finished test's state to an after-hook; the Reproducible attribute cannot decide whether to report the seed.")
                 .That(state).IsNotNull();
            Check.WithCustomMessage("xUnit no longer reports a passing test as Passed; the failure-only rule cannot be trusted.")
                 .That(state!.Result).IsEqualTo(TestResult.Passed);
        }

    }

    #endregion

}

/// <summary>
///     A class-level application: every test the class declares is reproducible without repeating the attribute, and
///     a method-level declaration overrides it for the test that carries one.
/// </summary>
[Reproducible(Seed = 7)]
public sealed class ClassLevelReproducibleTests {

    [Fact(DisplayName = "A class-level attribute pins every test the class declares.")]
    public void AClassLevelAttributePinsEveryTest() {
        (int, string) drawn = ReproducibleAttributeTests.Batch();

        (int, string) expected;
        using (Any.UseSeed(7)) { expected = ReproducibleAttributeTests.Batch(); }

        Check.That(drawn).IsEqualTo(expected);
    }

    [Fact(DisplayName = "A method-level attribute wins over the class-level one.")]
    [Reproducible(Seed = 4242)]
    public void AMethodLevelAttributeWins() {
        (int, string) drawn = ReproducibleAttributeTests.Batch();

        (int, string) expected;
        using (Any.UseSeed(4242)) { expected = ReproducibleAttributeTests.Batch(); }

        Check.That(drawn).IsEqualTo(expected);
    }

}

/// <summary>
///     Without the attribute, nothing is pinned: two runs of the same draw differ. This is the arbitrary-by-default
///     behaviour the attribute is opt-in over, and the guard that a scope opened for another test never leaks here.
/// </summary>
public sealed class UndecoratedTests {

    [Fact(DisplayName = "Without the attribute the ambient source stays unpinned.")]
    public void WithoutTheAttributeNothingIsPinned() {
        (int, string) first  = ReproducibleAttributeTests.Batch();
        (int, string) second = ReproducibleAttributeTests.Batch();

        Check.That(second).IsNotEqualTo(first);
    }

}

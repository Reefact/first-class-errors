#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.Xunit.UnitTests;

/// <summary>
///     The adapter's hooks, driven directly rather than through the xUnit pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Everything the surrounding suite asserts is observed from <i>inside</i> a decorated test, which is the right
///         way to prove that pinning works — but it leaves the hooks' own defensive behaviour unobserved: the test never
///         sees <c>After</c> run, so it cannot tell whether the scope was closed or merely abandoned, nor what happens
///         when <c>After</c> runs without a matching <c>Before</c>.
///     </para>
///     <para>
///         Both hooks ignore their two parameters entirely, so they are passed as <c>null</c> here: naming a real
///         <c>MethodInfo</c> and a fake <c>IXunitTest</c> would suggest they matter.
///     </para>
/// </remarks>
[TestSubject(typeof(ReproducibleAttribute))]
public sealed class ReproducibleAttributeHookTests {

    #region Statics members declarations

    private static (int, string) Batch() {
        return (Any.Int32().Generate(), Any.String().NonEmpty().Generate());
    }

    #endregion

    [Fact(DisplayName = "A declared seed is the seed the attribute reports.")]
    public void ADeclaredSeedIsReadBack() {
        // The property is what a reader sets and what the replay snippet echoes; nothing else asserted that setting
        // it had any effect at all.
        ReproducibleAttribute attribute = new() { Seed = 1234 };

        Check.That(attribute.Seed).IsEqualTo(1234);
    }

    [Fact(DisplayName = "An undeclared seed reads as zero.")]
    public void AnUndeclaredSeedReadsAsZero() {
        Check.That(new ReproducibleAttribute().Seed).IsEqualTo(0);
    }

    [Fact(DisplayName = "The after-hook is a no-op when no scope was opened for it.")]
    public void TheAfterHookToleratesAMissingScope() {
        // xUnit is not obliged to have run Before — a failure in another hook can skip it — and the after-hook must
        // survive that rather than take the whole test run down with a NullReferenceException.
        ReproducibleAttribute attribute = new();

        Check.ThatCode(() => attribute.After(null!, null!)).DoesNotThrow();
    }

    [Fact(DisplayName = "The after-hook closes the scope it opened, releasing the ambient source.")]
    public void TheAfterHookClosesTheScope() {
        // What seed 555 produces on its second draw. If the scope were left open, the draw after the hook would
        // continue that sequence and match; a closed scope hands the ambient source back and it will not.
        (int, string) secondOfTheSeededRun;
        using (Any.UseSeed(555)) {
            Batch();
            secondOfTheSeededRun = Batch();
        }

        ReproducibleAttribute attribute = new() { Seed = 555 };
        attribute.Before(null!, null!);
        Batch();
        attribute.After(null!, null!);

        Check.That(Batch()).IsNotEqualTo(secondOfTheSeededRun);
    }

    [Fact(DisplayName = "Nested scopes unwind in order, restoring the outer seed.")]
    public void NestedScopesUnwindInOrder() {
        // The method, class and assembly levels nest, and xUnit closes them in reverse. Closing the inner one must
        // restore the outer seed rather than the unpinned source.
        ReproducibleAttribute outer = new() { Seed = 111 };
        ReproducibleAttribute inner = new() { Seed = 222 };

        (int, string) expectedSecondOfOuter;
        using (Any.UseSeed(111)) {
            Batch();
            expectedSecondOfOuter = Batch();
        }

        outer.Before(null!, null!);
        try {
            Batch();

            inner.Before(null!, null!);
            Batch();
            inner.After(null!, null!);

            Check.That(Batch()).IsEqualTo(expectedSecondOfOuter);
        } finally {
            outer.After(null!, null!);
        }
    }

}

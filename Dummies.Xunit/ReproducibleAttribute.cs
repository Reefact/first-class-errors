#region Usings declarations

using System.Reflection;

using Xunit;
using Xunit.v3;

#endregion

namespace Dummies.Xunit;

/// <summary>
///     Makes a test's arbitrary values reproducible: the ambient <see cref="Any" /> context is pinned to a seed for
///     the duration of the test, and that seed is reported <b>only when the test fails</b> — so a red test names the
///     exact seed to replay while a green one stays silent. This is the declarative form of
///     <c>Any.Reproducibly(() =&gt; { ... })</c>: the values still vary between runs, which is what surfaces a test
///     secretly depending on one, but a failure is recoverable without the body having been wrapped in advance.
/// </summary>
/// <remarks>
///     <para>
///         Apply it next to <c>[Fact]</c> or <c>[Theory]</c>, on a class to cover every test it declares, or on the
///         assembly to cover a whole suite. The hooks run once per test <i>case</i>, so each case of a theory gets its
///         own seed rather than sharing one with its siblings. When several levels apply, the most specific one wins
///         for the duration of the test and the outer ones are restored after it — so an assembly-wide
///         <c>[Reproducible]</c> can pin the suite while one test replays a particular seed.
///     </para>
///     <para>
///         Pin <see cref="Seed" /> to replay a reported run. Left unset, a fresh seed is drawn for every test case.
///     </para>
///     <example>
///         <code>
///         [Fact, Reproducible]
///         public void Order_reference_is_accepted() {
///             string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
///             // ... act, assert ...
///         }
///
///         // Replay the seed a failing run reported:
///         [Fact, Reproducible(Seed = 1234)]
///         public void Order_reference_is_accepted() { /* ... */ }
///         </code>
///     </example>
///     <para>
///         The seed reaches the test's output, which xUnit attaches to the failing test's result. Values drawn from an
///         explicit <c>Any.WithSeed(...)</c> context are unaffected: that context is isolated by design and does not
///         draw from the ambient source this attribute pins.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ReproducibleAttribute : BeforeAfterTestAttribute {

    #region Statics members declarations

    // The scopes opened for the current test, innermost first. An AsyncLocal rather than an instance field
    // because one attribute instance serves every test it is applied to, including tests running in parallel;
    // the ambient context this pins is itself AsyncLocal-backed, so the two flow together. A stack rather than
    // a single slot because the method, class and assembly levels nest, and xUnit closes them in reverse.
    private static readonly AsyncLocal<Scope?> Open = new();

    #endregion

    #region Fields declarations

    // Nullable behind a non-nullable property: an attribute argument cannot be an int?, but the setter running
    // at all is what distinguishes "Seed = 0" (a legitimate seed) from a seed that was never declared.
    private int? _seed;

    #endregion

    /// <summary>
    ///     The seed to pin, to replay a run a previous failure reported. Left unset, every test case draws a fresh
    ///     seed — the arbitrary-by-default behaviour that surfaces a test depending on one particular value.
    /// </summary>
    public int Seed {
        get => _seed ?? 0;
        set => _seed = value;
    }

    /// <inheritdoc />
    public override void Before(MethodInfo methodUnderTest, IXunitTest test) {
        int seed = _seed ?? NewSeed();

        Open.Value = new Scope(Any.UseSeed(seed, ReplayInstruction(seed)), seed, Open.Value);
    }

    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest, IXunitTest test) {
        Scope? scope = Open.Value;
        if (scope is null) { return; }

        Open.Value = scope.Outer;

        try {
            string? report = ReportFor(HasFailed(), scope.Seed);
            if (report is not null) { TestContext.Current.TestOutputHelper?.WriteLine(report); }
        } finally {
            // Restoring the ambient context must happen even if reporting throws: a scope left open would pin
            // the seed for whatever runs next in this execution context.
            scope.Handle.Dispose();
        }
    }

    /// <summary>
    ///     What the reader must write to replay the run — the attribute with its seed, not the delegate runner the
    ///     ambient source names by default, because a test carrying this attribute contains no such call.
    /// </summary>
    private static string ReplayInstruction(int seed) {
        return $"[Reproducible(Seed = {seed})]";
    }

    /// <summary>
    ///     Whether the test that just ran failed. Read from the ambient test context, which carries the finished
    ///     test's outcome by the time the after-hook runs. A context that cannot be read is treated as a pass: a
    ///     spurious seed on a green test is noise, and silence is the safer default for a diagnostic aid.
    /// </summary>
    private static bool HasFailed() {
        return TestContext.Current.TestState?.Result == TestResult.Failed;
    }

    /// <summary>
    ///     What to tell the reader once the outcome is known: the seed and how to replay it when the test failed,
    ///     nothing at all when it passed. Kept apart from reading the outcome so the rule — report only on failure,
    ///     and name the attribute rather than the delegate runner — is verifiable without a failing test.
    /// </summary>
    internal static string? ReportFor(bool failed, int seed) {
        return failed
            ? $"[Dummies] These arbitrary values were seeded with {seed}. Reproduce this run with {ReplayInstruction(seed)}."
            : null;
    }

    /// <summary>
    ///     A fresh seed per test case. Collision-tolerant by construction: the seed identifies a run to replay, it is
    ///     never asserted on, so two runs coinciding is harmless.
    /// </summary>
    private static int NewSeed() {
        return Guid.NewGuid().GetHashCode();
    }

    #region Nested types

    /// <summary>One pinned ambient context and the one it displaced, so nested levels unwind in order.</summary>
    private sealed class Scope {

        internal Scope(IDisposable handle, int seed, Scope? outer) {
            Handle = handle;
            Seed   = seed;
            Outer  = outer;
        }

        internal IDisposable Handle { get; }
        internal int         Seed   { get; }
        internal Scope?      Outer  { get; }

    }

    #endregion

}

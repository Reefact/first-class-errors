namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Creates an isolated, deterministic generation context: every generator created from it draws from a
    ///     dedicated source seeded with <paramref name="seed" />, independent of the ambient context. Two contexts
    ///     created with the same seed yield the same sequence of values. Prefer
    ///     <see cref="Reproducibly(Action, Action{String})" /> inside tests — it keeps the arbitrary-by-default
    ///     behavior and reports the seed only when the test fails; reach for <see cref="WithSeed" /> when you need an
    ///     explicit generator object, for example outside a test body.
    /// </summary>
    /// <remarks>
    ///     A context is safe to draw from concurrently, but sharing one across threads costs the replay rather than
    ///     the values: interleaved draws make neither the sequence nor the multiset stable across runs. Keep a
    ///     context to one thread at a time, or give each unit of work its own <see cref="UseSeed(int)" /> scope.
    /// </remarks>
    /// <param name="seed">The seed pinning the context's value sequence.</param>
    /// <returns>A deterministic generation context.</returns>
    public static AnyContext WithSeed(int seed) {
        return new AnyContext(seed);
    }

    /// <summary>
    ///     Pins the ambient random context to <paramref name="seed" /> until the returned handle is disposed — the
    ///     scope form of <see cref="Reproducibly(int, Action, Action{String})" />, for a caller that cannot wrap the
    ///     code it pins in a delegate. A test-framework adapter is the case this exists for: it observes a test through
    ///     hooks that run before and after it, so it opens the scope in one and disposes it in the other.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Inside a test body, prefer <see cref="Reproducibly(Action, Action{String})" />: it also reports the seed
    ///         when the body fails, which this handle does not — whoever opens the scope owns telling the reader which
    ///         seed to replay. Prefer <see cref="WithSeed" /> when an explicit generator object fits better than an
    ///         ambient scope.
    ///     </para>
    ///     <para>
    ///         Like the ambient context itself, the scope flows with the current execution context, so it never leaks
    ///         across tests running in parallel. Scopes nest: disposing restores whatever was pinned before, and
    ///         disposing twice is harmless. Failing to dispose leaves the seed pinned for whatever runs next in the
    ///         same execution context.
    ///     </para>
    ///     <para>
    ///         Flowing with the execution context also means a scope opened around a parallel loop reaches every
    ///         worker, which is what makes this the seam for a <b>reproducible parallel</b> run: draws are safe under
    ///         concurrency but interleave, so one shared scope replays nothing, whereas a scope opened <i>inside</i>
    ///         the loop body gives each unit of work its own sequence and the whole run replays.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         const int runSeed = 20240501; // recorded by hand: keep it to replay, change it to explore
    ///         Parallel.For(0, 64, index =&gt; {
    ///             // a distinct, deterministic sub-seed per work item, floor-safe on netstandard2.0 (no System.HashCode)
    ///             using (Any.UseSeed(unchecked(runSeed * 397 ^ index))) {
    ///                 sut.Handle(Any.String().NonEmpty().Generate());
    ///             }
    ///         });
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="seed">The seed pinning the ambient context's value sequence.</param>
    /// <returns>A handle that restores the previous ambient context when disposed.</returns>
    public static IDisposable UseSeed(int seed) {
        return AmbientRandomSource.UseSeed(seed);
    }

    /// <summary>
    ///     Pins the ambient random context to <paramref name="seed" /> and supplies the <b>replay snippet</b> — the
    ///     code a reader copies to replay this run — that generation-failure guidance will embed. This is the form a
    ///     test-framework adapter uses: the default snippet is <c>Any.Reproducibly(seed, ...)</c>, which points at a
    ///     call a test pinned from outside its own body does not contain, where replaying means changing what the
    ///     adapter reads instead.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A failure's guidance is one sentence embedding this snippet, so pass the code itself — an attribute with
    ///         its seed argument, a runner setting — not a sentence about it. It is quoted verbatim and validated only
    ///         for being non-blank: a badly phrased snippet degrades the very diagnostic it is meant to improve.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         using (Any.UseSeed(1234, "[Reproducible(Seed = 1234)]")) { /* ... */ }
    ///         // A generation failure then reads:
    ///         //   The arbitrary values were seeded with 1234; reproduce this run with [Reproducible(Seed = 1234)].
    ///         </code>
    ///     </example>
    ///     <para>
    ///         Everything else matches <see cref="UseSeed(int)" />: the scope flows with the execution context, nests,
    ///         and restores the previous ambient context when disposed.
    ///     </para>
    /// </remarks>
    /// <param name="seed">The seed pinning the ambient context's value sequence.</param>
    /// <param name="replaySnippet">The code a reader copies to replay this run, quoted verbatim into generation-failure guidance.</param>
    /// <returns>A handle that restores the previous ambient context when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replaySnippet" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="replaySnippet" /> is empty or white space.</exception>
    public static IDisposable UseSeed(int seed, string replaySnippet) {
        if (replaySnippet is null) { throw new ArgumentNullException(nameof(replaySnippet)); }
        if (replaySnippet.Trim().Length == 0) { throw new ArgumentException("The replay snippet must be the code a reader copies to replay the run; pass a non-blank snippet, or use the overload without one to name Any.Reproducibly(seed, ...).", nameof(replaySnippet)); }

        return AmbientRandomSource.UseSeed(seed, replaySnippet);
    }

    /// <summary>
    ///     Runs <paramref name="body" /> with the ambient random context pinned to a fresh seed and, if the body
    ///     throws, reports that seed before letting the exception propagate. This is how a test that draws on
    ///     <see cref="Any" /> stays reproducible: the values still vary between runs (which surfaces accidental
    ///     dependencies), yet a failure names the exact seed to replay.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On failure the seed is written to <paramref name="report" /> (by default <see cref="Console.Error" />),
    ///         with a message naming the <c>Any.Reproducibly(seed, ...)</c> call that reproduces the run. Pass your
    ///         test framework's output writer (for example xUnit's <c>ITestOutputHelper.WriteLine</c>) to route it
    ///         there instead. The original exception is rethrown unchanged, so the test still fails with its real
    ///         message.
    ///     </para>
    ///     <para>
    ///         Reproducing a run needs the same sequence of draws, so a body whose generation order depends on
    ///         non-deterministic external state is not fully replayable from the seed alone.
    ///     </para>
    /// </remarks>
    /// <param name="body">The test body to run under a reproducible random context.</param>
    /// <param name="report">The sink the seed is written to on failure. Defaults to <see cref="Console.Error" /> when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    public static void Reproducibly(Action body, Action<string>? report = null) {
        Reproducibly(AmbientRandomSource.NewSeed(), body, report);
    }

    /// <summary>
    ///     Replays <paramref name="body" /> with the ambient random context pinned to <paramref name="seed" />, so a
    ///     run first seen through the parameterless <see cref="Reproducibly(Action, Action{String})" /> overload can
    ///     be reproduced exactly. If the body throws, the seed is reported before the exception propagates.
    /// </summary>
    /// <param name="seed">The seed to replay — typically the one a previous failure reported.</param>
    /// <param name="body">The test body to run under the seeded random context.</param>
    /// <param name="report">The sink the seed is written to on failure. Defaults to <see cref="Console.Error" /> when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    public static void Reproducibly(int seed, Action body, Action<string>? report = null) {
        if (body is null) { throw new ArgumentNullException(nameof(body)); }

        using (AmbientRandomSource.UseSeed(seed)) {
            try {
                body();
            } catch {
                Report(report, seed);

                throw;
            }
        }
    }

    /// <summary>
    ///     Asynchronous counterpart of <see cref="Reproducibly(Action, Action{String})" />: awaits
    ///     <paramref name="body" /> under a fresh seed and reports it if the body faults.
    /// </summary>
    /// <remarks>
    ///     <b>The returned task must be awaited.</b> Dropping it silences the body's failures — the assertions run
    ///     on a continuation after the caller has already moved on, and a discarded fault never reaches the test
    ///     runner. Discarding it is a compile error (diagnostic <c>JD002</c>); passing an asynchronous body to the
    ///     synchronous <see cref="Reproducibly(Action, Action{String})" /> instead is a compile error (<c>JD001</c>).
    /// </remarks>
    /// <param name="body">The asynchronous test body to run under a reproducible random context.</param>
    /// <param name="report">The sink the seed is written to on failure. Defaults to <see cref="Console.Error" /> when <c>null</c>.</param>
    /// <returns>A task that completes when <paramref name="body" /> completes, and faults with the body's exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    public static Task ReproduciblyAsync(Func<Task> body, Action<string>? report = null) {
        if (body is null) { throw new ArgumentNullException(nameof(body)); }

        return ReproduciblyAsync(AmbientRandomSource.NewSeed(), body, report);
    }

    /// <summary>
    ///     Asynchronous counterpart of <see cref="Reproducibly(int, Action, Action{String})" />: awaits
    ///     <paramref name="body" /> under <paramref name="seed" /> and reports it if the body faults.
    /// </summary>
    /// <param name="seed">The seed to replay — typically the one a previous failure reported.</param>
    /// <param name="body">The asynchronous test body to run under the seeded random context.</param>
    /// <param name="report">The sink the seed is written to on failure. Defaults to <see cref="Console.Error" /> when <c>null</c>.</param>
    /// <returns>A task that completes when <paramref name="body" /> completes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    public static Task ReproduciblyAsync(int seed, Func<Task> body, Action<string>? report = null) {
        if (body is null) { throw new ArgumentNullException(nameof(body)); }

        return RunReproduciblyAsync(seed, body, report);
    }

    // Kept separate from the public entry so the null-argument guard above throws synchronously at the call site,
    // rather than being deferred into the returned task's fault — which a caller who forgets to await would miss.
    private static async Task RunReproduciblyAsync(int seed, Func<Task> body, Action<string>? report) {
        using (AmbientRandomSource.UseSeed(seed)) {
            try {
                await body().ConfigureAwait(false);
            } catch {
                Report(report, seed);

                throw;
            }
        }
    }

    private static void Report(Action<string>? report, int seed) {
        string message = $"[JustDummies] These arbitrary values were seeded with {seed}. Reproduce this run with Any.Reproducibly({seed}, ...).";

        // The seed report is a best-effort diagnostic aid, called while an exception is already propagating: a
        // caller-supplied sink that throws must never mask the failure the seed exists to help diagnose. Try the
        // caller's sink first; if it throws, fall back to the default console sink so the seed still surfaces, and
        // swallow even the fallback's failure so the body's exception always propagates unchanged.
        if (report is not null && TryWrite(report, message)) { return; }

        TryWrite(Console.Error.WriteLine, message);
    }

    private static bool TryWrite(Action<string> sink, string message) {
        try {
            sink(message);

            return true;
        } catch {
            return false;
        }
    }

}

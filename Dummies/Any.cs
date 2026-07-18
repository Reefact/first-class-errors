namespace Dummies;

/// <summary>
///     The entry point of the library: supplies arbitrary, valid values for the parts of a test that are <b>not</b>
///     under assertion — the <i>dummies</i> a test needs so its <c>Arrange</c> stops advertising values it never
///     checks. The constraints chained on a generator express what the surrounding code requires of the value (a
///     value object's invariant, a contract precondition), never what the test asserts: an explicit <see cref="Any" />
///     call reads as "this is arbitrary" where a hand-picked literal reads as "this matters".
/// </summary>
/// <remarks>
///     <para>
///         Values are <b>built to satisfy</b> the declared constraints — the library never generates candidates and
///         filters them afterwards. Constraints that contradict each other fail at declaration time with a
///         <see cref="ConflictingAnyConstraintException" /> naming both sides.
///     </para>
///     <para>
///         Every value is drawn from a pseudo-random source. By default that source is unseeded, so each run produces
///         fresh values — which surfaces a test that secretly depends on one. Wrap a value-sensitive test in
///         <see cref="Reproducibly(Action, Action{String})" /> to make a failing run replayable: the source flows with
///         the current execution context, so it never leaks across tests running in parallel. For an explicit,
///         isolated deterministic context — for example outside a test body — use <see cref="WithSeed" />.
///     </para>
///     <example>
///         <code>
///         // The reference format is the invariant; the exact value is irrelevant — so it is Any.
///         string reference = Any.String().StartingWith("ORD-").WithLength(12);
///
///         // Turn a constrained primitive into a value object, without reflection:
///         OrderReference order = Any.String().StartingWith("ORD-").WithLength(12)
///                                   .As(OrderReference.Create)
///                                   .Generate();
///
///         // Make a value-sensitive test replayable: the seed is reported on failure...
///         Any.Reproducibly(() => { /* arrange with Any, act, assert */ });
///         // ...and replayed by passing it back:
///         Any.Reproducibly(1234, () => { /* ... */ });
///         </code>
///     </example>
/// </remarks>
public static class Any {

    /// <summary>
    ///     Starts an arbitrary <see cref="string" /> generator drawing from the ambient random context. Unconstrained,
    ///     it yields a string of 0 to 16 ASCII letters and digits; chain constraints to express what the surrounding
    ///     code requires (<c>NonEmpty()</c>, <c>WithLength(...)</c>, <c>StartingWith(...)</c>, ...).
    /// </summary>
    /// <returns>A string generator to constrain fluently.</returns>
    public static AnyString String() {
        return new AnyString(AmbientRandomSource.Instance, StringSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="int" /> generator drawing from the ambient random context. Unconstrained, it
    ///     draws from the full <see cref="int" /> range; chain constraints to express what the surrounding code
    ///     requires (<c>Positive()</c>, <c>Between(...)</c>, ...).
    /// </summary>
    /// <returns>An integer generator to constrain fluently.</returns>
    public static AnyInt32 Int32() {
        return AnyInt32.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="sbyte" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnySByte SByte() {
        return AnySByte.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="byte" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyByte Byte() {
        return AnyByte.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="short" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyInt16 Int16() {
        return AnyInt16.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ushort" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyUInt16 UInt16() {
        return AnyUInt16.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="uint" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyUInt32 UInt32() {
        return AnyUInt32.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="long" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyInt64 Int64() {
        return AnyInt64.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ulong" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyUInt64 UInt64() {
        return AnyUInt64.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="TimeSpan" /> generator drawing from the ambient random context:
    ///     full range unless constrained, negative durations included. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyTimeSpan TimeSpan() {
        return AnyTimeSpan.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="DateTime" /> generator drawing from the ambient random context:
    ///     any representable instant unless constrained; generated values carry Utc kind. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDateTime DateTime() {
        return AnyDateTime.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="DateTimeOffset" /> generator drawing from the ambient random context:
    ///     any representable instant unless constrained; generated values carry a zero (UTC) offset. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDateTimeOffset DateTimeOffset() {
        return AnyDateTimeOffset.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Creates an isolated, deterministic generation context: every generator created from it draws from a
    ///     dedicated source seeded with <paramref name="seed" />, independent of the ambient context. Two contexts
    ///     created with the same seed yield the same sequence of values. Prefer
    ///     <see cref="Reproducibly(Action, Action{String})" /> inside tests — it keeps the arbitrary-by-default
    ///     behavior and reports the seed only when the test fails; reach for <see cref="WithSeed" /> when you need an
    ///     explicit generator object, for example outside a test body.
    /// </summary>
    /// <param name="seed">The seed pinning the context's value sequence.</param>
    /// <returns>A deterministic generation context.</returns>
    public static AnyContext WithSeed(int seed) {
        return new AnyContext(seed);
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
    /// <param name="body">The asynchronous test body to run under a reproducible random context.</param>
    /// <param name="report">The sink the seed is written to on failure. Defaults to <see cref="Console.Error" /> when <c>null</c>.</param>
    /// <returns>A task that completes when <paramref name="body" /> completes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    public static Task Reproducibly(Func<Task> body, Action<string>? report = null) {
        return Reproducibly(AmbientRandomSource.NewSeed(), body, report);
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
    public static async Task Reproducibly(int seed, Func<Task> body, Action<string>? report = null) {
        if (body is null) { throw new ArgumentNullException(nameof(body)); }

        using (AmbientRandomSource.UseSeed(seed)) {
            try {
                await body().ConfigureAwait(false);
            } catch {
                Report(report, seed);

                throw;
            }
        }
    }

    /// <summary>
    ///     Composes two generators into one through a constructor lambda — the reflection-free way to assemble an
    ///     object from constrained parts. Each part draws from its own random context when the composed generator
    ///     generates.
    /// </summary>
    /// <remarks>
    ///     <example>
    ///         <code>
    ///         IAny&lt;Customer&gt; customer = Any.Combine(
    ///             Any.String().NonEmpty().WithMaxLength(50),
    ///             Any.String().StartingWith("ORD-").WithLength(12),
    ///             (name, reference) =&gt; new Customer(name, OrderReference.Create(reference)));
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<TResult> Combine<T1, T2, TResult>(IAny<T1> first, IAny<T2> second, Func<T1, T2, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)})");
        });
    }

    /// <summary>
    ///     Composes three generators into one through a constructor lambda — see
    ///     <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<TResult> Combine<T1, T2, T3, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, Func<T1, T2, T3, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)})");
        });
    }

    private static void Report(Action<string>? report, int seed) {
        (report ?? Console.Error.WriteLine)(
            $"[Dummies] These arbitrary values were seeded with {seed}. Reproduce this run with Any.Reproducibly({seed}, ...).");
    }

}

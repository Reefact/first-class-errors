#region Usings declarations

using System.Text.RegularExpressions;

#endregion

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
///         string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
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
    ///     Starts a generator of arbitrary strings that <b>match <paramref name="pattern" /></b>, drawing from the
    ///     ambient random context. The pattern is the whole specification — the returned generator carries no further
    ///     shape or length constraints; express those inside the pattern. It still composes through <c>As(...)</c>,
    ///     <c>OrNull()</c>, <c>Combine(...)</c> and the collection generators.
    /// </summary>
    /// <remarks>
    ///     Supported is the <b>regular</b> subset of the pattern language: literals and escapes (metacharacters,
    ///     control characters, <c>\xHH</c>, <c>\uHHHH</c>), the shorthands <c>\d \D \w \W \s \S</c>, character classes
    ///     (ranges, negation), the quantifiers <c>? * + {n} {n,} {n,m}</c> (an unbounded quantifier draws its minimum
    ///     plus 0 to 8 repetitions), alternation, grouping (capturing, non-capturing and named), the dot, and the
    ///     anchors <c>^ $</c> at the start and end of the pattern or of a top-level alternation branch (no-ops there,
    ///     since a whole matching string is generated). Values are drawn from printable ASCII. A well-formed but
    ///     non-regular or not-generatable construct — a lookaround, a backreference, a word boundary, a Unicode
    ///     category, an atomic group, a class subtraction, an anchor placed where it could never match — raises an
    ///     <see cref="UnsupportedRegexException" />; a malformed pattern raises an <see cref="ArgumentException" />,
    ///     mirroring what the real engine rejects.
    /// </remarks>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public static AnyPattern StringMatching(string pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        return AnyPattern.FromPattern(AmbientRandomSource.Instance, pattern, ignoreCase: false);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> — the same contract as
    ///     <see cref="StringMatching(string)" />, taking a compiled <see cref="Regex" /> so a test can reuse the very
    ///     object its production code validates with. <see cref="RegexOptions.IgnoreCase" /> is honoured.
    ///     <see cref="RegexOptions.IgnorePatternWhitespace" /> changes how the pattern text itself is read and is
    ///     rejected; the remaining options do not change which strings the pattern matches and are ignored.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern, or carries <see cref="RegexOptions.IgnorePatternWhitespace" />.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public static AnyPattern StringMatching(Regex pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }
        if ((pattern.Options & RegexOptions.IgnorePatternWhitespace) != 0) { throw new ArgumentException("RegexOptions.IgnorePatternWhitespace changes how the pattern text is read; pass the pattern without it (or with its whitespace and comments removed).", nameof(pattern)); }

        return AnyPattern.FromPattern(AmbientRandomSource.Instance, pattern.ToString(), (pattern.Options & RegexOptions.IgnoreCase) != 0);
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
    ///     Starts an arbitrary <see cref="double" /> generator drawing from the ambient random context:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDouble Double() {
        return AnyDouble.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="float" /> generator drawing from the ambient random context:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnySingle Single() {
        return AnySingle.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="decimal" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDecimal Decimal() {
        return AnyDecimal.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="bool" /> generator drawing from the ambient random context — an even coin
    ///     flip unless pinned with <c>True()</c> or <c>False()</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyBool Bool() {
        return AnyBool.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Guid" /> generator drawing from the ambient random context — unlike
    ///     <see cref="System.Guid.NewGuid" />, reproducible inside an <c>Any.Reproducibly(...)</c> run, and for every
    ///     practical purpose never empty.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyGuid Guid() {
        return AnyGuid.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <typeparamref name="TEnum" /> generator drawing from the ambient random context —
    ///     uniformly across the enum's declared members, never an undeclared numeric value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
    /// <returns>A generator to constrain fluently.</returns>
    /// <exception cref="AnyGenerationException">Thrown when <typeparamref name="TEnum" /> declares no members.</exception>
    public static AnyEnum<TEnum> Enum<TEnum>()
        where TEnum : struct, Enum {
        return AnyEnum<TEnum>.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="char" /> generator drawing from the ambient random context — ASCII letters
    ///     and digits unless constrained, mirroring <see cref="AnyString" />'s character families.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyChar Char() {
        return AnyChar.Create(AmbientRandomSource.Instance);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateOnly" /> generator drawing from the ambient random context — any
    ///     representable date unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDateOnly DateOnly() {
        return AnyDateOnly.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeOnly" /> generator drawing from the ambient random context — any
    ///     time of day unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyTimeOnly TimeOnly() {
        return AnyTimeOnly.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Int128" /> generator drawing from the ambient random context — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyInt128 Int128() {
        return AnyInt128.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.UInt128" /> generator drawing from the ambient random context — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyUInt128 UInt128() {
        return AnyUInt128.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Half" /> generator drawing from the ambient random context — finite
    ///     values only. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyHalf Half() {
        return AnyHalf.Create(AmbientRandomSource.Instance);
    }
#endif

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

    /// <summary>
    ///     Composes four generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<TResult> Combine<T1, T2, T3, T4, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, IAny<T4> fourth, Func<T1, T2, T3, T4, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third) ?? AnyDerivation.SourceOf(fourth);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)}, {AnyDerivation.Display(fourthValue)})");
        });
    }

    /// <summary>
    ///     Composes five generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<TResult> Combine<T1, T2, T3, T4, T5, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, IAny<T4> fourth, IAny<T5> fifth, Func<T1, T2, T3, T4, T5, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third) ?? AnyDerivation.SourceOf(fourth) ?? AnyDerivation.SourceOf(fifth);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();
            T5 fifthValue  = fifth.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)}, {AnyDerivation.Display(fourthValue)}, {AnyDerivation.Display(fifthValue)})");
        });
    }

    /// <summary>
    ///     Composes six generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<TResult> Combine<T1, T2, T3, T4, T5, T6, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, IAny<T4> fourth, IAny<T5> fifth, IAny<T6> sixth, Func<T1, T2, T3, T4, T5, T6, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third) ?? AnyDerivation.SourceOf(fourth) ?? AnyDerivation.SourceOf(fifth) ?? AnyDerivation.SourceOf(sixth);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();
            T5 fifthValue  = fifth.Generate();
            T6 sixthValue  = sixth.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)}, {AnyDerivation.Display(fourthValue)}, {AnyDerivation.Display(fifthValue)}, {AnyDerivation.Display(sixthValue)})");
        });
    }

    /// <summary>
    ///     Composes seven generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="seventh">The generator of the seventh part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="T7">The type of the seventh part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
                                                     Justification = "Heterogeneous composition needs one generator parameter per part; the arity-8 ceiling is a deliberate ergonomic decision (ADR-0015), and a flat parameter list reads better at the call site than nested Combine calls.")]
    public static IAny<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, IAny<T4> fourth, IAny<T5> fifth, IAny<T6> sixth, IAny<T7> seventh, Func<T1, T2, T3, T4, T5, T6, T7, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (seventh is null) { throw new ArgumentNullException(nameof(seventh)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third) ?? AnyDerivation.SourceOf(fourth) ?? AnyDerivation.SourceOf(fifth) ?? AnyDerivation.SourceOf(sixth) ?? AnyDerivation.SourceOf(seventh);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue   = first.Generate();
            T2 secondValue  = second.Generate();
            T3 thirdValue   = third.Generate();
            T4 fourthValue  = fourth.Generate();
            T5 fifthValue   = fifth.Generate();
            T6 sixthValue   = sixth.Generate();
            T7 seventhValue = seventh.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue, seventhValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)}, {AnyDerivation.Display(fourthValue)}, {AnyDerivation.Display(fifthValue)}, {AnyDerivation.Display(sixthValue)}, {AnyDerivation.Display(seventhValue)})");
        });
    }

    /// <summary>
    ///     Composes eight generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    ///     Eight is the ceiling; a constructor needing more parts is better assembled from intermediate value objects.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="seventh">The generator of the seventh part.</param>
    /// <param name="eighth">The generator of the eighth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="T7">The type of the seventh part.</typeparam>
    /// <typeparam name="T8">The type of the eighth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
                                                     Justification = "Heterogeneous composition needs one generator parameter per part; the arity-8 ceiling is a deliberate ergonomic decision (ADR-0015), and a flat parameter list reads better at the call site than nested Combine calls.")]
    public static IAny<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IAny<T1> first, IAny<T2> second, IAny<T3> third, IAny<T4> fourth, IAny<T5> fifth, IAny<T6> sixth, IAny<T7> seventh, IAny<T8> eighth, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (seventh is null) { throw new ArgumentNullException(nameof(seventh)); }
        if (eighth is null) { throw new ArgumentNullException(nameof(eighth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second) ?? AnyDerivation.SourceOf(third) ?? AnyDerivation.SourceOf(fourth) ?? AnyDerivation.SourceOf(fifth) ?? AnyDerivation.SourceOf(sixth) ?? AnyDerivation.SourceOf(seventh) ?? AnyDerivation.SourceOf(eighth);

        return new DerivedAny<TResult>(source, () => {
            T1 firstValue   = first.Generate();
            T2 secondValue  = second.Generate();
            T3 thirdValue   = third.Generate();
            T4 fourthValue  = fourth.Generate();
            T5 fifthValue   = fifth.Generate();
            T6 sixthValue   = sixth.Generate();
            T7 seventhValue = seventh.Generate();
            T8 eighthValue  = eighth.Generate();

            return AnyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue, seventhValue, eighthValue), source, $"the composer passed to Combine(...) threw for the generated values ({AnyDerivation.Display(firstValue)}, {AnyDerivation.Display(secondValue)}, {AnyDerivation.Display(thirdValue)}, {AnyDerivation.Display(fourthValue)}, {AnyDerivation.Display(fifthValue)}, {AnyDerivation.Display(sixthValue)}, {AnyDerivation.Display(seventhValue)}, {AnyDerivation.Display(eighthValue)})");
        });
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="List{T}" /> generator over <paramref name="item" />. Unconstrained, it yields
    ///     0 to 8 elements; chain constraints to express what the surrounding code requires (<c>NonEmpty()</c>,
    ///     <c>WithCount(...)</c>, <c>Distinct()</c>, <c>Containing(...)</c>).
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A list generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnyList<T> ListOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnyList<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary array (<c>T[]</c>) generator over <paramref name="item" /> — same constraint surface as
    ///     <see cref="ListOf{T}" />, producing an array.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An array generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnyArray<T> ArrayOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnyArray<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="IEnumerable{T}" /> generator over <paramref name="item" /> — same constraint
    ///     surface as <see cref="ListOf{T}" />. The generated sequence is fully materialized, so it never re-draws when
    ///     enumerated more than once.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A sequence generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnySequence<T> SequenceOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnySequence<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, false, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="HashSet{T}" /> generator over <paramref name="item" /> — distinct by nature.
    ///     When the count exceeds the number of distinct values <paramref name="item" /> can produce, the conflict is
    ///     reported eagerly.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A set generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <c>null</c>.</exception>
    public static AnySet<T> SetOf<T>(IAny<T> item) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }

        return new AnySet<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, true, null));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="HashSet{T}" /> generator over <paramref name="item" />, deduplicating
    ///     elements with <paramref name="comparer" /> — the same comparer the resulting set carries.
    /// </summary>
    /// <param name="item">The generator each element is drawn from.</param>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A set generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> or <paramref name="comparer" /> is <c>null</c>.</exception>
    public static AnySet<T> SetOf<T>(IAny<T> item, IEqualityComparer<T> comparer) {
        if (item is null) { throw new ArgumentNullException(nameof(item)); }
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return new AnySet<T>(AnyDerivation.SourceOf(item), CollectionState<T>.Create(item, true, comparer));
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="Dictionary{TKey,TValue}" /> generator drawing keys from
    ///     <paramref name="keys" /> and values from <paramref name="values" />. Keys are distinct by nature, so the key
    ///     generator's domain gates feasibility exactly as it does for <see cref="SetOf{T}(IAny{T})" />.
    /// </summary>
    /// <param name="keys">The generator each key is drawn from.</param>
    /// <param name="values">The generator each value is drawn from.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A dictionary generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keys" /> or <paramref name="values" /> is <c>null</c>.</exception>
    public static AnyDictionary<TKey, TValue> DictionaryOf<TKey, TValue>(IAny<TKey> keys, IAny<TValue> values)
        where TKey : notnull {
        if (keys is null) { throw new ArgumentNullException(nameof(keys)); }
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        RandomSource? source = AnyDerivation.SourceOf(keys) ?? AnyDerivation.SourceOf(values);

        return new AnyDictionary<TKey, TValue>(source, CollectionState<TKey>.Create(keys, true, null), values);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="Dictionary{TKey,TValue}" /> generator whose keys are deduplicated with
    ///     <paramref name="keyComparer" /> — the same comparer the resulting dictionary carries.
    /// </summary>
    /// <param name="keys">The generator each key is drawn from.</param>
    /// <param name="values">The generator each value is drawn from.</param>
    /// <param name="keyComparer">The equality comparer deciding whether two keys are the same.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A dictionary generator to constrain fluently.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static AnyDictionary<TKey, TValue> DictionaryOf<TKey, TValue>(IAny<TKey> keys, IAny<TValue> values, IEqualityComparer<TKey> keyComparer)
        where TKey : notnull {
        if (keys is null) { throw new ArgumentNullException(nameof(keys)); }
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (keyComparer is null) { throw new ArgumentNullException(nameof(keyComparer)); }

        RandomSource? source = AnyDerivation.SourceOf(keys) ?? AnyDerivation.SourceOf(values);

        return new AnyDictionary<TKey, TValue>(source, CollectionState<TKey>.Create(keys, true, keyComparer), values);
    }

    /// <summary>
    ///     Composes two generators into a generator of the value tuple <c>(<typeparamref name="T1" />,
    ///     <typeparamref name="T2" />)</c> — sugar over <see cref="Combine{T1,T2,TResult}" /> for the common case of
    ///     pairing two arbitrary values.
    /// </summary>
    /// <param name="first">The generator of the first component.</param>
    /// <param name="second">The generator of the second component.</param>
    /// <typeparam name="T1">The type of the first component.</typeparam>
    /// <typeparam name="T2">The type of the second component.</typeparam>
    /// <returns>A generator of the paired value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<(T1, T2)> PairOf<T1, T2>(IAny<T1> first, IAny<T2> second) {
        return Combine(first, second, (one, two) => (one, two));
    }

    /// <summary>
    ///     Composes three generators into a generator of the value tuple <c>(<typeparamref name="T1" />,
    ///     <typeparamref name="T2" />, <typeparamref name="T3" />)</c> — sugar over
    ///     <see cref="Combine{T1,T2,T3,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first component.</param>
    /// <param name="second">The generator of the second component.</param>
    /// <param name="third">The generator of the third component.</param>
    /// <typeparam name="T1">The type of the first component.</typeparam>
    /// <typeparam name="T2">The type of the second component.</typeparam>
    /// <typeparam name="T3">The type of the third component.</typeparam>
    /// <returns>A generator of the tripled value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IAny<(T1, T2, T3)> TripleOf<T1, T2, T3>(IAny<T1> first, IAny<T2> second, IAny<T3> third) {
        return Combine(first, second, third, (one, two, three) => (one, two, three));
    }

    private static void Report(Action<string>? report, int seed) {
        (report ?? Console.Error.WriteLine)(
            $"[Dummies] These arbitrary values were seeded with {seed}. Reproduce this run with Any.Reproducibly({seed}, ...).");
    }

}

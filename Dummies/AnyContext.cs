#region Usings declarations

using System.Text.RegularExpressions;

#endregion

namespace Dummies;

/// <summary>
///     An isolated, deterministic generation context created by <see cref="Any.WithSeed" />: every generator created
///     from it draws from a dedicated source seeded with <see cref="Seed" />, independent of the ambient context the
///     static <see cref="Any" /> entry points use. Two contexts created with the same seed yield the same sequence
///     of values.
/// </summary>
/// <remarks>
///     <para>
///         Inside a test, prefer wrapping the body in <c>Any.Reproducibly(...)</c>: it keeps values arbitrary by
///         default and reports a replayable seed only when the test fails. A context is the explicit-object
///         alternative for when that scope does not fit — generating a deterministic dataset outside a test body,
///         for example.
///     </para>
///     <para>
///         A context owns a single pseudo-random generator and is <b>not</b> thread-safe: use one context per test
///         or per generation sequence, not a shared one.
///     </para>
/// </remarks>
public sealed class AnyContext {

    #region Fields declarations

    private readonly FixedRandomSource _source;

    #endregion

    internal AnyContext(int seed) {
        Seed    = seed;
        _source = new FixedRandomSource(seed);
    }

    /// <summary>The seed pinning this context's value sequence.</summary>
    public int Seed { get; }

    /// <summary>
    ///     Starts an arbitrary <see cref="string" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Any.String" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>A string generator to constrain fluently.</returns>
    public AnyString String() {
        return new AnyString(_source, StringSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> drawing from this context —
    ///     same fluent surface as <see cref="Any.StringMatching(string)" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public AnyPattern StringMatching(string pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        return AnyPattern.FromPattern(_source, pattern, ignoreCase: false);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> drawing from this context —
    ///     same fluent surface as <see cref="Any.StringMatching(Regex)" />, deterministic under this context's seed.
    ///     <see cref="RegexOptions.IgnoreCase" /> is honoured; <see cref="RegexOptions.IgnorePatternWhitespace" /> is
    ///     rejected; the remaining options are ignored.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern, or carries <see cref="RegexOptions.IgnorePatternWhitespace" />.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public AnyPattern StringMatching(Regex pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }
        if ((pattern.Options & RegexOptions.IgnorePatternWhitespace) != 0) { throw new ArgumentException("RegexOptions.IgnorePatternWhitespace changes how the pattern text is read; pass the pattern without it (or with its whitespace and comments removed).", nameof(pattern)); }

        return AnyPattern.FromPattern(_source, pattern.ToString(), (pattern.Options & RegexOptions.IgnoreCase) != 0);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Uri" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Any.Uri" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>A URI generator to narrow fluently.</returns>
    public AnyUri Uri() {
        return new AnyUri(_source, UriSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="int" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Any.Int32" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>An integer generator to constrain fluently.</returns>
    public AnyInt32 Int32() {
        return AnyInt32.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="sbyte" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnySByte SByte() {
        return AnySByte.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="byte" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyByte Byte() {
        return AnyByte.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="short" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyInt16 Int16() {
        return AnyInt16.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ushort" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyUInt16 UInt16() {
        return AnyUInt16.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="uint" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyUInt32 UInt32() {
        return AnyUInt32.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="long" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyInt64 Int64() {
        return AnyInt64.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ulong" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyUInt64 UInt64() {
        return AnyUInt64.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="TimeSpan" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained, negative durations included. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyTimeSpan TimeSpan() {
        return AnyTimeSpan.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="DateTime" /> generator drawing from this context — deterministic under this context's seed:
    ///     any representable instant unless constrained; generated values carry Utc kind. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyDateTime DateTime() {
        return AnyDateTime.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="DateTimeOffset" /> generator drawing from this context — deterministic under this context's seed:
    ///     any representable instant unless constrained; generated values carry a zero (UTC) offset. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyDateTimeOffset DateTimeOffset() {
        return AnyDateTimeOffset.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="double" /> generator drawing from this context — deterministic under this context's seed:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyDouble Double() {
        return AnyDouble.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="float" /> generator drawing from this context — deterministic under this context's seed:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnySingle Single() {
        return AnySingle.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="decimal" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyDecimal Decimal() {
        return AnyDecimal.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="bool" /> generator drawing from this context (deterministic under this context's seed) — an even coin
    ///     flip unless pinned with <c>True()</c> or <c>False()</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyBoolean Boolean() {
        return AnyBoolean.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Guid" /> generator drawing from this context (deterministic under this context's seed) — unlike
    ///     <see cref="System.Guid.NewGuid" />, reproducible inside an <c>Any.Reproducibly(...)</c> run, and for every
    ///     practical purpose never empty.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyGuid Guid() {
        return AnyGuid.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <typeparamref name="TEnum" /> generator drawing from this context (deterministic under this context's seed) —
    ///     uniformly across the enum's declared members, never an undeclared numeric value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
    /// <returns>A generator to constrain fluently.</returns>
    /// <exception cref="AnyGenerationException">Thrown when <typeparamref name="TEnum" /> declares no members.</exception>
    public AnyEnum<TEnum> Enum<TEnum>()
        where TEnum : struct, Enum {
        return AnyEnum<TEnum>.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="char" /> generator drawing from this context (deterministic under this context's seed) — ASCII letters
    ///     and digits unless constrained, mirroring <see cref="AnyString" />'s character families.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyChar Char() {
        return AnyChar.Create(_source);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateOnly" /> generator drawing from this context (deterministic under this context's seed) — any
    ///     representable date unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyDateOnly DateOnly() {
        return AnyDateOnly.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeOnly" /> generator drawing from this context (deterministic under this context's seed) — any
    ///     time of day unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyTimeOnly TimeOnly() {
        return AnyTimeOnly.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Int128" /> generator drawing from this context (deterministic under this context's seed) — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyInt128 Int128() {
        return AnyInt128.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.UInt128" /> generator drawing from this context (deterministic under this context's seed) — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyUInt128 UInt128() {
        return AnyUInt128.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Half" /> generator drawing from this context (deterministic under this context's seed) — finite
    ///     values only. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public AnyHalf Half() {
        return AnyHalf.Create(_source);
    }
#endif

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool of caller-supplied <paramref name="values" /> drawing from
    ///     this context — same surface as <see cref="Any.OneOf{T}(T[])" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public AnyOneOf<T> OneOf<T>(params T[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(_source, values);
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a list drawing from this context — same surface as
    ///     <see cref="Any.ElementOf{T}(IReadOnlyList{T})" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public AnyOneOf<T> ElementOf<T>(IReadOnlyList<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(_source, values);
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a sequence drawing from this context — same surface
    ///     as <see cref="Any.ElementOf{T}(IEnumerable{T})" />, deterministic under this context's seed. The sequence is
    ///     materialized once.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A terminal generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public AnyOneOf<T> ElementOf<T>(IEnumerable<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return AnyOneOf<T>.FromPool(_source, values as IReadOnlyList<T> ?? values.ToArray());
    }

}

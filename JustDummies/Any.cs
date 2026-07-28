#region Usings declarations

using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

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
public static partial class Any {

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
    ///     Starts an arbitrary <see cref="System.Uri" /> generator drawing from the ambient random context.
    ///     Unconstrained, it yields any valid URI from the safe space — an absolute web (<c>http</c>/<c>https</c>),
    ///     WebSocket (<c>ws</c>/<c>wss</c>), FTP or mailto URI, or a relative reference. Narrow it to a family
    ///     (<c>Web()</c>, <c>WebSocket()</c>, <c>Ftp()</c>, <c>Mailto()</c>, <c>Relative()</c>) to reach that family's
    ///     component constraints; each narrowing returns a builder exposing only that family's valid components.
    /// </summary>
    /// <returns>A URI generator to narrow fluently.</returns>
    public static AnyUri Uri() {
        return new AnyUri(AmbientRandomSource.Instance, UriSpec.Unconstrained);
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
    public static AnyBoolean Boolean() {
        return AnyBoolean.Create(AmbientRandomSource.Instance);
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

}

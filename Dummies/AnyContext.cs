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
    public AnyBool Bool() {
        return AnyBool.Create(_source);
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

}

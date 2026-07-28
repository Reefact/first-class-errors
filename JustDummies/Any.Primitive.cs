namespace JustDummies;

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
    ///     Starts an arbitrary <see cref="System.TimeSpan" /> generator drawing from the ambient random context:
    ///     full range unless constrained, negative durations included. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyTimeSpan TimeSpan() {
        return AnyTimeSpan.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTime" /> generator drawing from the ambient random context:
    ///     any representable instant unless constrained; generated values carry Utc kind. Same constraint algebra as <see cref="AnyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static AnyDateTime DateTime() {
        return AnyDateTime.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTimeOffset" /> generator drawing from the ambient random context:
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

namespace JustDummies;

/// <summary>
///     The magnitude an arbitrary number stays within unless a declared constraint leaves no room for it — the
///     numeric counterpart of the small default spread the string and collection generators use, and the reason a
///     dummy number stays unremarkable (ADR-0052).
/// </summary>
/// <remarks>
///     <para>
///         A dummy exists to fill a slot whose content the test does not care about. A value drawn uniformly across a
///         floating-point type's whole domain is not that: almost every draw lands within a few decades of the
///         type's maximum, where the type stops behaving like arithmetic — a further multiplication overflows,
///         <c>x + 1 == x</c>, and a scale constraint has no fractional digits left to constrain. Such a value makes
///         the test fail for reasons that have nothing to do with what it asserts, and never visits the magnitudes
///         where real defects live.
///     </para>
///     <para>
///         The window <b>clips</b> a draw; it never widens one, and it never overrides a declared bound. A generator
///         whose declared interval lies entirely outside it — <c>Between(1e300, 1e308)</c> — draws from that interval
///         as declared, because the caller asked for that magnitude explicitly. A generator whose declared interval
///         merely <i>permits</i> large values — <c>Between(0, double.MaxValue)</c> — keeps drawing ordinary ones,
///         because permitting is not requesting. The same principle governs sizes under ADR-0050.
///     </para>
///     <para>
///         Both constants carry the same magnitude in the two arithmetics the numeric engines use. A type whose whole
///         domain is already ordinary — <c>Half</c>, which stops at 65 504 — is unaffected, since clipping to a
///         window wider than its domain changes nothing.
///     </para>
/// </remarks>
internal static class OrdinaryMagnitude {

    /// <summary>
    ///     The window's half-width for the binary floating-point engine. Large enough to look like a real quantity
    ///     and to exercise multi-digit formatting, small enough that any plausible further arithmetic — a rate, a
    ///     tax, a conversion factor — stays hundreds of decades away from overflow, and that a <c>double</c> keeps
    ///     about nine significant digits below the decimal point for a scale constraint to act on.
    /// </summary>
    internal const double AsDouble = 1_000_000d;

    /// <summary>The same magnitude for the decimal engine.</summary>
    internal const decimal AsDecimal = 1_000_000m;

}

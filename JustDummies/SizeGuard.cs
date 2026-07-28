#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     The argument validation every length and count constraint shares, defined once so the two surfaces — a string's
///     lengths and a collection's counts — cannot drift apart. It distinguishes the two kinds of size a caller can
///     declare, which ADR-0050 separates: a bound that only <b>caps</b> a draw, and a size the generator must actually
///     <b>produce</b>.
/// </summary>
/// <remarks>
///     <para>
///         A maximum is a permission, not a request: it narrows a draw and never widens it, so honouring one costs
///         nothing and any non-negative value is legal — a cap mirroring a storage limit far above
///         <see cref="MaxProducibleSize" /> stays declarable and still yields small dummies.
///     </para>
///     <para>
///         An exact or minimum size is the opposite: it is what the generator must materialize, so it decides how much
///         memory and work a draw costs. Above the ceiling it is refused at declaration time, as an
///         <see cref="ArgumentOutOfRangeException" /> naming the parameter the caller wrote — a single argument
///         unusable on its own is a caller mistake, not a contradiction between constraints and not a generation
///         failure, so it belongs to the same category as the negative size rejected right beside it rather than to
///         the library's own exception hierarchy.
///     </para>
/// </remarks>
internal static class SizeGuard {

    /// <summary>
    ///     The largest size a generator will be asked to produce. It sits in the gap between the legitimate and the
    ///     absurd: five orders of magnitude above the unconstrained spread, so ordinary use cannot approach it, and two
    ///     above the largest business limit a boundary test plausibly exercises, so such a test is never refused. A
    ///     value of this size still materializes in milliseconds — the ceiling therefore never turns a slow test into a
    ///     fast one, it turns a hang or an allocation failure into a diagnosable error.
    /// </summary>
    internal const int MaxProducibleSize = 1_000_000;

    #region Statics members declarations

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Validates a bound that only ever caps a draw. Any non-negative value is legal; the ceiling deliberately does
    ///     not apply, because nothing has to be produced to honour a maximum.
    /// </summary>
    /// <param name="value">The declared bound.</param>
    /// <param name="parameterName">The name of the parameter the caller wrote.</param>
    /// <param name="subject">How the size reads in a message — <c>"length"</c> or <c>"count"</c>.</param>
    /// <returns>The validated value, so a caller can guard and pass in one expression.</returns>
    internal static int RequireNonNegative(int value, string parameterName, string subject) {
        if (parameterName is null) { throw new ArgumentNullException(nameof(parameterName)); }
        if (subject is null) { throw new ArgumentNullException(nameof(subject)); }
        if (value < 0) { throw new ArgumentOutOfRangeException(parameterName, value, $"The {subject} must not be negative."); }

        return value;
    }

    /// <summary>
    ///     Validates a size the generator must actually produce: non-negative, and no larger than
    ///     <see cref="MaxProducibleSize" />.
    /// </summary>
    /// <param name="value">The declared size.</param>
    /// <param name="parameterName">The name of the parameter the caller wrote.</param>
    /// <param name="subject">How the size reads in a message — <c>"length"</c> or <c>"count"</c>.</param>
    /// <returns>The validated value, so a caller can guard and pass in one expression.</returns>
    /// <remarks>
    ///     The ceiling applies to the size the caller states, not to the effective minimum that required fragments
    ///     (a prefix, a suffix, contained values) raise it to. The message can then name the parameter the caller
    ///     wrote, and a fragment large enough to matter is a literal the caller has already allocated — guarding it
    ///     here would report a size no argument of the call carries.
    /// </remarks>
    internal static int RequireProducible(int value, string parameterName, string subject) {
        RequireNonNegative(value, parameterName, subject); // guards both strings for this method too
        if (value > MaxProducibleSize) {
            throw new ArgumentOutOfRangeException(parameterName, value,
                                                  $"The {subject} must not exceed {V(MaxProducibleSize)}. Only a size the generator must actually produce is capped; a maximum, which only narrows the draw, accepts any non-negative value.");
        }

        return value;
    }

    #endregion

}

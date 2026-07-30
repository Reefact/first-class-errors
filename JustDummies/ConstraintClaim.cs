namespace JustDummies;

/// <summary>
///     A declared constraint and what it claims about the values it admits — <c>WithLength(3)</c> paired with
///     "already fixes the length at 3". The pair exists so a conflict can be reported against whichever of two
///     constraints is not the one being applied, without the reporting code taking four loose strings in an order
///     nothing checks.
/// </summary>
/// <remarks>
///     Immutable, and a class rather than a struct, like every value in this repository: a struct would expose a
///     parameterless constructor yielding a pair with no name and no claim.
///     <para>
///         It guards its arguments, unlike the exception factories it feeds. The difference is that this is not an
///         exception: ADR-0045 exempts exception types — building one must never throw — and the reflection
///         convention that enforces the rule skips them, but it does inspect this type and requires the guard. The
///         null it defends against is unreachable in practice, since every call site passes values the compiler has
///         proven non-null; the guard is what the convention checks, not what the code relies on.
///     </para>
/// </remarks>
internal sealed class ConstraintClaim {

    #region Statics members declarations

    /// <summary>
    ///     Pairs <paramref name="constraint" /> with what it <paramref name="claims" />, written as a clause that
    ///     follows the constraint's name — "already caps the count at 3", not "caps".
    /// </summary>
    internal static ConstraintClaim Of(string constraint, string claims) {
        if (constraint is null) { throw new ArgumentNullException(nameof(constraint)); }
        if (claims is null) { throw new ArgumentNullException(nameof(claims)); }

        return new ConstraintClaim(constraint, claims);
    }

    #endregion

    private ConstraintClaim(string constraint, string claims) {
        Constraint = constraint;
        Claims     = claims;
    }

    /// <summary>What the constraint claims about the values it admits, as a clause following its name.</summary>
    internal string Claims { get; }

    /// <summary>The constraint as the caller spelled it.</summary>
    internal string Constraint { get; }

    /// <summary>The constraint and its claim, as they read inside a conflict message.</summary>
    public override string ToString() {
        return $"{Constraint} {Claims}";
    }

}

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
///         It carries no argument guard, and says so with <see cref="BuiltOnTheFailurePathAttribute" />: instances are
///         built at a throw site, as an argument to an exception factory, so a guard here would throw while a failure
///         is being reported and lose it (ADR-0064). The contract is the compiler's — both members are non-nullable,
///         so a caller that cannot prove a value is <c>CS8604</c> at build time.
///     </para>
/// </remarks>
[BuiltOnTheFailurePath]
internal sealed class ConstraintClaim {

    #region Statics members declarations

    /// <summary>
    ///     Pairs <paramref name="constraint" /> with what it <paramref name="claims" />, written as a clause that
    ///     follows the constraint's name — "already caps the count at 3", not "caps".
    /// </summary>
    internal static ConstraintClaim Of(string constraint, string claims) {
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

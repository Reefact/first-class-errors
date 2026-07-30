namespace JustDummies;

/// <summary>
///     A blamed subject and what it claims about the values it admits — <c>WithLength(3)</c> paired with "already
///     fixes the length at 3". The pair exists so a conflict can be reported against whichever of two subjects is
///     not the one being applied, without the reporting code taking four loose strings in an order nothing checks.
/// </summary>
/// <remarks>
///     Immutable, and a class rather than a struct, like every value in this repository: a struct would expose a
///     parameterless constructor yielding a pair with no name and no claim.
///     <para>
///         The subject is usually a constraint the caller wrote, and <see cref="Of" /> takes it as one. It is not
///         always: a shape's part can be blamed too — the contained value <c>"ABC"</c>, the prefix <c>"ORD-"</c> —
///         and those are phrases the library composes, not calls anyone made. <see cref="OfPhrase" /> is that case,
///         named rather than smuggled through as a string, so the constraint slot keeps meaning a constraint. A
///         phrase carries no <see cref="Constraint" />, which is what makes it never equal to the constraint being
///         applied — the comparison the blame choice turns on.
///     </para>
///     <para>
///         Two claims are equal when they blame the same subject for the same thing. Being a value with no identity
///         beyond what it holds, it says so rather than leaving the reference comparison a reader would get by
///         default — the same reason <see cref="ConstraintCall" /> carries its own (ADR-0065). Nothing compares two
///         claims today; a value that answers the question wrongly the first time it is asked is worse than one that
///         answers it, so the answer is written now rather than when a caller needs it.
///     </para>
///     <para>
///         It carries no argument guard, and says so with <see cref="BuiltOnTheFailurePathAttribute" />: instances are
///         built at a throw site, as an argument to an exception factory, so a guard here would throw while a failure
///         is being reported and lose it (ADR-0064). The contract is the compiler's — the members are non-nullable
///         where a value is required, so a caller that cannot prove one is <c>CS8604</c> at build time. Comparing and
///         hashing stay on that footing: neither composes anything, so neither can fail while a failure is reported.
///     </para>
/// </remarks>
[BuiltOnTheFailurePath]
internal sealed class ConstraintClaim : IEquatable<ConstraintClaim> {

    #region Statics members declarations

    /// <summary>
    ///     Pairs <paramref name="constraint" /> with what it <paramref name="claims" />, written as a clause that
    ///     follows the constraint's name — "already caps the count at 3", not "caps".
    /// </summary>
    internal static ConstraintClaim Of(ConstraintCall constraint, string claims) {
        return new ConstraintClaim(constraint.ToString(), constraint, claims);
    }

    /// <summary>
    ///     Pairs a <paramref name="subject" /> the library phrases itself — "the contained value <c>"ABC"</c>" — with
    ///     what it <paramref name="claims" />. For a part of a shape rather than a call the caller wrote.
    /// </summary>
    internal static ConstraintClaim OfPhrase(string subject, string claims) {
        return new ConstraintClaim(subject, null, claims);
    }

    #endregion

    /// <summary>Determines whether two claims blame the same subject for the same thing.</summary>
    /// <param name="left">The first claim to compare.</param>
    /// <param name="right">The second claim to compare.</param>
    /// <returns><c>true</c> when both hold the same subject and claim, or both are <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator ==(ConstraintClaim? left, ConstraintClaim? right) {
        return Equals(left, right);
    }

    /// <summary>Determines whether two claims differ in their subject or in what they claim.</summary>
    /// <param name="left">The first claim to compare.</param>
    /// <param name="right">The second claim to compare.</param>
    /// <returns><c>true</c> when they differ, or exactly one is <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator !=(ConstraintClaim? left, ConstraintClaim? right) {
        return !Equals(left, right);
    }

    private ConstraintClaim(string subject, ConstraintCall? constraint, string claims) {
        Subject    = subject;
        Constraint = constraint;
        Claims     = claims;
    }

    /// <summary>What the subject claims about the values it admits, as a clause following its name.</summary>
    internal string Claims { get; }

    /// <summary>The constraint the subject is, when it is one; <c>null</c> for a phrase the library composed.</summary>
    internal ConstraintCall? Constraint { get; }

    /// <summary>The subject as it reads in the message — a constraint as the caller spelled it, or a phrase.</summary>
    internal string Subject { get; }

    /// <summary>The subject and its claim, as they read inside a conflict message.</summary>
    public override string ToString() {
        return $"{Subject} {Claims}";
    }

    /// <inheritdoc />
    /// <remarks>
    ///     The constraint is compared alongside the text, not merely implied by it: a claim whose subject is a
    ///     constraint and a phrase that happens to read the same are not the same value, because only the first can
    ///     be recognised as the constraint being applied.
    /// </remarks>
    public bool Equals(ConstraintClaim? other) {
        return other is not null
            && string.Equals(Subject, other.Subject, StringComparison.Ordinal)
            && string.Equals(Claims, other.Claims, StringComparison.Ordinal)
            && Constraint == other.Constraint;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is ConstraintClaim other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            int hash = StringComparer.Ordinal.GetHashCode(Subject);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Claims);

            return (hash * 397) ^ (Constraint?.GetHashCode() ?? 0);
        }
    }

}

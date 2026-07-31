#region Usings declarations

using System.Diagnostics;

#endregion

namespace JustDummies;

/// <summary>
///     A constraint as the caller spelled it — the declaring method's name and what it was given — rendered into the
///     form the diagnostics quote back: <c>Zero()</c>, <c>Between(0, 100)</c>, <c>OneOf(...)</c>. It is the unit a
///     conflict message names, so the punctuation that makes a constraint read as a call is written here once
///     instead of at every site that declares one.
/// </summary>
/// <remarks>
///     <para>
///         The two factories are the two things a generator can say about a constraint's arguments: it can render
///         them — none at all being the ordinary case of <see cref="Of" /> called with no argument — or it cannot,
///         and <see cref="OfElided" /> stands in for them. The second is a claim, not a shortcut: a pool of an
///         opaque <c>T</c> has arguments the library must <b>not</b> render, because their <c>ToString</c> belongs
///         to the caller and could be anything.
///     </para>
///     <para>
///         The rendering happens once, in the constructor, and <see cref="ToString" /> only reads it back. That is
///         deliberate rather than an optimization: a constraint is quoted while a
///         <see cref="ConflictingAnyConstraintException" /> is being built, and building an exception must never
///         throw (ADR-0045). Rendering when the constraint is declared — on the path that succeeds — leaves nothing
///         on the failing path that could fail in its turn.
///     </para>
///     <para>
///         Pass the name as <c>nameof(...)</c>. It ties the message to the API it names, so renaming the method
///         carries its diagnostics along, and a misspelling stops being a string literal that compiles.
///     </para>
///     <para>
///         Two constraints are equal when they read the same, ordinally. That is not a convenience: a spec compares
///         the constraint being applied against the one it already recorded to tell a harmless redeclaration
///         (<c>Between(0, 100)</c> twice, which returns the spec untouched) from a real conflict
///         (<c>Between(0, 100)</c> then <c>Between(5, 50)</c>). <c>==</c> is defined for the same reason rather
///         than for symmetry — those comparisons are written with it, and a reference type without it would compare
///         identities in silence, turning every redeclaration into a conflict.
///     </para>
///     <para>
///         Nothing checks the rendered arguments for <c>null</c>, and that is the compiler's job rather than an
///         omission: the parameter is a non-nullable <c>string[]</c>, so a caller that cannot prove an argument
///         non-null is CS8604 at build time, which this repository promotes to an error. A runtime guard would only
///         restate it, and could not be reached from C# without defeating the annotation it duplicates.
///     </para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
[ValueObject]
internal sealed class ConstraintCall : IEquatable<ConstraintCall> {

    #region Statics members declarations

    /// <summary>
    ///     A constraint whose arguments are rendered, including the common case of a constraint that takes none —
    ///     which is this factory called with no <paramref name="arguments" /> (<c>Zero()</c>, <c>Distinct()</c>).
    /// </summary>
    /// <param name="name">The declaring method's name, passed as <c>nameof(...)</c>.</param>
    /// <param name="arguments">The arguments, each already rendered by the declaring generator.</param>
    /// <returns>The constraint, rendered as <c>name(argument, argument)</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> or <paramref name="arguments" /> is <c>null</c>.</exception>
    internal static ConstraintCall Of(string name, params string[] arguments) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }
        if (arguments is null) { throw new ArgumentNullException(nameof(arguments)); }

        return new ConstraintCall(name, string.Join(", ", arguments));
    }

    /// <summary>
    ///     A constraint carrying arguments the library cannot render, an ellipsis standing in for them —
    ///     <c>OneOf(...)</c>, <c>Except(...)</c> over a pool whose element type is opaque to the library.
    /// </summary>
    /// <param name="name">The declaring method's name, passed as <c>nameof(...)</c>.</param>
    /// <returns>The constraint, rendered as <c>name(...)</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is <c>null</c>.</exception>
    internal static ConstraintCall OfElided(string name) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }

        return new ConstraintCall(name, "...");
    }

    #endregion

    /// <summary>
    ///     Determines whether two constraints read the same.
    /// </summary>
    /// <param name="left">The first constraint to compare.</param>
    /// <param name="right">The second constraint to compare.</param>
    /// <returns><c>true</c> when both render the same text, or both are <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator ==(ConstraintCall? left, ConstraintCall? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two constraints read differently.
    /// </summary>
    /// <param name="left">The first constraint to compare.</param>
    /// <param name="right">The second constraint to compare.</param>
    /// <returns><c>true</c> when they render different text, or exactly one is <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator !=(ConstraintCall? left, ConstraintCall? right) {
        return !Equals(left, right);
    }

    #region Fields declarations

    private readonly string _rendered;

    #endregion

    private ConstraintCall(string name, string arguments) {
        _rendered = name + "(" + arguments + ")";
    }

    /// <summary>
    ///     Returns the constraint as the caller spelled it. Total by construction: the text was built when the
    ///     constraint was declared, so quoting one into a message cannot fail.
    /// </summary>
    /// <returns>The rendered constraint, such as <c>Between(0, 100)</c>.</returns>
    public override string ToString() {
        return _rendered;
    }

    /// <inheritdoc />
    public bool Equals(ConstraintCall? other) {
        return other is not null && string.Equals(_rendered, other._rendered, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is ConstraintCall other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(_rendered);
    }

}

namespace JustDummies;

/// <summary>
///     Thrown at the moment a constraint is declared when it cannot be satisfied together with the constraints
///     already declared on the same generator — for example
///     <c>Any.String().WithLength(3).StartingWith("ORD-")</c>, where the prefix alone already requires 4
///     characters. Failing at declaration time, with a message that names both constraints, is a deliberate part of
///     the library's contract: a contradiction in a test's <c>Arrange</c> is a defect of the test, and it should
///     read as one — not surface later as a puzzling generation failure.
/// </summary>
public sealed class ConflictingAnyConstraintException : DummyException {

    #region Statics members declarations

    /// <summary>
    ///     Builds the exception for a constraint that no value of <paramref name="typeName" /> can satisfy — the
    ///     constraint is unsatisfiable on its own, before any other is considered.
    /// </summary>
    internal static ConflictingAnyConstraintException NoValueSatisfies(string applying, string typeName) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (typeName is null) { throw new ArgumentNullException(nameof(typeName)); }

        return Sentence(applying, $"no {typeName} value satisfies it");
    }

    /// <summary>
    ///     Builds the exception for a constraint that leaves no value available once the constraints already declared
    ///     are taken together, <paramref name="exhaustion" /> naming what exhausted the domain. The counterpart of
    ///     <see cref="NoValueSatisfies" />: nothing survives the combination, rather than the constraint admitting
    ///     nothing by itself.
    /// </summary>
    internal static ConflictingAnyConstraintException NoValueRemains(string applying, string exhaustion) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (exhaustion is null) { throw new ArgumentNullException(nameof(exhaustion)); }

        return Sentence(applying, exhaustion);
    }

    /// <summary>
    ///     Builds the exception for a constraint that <paramref name="existingConstraint" /> has already settled.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyDefined(string applying, string existingConstraint) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }

        return Sentence(applying, $"{existingConstraint} is already defined");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts an upper bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedAbove(string applying, string existingConstraint, string bound) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }
        if (bound is null) { throw new ArgumentNullException(nameof(bound)); }

        return Sentence(applying, $"{existingConstraint} already requires values less than or equal to {bound}");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts a lower bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedBelow(string applying, string existingConstraint, string bound) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }
        if (bound is null) { throw new ArgumentNullException(nameof(bound)); }

        return Sentence(applying, $"{existingConstraint} already requires values greater than or equal to {bound}");
    }

    /// <summary>
    ///     Writes the conflict sentence, which every factory above funnels through so its shape exists in exactly one
    ///     place — it was written out at each throw site before, and had that many chances to drift.
    /// </summary>
    /// <remarks>
    ///     Private on purpose. It names the grammar of the message, not a failure, so it is no one's factory: every
    ///     caller is a named case above, and a new case gets a name of its own rather than a free-form reason passed
    ///     through here. Its arguments are guarded by those callers, each of which the reflection convention in
    ///     JustDummies.UnitTests exercises (ADR-0045).
    /// </remarks>
    /// <param name="applying">The constraint being declared, as the caller spelled it.</param>
    /// <param name="reason">Why it cannot be applied, written without a final period.</param>
    private static ConflictingAnyConstraintException Sentence(string applying, string reason) {
        return new ConflictingAnyConstraintException($"Cannot apply {applying} because {reason}.");
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConflictingAnyConstraintException" /> class.
    /// </summary>
    /// <param name="message">A description naming the newly declared constraint and the declared constraint it conflicts with.</param>
    public ConflictingAnyConstraintException(string message) : base(message) { }

}

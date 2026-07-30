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
    ///     Builds the exception for a conflict described by <paramref name="reason" />, which completes the sentence
    ///     "Cannot apply X because …" and is written without its final period.
    /// </summary>
    /// <remarks>
    ///     Every factory here funnels through this one, so the sentence shape exists in exactly one place in the
    ///     library. It was written out at each throw site before, and the wording had that many chances to drift.
    /// </remarks>
    /// <param name="applying">The constraint being declared, as the caller spelled it.</param>
    /// <param name="reason">Why it cannot be applied, without a final period.</param>
    internal static ConflictingAnyConstraintException Because(string applying, string reason) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (reason is null) { throw new ArgumentNullException(nameof(reason)); }

        return new ConflictingAnyConstraintException($"Cannot apply {applying} because {reason}.");
    }

    /// <summary>
    ///     Builds the exception for a constraint that no value of <paramref name="typeName" /> can satisfy.
    /// </summary>
    internal static ConflictingAnyConstraintException NoValueSatisfies(string applying, string typeName) {
        if (typeName is null) { throw new ArgumentNullException(nameof(typeName)); }

        return Because(applying, $"no {typeName} value satisfies it");
    }

    /// <summary>
    ///     Builds the exception for a constraint that <paramref name="existingConstraint" /> has already settled.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyDefined(string applying, string existingConstraint) {
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }

        return Because(applying, $"{existingConstraint} is already defined");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts an upper bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedAbove(string applying, string existingConstraint, string bound) {
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }
        if (bound is null) { throw new ArgumentNullException(nameof(bound)); }

        return Because(applying, $"{existingConstraint} already requires values less than or equal to {bound}");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts a lower bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedBelow(string applying, string existingConstraint, string bound) {
        if (existingConstraint is null) { throw new ArgumentNullException(nameof(existingConstraint)); }
        if (bound is null) { throw new ArgumentNullException(nameof(bound)); }

        return Because(applying, $"{existingConstraint} already requires values greater than or equal to {bound}");
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConflictingAnyConstraintException" /> class.
    /// </summary>
    /// <param name="message">A description naming the newly declared constraint and the declared constraint it conflicts with.</param>
    public ConflictingAnyConstraintException(string message) : base(message) { }

}

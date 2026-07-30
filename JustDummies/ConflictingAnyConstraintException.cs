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
    internal static ConflictingAnyConstraintException NoValueSatisfies(ConstraintCall applying, string typeName) {
        return Sentence(applying, $"no {typeName} value satisfies it");
    }

    /// <summary>
    ///     Builds the exception for a constraint that leaves no value available once the constraints already declared
    ///     are taken together, <paramref name="exhaustion" /> naming what exhausted the domain. The counterpart of
    ///     <see cref="NoValueSatisfies" />: nothing survives the combination, rather than the constraint admitting
    ///     nothing by itself.
    /// </summary>
    internal static ConflictingAnyConstraintException NoValueRemains(ConstraintCall applying, string exhaustion) {
        return Sentence(applying, exhaustion);
    }

    /// <summary>
    ///     Builds the exception for a constraint that <paramref name="existingConstraint" /> has already settled.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyDefined(ConstraintCall applying, ConstraintCall existingConstraint) {
        return Sentence(applying, $"{existingConstraint} is already defined");
    }

    /// <summary>
    ///     Builds the exception for two constraints that cannot hold together, blaming
    ///     <paramref name="culprit" /> — unless it is the one being applied, in which case
    ///     <paramref name="otherwise" /> is blamed instead.
    /// </summary>
    /// <remarks>
    ///     The choice is the whole point of this factory. A conflict always has two sides, and the message must name
    ///     the side the caller did NOT just write: telling someone that the constraint they are applying conflicts
    ///     with itself explains nothing. Every conflict between a fixed count or length and a bound is this shape, so
    ///     the rule is stated once here rather than re-derived at each throw site.
    /// </remarks>
    internal static ConflictingAnyConstraintException Contradicts(ConstraintCall applying, ConstraintClaim culprit, ConstraintClaim otherwise) {
        ConstraintClaim blamed = applying == culprit.Constraint ? otherwise : culprit;

        return Sentence(applying, blamed.ToString());
    }

    /// <summary>
    ///     Builds the exception for an allow-list none of whose values survives every constraint already declared,
    ///     <paramref name="exhaustion" /> naming what rejected them. The allow-list counterpart of
    ///     <see cref="NoValueRemains" />: the caller supplied the values, so the failure names what turned them all
    ///     away rather than what the domain could not produce.
    /// </summary>
    internal static ConflictingAnyConstraintException NoPooledValueSurvives(ConstraintCall applying, string exhaustion) {
        return Sentence(applying, exhaustion);
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts a value already pinned.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyPinned(ConstraintCall applying, ConstraintCall pinningConstraint, string value) {
        return Sentence(applying, $"{pinningConstraint} already pins the value to {value}");
    }

    /// <summary>
    ///     Builds the exception for a pinned value the exclusions declared alongside it forbid.
    /// </summary>
    internal static ConflictingAnyConstraintException PinnedValueExcluded(ConstraintCall applying, ConstraintCall pinningConstraint, string value) {
        return Sentence(applying, $"{pinningConstraint} already pins the value to {value}, which the exclusions forbid");
    }

    /// <summary>
    ///     Builds the exception for a pinned value the allow-list declared alongside it does not admit.
    /// </summary>
    internal static ConflictingAnyConstraintException PinnedValueNotAllowed(ConstraintCall applying, ConstraintCall pinningConstraint, string value, ConstraintCall allowingConstraint) {
        return Sentence(applying, $"{pinningConstraint} already pins the value to {value}, which {allowingConstraint} does not allow");
    }

    /// <summary>
    ///     Builds the exception for combinations asked of an enum the runtime would not recognise them on.
    /// </summary>
    internal static ConflictingAnyConstraintException EnumIsNotFlags(ConstraintCall applying, string enumName) {
        return Sentence(applying, $"{enumName} is not declared [Flags]: OR-ing its members would produce values the type does not define");
    }

    /// <summary>
    ///     Builds the exception for an enum with more combinable members than the library will enumerate.
    /// </summary>
    internal static ConflictingAnyConstraintException TooManyCombinableMembers(ConstraintCall applying, string enumName, string declared, string maximum) {
        return Sentence(applying, $"{enumName} declares {declared} non-zero members, more than the {maximum} whose combinations can be enumerated. " +
                                  "Draw from an explicit set with OneOf(...) instead");
    }

    /// <summary>
    ///     Builds the exception for elements required to be contained that cannot fit the capacity already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException ContainedElementsDoNotFit(ConstraintCall applying, string required, string capacity) {
        return Sentence(applying, $"{required} required to be contained cannot fit in a collection of at most {capacity}");
    }

    /// <summary>
    ///     Builds the exception for a second, different equality on a collection already required to be distinct. One
    ///     collection is distinct under one equality, so the two cannot both be honoured.
    /// </summary>
    internal static ConflictingAnyConstraintException ComparerAlreadyDefined(ConstraintCall applying) {
        return Sentence(applying, $"a different comparer is already defined by an earlier {applying}");
    }

    /// <summary>
    ///     Builds the exception for a value required to be contained twice in a collection required to be distinct.
    /// </summary>
    internal static ConflictingAnyConstraintException DuplicateInDistinctCollection(ConstraintCall applying, string value) {
        return Sentence(applying, $"a distinct collection cannot contain {value} more than once");
    }

    /// <summary>
    ///     Builds the exception for more distinct elements than the element generator has distinct values to give.
    /// </summary>
    internal static ConflictingAnyConstraintException DistinctElementsExceedCardinality(ConstraintCall applying, string required, string cardinality) {
        return Sentence(applying, $"{required} required to be distinct exceed the {cardinality} distinct value(s) the element generator can produce");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts an upper bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedAbove(ConstraintCall applying, ConstraintCall existingConstraint, string bound) {
        return Sentence(applying, $"{existingConstraint} already requires values less than or equal to {bound}");
    }

    /// <summary>
    ///     Builds the exception for a constraint that contradicts a lower bound already declared.
    /// </summary>
    internal static ConflictingAnyConstraintException AlreadyBoundedBelow(ConstraintCall applying, ConstraintCall existingConstraint, string bound) {
        return Sentence(applying, $"{existingConstraint} already requires values greater than or equal to {bound}");
    }

    /// <summary>
    ///     Writes the conflict sentence, which every factory above funnels through so its shape exists in exactly one
    ///     place — it was written out at each throw site before, and had that many chances to drift.
    /// </summary>
    /// <remarks>
    ///     Private on purpose. It names the grammar of the message, not a failure, so it is no one's factory: every
    ///     caller is a named case above, and a new case gets a name of its own rather than a free-form reason passed
    ///     through here.
    ///     <para>
    ///         Nothing here guards its arguments, and that is the rule rather than an omission: building an exception
    ///         must never throw. A guard would replace the failure being reported with a failure about reporting it,
    ///         losing the original. ADR-0045 exempts exception types for exactly that reason, and the reflection
    ///         convention that enforces it skips them outright. The contract is the compiler's instead — these
    ///         parameters are non-nullable, so a caller that cannot prove a value is CS8604 at build time, which is
    ///         how the one nullable constraint name in the interval specs was found.
    ///     </para>
    ///     <para>
    ///         Interpolating a <see cref="ConstraintCall" /> here calls its <c>ToString</c>, which reads back text
    ///         rendered when the constraint was declared rather than composing any. The rule above therefore holds
    ///         for the constraints too, by construction rather than by inspection.
    ///     </para>
    /// </remarks>
    /// <param name="applying">The constraint being declared, as the caller spelled it.</param>
    /// <param name="reason">Why it cannot be applied, written without a final period.</param>
    private static ConflictingAnyConstraintException Sentence(ConstraintCall applying, string reason) {
        return new ConflictingAnyConstraintException($"Cannot apply {applying} because {reason}.");
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConflictingAnyConstraintException" /> class.
    /// </summary>
    /// <param name="message">A description naming the newly declared constraint and the declared constraint it conflicts with.</param>
    public ConflictingAnyConstraintException(string message) : base(message) { }

}

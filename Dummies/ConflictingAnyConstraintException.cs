namespace Dummies;

/// <summary>
///     Thrown at the moment a constraint is declared when it cannot be satisfied together with the constraints
///     already declared on the same generator — for example
///     <c>Any.String().WithLength(3).StartingWith("ORD-")</c>, where the prefix alone already requires 4
///     characters. Failing at declaration time, with a message that names both constraints, is a deliberate part of
///     the library's contract: a contradiction in a test's <c>Arrange</c> is a defect of the test, and it should
///     read as one — not surface later as a puzzling generation failure.
/// </summary>
public sealed class ConflictingAnyConstraintException : AnyException {

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConflictingAnyConstraintException" /> class.
    /// </summary>
    /// <param name="message">A description naming the newly declared constraint and the declared constraint it conflicts with.</param>
    public ConflictingAnyConstraintException(string message) : base(message) { }

}

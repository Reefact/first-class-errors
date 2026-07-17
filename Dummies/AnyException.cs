namespace Dummies;

/// <summary>
///     Base class of every exception the library throws on its own behalf, so a caller can catch "anything Dummies
///     rejected" with a single clause. Concrete cases:
///     <see cref="ConflictingAnyConstraintException" /> when two declared constraints cannot be satisfied together,
///     and <see cref="AnyGenerationException" /> when a generation fails even though the constraints were accepted.
/// </summary>
public abstract class AnyException : Exception {

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyException" /> class.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    protected AnyException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnyException" /> class wrapping an underlying failure.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying failure.</param>
    protected AnyException(string message, Exception innerException) : base(message, innerException) { }

}

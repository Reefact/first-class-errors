namespace DiagnosableExceptions;

/// <summary>
///     Represents an error that occurs within the infrastructure layer of the application.
/// </summary>
/// <remarks>
///     This class is a specialized type of <see cref="Error" /> that includes additional information
///     about whether the error is transient, allowing for more granular handling of infrastructure-related issues.
/// </remarks>
public class InfrastructureError : Error {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureError" /> class.
    /// </summary>
    /// <param name="code">The error code representing the type of the error.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="direction">The direction of the interaction associated with the error.</param>
    /// <param name="transience">The transience level of the error, indicating whether it is transient or non-transient.</param>
    /// <param name="shortMessage">An optional short message providing a concise description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the error context using an <see cref="ErrorContextBuilder" />.
    /// </param>
    public InfrastructureError(ErrorCode code, string detailedMessage, InteractionDirection direction, Transience transience, string? shortMessage = null, Action<ErrorContextBuilder>? configureContext = null) : base(code, detailedMessage, shortMessage, configureContext) {
        Direction  = direction;
        Transience = transience;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureError" /> class with the specified error code, detailed
    ///     message,
    ///     interaction direction, transience, a collection of inner errors, an optional short message, and an optional context
    ///     configuration.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="direction">
    ///     The <see cref="InteractionDirection" /> indicating the direction of the interaction associated
    ///     with the error.
    /// </param>
    /// <param name="transience">The <see cref="Transience" /> indicating whether the error is transient or non-transient.</param>
    /// <param name="innerErrors">
    ///     A collection of <see cref="Error" /> instances representing the inner errors associated with
    ///     this error.
    /// </param>
    /// <param name="shortMessage">An optional short message providing a concise description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContextBuilder" /> for additional error context.
    /// </param>
    public InfrastructureError(ErrorCode code, string detailedMessage, InteractionDirection direction, Transience transience, IEnumerable<Error> innerErrors, string? shortMessage = null, Action<ErrorContextBuilder>? configureContext = null) : base(code, detailedMessage, shortMessage, innerErrors, configureContext) {
        Direction  = direction;
        Transience = transience;
    }

    #endregion

    /// <summary>
    ///     Gets a value indicating whether the error is transient and can potentially be retried.
    /// </summary>
    /// <remarks>
    ///     A transient error is typically a temporary issue, such as a network glitch or a service unavailability,
    ///     which might resolve itself after some time or upon retrying the operation.
    /// </remarks>
    public Transience Transience { get; }

    /// <summary>
    ///     Gets the architectural direction of the infrastructure interaction associated with this error.
    /// </summary>
    /// <remarks>
    ///     The direction indicates whether the error originates from an incoming request, an outgoing dependency, or if the
    ///     direction is unknown.
    /// </remarks>
    public InteractionDirection Direction { get; }

    /// <inheritdoc />
    public override DiagnosableException ToException() {
        return new InfrastructureException(this);
    }

}
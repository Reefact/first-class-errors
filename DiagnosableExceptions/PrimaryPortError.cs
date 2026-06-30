namespace DiagnosableExceptions;

/// <summary>
///     Represents an infrastructure error occurring on an incoming interaction
///     (Primary Port in hexagonal architecture).
/// </summary>
/// <remarks>
///     A <see cref="PrimaryPortError" /> models errors that originate from the system boundary
///     when handling an incoming request (e.g., HTTP request, message consumption, CLI input).
///     It enforces that all nested infrastructure errors belong to the same direction (Incoming),
///     while allowing domain errors to be included as part of the diagnostic context.
/// </remarks>
public class PrimaryPortError : InfrastructureError {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="PrimaryPortError" /> class with the specified error code, detailed
    ///     message, transience,
    ///     optional short message, and an optional context configuration action.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="detailedMessage">A detailed description of the error.</param>
    /// <param name="transience">The <see cref="Transience" /> indicating whether the error is transient or non-transient.</param>
    /// <param name="shortMessage">An optional short message providing a brief description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the error context using an <see cref="ErrorContextBuilder" />.
    /// </param>
    public PrimaryPortError(ErrorCode                    code,
                            string                       detailedMessage,
                            Transience                   transience,
                            string?                      shortMessage     = null,
                            Action<ErrorContextBuilder>? configureContext = null)
        : base(code, detailedMessage, InteractionDirection.Incoming, transience, shortMessage, configureContext) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PrimaryPortError" /> class with the specified error details.
    /// </summary>
    /// <param name="code">The unique <see cref="ErrorCode" /> representing the error.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="innerErrors">
    ///     The collection of <see cref="PrimaryPortInnerErrors" /> providing additional context about
    ///     the error.
    /// </param>
    /// <param name="shortMessage">An optional short message summarizing the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContextBuilder" /> for additional error context.
    /// </param>
    public PrimaryPortError(ErrorCode                    code,
                            string                       detailedMessage,
                            PrimaryPortInnerErrors       innerErrors,
                            string?                      shortMessage     = null,
                            Action<ErrorContextBuilder>? configureContext = null)
        : base(code, detailedMessage, InteractionDirection.Incoming, innerErrors.ComputeTransience(), innerErrors.ToList(), shortMessage, configureContext) { }

    #endregion

    /// <inheritdoc />
    public override DiagnosableException ToException() {
        return new PrimaryPortException(this);
    }

}
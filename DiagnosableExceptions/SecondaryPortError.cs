namespace DiagnosableExceptions;

/// <summary>
///     Represents an infrastructure error occurring on an outgoing interaction
///     (Secondary Port in hexagonal architecture).
/// </summary>
/// <remarks>
///     A <see cref="SecondaryPortError" /> models errors that originate from calls to external
///     dependencies (e.g., HTTP services, databases, message brokers).
///     It enforces that all nested infrastructure errors belong to the same direction (Outgoing),
///     while allowing domain errors to be included as part of the diagnostic context.
/// </remarks>
public class SecondaryPortError : InfrastructureError {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="SecondaryPortError" /> class with the specified error code, detailed
    ///     message, transience,
    ///     optional short message, and an optional context configuration action.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="detailedMessage">A detailed description of the error.</param>
    /// <param name="transience">The <see cref="Transience" /> indicating whether the error is transient or non-transient.</param>
    /// <param name="shortMessage">An optional short message providing a brief description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContextBuilder" /> for additional error context.
    /// </param>
    public SecondaryPortError(ErrorCode                    code,
                              string                       detailedMessage,
                              Transience                   transience,
                              string?                      shortMessage     = null,
                              Action<ErrorContextBuilder>? configureContext = null)
        : base(code, detailedMessage, InteractionDirection.Outgoing, transience, shortMessage, configureContext) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SecondaryPortError" /> class with the specified error details.
    /// </summary>
    /// <param name="code">The unique <see cref="ErrorCode" /> identifying the error.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="innerErrors">The collection of <see cref="SecondaryPortInnerErrors" /> associated with this error.</param>
    /// <param name="shortMessage">An optional short message providing a brief description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContextBuilder" /> for additional error context.
    /// </param>
    public SecondaryPortError(ErrorCode                    code,
                              string                       detailedMessage,
                              SecondaryPortInnerErrors     innerErrors,
                              string?                      shortMessage     = null,
                              Action<ErrorContextBuilder>? configureContext = null)
        : base(code, detailedMessage, InteractionDirection.Outgoing, innerErrors.ComputeTransience(), innerErrors.ToList(), shortMessage, configureContext) { }

    #endregion

    /// <inheritdoc />
    public override DiagnosableException ToException() {
        return new SecondaryPortException(this);
    }

}
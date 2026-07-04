namespace FirstClassErrors;

/// <summary>
///     Represents an error that occurs within the infrastructure layer of the application.
/// </summary>
/// <remarks>
///     This class is a specialized type of <see cref="Error" /> that includes additional information
///     about the interaction direction and whether the error is transient, allowing for more granular handling of
///     infrastructure-related issues. Instances are created through the staged builder
///     (<see cref="Create(ErrorCode, string, InteractionDirection, Transience, Action{ErrorContextBuilder})" />).
/// </remarks>
public class InfrastructureError : Error {

    #region Constructors declarations

    internal InfrastructureError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, InteractionDirection direction, Transience transience, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) {
        Direction  = direction;
        Transience = transience;
    }

    internal InfrastructureError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, InteractionDirection direction, Transience transience, IEnumerable<Error> innerErrors, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext) {
        Direction  = direction;
        Transience = transience;
    }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Begins the creation of an <see cref="InfrastructureError" /> with its mandatory internal information.
    /// </summary>
    /// <param name="code">The error code representing the type of the error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="direction">The direction of the interaction associated with the error.</param>
    /// <param name="transience">The transience level of the error.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<InfrastructureError> Create(ErrorCode code, string diagnosticMessage, InteractionDirection direction, Transience transience, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<InfrastructureError>((shortMessage, detailedMessage) =>
                                                               new InfrastructureError(code, safeDiagnosticMessage, shortMessage, detailedMessage, direction, transience, configureContext));
    }

    /// <summary>
    ///     Begins the creation of an <see cref="InfrastructureError" /> that collects a set of inner errors.
    /// </summary>
    /// <param name="code">The error code representing the type of the error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="direction">The direction of the interaction associated with the error.</param>
    /// <param name="transience">The transience level of the error.</param>
    /// <param name="innerErrors">A collection of inner <see cref="Error" /> instances associated with this error.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<InfrastructureError> Create(ErrorCode code, string diagnosticMessage, InteractionDirection direction, Transience transience, IEnumerable<Error> innerErrors, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<InfrastructureError>((shortMessage, detailedMessage) =>
                                                               new InfrastructureError(code, safeDiagnosticMessage, shortMessage, detailedMessage, direction, transience, innerErrors, configureContext));
    }

    #endregion

    /// <summary>
    ///     Gets the transience classification of the error: <see cref="FirstClassErrors.Transience.Transient" />,
    ///     <see cref="FirstClassErrors.Transience.NonTransient" />, or
    ///     <see cref="FirstClassErrors.Transience.Unknown" /> when it cannot be determined.
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

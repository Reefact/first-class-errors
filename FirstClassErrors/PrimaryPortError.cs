namespace FirstClassErrors;

/// <summary>
///     Represents an infrastructure error occurring on an incoming interaction
///     (Primary Port in hexagonal architecture).
/// </summary>
/// <remarks>
///     A <see cref="PrimaryPortError" /> models errors that originate from the system boundary
///     when handling an incoming request (e.g., HTTP request, message consumption, CLI input).
///     It enforces that all nested infrastructure errors belong to the same direction (Incoming),
///     while allowing domain errors to be included as part of the diagnostic context. Instances are created through the
///     staged builder (<see cref="Create(ErrorCode, string, Transience, Action{ErrorContextBuilder})" />).
/// </remarks>
public class PrimaryPortError : InfrastructureError {

    #region Constructors declarations

    internal PrimaryPortError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, Transience transience, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, InteractionDirection.Incoming, transience, configureContext) { }

    internal PrimaryPortError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, PrimaryPortInnerErrors innerErrors, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, InteractionDirection.Incoming, innerErrors.ComputeTransience(), innerErrors.ToList(), configureContext) { }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Begins the creation of a <see cref="PrimaryPortError" /> with its mandatory internal information.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="transience">The <see cref="Transience" /> indicating whether the error is transient or non-transient.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<PrimaryPortError> Create(ErrorCode code, string diagnosticMessage, Transience transience, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<PrimaryPortError>((shortMessage, detailedMessage) =>
                                                            new PrimaryPortError(code, safeDiagnosticMessage, shortMessage, detailedMessage, transience, configureContext));
    }

    /// <summary>
    ///     Begins the creation of a <see cref="PrimaryPortError" /> that aggregates a set of inner errors; its transience is
    ///     computed from them.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="innerErrors">The <see cref="PrimaryPortInnerErrors" /> providing additional context about the error.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<PrimaryPortError> Create(ErrorCode code, string diagnosticMessage, PrimaryPortInnerErrors innerErrors, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<PrimaryPortError>((shortMessage, detailedMessage) =>
                                                            new PrimaryPortError(code, safeDiagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext));
    }

    #endregion

    /// <inheritdoc />
    public override DiagnosableException ToException() {
        return new PrimaryPortException(this);
    }

}

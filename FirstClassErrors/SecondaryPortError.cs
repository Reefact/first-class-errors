namespace FirstClassErrors;

/// <summary>
///     Represents an infrastructure error occurring on an outgoing interaction
///     (Secondary Port in hexagonal architecture).
/// </summary>
/// <remarks>
///     A <see cref="SecondaryPortError" /> models errors that originate from calls to external
///     dependencies (e.g., HTTP services, databases, message brokers).
///     It enforces that all nested infrastructure errors belong to the same direction (Outgoing),
///     while allowing domain errors to be included as part of the diagnostic context. Instances are created through the
///     staged builder (<see cref="Create(ErrorCode, string, Transience, Action{ErrorContextBuilder})" />).
/// </remarks>
public class SecondaryPortError : InfrastructureError {

    #region Constructors declarations

    internal SecondaryPortError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, Transience transience, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, InteractionDirection.Outgoing, transience, configureContext) { }

    internal SecondaryPortError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, SecondaryPortInnerErrors innerErrors, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, InteractionDirection.Outgoing, innerErrors.ComputeTransience(), innerErrors.ToList(), configureContext) { }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Begins the creation of a <see cref="SecondaryPortError" /> with its mandatory internal information.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="transience">The <see cref="Transience" /> indicating whether the error is transient or non-transient.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<SecondaryPortError> Create(ErrorCode code, string diagnosticMessage, Transience transience, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<SecondaryPortError>((shortMessage, detailedMessage) =>
                                                              new SecondaryPortError(code, safeDiagnosticMessage, shortMessage, detailedMessage, transience, configureContext));
    }

    /// <summary>
    ///     Begins the creation of a <see cref="SecondaryPortError" /> that aggregates a set of inner errors; its transience is
    ///     computed from them.
    /// </summary>
    /// <param name="code">The unique <see cref="ErrorCode" /> identifying the error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="innerErrors">The <see cref="SecondaryPortInnerErrors" /> associated with this error.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<SecondaryPortError> Create(ErrorCode code, string diagnosticMessage, SecondaryPortInnerErrors innerErrors, Action<ErrorContextBuilder>? configureContext = null) {
        string safeDiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));

        return new PublicMessageStage<SecondaryPortError>((shortMessage, detailedMessage) =>
                                                              new SecondaryPortError(code, safeDiagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext));
    }

    #endregion

    /// <inheritdoc />
    public override DiagnosableException ToException() {
        return new SecondaryPortException(this);
    }

}

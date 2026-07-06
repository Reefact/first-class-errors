namespace FirstClassErrors;

/// <summary>
///     Represents an error specific to the domain layer of an application.
/// </summary>
/// <remarks>
///     This class is a specialization of the <see cref="Error" /> class, designed to handle domain-related errors.
///     Instances are created through the staged builder: <see cref="Create(ErrorCode, string, Action{ErrorContextBuilder})" />
///     captures the mandatory internal information (code and diagnostic message) and returns a
///     <see cref="PublicMessageStage{TError}" />; the final error is produced by
///     <see cref="PublicMessageStage{TError}.WithPublicMessage(string, string?)" />.
/// </remarks>
public class DomainError : Error {

    #region Constructors declarations

    internal DomainError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, configureContext) { }

    internal DomainError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, DomainError innerError, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, innerError, configureContext) { }

    internal DomainError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, IEnumerable<DomainError> innerErrors, Action<ErrorContextBuilder>? configureContext = null)
        : base(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext) { }

    #endregion

    #region Statics members declarations

    /// <summary>
    ///     Begins the creation of a <see cref="DomainError" /> with its mandatory internal information.
    /// </summary>
    /// <param name="code">The error code that identifies the type of error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<DomainError> Create(ErrorCode code, string diagnosticMessage, Action<ErrorContextBuilder>? configureContext = null) {
        return new PublicMessageStage<DomainError>((shortMessage, detailedMessage) =>
                                                       new DomainError(code, diagnosticMessage, shortMessage, detailedMessage, configureContext));
    }

    /// <summary>
    ///     Begins the creation of a <see cref="DomainError" /> that wraps a single inner domain error.
    /// </summary>
    /// <param name="code">The error code that identifies the type of error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="innerError">The inner <see cref="DomainError" /> that provides additional context.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<DomainError> Create(ErrorCode code, string diagnosticMessage, DomainError innerError, Action<ErrorContextBuilder>? configureContext = null) {
        return new PublicMessageStage<DomainError>((shortMessage, detailedMessage) =>
                                                       new DomainError(code, diagnosticMessage, shortMessage, detailedMessage, innerError, configureContext));
    }

    /// <summary>
    ///     Begins the creation of an aggregated <see cref="DomainError" /> that collects several inner domain errors.
    /// </summary>
    /// <param name="code">The error code that identifies the type of error.</param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message (for logs, support and developers).</param>
    /// <param name="innerErrors">A collection of <see cref="DomainError" /> instances representing the inner errors.</param>
    /// <param name="configureContext">An optional action to configure additional error context.</param>
    /// <returns>A <see cref="PublicMessageStage{TError}" /> to supply the public messages and finalize the error.</returns>
    public static PublicMessageStage<DomainError> Create(ErrorCode code, string diagnosticMessage, IEnumerable<DomainError> innerErrors, Action<ErrorContextBuilder>? configureContext = null) {
        return new PublicMessageStage<DomainError>((shortMessage, detailedMessage) =>
                                                       new DomainError(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors, configureContext));
    }

    #endregion

    /// <summary>
    ///     Converts the current <see cref="DomainError" /> instance into a <see cref="DomainException" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="DomainException" /> that encapsulates the current <see cref="DomainError" />.
    /// </returns>
    /// <remarks>
    ///     This method facilitates the transformation of a domain-specific error into an exception,
    ///     enabling it to be thrown and handled as part of exception-based error handling mechanisms.
    /// </remarks>
    public override DiagnosableException ToException() {
        return new DomainException(this);
    }

}

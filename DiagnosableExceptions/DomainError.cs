namespace DiagnosableExceptions;

/// <summary>
///     Represents an error specific to the domain layer of an application.
/// </summary>
/// <remarks>
///     This class is a specialization of the <see cref="Error" /> class, designed to handle domain-related errors.
///     It provides constructors to define the error code, detailed message, optional short message,
///     and additional context or inner errors for enhanced diagnostic capabilities.
/// </remarks>
public sealed class DomainError : Error {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainError" /> class with the specified error code, detailed message,
    ///     optional short message, and an optional action to configure the error context.
    /// </summary>
    /// <param name="code">The error code that identifies the type of error.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="shortMessage">An optional short message providing a concise description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContextBuilder" /> for additional error context.
    /// </param>
    public DomainError(ErrorCode code, string detailedMessage, string? shortMessage = null, Action<ErrorContextBuilder>? configureContext = null) : base(code, detailedMessage, shortMessage, configureContext) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainError" /> class with the specified error code, detailed message,
    ///     inner error, optional short message, and an optional context configuration action.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error.</param>
    /// <param name="detailedMessage">A detailed description of the error.</param>
    /// <param name="innerError">The inner <see cref="DomainError" /> that provides additional context for this error.</param>
    /// <param name="shortMessage">An optional short message summarizing the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure additional context for the error using an <see cref="ErrorContextBuilder" />.
    /// </param>
    public DomainError(ErrorCode code, string detailedMessage, DomainError innerError, string? shortMessage = null, Action<ErrorContextBuilder>? configureContext = null) : base(code, detailedMessage, shortMessage, innerError, configureContext) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainError" /> class with the specified error code, detailed message,
    ///     a collection of inner domain errors, an optional short message, and an optional context configuration action.
    /// </summary>
    /// <param name="code">The <see cref="ErrorCode" /> representing the specific error condition.</param>
    /// <param name="detailedMessage">A detailed message describing the error.</param>
    /// <param name="innerErrors">A collection of <see cref="DomainError" /> instances representing inner errors.</param>
    /// <param name="shortMessage">An optional short message providing a brief description of the error.</param>
    /// <param name="configureContext">
    ///     An optional action to configure additional context for the error using an <see cref="ErrorContextBuilder" />.
    /// </param>
    public DomainError(ErrorCode code, string detailedMessage, IEnumerable<DomainError> innerErrors, string? shortMessage = null, Action<ErrorContextBuilder>? configureContext = null) : base(code, detailedMessage, shortMessage, innerErrors, configureContext) { }

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
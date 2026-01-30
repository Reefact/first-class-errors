namespace DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from infrastructure concerns, such as databases, filesystems,
///     network calls, or external APIs.
/// </summary>
public abstract class InfrastructureException : DiagnosableException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected InfrastructureException(string errorCode,
                                      string errorMessage)
        : base(errorCode, errorMessage) { }

    /// <inheritdoc />
    protected InfrastructureException(string           errorCode,
                                      ErrorDescription errorDescription)
        : base(errorCode, errorDescription) { }

    /// <inheritdoc />
    protected InfrastructureException(string    errorCode,
                                      string    errorMessage,
                                      Exception innerException)
        : base(errorCode, errorMessage, innerException) { }

    /// <inheritdoc />
    protected InfrastructureException(string           errorCode,
                                      ErrorDescription errorDescription,
                                      Exception        innerException)
        : base(errorCode, errorDescription, innerException) { }

    /// <inheritdoc />
    protected InfrastructureException(string                 errorCode,
                                      string                 errorMessage,
                                      IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorMessage, innerExceptions) { }

    /// <inheritdoc />
    protected InfrastructureException(string                 errorCode,
                                      ErrorDescription       errorDescription,
                                      IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorDescription, innerExceptions) { }

    #endregion

    /// <summary>
    ///     Indicates whether this infrastructure error is considered transient.
    ///     <list type="bullet">
    ///         <item><c>true</c>: retry may succeed without intervention</item>
    ///         <item><c>false</c>: retry will not help</item>
    ///         <item><c>null</c>: unknown / not specified</item>
    ///     </list>
    /// </summary>
    public bool? IsTransient { get; set; }

}
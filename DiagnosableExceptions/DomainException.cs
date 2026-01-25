namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from
///     domain rules, business processes, or invariant violations.
///     These exceptions are always non-transient and should be interpreted
///     strictly in the context of the business model.
/// </summary>
public abstract class DomainException : DiagnosableException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected DomainException(string errorCode,
                              string errorMessage)
        : base(errorCode, errorMessage) { }

    /// <inheritdoc />
    protected DomainException(string           errorCode,
                              ErrorDescription errorDescription)
        : base(errorCode, errorDescription) { }

    /// <inheritdoc />
    protected DomainException(string    errorCode,
                              string    errorMessage,
                              Exception innerException)
        : base(errorCode, errorMessage, innerException) { }

    /// <inheritdoc />
    protected DomainException(string           errorCode,
                              ErrorDescription errorDescription,
                              Exception        innerException)
        : base(errorCode, errorDescription, innerException) { }

    /// <inheritdoc />
    protected DomainException(string                 errorCode,
                              string                 errorMessage,
                              IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorMessage, innerExceptions) { }

    /// <inheritdoc />
    protected DomainException(string                 errorCode,
                              ErrorDescription       errorDescription,
                              IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorDescription, innerExceptions) { }

    #endregion

}
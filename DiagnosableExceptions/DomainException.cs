namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from
///     domain rules, business processes, or invariant violations.
///     These exceptions are always non-transient and should be interpreted
///     strictly in the context of the business model.
/// </summary>
public class DomainException : DiagnosableException {

    #region Constructors declarations

    /// <inheritdoc />
    protected DomainException(string           errorCode,
                              ErrorDescription description,
                              object?          errorDetails = null)
        : base(errorCode, description, errorDetails) { }

    /// <inheritdoc />
    protected DomainException(string           errorCode,
                              ErrorDescription description,
                              Exception        cause,
                              object?          errorDetails = null)
        : base(errorCode, description, cause, errorDetails) { }

    /// <inheritdoc />
    protected DomainException(string                 errorCode,
                              ErrorDescription       description,
                              IEnumerable<Exception> causes,
                              object?                errorDetails = null)
        : base(errorCode, description, causes, errorDetails) { }

    #endregion

}
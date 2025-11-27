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
    protected DomainException(string        errorCode,
                              string        description,
                              ErrorContext? context = null)
        : base(errorCode, description, context) { }

    /// <inheritdoc />
    protected DomainException(string        errorCode,
                              string        description,
                              Exception     cause,
                              ErrorContext? context = null)
        : base(errorCode, description, cause, context) { }

    /// <inheritdoc />
    protected DomainException(string                 errorCode,
                              string                 description,
                              IEnumerable<Exception> causes,
                              ErrorContext?          context = null)
        : base(errorCode, description, causes, context) { }

    #endregion

}
namespace DiagnosableExceptions;

/// <summary>
///     Represents an exception that originates from a primary adapter in the infrastructure layer.
/// </summary>
/// <remarks>
///     This exception serves as a specialized type of <see cref="InfrastructureException" />, designed to handle errors
///     specific to primary adapters.
/// </remarks>
public abstract class PrimaryAdapterException : InfrastructureException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected PrimaryAdapterException(string errorCode,
                                      string errorMessage)
        : base(errorCode, errorMessage) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string           errorCode,
                                      ErrorDescription errorDescription)
        : base(errorCode, errorDescription) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string    errorCode,
                                      string    errorMessage,
                                      Exception innerException)
        : base(errorCode, errorMessage, innerException) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string           errorCode,
                                      ErrorDescription errorDescription,
                                      Exception        innerException)
        : base(errorCode, errorDescription, innerException) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string                 errorCode,
                                      string                 errorMessage,
                                      IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorMessage, innerExceptions) { }

    /// <inheritdoc />
    protected PrimaryAdapterException(string                 errorCode,
                                      ErrorDescription       errorDescription,
                                      IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorDescription, innerExceptions) { }

    #endregion

}
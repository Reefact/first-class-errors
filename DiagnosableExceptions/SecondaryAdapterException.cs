namespace DiagnosableExceptions;

public abstract class SecondaryAdapterException : InfrastructureException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected SecondaryAdapterException(string errorCode,
                                        string errorMessage)
        : base(errorCode, errorMessage) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(string           errorCode,
                                        ErrorDescription errorDescription)
        : base(errorCode, errorDescription) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(string    errorCode,
                                        string    errorMessage,
                                        Exception innerException)
        : base(errorCode, errorMessage, innerException) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(string           errorCode,
                                        ErrorDescription errorDescription,
                                        Exception        innerException)
        : base(errorCode, errorDescription, innerException) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(string                 errorCode,
                                        string                 errorMessage,
                                        IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorMessage, innerExceptions) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(string                 errorCode,
                                        ErrorDescription       errorDescription,
                                        IEnumerable<Exception> innerExceptions)
        : base(errorCode, errorDescription, innerExceptions) { }

    #endregion

}
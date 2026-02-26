namespace DiagnosableExceptions;

/// <summary>
///     Represents an infrastructure exception originating from a secondary adapter, that is, a component through which the
///     system interacts with external dependencies.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="SecondaryAdapterException" /> models technical failures occurring at the system boundary where the
///         application communicates with external systems such as databases, external APIs, file storage, or messaging
///         platforms.
///     </para>
///     <para>
///         This exception type is used to signal that an outgoing technical interaction failed, independently of domain
///         logic or business rules.
///     </para>
///     <para>
///         It belongs to the infrastructure layer but specifically reflects failures on the <b>output side</b> of the
///         system (inside → outside).
///     </para>
///     <para>
///         <b>Typical scenarios include:</b>
///     </para>
///     <list type="bullet">
///         <item>Database connectivity issues</item>
///         <item>Remote service call failures</item>
///         <item>File storage or retrieval errors</item>
///         <item>Message broker communication problems</item>
///     </list>
/// </remarks>
public abstract class SecondaryAdapterException : InfrastructureException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected SecondaryAdapterException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        string?                      shortMessage     = null,
                                        bool?                        isTransient      = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, shortMessage, isTransient, configureContext) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        Exception                    innerException,
                                        string?                      shortMessage     = null,
                                        bool?                        isTransient      = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, innerException, shortMessage, isTransient, configureContext) { }

    /// <inheritdoc />
    protected SecondaryAdapterException(ErrorCode                    errorCode,
                                        string                       errorMessage,
                                        IEnumerable<Exception>       innerExceptions,
                                        string?                      shortMessage     = null,
                                        bool?                        isTransient      = null,
                                        Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, innerExceptions, shortMessage, isTransient, configureContext) { }

    #endregion

}
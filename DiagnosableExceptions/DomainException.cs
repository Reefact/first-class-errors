namespace DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from domain rules, business logic, or invariant violations
///     within the core model.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="DomainException" /> signals that the system has reached a state that is invalid according to the
///         business or domain model. These exceptions do not indicate technical failures, infrastructure issues, or
///         environmental problems — they represent logical inconsistencies or rule violations.
///     </para>
///     <para>
///         Domain exceptions are considered <b>non-transient</b> by nature. Retrying the same operation without changing
///         input or state will typically lead to the same failure.
///     </para>
///     <para>
///         <b>Typical examples include:</b>
///     </para>
///     <list type="bullet">
///         <item>Violations of business rules or invariants</item>
///         <item>Invalid state transitions in aggregates</item>
///         <item>Operations that contradict domain constraints</item>
///     </list>
///     <para>
///         These exceptions are part of the system's semantic layer and should be used to express domain truth, not
///         technical malfunction.
///     </para>
///     <para>
///         <b>Authoring guidance for derived exceptions:</b>
///     </para>
///     <list type="bullet">
///         <item>The message should describe the violated rule or domain condition.</item>
///         <item>The error code should identify a stable business error category.</item>
///         <item>Do not use this type for network, I/O, or dependency failures.</item>
///     </list>
/// </remarks>
public abstract class DomainException : DiagnosableException {

    #region Constructors & Destructor

    /// <inheritdoc />
    protected DomainException(ErrorCode                    errorCode,
                              string                       errorMessage,
                              string?                      shortMessage     = null,
                              Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, shortMessage, configureContext) { }

    /// <inheritdoc />
    protected DomainException(ErrorCode                    errorCode,
                              string                       errorMessage,
                              Exception                    innerException,
                              string?                      shortMessage     = null,
                              Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, innerException, shortMessage, configureContext) { }

    /// <inheritdoc />
    protected DomainException(ErrorCode                    errorCode,
                              string                       errorMessage,
                              IEnumerable<Exception>       innerExceptions,
                              string?                      shortMessage     = null,
                              Action<ErrorContextBuilder>? configureContext = null)
        : base(errorCode, errorMessage, innerExceptions, shortMessage, configureContext) { }

    #endregion

}
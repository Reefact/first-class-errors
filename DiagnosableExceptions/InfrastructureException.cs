namespace DiagnosableExceptions;

/// <summary>
///     Represents an application exception that originates from technical concerns outside the domain model, including
///     infrastructure, integration, and adaptation logic.
/// </summary>
/// <remarks>
///     <para>
///         An <see cref="InfrastructureException" /> indicates that the system could not complete an operation due to a
///         technical failure unrelated to domain rules or business logic. These exceptions arise from the system’s
///         interaction with its technical environment or from transformation layers that connect the domain to that
///         environment.
///     </para>
///     <para>
///         This type includes failures related to:
///     </para>
///     <list type="bullet">
///         <item>External dependencies (databases, filesystems, network services, APIs)</item>
///         <item>Messaging and transport mechanisms</item>
///         <item>Serialization, parsing, or format handling</item>
///         <item>Mapping or transformation between technical and domain models</item>
///         <item>Configuration or environment setup</item>
///     </list>
///     <para>
///         Unlike <see cref="DomainException" />, infrastructure exceptions represent technical or environmental
///         conditions rather than incorrect business state. They may be transient or permanent depending on the failure.
///     </para>
///     <para>
///         <b>Authoring guidance for derived exceptions:</b>
///     </para>
///     <list type="bullet">
///         <item>The message should describe the technical operation that failed.</item>
///         <item>Do not encode domain rule violations in this type.</item>
///         <item>Use this class to clearly separate technical failures from domain errors.</item>
///     </list>
/// </remarks>
public abstract class InfrastructureException : DiagnosableException {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureException" /> class with the specified error code,
    ///     message.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of infrastructure error.
    /// </param>
    /// <param name="errorMessage">
    ///     A descriptive message explaining the technical failure that prevented the operation from completing.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise message suitable for compact displays or user interfaces.
    /// </param>
    /// <param name="isTransient">
    ///     Indicates whether the failure is considered transient.
    ///     <list type="bullet">
    ///         <item><c>true</c>: retrying the same operation may succeed.</item>
    ///         <item><c>false</c>: retrying will not resolve the issue.</item>
    ///         <item><c>null</c>: the transient nature is unknown or not specified.</item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor is used to represent failures originating from technical or environmental concerns outside the
    ///         domain model (e.g., integration, mapping, transport, storage, or configuration).
    ///     </para>
    ///     <para>
    ///         Derived exceptions should set <paramref name="isTransient" /> based on the expected behavior of the failing
    ///         technical dependency, not on business semantics.
    ///     </para>
    /// </remarks>
    protected InfrastructureException(string  errorCode,
                                      string  errorMessage,
                                      string? shortMessage = null,
                                      bool?   isTransient  = null) : base(errorCode, errorMessage, shortMessage) {
        IsTransient = isTransient;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureException" /> class with the specified error code,
    ///     message, and a single underlying exception.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of infrastructure error.
    /// </param>
    /// <param name="errorMessage">
    ///     A descriptive message explaining the technical failure that prevented the operation from completing.
    /// </param>
    /// <param name="innerException">
    ///     The underlying exception that contributed to this infrastructure failure.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise message suitable for compact displays or user interfaces.
    /// </param>
    /// <param name="isTransient">
    ///     Indicates whether the failure is considered transient.
    ///     <list type="bullet">
    ///         <item><c>true</c>: retrying the same operation may succeed.</item>
    ///         <item><c>false</c>: retrying will not resolve the issue.</item>
    ///         <item><c>null</c>: the transient nature is unknown or not specified.</item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor is used when a technical failure directly results from another exception. It preserves the
    ///         causal chain while still expressing the nature of the infrastructure problem at this abstraction level.
    ///     </para>
    ///     <para>
    ///         Derived exceptions should set <paramref name="isTransient" /> according to the expected behavior of the failing
    ///         technical dependency (e.g., network, storage, transformation layer), not based on business logic.
    ///     </para>
    /// </remarks>
    protected InfrastructureException(string    errorCode,
                                      string    errorMessage,
                                      Exception innerException,
                                      string?   shortMessage = null,
                                      bool?     isTransient  = null) : base(errorCode, errorMessage, innerException, shortMessage) {
        IsTransient = isTransient;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfrastructureException" /> class with the specified error code,
    ///     message, multiple underlying exceptions, and optional transient classification.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of infrastructure error.
    /// </param>
    /// <param name="errorMessage">
    ///     A descriptive message explaining the technical failure that prevented the operation from completing.
    /// </param>
    /// <param name="innerExceptions">
    ///     A collection of underlying exceptions that contributed to this infrastructure failure.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise message suitable for compact displays or user interfaces.
    /// </param>
    /// <param name="isTransient">
    ///     Indicates whether the failure is considered transient.
    ///     <list type="bullet">
    ///         <item><c>true</c>: retrying the same operation may succeed.</item>
    ///         <item><c>false</c>: retrying will not resolve the issue.</item>
    ///         <item><c>null</c>: the transient nature is unknown or not specified.</item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor supports diagnostic scenarios where multiple technical issues contribute to a single
    ///         infrastructure failure (e.g., parsing errors, transformation problems, or aggregated dependency failures).
    ///     </para>
    ///     <para>
    ///         Derived exceptions should set <paramref name="isTransient" /> based on the expected behavior of the failing
    ///         technical dependency, not on business semantics.
    ///     </para>
    /// </remarks>
    protected InfrastructureException(string                 errorCode,
                                      string                 errorMessage,
                                      IEnumerable<Exception> innerExceptions,
                                      string?                shortMessage = null,
                                      bool?                  isTransient  = null) : base(errorCode, errorMessage, innerExceptions, shortMessage) {
        IsTransient = isTransient;
    }

    #endregion

    /// <summary>
    ///     Indicates whether this infrastructure error is considered transient.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A transient error is one that may succeed if the operation is retried
    ///         without changing business input, for example due to temporary resource
    ///         contention or communication issues.
    ///     </para>
    ///     <para>
    ///         <list type="bullet">
    ///             <item><c>true</c>: retry may succeed without intervention</item>
    ///             <item><c>false</c>: retry will not help</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This property is optional and may remain <c>null</c> when the transient
    ///         nature of the failure is unknown or not specified.
    ///     </para>
    /// </remarks>
    public bool? IsTransient { get; }

}
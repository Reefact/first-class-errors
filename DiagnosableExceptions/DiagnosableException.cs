namespace DiagnosableExceptions;

/// <summary>
///     Represents the base class for application exceptions that are designed to be diagnosable, identifiable, and
///     observable beyond their raw message and stack trace.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="DiagnosableException" /> extends the traditional exception model by treating an exception as a
///         <b>diagnostic event</b>, not only a control-flow mechanism. Each instance captures stable identity, occurrence
///         metadata, and structured relationships to other exceptions, enabling deeper analysis in logs, monitoring, and
///         support workflows.
///     </para>
///     <para>
///         This type is intended to serve as the foundation for application-level exceptions whose occurrences must be
///         traceable, comparable, and understandable across time and system boundaries.
///     </para>
///     <para>
///         <b>Conceptual model:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>ErrorCode</b> identifies the type of error (stable across occurrences).
///         </item>
///         <item>
///             <b>InstanceId</b> identifies this specific occurrence (unique per throw).
///         </item>
///         <item>
///             <b>OccurredAt</b> captures when the error event happened.
///         </item>
///         <item>
///             <b>InnerExceptions</b> represent underlying contributing failures.
///         </item>
///     </list>
///     <para>
///         Unlike standard exceptions, this class promotes a view where exceptions are part of the system’s observable
///         diagnostic surface.
///     </para>
///     <para>
///         <b>Authoring guidance for derived exceptions:</b>
///     </para>
///     <list type="bullet">
///         <item>Use meaningful and stable error codes.</item>
///         <item>Ensure the message expresses the error clearly and in domain terms.</item>
///         <item>Prefer immutable state and avoid modifying properties after construction.</item>
///     </list>
/// </remarks>
public abstract class DiagnosableException : Exception {

    public const string UnknownErrorCde = "UNKNOWN_ERROR";

    #region Statics members declarations

    private static IReadOnlyList<Exception> CreateInnerExceptionList(Exception? innerException) {
        if (innerException is null) { return CreateInnerExceptionList(); }

        return Array.AsReadOnly([innerException]);
    }

    private static IReadOnlyList<Exception> CreateInnerExceptionList() {
        return Array.AsReadOnly(Array.Empty<Exception>());
    }

    private static string CreateSafeErrorCode(string? errorCode) {
        return errorCode ?? UnknownErrorCde;
    }

    private static IReadOnlyList<Exception> CreateInnerExceptionList(IEnumerable<Exception>? innerExceptions) {
        if (innerExceptions is null) { return CreateInnerExceptionList(); }

        Exception[] array = innerExceptions as Exception[] ?? innerExceptions.ToArray();

        return Array.AsReadOnly(array);
    }

    #endregion

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with an error code, and a descriptive
    ///     message.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of error. This value should remain the same for all occurrences of the
    ///     same logical error.
    /// </param>
    /// <param name="errorMessage">
    ///     A human-readable message describing the error in domain or system terms. This message is intended to explain the
    ///     failure, not the technical mechanism.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise version of the error message suitable for user interfaces or compact displays.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor captures the essential identity and occurrence metadata of a diagnosable exception. A unique
    ///         <see cref="InstanceId" /> is generated and the timestamp of occurrence is recorded.
    ///     </para>
    ///     <para>
    ///         Derived exceptions should use this constructor when the error represents a single diagnostic event without
    ///         additional underlying causes.
    ///     </para>
    /// </remarks>
    protected DiagnosableException(string  errorCode,
                                   string  errorMessage,
                                   string? shortMessage = null)
        : base(errorMessage) {
        InstanceId      = Guid.NewGuid();
        OccurredAt      = DateTimeOffset.UtcNow;
        ErrorCode       = CreateSafeErrorCode(errorCode);
        ShortMessage    = shortMessage;
        InnerExceptions = CreateInnerExceptionList();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with an error code, a descriptive
    ///     message, and a single underlying exception.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of error. This value should remain the same for all occurrences of the
    ///     same logical error.
    /// </param>
    /// <param name="errorMessage">
    ///     A human-readable message describing the error in domain or system terms. This message is intended to explain the
    ///     failure, not the technical mechanism.
    /// </param>
    /// <param name="innerException">
    ///     The underlying exception that contributed to this error.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise version of the error message suitable for user interfaces or compact displays.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor captures the essential identity and occurrence metadata of a diagnosable exception. A unique
    ///         <see cref="InstanceId" /> is generated and the timestamp of occurrence is recorded.
    ///     </para>
    ///     <para>
    ///         This constructor should be used when the error results directly from another exception. The inner exception is
    ///         preserved both in the base <see cref="Exception" /> chain and in the structured <see cref="InnerExceptions" />
    ///         collection.
    ///     </para>
    /// </remarks>
    protected DiagnosableException(string    errorCode,
                                   string    errorMessage,
                                   Exception innerException,
                                   string?   shortMessage = null)
        : base(errorMessage, innerException) {
        InstanceId      = Guid.NewGuid();
        OccurredAt      = DateTimeOffset.UtcNow;
        ErrorCode       = CreateSafeErrorCode(errorCode);
        ShortMessage    = shortMessage;
        InnerExceptions = CreateInnerExceptionList(innerException);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with an error code, a descriptive
    ///     message, and multiple contributing exceptions.
    /// </summary>
    /// <param name="errorCode">
    ///     A stable identifier representing the type of error. This value should remain the same for all occurrences of the
    ///     same logical error.
    /// </param>
    /// <param name="errorMessage">
    ///     A human-readable message describing the error in domain or system terms. This message is intended to explain the
    ///     failure, not the technical mechanism.
    /// </param>
    /// <param name="innerExceptions">
    ///     A collection of underlying exceptions that contributed to this error.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional concise version of the error message suitable for user interfaces or compact displays.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor captures the essential identity and occurrence metadata of a diagnosable exception. A unique
    ///         <see cref="InstanceId" /> is generated and the timestamp of occurrence is recorded.
    ///     </para>
    ///     <para>
    ///         This constructor supports diagnostic scenarios where multiple failures or validation issues contributed to a
    ///         single error event.
    ///     </para>
    /// </remarks>
    protected DiagnosableException(string                 errorCode,
                                   string                 errorMessage,
                                   IEnumerable<Exception> innerExceptions,
                                   string?                shortMessage = null)
        : base(errorMessage) {
        InstanceId      = Guid.NewGuid();
        OccurredAt      = DateTimeOffset.UtcNow;
        ErrorCode       = CreateSafeErrorCode(errorCode);
        ShortMessage    = shortMessage;
        InnerExceptions = CreateInnerExceptionList(innerExceptions);
    }

    #endregion

    /// <summary>
    ///     Gets the unique identifier for this specific exception occurrence.
    /// </summary>
    /// <remarks>
    ///     Each thrown instance receives a new <see cref="Guid" /> allowing correlation of logs and diagnostic events related
    ///     to this particular failure.
    /// </remarks>
    public Guid InstanceId { get; }

    /// <summary>
    ///     Gets the stable code identifying the type of error.
    /// </summary>
    /// <remarks>
    ///     Unlike <see cref="InstanceId" />, this value is shared across all occurrences
    ///     of the same logical error and is intended for grouping, monitoring, and alerting. Examples: <c>PAYMENT.DECLINED</c>
    ///     or <c>INVENTORY.OUT_OF_STOCK</c>.
    /// </remarks>
    public string ErrorCode { get; }

    /// <summary>
    ///     Gets the timestamp indicating when the exception instance was created.
    /// </summary>
    /// <remarks>
    ///     This value is captured in UTC and represents the moment the diagnostic event occurred.
    /// </remarks>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    ///     Gets a concise message that summarizes the error described by this exception.
    /// </summary>
    /// <value>
    ///     A short, human-readable string that provides a brief description of the error.
    /// </value>
    /// <remarks>
    ///     This property is intended to provide a simplified error message that can be displayed
    ///     in user interfaces where a full error message might be too verbose.
    /// </remarks>
    public string? ShortMessage { get; }

    /// <summary>
    ///     Gets the collection of inner exceptions that contribute to this exception.
    /// </summary>
    /// <remarks>
    ///     This property provides an immutable array of exceptions that were the cause of the current exception.
    ///     It is useful for diagnosing errors that involve multiple underlying issues.
    /// </remarks>
    public IReadOnlyList<Exception> InnerExceptions { get; }

    /// <summary>
    ///     Gets a value indicating whether this exception contains one or more inner exceptions.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the exception has inner exceptions; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///     This property is useful for determining if additional exceptions are encapsulated within this exception,
    ///     which can provide more context about the error.
    /// </remarks>
    public bool HasInnerExceptions() {
        return InnerExceptions.Any();
    }

}
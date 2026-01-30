namespace DiagnosableExceptions;

/// <summary>
///     Represents the base type for all application-specific exceptions
///     that are designed to be diagnosable.
/// </summary>
/// <remarks>
///     This class provides an immutable structure for identifying, tracing, and documenting errors.
///     It serves as a foundation for creating exceptions that include additional diagnostic information,
///     such as error codes, timestamps, and optional contextual details.
/// </remarks>
public abstract class DiagnosableException : Exception {

    #region Constructors & Destructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorMessage">
    ///     A detailed message that describes the error. This value cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string errorCode,
                                   string errorMessage)
        : base(errorMessage) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = string.Empty;
        InnerExceptions    = [];
        HasInnerExceptions = false;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorDescription">
    ///     An <see cref="ErrorDescription" /> instance that provides detailed and short messages describing the error. This
    ///     value cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> or <paramref name="errorDescription" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string           errorCode,
                                   ErrorDescription errorDescription)
        : base(errorDescription?.DetailedMessage) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (errorDescription is null) { throw new ArgumentNullException(nameof(errorDescription)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = errorDescription.ShortMessage;
        InnerExceptions    = [];
        HasInnerExceptions = false;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorMessage">
    ///     A message that describes the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception. This value cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> or <paramref name="innerException" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string    errorCode,
                                   string    errorMessage,
                                   Exception innerException)
        : base(errorMessage, innerException) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (innerException is null) { throw new ArgumentNullException(nameof(innerException)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = string.Empty;
        InnerExceptions    = [innerException];
        HasInnerExceptions = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorDescription">
    ///     An <see cref="ErrorDescription" /> instance containing detailed and short messages describing the error. This value
    ///     cannot be <c>null</c>.
    /// </param>
    /// <param name="innerException">
    ///     The exception that caused the current exception. This value cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" />, <paramref name="errorDescription" />, or
    ///     <paramref name="innerException" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string           errorCode,
                                   ErrorDescription errorDescription,
                                   Exception        innerException)
        : base(errorDescription?.DetailedMessage, innerException) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (errorDescription is null) { throw new ArgumentNullException(nameof(errorDescription)); }
        if (innerException is null) { throw new ArgumentNullException(nameof(innerException)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = errorDescription.ShortMessage;
        InnerExceptions    = [innerException];
        HasInnerExceptions = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorMessage">
    ///     A message that describes the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="innerExceptions">
    ///     A collection of exceptions that provide additional context for the error. This value cannot be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> or <paramref name="innerExceptions" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string                 errorCode,
                                   string                 errorMessage,
                                   IEnumerable<Exception> innerExceptions)
        : base(errorMessage) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (innerExceptions is null) { throw new ArgumentNullException(nameof(innerExceptions)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = string.Empty;
        InnerExceptions    = [.. innerExceptions];
        HasInnerExceptions = InnerExceptions.Any();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique code that identifies the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="errorDescription">
    ///     An <see cref="ErrorDescription" /> instance containing detailed and short messages describing the error. This value
    ///     cannot be <c>null</c>.
    /// </param>
    /// <param name="innerExceptions">
    ///     A collection of exceptions that represent the underlying causes of this exception. This value cannot be <c>null</c>
    ///     .
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" />, <paramref name="errorDescription" />, or
    ///     <paramref name="innerExceptions" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string                 errorCode,
                                   ErrorDescription       errorDescription,
                                   IEnumerable<Exception> innerExceptions)
        : base(errorDescription?.DetailedMessage) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (errorDescription is null) { throw new ArgumentNullException(nameof(errorDescription)); }
        if (innerExceptions is null) { throw new ArgumentNullException(nameof(innerExceptions)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        OccurredAt         = DateTimeOffset.UtcNow;
        ShortMessage       = errorDescription.ShortMessage;
        InnerExceptions    = [..innerExceptions];
        HasInnerExceptions = InnerExceptions.Any();
    }

    #endregion

    /// <summary>
    ///     Gets a unique identifier for this specific exception instance.
    /// </summary>
    /// <remarks>
    ///     This identifier is automatically generated when the exception is instantiated,
    ///     ensuring that each occurrence of the exception has a distinct <see cref="Guid" />.
    ///     It can be used for tracking and correlating exception occurrences in logs or diagnostics.
    /// </remarks>
    public Guid InstanceId { get; }

    /// <summary>
    ///     Stable code that identifies the type of error. Unlike <see cref="InstanceId" />, this value is the same across all
    ///     occurrences of the same error type. Examples: <c>PAYMENT.DECLINED</c> or <c>INVENTORY.OUT_OF_STOCK</c>.
    /// </summary>

    public string ErrorCode { get; }

    /// <summary>
    ///     Gets the date and time, in Coordinated Universal Time (UTC), when the exception occurred.
    /// </summary>
    /// <value>
    ///     A <see cref="DateTimeOffset" /> representing the timestamp of when the exception was instantiated.
    /// </value>
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
    public string ShortMessage { get; }

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
    public bool HasInnerExceptions { get; }

}
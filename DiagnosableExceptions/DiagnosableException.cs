#region Usings declarations

using System.Collections.Immutable;

#endregion

namespace Reefact.DiagnosableExceptions;

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

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with a specified error code,
    ///     error message, and optional error context.
    /// </summary>
    /// <param name="errorCode">
    ///     A unique identifier for the error. This value cannot be <c>null</c>.
    /// </param>
    /// <param name="message">
    ///     A message that describes the error.
    /// </param>
    /// <param name="context">
    ///     Optional. An <see cref="ErrorContext" /> object that provides additional details about the error.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> is <c>null</c>.
    /// </exception>
    protected DiagnosableException(string        errorCode,
                                   string        message,
                                   ErrorContext? context = null)
        : base(message) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (message is null) { throw new ArgumentNullException(nameof(message)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        Context            = context;
        OccurredAt         = DateTimeOffset.UtcNow;
        InnerExceptions    = ImmutableArray<Exception>.Empty;
        HasInnerExceptions = false;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with a specified error code,
    ///     error message, a reference to the inner exception that is the cause of this exception, and an optional error
    ///     context.
    /// </summary>
    /// <param name="errorCode">
    ///     A string that uniquely identifies the error. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="message">
    ///     A message that describes the error. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="context">
    ///     An optional <see cref="ErrorContext" /> object that provides additional context for the error.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" /> or <paramref name="innerException" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This constructor is used to create an exception that includes details about the error, its cause, and
    ///     additional context for diagnosing the issue.
    /// </remarks>
    protected DiagnosableException(string        errorCode,
                                   string        message,
                                   Exception     innerException,
                                   ErrorContext? context = null)
        : base(message, innerException) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (message is null) { throw new ArgumentNullException(nameof(message)); }
        if (innerException is null) { throw new ArgumentNullException(nameof(innerException)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        Context            = context;
        OccurredAt         = DateTimeOffset.UtcNow;
        InnerExceptions    = [innerException];
        HasInnerExceptions = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiagnosableException" /> class with the specified error code, message,
    ///     collection of inner exceptions, and optional error context.
    /// </summary>
    /// <param name="errorCode">
    ///     The unique code that identifies the error. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="message">
    ///     A message that describes the error. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="innerExceptions">
    ///     A collection of exceptions that caused the current exception. This parameter cannot be <c>null</c>.
    /// </param>
    /// <param name="context">
    ///     An optional <see cref="ErrorContext" /> object that provides additional information about the error.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="errorCode" />, <paramref name="message" />, or <paramref name="innerExceptions" /> is
    ///     <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This constructor is designed for scenarios where multiple exceptions contribute to the current error.
    /// </remarks>
    protected DiagnosableException(string                 errorCode,
                                   string                 message,
                                   IEnumerable<Exception> innerExceptions,
                                   ErrorContext?          context = null)
        : base(message) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }
        if (message is null) { throw new ArgumentNullException(nameof(message)); }
        if (innerExceptions is null) { throw new ArgumentNullException(nameof(innerExceptions)); }

        InstanceId         = Guid.NewGuid();
        ErrorCode          = errorCode;
        Context            = context;
        OccurredAt         = DateTimeOffset.UtcNow;
        InnerExceptions    = [..innerExceptions];
        HasInnerExceptions = true;
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
    ///     Gets the collection of inner exceptions that contribute to this exception.
    /// </summary>
    /// <remarks>
    ///     This property provides an immutable array of exceptions that were the cause of the current exception.
    ///     It is useful for diagnosing errors that involve multiple underlying issues.
    /// </remarks>
    public ImmutableArray<Exception> InnerExceptions { get; }

    /// <summary>
    ///     Provides structured details about the context in which the error occurred.
    ///     This may include domain-specific metadata such as user identifiers, correlation IDs,
    ///     or operation parameters, aiding in diagnosing the error without relying on
    ///     <see cref="Exception.Data" />.
    /// </summary>
    public ErrorContext? Context { get; }

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
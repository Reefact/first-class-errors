#region Usings declarations

using System.Collections;

#endregion

namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the base type for all application-specific exceptions
///     that are designed to be diagnosable.
///     Provides an immutable structure for identifying, tracing, and documenting errors.
/// </summary>
public abstract class DiagnosableException : Exception {

    #region Constructors declarations

    protected DiagnosableException(string        errorCode,
                                   string        message,
                                   ErrorContext? context = null)
        : base(message) {
        ArgumentNullException.ThrowIfNull(errorCode);

        InstanceId = Guid.NewGuid();
        ErrorCode  = errorCode;
        Context    = context;
        OccurredAt = DateTimeOffset.UtcNow;
        Causes     = new List<Exception>();
    }

    protected DiagnosableException(string        errorCode,
                                   string        message,
                                   Exception     cause,
                                   ErrorContext? context = null)
        : base(message, cause) {
        ArgumentNullException.ThrowIfNull(errorCode);
        ArgumentNullException.ThrowIfNull(cause);

        InstanceId = Guid.NewGuid();
        ErrorCode  = errorCode;
        Context    = context;
        OccurredAt = DateTimeOffset.UtcNow;
        Causes     = new List<Exception>([cause]);
    }

    protected DiagnosableException(string                 errorCode,
                                   string                 message,
                                   IEnumerable<Exception> causes,
                                   ErrorContext?          context = null)
        : base(message) {
        ArgumentNullException.ThrowIfNull(errorCode);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(causes);

        InstanceId = Guid.NewGuid();
        ErrorCode  = errorCode;
        Context    = context;
        OccurredAt = DateTimeOffset.UtcNow;
        Causes     = new List<Exception>(causes);
    }

    #endregion

    /// <summary>
    ///     Unique identifier for this specific error occurrence (instance). Useful for tracing a single error across logs,
    ///     support tickets, or monitoring systems.
    /// </summary>
    public Guid InstanceId { get; }

    /// <summary>
    ///     Stable code that identifies the type of error. Unlike <see cref="InstanceId" />, this value is the same across all
    ///     occurrences of the same error type. Examples: <c>PAYMENT.DECLINED</c> or <c>INVENTORY.OUT_OF_STOCK</c>.
    /// </summary>

    public string ErrorCode { get; }

    /// <summary>
    ///     UTC timestamp indicating when the error occurred. Provides temporal context for diagnosis and auditing.
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    ///     List of underlying exceptions that contributed to this error.
    /// </summary>
    public IReadOnlyList<Exception> Causes { get; }

    [Obsolete($"Use property {nameof(Context)} instead.", true)]
    public new IDictionary Data => base.Data;

    // TODO: documenter !
    public ErrorContext? Context { get; }

}
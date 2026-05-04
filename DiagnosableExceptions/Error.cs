#region Usings declarations

using System.Diagnostics;

#endregion

namespace DiagnosableExceptions;

/// <summary>
///     Represents the base class for all diagnosable errors within the application.
/// </summary>
/// <remarks>
///     This abstract class provides a foundation for defining errors with detailed diagnostic information.
///     It includes properties for error identification, occurrence time, detailed and short messages,
///     inner errors for nested diagnostics, and a context for additional metadata.
///     Derived classes can extend this functionality to represent specific types of errors.
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public abstract class Error {

    /// <summary>
    ///     Represents the fallback detailed message used when no error message is provided.
    /// </summary>
    /// <remarks>
    ///     This constant is used when an exception is created without an error message,
    ///     typically due to missing or incomplete initialization. It ensures that a
    ///     meaningful diagnostic message is always available.
    /// </remarks>
    private const string UnknownDetailedMessage = "Exception created without an error message. This typically indicates a missing or incomplete exception initialization.";

    #region Statics members declarations

    private static ErrorCode CreateSafeCode(ErrorCode? errorCode) {
        return errorCode == null ? ErrorCode.Unspecified : errorCode;
    }

    private static string CreateSafeDetailedMessage(string? detailedMessage) {
        return string.IsNullOrWhiteSpace(detailedMessage) ? UnknownDetailedMessage : detailedMessage!.Trim();
    }

    private static ErrorContext BuildContext(Action<ErrorContextBuilder>? configure) {
        ErrorContextBuilder builder = new();

        try {
            configure?.Invoke(builder);
        } catch (Exception ex) {
            Dictionary<ErrorContextKey, object?> dictionary = new() { { ErrorContextKey.CannotBuildErrorContext, ex } };

            return new ErrorContext(dictionary);
        }

        return builder.Build();
    }

    private static IReadOnlyList<Error> CreateSafeInnerErrors(IEnumerable<Error>? innerErrors) {
        return innerErrors == null ? new List<Error>() : innerErrors.ToList();
    }

    private static IReadOnlyList<Error> CreateSafeInnerErrors(Error? innerError) {
        return innerError == null ? new List<Error>() : new List<Error> { innerError };
    }

    #endregion

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, detailed message,
    ///     optional short message, and an optional configuration for the error context.
    /// </summary>
    /// <param name="code">
    ///     The unique code representing the error. This value cannot be null or whitespace.
    /// </param>
    /// <param name="detailedMessage">
    ///     A detailed description of the error. This value cannot be null or whitespace.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional short description of the error. This value can be null.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> using an <see cref="ErrorContextBuilder" />.
    /// </param>
    /// <remarks>
    ///     This constructor generates a unique identifier for the error instance, sets the occurrence timestamp,
    ///     and initializes the error context using the provided configuration.
    /// </remarks>
    protected Error(ErrorCode                    code,
                    string                       detailedMessage, string? shortMessage,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId      = Guid.NewGuid();
        Code            = CreateSafeCode(code);
        OccurredAt      = DateTimeOffset.UtcNow;
        DetailedMessage = CreateSafeDetailedMessage(detailedMessage);
        ShortMessage    = shortMessage;
        InnerErrors     = new List<Error>();
        Context         = BuildContext(configureContext);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, detailed message,
    ///     optional short message, and an optional configuration for the error context.
    /// </summary>
    /// <param name="code">
    ///     The unique code representing the error. This value cannot be null or whitespace.
    /// </param>
    /// <param name="detailedMessage">
    ///     A detailed description of the error. This value cannot be null or whitespace.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional short description of the error. This value can be null.
    /// </param>
    /// <param name="innerError">
    ///     An inner <see cref="Error" /> instances that provide additional context for the error.
    ///     If null, an empty collection is used.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> using an <see cref="ErrorContextBuilder" />.
    /// </param>
    /// <remarks>
    ///     This constructor generates a unique identifier for the error instance, sets the occurrence timestamp,
    ///     and initializes the error context using the provided configuration.
    /// </remarks>
    protected Error(ErrorCode                    code,
                    string                       detailedMessage, string? shortMessage,
                    Error                        innerError,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId      = Guid.NewGuid();
        Code            = CreateSafeCode(code);
        OccurredAt      = DateTimeOffset.UtcNow;
        DetailedMessage = CreateSafeDetailedMessage(detailedMessage);
        ShortMessage    = shortMessage;
        InnerErrors     = CreateSafeInnerErrors(innerError);
        Context         = BuildContext(configureContext);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, detailed message,
    ///     optional short message, a collection of inner errors, and an optional context configuration.
    /// </summary>
    /// <param name="code">
    ///     The error code that uniquely identifies the error. If the provided value is null or whitespace, a default code is
    ///     used.
    /// </param>
    /// <param name="detailedMessage">
    ///     A detailed message describing the error. If the provided value is null or whitespace, a default message is used.
    /// </param>
    /// <param name="shortMessage">
    ///     An optional short message providing a concise description of the error.
    /// </param>
    /// <param name="innerErrors">
    ///     A collection of inner <see cref="Error" /> instances that provide additional context for the error.
    ///     If null, an empty collection is used.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> for the error. If null, a default context is
    ///     created.
    /// </param>
    protected Error(ErrorCode                    code,
                    string                       detailedMessage, string? shortMessage,
                    IEnumerable<Error>           innerErrors,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId      = Guid.NewGuid();
        Code            = CreateSafeCode(code);
        OccurredAt      = DateTimeOffset.UtcNow;
        DetailedMessage = CreateSafeDetailedMessage(detailedMessage);
        ShortMessage    = shortMessage;
        InnerErrors     = CreateSafeInnerErrors(innerErrors);
        Context         = BuildContext(configureContext);
    }

    #endregion

    /// <summary>
    ///     Gets the unique identifier for this specific error occurrence.
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
    public ErrorCode Code { get; }

    /// <summary>
    ///     Gets the timestamp indicating when the error instance was created.
    /// </summary>
    /// <remarks>
    ///     This value is captured in UTC and represents the moment the diagnostic event occurred.
    /// </remarks>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    ///     Gets the detailed message associated with the error.
    /// </summary>
    /// <value>
    ///     An auto-descriptive error message that fully explains the nature of the failure.
    /// </value>
    /// <remarks>
    ///     This property provides a detailed error message that can be used for diagnostic logs
    ///     or exposed to end users in reports when a more complete explanation is needed.
    /// </remarks>
    public string DetailedMessage { get; }

    /// <summary>
    ///     Gets a concise message that summarizes the error described by this error.
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
    ///     Gets a collection of inner errors that provide additional context or details about this error.
    /// </summary>
    /// <remarks>
    ///     This property contains a list of related errors that may have contributed to or been caused by this error.
    ///     It is useful for representing hierarchical or aggregated error information.
    /// </remarks>
    public IReadOnlyList<Error> InnerErrors { get; }

    /// <summary>
    ///     Gets the context that provides additional information about the error.
    /// </summary>
    /// <remarks>
    ///     The <see cref="ErrorContext" /> contains supplementary details about the error,
    ///     which can be used for diagnostics and troubleshooting. It encapsulates key-value
    ///     pairs that describe the error context and supports operations such as retrieving
    ///     values by their associated keys.
    /// </remarks>
    public ErrorContext Context { get; }

    /// <summary>
    ///     Converts the current <see cref="Error" /> instance into a <see cref="DiagnosableException" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="DiagnosableException" /> that represents the current error, including all diagnostic information.
    /// </returns>
    /// <remarks>
    ///     This method allows the transformation of an error into an exception, enabling it to be thrown and caught
    ///     within exception-handling mechanisms. The resulting exception retains all diagnostic details of the error,
    ///     such as its code, messages, context, and inner errors.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the conversion cannot be performed due to an invalid state of the <see cref="Error" /> instance.
    /// </exception>
    public abstract DiagnosableException ToException();

    /// <inheritdoc />
    public override string ToString() {
        return $"{DetailedMessage} ({Code})";
    }

}
#region Usings declarations

using System.Diagnostics;

#endregion

namespace FirstClassErrors;

/// <summary>
///     Represents the base class for all diagnosable errors within the application.
/// </summary>
/// <remarks>
///     <para>
///         This abstract class provides a foundation for defining errors with a clear separation between what is safe to
///         expose to a caller and what is meant to stay internal. Every error carries three distinct messages:
///     </para>
///     <list type="bullet">
///         <item>
///             <see cref="ShortMessage" /> — a short public summary, safe to surface to an end user or an API client.
///         </item>
///         <item>
///             <see cref="DetailedMessage" /> — an optional, controlled public detail. It may be exposed (for instance in
///             an RFC 9457 <c>problem+json</c> response) but only when the application explicitly chooses to.
///         </item>
///         <item>
///             <see cref="DiagnosticMessage" /> — the mandatory internal diagnostic message, intended for logs, support and
///             developers. It must never be exposed to external clients by default.
///         </item>
///     </list>
///     <para>
///         Instances are not created through public constructors. Each concrete error type exposes a <c>Create(...)</c>
///         staged-builder entry point that captures the mandatory internal information (<see cref="Code" /> and
///         <see cref="DiagnosticMessage" />) and returns a <see cref="PublicMessageStage{TError}" />; the final error is
///         produced by <see cref="PublicMessageStage{TError}.WithPublicMessage(string, string?)" />.
///     </para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
public abstract class Error {

    #region Statics members declarations

    private static ErrorCode CreateSafeCode(ErrorCode? errorCode) {
        return errorCode == null ? ErrorCode.Unspecified : errorCode;
    }

    /// <summary>
    ///     Validates a mandatory message and returns its trimmed value.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is empty or whitespace.</exception>
    internal static string RequireMessage(string value, string paramName) {
        if (value is null) { throw new ArgumentNullException(paramName); }
        if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("Value cannot be empty or whitespace.", paramName); }

        return value.Trim();
    }

    /// <summary>
    ///     Normalizes an optional message: <c>null</c> or whitespace becomes <c>null</c>, otherwise the value is trimmed.
    /// </summary>
    internal static string? NormalizeOptionalMessage(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
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
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, diagnostic message,
    ///     public short message, optional public detailed message, and an optional configuration for the error context.
    /// </summary>
    /// <param name="code">
    ///     The <see cref="ErrorCode" /> identifying the error. If <c>null</c>, <see cref="ErrorCode.Unspecified" /> is used.
    /// </param>
    /// <param name="diagnosticMessage">
    ///     The mandatory internal diagnostic message. This value cannot be null or whitespace.
    /// </param>
    /// <param name="shortMessage">
    ///     The mandatory public short summary of the error. This value cannot be null or whitespace.
    /// </param>
    /// <param name="detailedMessage">
    ///     An optional, controlled public detail. This value can be null.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> using an <see cref="ErrorContextBuilder" />.
    /// </param>
    /// <remarks>
    ///     This constructor generates a unique identifier for the error instance, sets the occurrence timestamp,
    ///     and initializes the error context using the provided configuration.
    /// </remarks>
    protected Error(ErrorCode                    code,
                    string                       diagnosticMessage, string shortMessage, string? detailedMessage,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId        = Guid.NewGuid();
        Code              = CreateSafeCode(code);
        OccurredAt        = DateTimeOffset.UtcNow;
        DiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));
        ShortMessage      = RequireMessage(shortMessage, nameof(shortMessage));
        DetailedMessage   = NormalizeOptionalMessage(detailedMessage);
        InnerErrors       = new List<Error>();
        Context           = BuildContext(configureContext);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, diagnostic message,
    ///     public short message, optional public detailed message, an inner error, and an optional context configuration.
    /// </summary>
    /// <param name="code">
    ///     The <see cref="ErrorCode" /> identifying the error. If <c>null</c>, <see cref="ErrorCode.Unspecified" /> is used.
    /// </param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message. This value cannot be null or whitespace.</param>
    /// <param name="shortMessage">The mandatory public short summary of the error. This value cannot be null or whitespace.</param>
    /// <param name="detailedMessage">An optional, controlled public detail. This value can be null.</param>
    /// <param name="innerError">
    ///     The inner <see cref="Error" /> that provides additional context for the error.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> using an <see cref="ErrorContextBuilder" />.
    /// </param>
    protected Error(ErrorCode                    code,
                    string                       diagnosticMessage, string shortMessage, string? detailedMessage,
                    Error                        innerError,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId        = Guid.NewGuid();
        Code              = CreateSafeCode(code);
        OccurredAt        = DateTimeOffset.UtcNow;
        DiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));
        ShortMessage      = RequireMessage(shortMessage, nameof(shortMessage));
        DetailedMessage   = NormalizeOptionalMessage(detailedMessage);
        InnerErrors       = CreateSafeInnerErrors(innerError);
        Context           = BuildContext(configureContext);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Error" /> class with the specified error code, diagnostic message,
    ///     public short message, optional public detailed message, a collection of inner errors, and an optional context
    ///     configuration.
    /// </summary>
    /// <param name="code">
    ///     The error code that uniquely identifies the error. If <c>null</c>, <see cref="ErrorCode.Unspecified" /> is used.
    /// </param>
    /// <param name="diagnosticMessage">The mandatory internal diagnostic message. This value cannot be null or whitespace.</param>
    /// <param name="shortMessage">The mandatory public short summary of the error. This value cannot be null or whitespace.</param>
    /// <param name="detailedMessage">An optional, controlled public detail. This value can be null.</param>
    /// <param name="innerErrors">
    ///     A collection of inner <see cref="Error" /> instances that provide additional context for the error.
    ///     If null, an empty collection is used.
    /// </param>
    /// <param name="configureContext">
    ///     An optional action to configure the <see cref="ErrorContext" /> for the error. If null, a default context is
    ///     created.
    /// </param>
    protected Error(ErrorCode                    code,
                    string                       diagnosticMessage, string shortMessage, string? detailedMessage,
                    IEnumerable<Error>           innerErrors,
                    Action<ErrorContextBuilder>? configureContext = null) {
        InstanceId        = Guid.NewGuid();
        Code              = CreateSafeCode(code);
        OccurredAt        = DateTimeOffset.UtcNow;
        DiagnosticMessage = RequireMessage(diagnosticMessage, nameof(diagnosticMessage));
        ShortMessage      = RequireMessage(shortMessage, nameof(shortMessage));
        DetailedMessage   = NormalizeOptionalMessage(detailedMessage);
        InnerErrors       = CreateSafeInnerErrors(innerErrors);
        Context           = BuildContext(configureContext);
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
    ///     Gets the mandatory internal diagnostic message associated with the error.
    /// </summary>
    /// <value>
    ///     A technical message describing the failure precisely, meant for logs, support and developers.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This message carries the diagnostic knowledge about the failure: it may contain technical, operational or
    ///         support-oriented details (identifiers, offending values, internal state, ...).
    ///     </para>
    ///     <para>
    ///         It is <b>not</b> safe for external exposure and must never be used by default in an HTTP response or returned
    ///         to an external client. It is intended for diagnostics, support, observability and developers.
    ///     </para>
    /// </remarks>
    public string DiagnosticMessage { get; }

    /// <summary>
    ///     Gets the short public summary of the error.
    /// </summary>
    /// <value>
    ///     A short, human-readable string that briefly describes the error.
    /// </value>
    /// <remarks>
    ///     This message is a concise public summary, safe to surface to an end user or an API client (for instance as the
    ///     <c>title</c> of an RFC 9457 problem detail). It is mandatory.
    /// </remarks>
    public string ShortMessage { get; }

    /// <summary>
    ///     Gets the optional controlled public detail of the error.
    /// </summary>
    /// <value>
    ///     A more complete public explanation of the error, or <c>null</c> when none is provided.
    /// </value>
    /// <remarks>
    ///     This message is a controlled public detail. It may be exposed to a caller (for instance as the <c>detail</c> of an
    ///     RFC 9457 problem detail) but only when the application explicitly chooses to. Unlike
    ///     <see cref="DiagnosticMessage" />, it must not carry sensitive or purely internal information.
    /// </remarks>
    public string? DetailedMessage { get; }

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
        return $"{DiagnosticMessage} ({Code})";
    }

}

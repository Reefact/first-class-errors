namespace FirstClassErrors;

/// <summary>
///     Represents the messages produced by a concrete error example: its public short summary, its optional controlled
///     public detail, and its internal diagnostic message.
/// </summary>
/// <remarks>
///     This class captures, for a documented example, the same three-way message separation carried by an
///     <see cref="Error" />:
///     <list type="bullet">
///         <item><see cref="ShortMessage" /> — short public summary.</item>
///         <item><see cref="DetailedMessage" /> — optional controlled public detail (opt-in exposure).</item>
///         <item><see cref="DiagnosticMessage" /> — internal diagnostic message; never exposed to external clients by default.</item>
///     </list>
/// </remarks>
public sealed class ErrorDescription {

    #region Constructors declarations

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorDescription" /> class.
    /// </summary>
    /// <param name="shortMessage">
    ///     The mandatory short public summary of the error. This parameter cannot be <c>null</c>, empty, or whitespace.
    /// </param>
    /// <param name="diagnosticMessage">
    ///     The mandatory internal diagnostic message. This parameter cannot be <c>null</c>, empty, or whitespace.
    /// </param>
    /// <param name="detailedMessage">
    ///     An optional controlled public detail. If not provided or whitespace, it is set to <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="shortMessage" /> or <paramref name="diagnosticMessage" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="shortMessage" /> or <paramref name="diagnosticMessage" /> is empty or whitespace.
    /// </exception>
    public ErrorDescription(string shortMessage, string diagnosticMessage, string? detailedMessage = null) {
        if (shortMessage is null) { throw new ArgumentNullException(nameof(shortMessage)); }
        if (string.IsNullOrWhiteSpace(shortMessage)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(shortMessage)); }
        if (diagnosticMessage is null) { throw new ArgumentNullException(nameof(diagnosticMessage)); }
        if (string.IsNullOrWhiteSpace(diagnosticMessage)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(diagnosticMessage)); }

        ShortMessage      = shortMessage.Trim();
        DiagnosticMessage = diagnosticMessage.Trim();
        // In this branch IsNullOrWhiteSpace(detailedMessage) is false, so detailedMessage is non-null; the null-forgiving
        // '!' states that for the netstandard2.0 compiler, which does not carry the [NotNullWhen(false)] flow annotation.
        DetailedMessage   = string.IsNullOrWhiteSpace(detailedMessage) ? null : detailedMessage!.Trim();
    }

    #endregion

    /// <summary>
    ///     Gets the short public summary of the error.
    /// </summary>
    /// <remarks>
    ///     A brief, safe-to-expose description, suitable for an end user or an API client (for instance the <c>title</c> of
    ///     an RFC 9457 problem detail).
    /// </remarks>
    public string ShortMessage { get; }

    /// <summary>
    ///     Gets the internal diagnostic message of the error.
    /// </summary>
    /// <remarks>
    ///     A technical message meant for logs, support and developers. It must never be exposed to external clients by
    ///     default.
    /// </remarks>
    public string DiagnosticMessage { get; }

    /// <summary>
    ///     Gets the optional controlled public detail of the error.
    /// </summary>
    /// <remarks>
    ///     A more complete public explanation that the application may choose to expose (for instance the <c>detail</c> of an
    ///     RFC 9457 problem detail), or <c>null</c> when none is provided.
    /// </remarks>
    public string? DetailedMessage { get; }

}

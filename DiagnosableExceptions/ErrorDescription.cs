namespace DiagnosableExceptions;

/// <summary>
///     Represents a description of an error, providing both a short and a detailed message.
/// </summary>
/// <remarks>
///     This class is used to encapsulate error information, including a concise summary of the error
///     and a more detailed explanation. It is typically utilized in exception handling scenarios to
///     provide meaningful error descriptions.
/// </remarks>
public sealed class ErrorDescription {

    #region Constructors & Destructor

    public ErrorDescription(string detailedMessage, string? shortMessage = null) {
        if (detailedMessage is null) { throw new ArgumentNullException(nameof(detailedMessage)); }
        if (string.IsNullOrWhiteSpace(detailedMessage)) { throw new ArgumentException("Value cannot be empty or whitespace.", nameof(detailedMessage)); }

        DetailedMessage = detailedMessage.Trim();
        ShortMessage    = string.IsNullOrWhiteSpace(shortMessage) ? null : shortMessage?.Trim();
    }

    #endregion

    /// <summary>
    ///     Gets or sets the detailed message describing the error.
    /// </summary>
    /// <value>
    ///     A string containing a detailed explanation of the error. Defaults to an empty string if not set.
    /// </value>
    /// <remarks>
    ///     This property provides a more comprehensive description of the error, which can be useful for logging,
    ///     debugging, or displaying detailed error information to users or developers.
    /// </remarks>
    public string DetailedMessage { get; }

    /// <summary>
    ///     Gets or sets a concise summary of the error.
    /// </summary>
    /// <remarks>
    ///     This property provides a brief description of the error, suitable for display in user interfaces where a short and
    ///     clear message is required.
    /// </remarks>
    public string? ShortMessage { get; }

}
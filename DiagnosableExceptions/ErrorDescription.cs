namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the structured messages and rule associated with a diagnosable error.
/// </summary>
public sealed record class ErrorDescription {

    #region Constructors declarations

    public ErrorDescription(string message, string shortMessage, string rule) {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(shortMessage);
        ArgumentNullException.ThrowIfNull(rule);

        Message      = message;
        ShortMessage = shortMessage;
        Rule         = rule;
    }

    #endregion

    /// <summary>
    ///     The full, self-contained message. This is always required.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     A short version of the message (optional), useful for UI labels near form fields.
    /// </summary>
    public string ShortMessage { get; }

    /// <summary>
    ///     The rule that was violated (optional), useful for documentation or tooltips.
    /// </summary>
    public string Rule { get; }

}
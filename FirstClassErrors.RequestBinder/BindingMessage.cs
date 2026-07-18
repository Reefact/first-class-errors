namespace FirstClassErrors.RequestBinder;

/// <summary>
///     The public messages of a binder structural error: the mandatory short summary surfaced to an end user or API
///     client, and an optional controlled detail. It is a plain carrier, not a validated value object: following the
///     library's "manufacturing an error never throws" doctrine it rejects nothing — a <c>null</c> or whitespace
///     <see cref="ShortMessage" /> is coalesced to <see cref="Error.MissingShortMessage" /> downstream, exactly as a
///     directly-authored error would be.
/// </summary>
public sealed class BindingMessage {

    #region Constructors declarations

    /// <summary>Instantiates the public messages of a binder structural error.</summary>
    /// <param name="shortMessage">The mandatory short public summary, safe to surface to an end user or API client.</param>
    /// <param name="detailedMessage">An optional, controlled public detail. Defaults to <c>null</c>.</param>
    public BindingMessage(string shortMessage, string? detailedMessage = null) {
        ShortMessage    = shortMessage;
        DetailedMessage = detailedMessage;
    }

    #endregion

    /// <summary>The mandatory short public summary of the error.</summary>
    public string ShortMessage { get; }

    /// <summary>The optional, controlled public detail of the error.</summary>
    public string? DetailedMessage { get; }

}

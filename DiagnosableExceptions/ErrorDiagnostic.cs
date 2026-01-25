namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents a diagnostic entry for an error, consisting of a cause and its associated corrective action.
/// </summary>
public sealed class ErrorDiagnostic {

    /// <summary>
    ///     Gets or sets the description of the cause of the error.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Cause" /> property provides a textual explanation of what led to the error.
    ///     It is intended to help identify the root issue and guide corrective actions.
    /// </remarks>
    public string Cause { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the type of cause for the error.
    /// </summary>
    /// <value>
    ///     An <see cref="ErrorCauseType" /> value that specifies the nature of the error's cause.
    /// </value>
    public ErrorCauseType Type { get; set; } = ErrorCauseType.SystemOrInput;

    /// <summary>
    ///     Gets or sets the corrective action associated with the error cause.
    /// </summary>
    /// <remarks>
    ///     This property provides a description of the steps or actions required to resolve the issue
    ///     identified by the <see cref="Cause" /> property.
    /// </remarks>
    public string Fix { get; set; } = string.Empty;

}
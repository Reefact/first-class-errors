namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents a diagnostic entry for an error, consisting of a cause and its associated corrective action.
/// </summary>
public sealed record ErrorDiagnostic {

    /// <summary>
    ///     Describes a possible cause for the error.
    ///     This field helps identify what could have triggered the exception in a given context.
    /// </summary>
    public required string Cause { get; init; }

    /// <summary>
    ///     Provides a corrective action associated with the specified cause.
    ///     It suggests what should be fixed or changed to avoid the error.
    /// </summary>
    public required string Fix { get; init; }

}
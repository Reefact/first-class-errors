namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the direction of the infrastructure call that failed.
/// </summary>
public enum FlowDirection {

    /// <summary>
    ///     The error occurred while receiving/processing input into the system.
    /// </summary>
    Inbound,
    /// <summary>
    ///     The error occurred while sending requests or data to an external system.
    /// </summary>
    Outbound

}
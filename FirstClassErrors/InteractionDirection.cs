namespace FirstClassErrors;

/// <summary>
///     Represents the architectural direction of an infrastructure interaction.
/// </summary>
/// <remarks>
///     Indicates whether an error originates from an incoming request, an outgoing dependency, or cannot be determined.
/// </remarks>
public enum InteractionDirection {

    /// <summary>
    ///     The origin of the error cannot be determined.
    /// </summary>
    Unknown = 0,
    /// <summary>
    ///     The error originates from an incoming interaction with the system (e.g., API request, user input, external
    ///     command).
    /// </summary>
    Incoming = 1,

    /// <summary>
    ///     The error originates from an outgoing interaction with an external dependency (e.g., database, HTTP service,
    ///     message broker).
    /// </summary>
    Outgoing = 2

}
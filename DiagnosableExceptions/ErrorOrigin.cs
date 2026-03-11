namespace DiagnosableExceptions;

/// <summary>
///     Indicates the most likely origin of a documented diagnostic cause.
/// </summary>
/// <remarks>
///     This classification helps orient investigation by identifying whether the issue is more likely related to
///     internal processing within the system, external data or dependencies, or both. It does not assign responsibility,
///     but highlights the area where analysis should begin.
/// </remarks>
public enum ErrorOrigin {

    /// <summary>
    ///     The error most likely originates from the system's own logic, rules, or implementation.
    /// </summary>
    /// <remarks>
    ///     This includes issues introduced during internal computations, transformations,
    ///     validations, or other processing performed by the system.
    /// </remarks>
    Internal,

    /// <summary>
    ///     The error most likely originates from outside the current system
    ///     (external systems, user input, configuration, persisted data, etc.).
    /// </summary>
    /// <remarks>
    ///     This category covers situations where the system behaves correctly but receives or depends on
    ///     data, configuration, or responses that do not comply with domain or format expectations.
    /// </remarks>
    External,

    /// <summary>
    ///     The error may originate from either the system logic or external data and requires investigation on both sides.
    /// </summary>
    /// <remarks>
    ///     Use this value when the observable symptom does not allow a clear distinction between an internal defect
    ///     and an issue with external inputs, dependencies, or configuration.
    /// </remarks>
    InternalOrExternal

}
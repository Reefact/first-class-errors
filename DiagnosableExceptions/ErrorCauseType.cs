namespace DiagnosableExceptions;

/// <summary>
///     Indicates the most likely origin of a documented diagnostic cause.
/// </summary>
/// <remarks>
///     This classification helps orient investigation by identifying whether the issue is more likely related to external
///     data, internal processing, or both. It does not assign responsibility, but highlights the area where analysis
///     should begin.
/// </remarks>
public enum ErrorCauseType {

    /// <summary>
    ///     The error most likely originates from the system's own logic, rules, or implementation.
    /// </summary>
    /// <remarks>
    ///     This includes issues introduced during internal computations, transformations,
    ///     validations, or other processing performed by the system.
    /// </remarks>
    System,

    /// <summary>
    ///     The error most likely originates from invalid or inconsistent input values provided from outside the current system
    ///     (user input, external systems, configuration, persisted data, etc.).
    /// </summary>
    /// <remarks>
    ///     This category covers situations where the system behaves correctly but receives data that does not comply with
    ///     domain or format expectations.
    /// </remarks>
    Input,

    /// <summary>
    ///     The error may originate from either the system logic or the input values and requires investigation on both sides.
    /// </summary>
    /// <remarks>
    ///     Use this value when the observable symptom does not allow a clear distinction between an internal defect and an
    ///     issue with incoming data.
    /// </remarks>
    SystemOrInput

}
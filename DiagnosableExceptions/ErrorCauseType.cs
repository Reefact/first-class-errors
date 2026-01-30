namespace DiagnosableExceptions;

/// <summary>
///     Specifies the type of cause for an error, categorizing it as a bug, data issue, or a combination of both.
/// </summary>
public enum ErrorCauseType {

    /// <summary>
    ///     The error originates from the system's own logic, rules, or implementation.
    /// </summary>
    System,

    /// <summary>
    ///     The error originates from invalid or inconsistent input values provided
    ///     from outside the current system (user input, external systems, configuration, etc.).
    /// </summary>
    Input,

    /// <summary>
    ///     The error may originate from either the system logic or the input values,
    ///     and requires investigation on both sides.
    /// </summary>
    SystemOrInput

}
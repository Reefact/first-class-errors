namespace DiagnosableExceptions;

/// <summary>
///     Represents the transient nature of an error or condition.
/// </summary>
/// <remarks>
///     This enumeration is used to indicate whether an error or condition is transient,
///     meaning it is likely to resolve itself without intervention, or non-transient,
///     requiring manual resolution. The <see cref="Unknown" /> value is used when the
///     transient nature cannot be determined.
/// </remarks>
public enum Transience {

    /// <summary>
    ///     Represents an unknown transient state.
    /// </summary>
    /// <remarks>
    ///     This value is used when it cannot be determined whether the error or condition is transient
    ///     or non-transient. It serves as a fallback state when the transient nature is ambiguous.
    /// </remarks>
    Unknown = 0,
    /// <summary>
    ///     Indicates that the error or condition is non-transient, meaning it requires manual resolution
    ///     and is unlikely to resolve itself without intervention.
    /// </summary>
    /// <remarks>
    ///     Use this value to explicitly mark errors or conditions that are persistent and require
    ///     corrective action to be resolved.
    /// </remarks>
    NonTransient = 1,
    /// <summary>
    ///     Indicates that the error or condition is transient and likely to resolve itself without intervention.
    /// </summary>
    /// <remarks>
    ///     Use this value to explicitly mark an error or condition as transient, signaling that it does not require manual
    ///     resolution.
    /// </remarks>
    Transient = 2

}
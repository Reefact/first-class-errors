namespace Reefact.DiagnosableExceptions.Usage;

/// <summary>
///     Provides diagnostic information for value object-related errors.
/// </summary>
/// <remarks>
///     This class contains predefined diagnostics for various scenarios where value objects may encounter issues.
///     It is intended to assist in identifying and resolving errors related to domain rules, data integrity, and system
///     behavior.
/// </remarks>
internal static class ValueObjectDiagnostic {

    #region Statics members declarations

    /// <summary>
    ///     Provides a collection of predefined diagnostics for common value object-related errors.
    /// </summary>
    /// <remarks>
    ///     This field contains diagnostic entries that describe typical causes of errors and their corresponding corrective
    ///     actions.
    /// </remarks>
    public static readonly ErrorDiagnostic[] Diagnostic = [
        new() {
            Cause = "The value entered manually by a user is invalid.",
            Type  = ErrorCauseType.Input,
            Fix   = "The user must correct the entered value so that it complies with the domain rules."
        },
        new() {
            Cause = "The value received from an external system (API, message, etc.) is invalid.",
            Type  = ErrorCauseType.Input,
            Fix   = "Ensure upstream systems validate or normalize their data before sending it."
        },
        new() {
            Cause = "The value was loaded from corrupted or outdated persisted data.",
            Type  = ErrorCauseType.Input,
            Fix   = "Correct or migrate persisted data to ensure compliance with current domain rules."
        },
        new() {
            Cause = "The value was computed internally without using domain-safe methods.",
            Type  = ErrorCauseType.System,
            Fix   = "Review and fix the computation logic to ensure it preserves domain invariants."
        },
        new() {
            Cause = "The value originates from system configuration or defaults that are incorrect or outdated.",
            Type  = ErrorCauseType.Input,
            Fix   = "Review and correct system configuration or default parameters so they comply with domain rules."
        }
    ];

    #endregion

}
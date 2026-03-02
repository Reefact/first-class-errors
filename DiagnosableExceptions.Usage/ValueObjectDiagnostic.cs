namespace DiagnosableExceptions.Usage;

/// <summary>
///     Provides diagnostic information for value object-related errors.
/// </summary>
/// <remarks>
///     This class contains predefined diagnostics for various scenarios where value objects may encounter issues.
///     It is intended to assist in identifying and resolving errors related to domain rules, data integrity, and system
///     behavior.
/// </remarks>
internal static class ValueObjectDiagnostic {

    #region Static members

    /// <summary>
    ///     Provides a collection of predefined diagnostics for common value object-related errors.
    /// </summary>
    /// <remarks>
    ///     This field contains diagnostic entries that describe typical causes of errors and their corresponding corrective
    ///     actions.
    /// </remarks>
    public static readonly ErrorDiagnostic[] Diagnostic = [
        new("The value entered manually by a user is invalid.",
            ErrorCauseType.Input,
            "Verify the value entered by the user and assess its compliance with domain rules."
        ),
        new("The value received from an external system (API, message, etc.) is invalid.",
            ErrorCauseType.Input,
            "Check the data provided by the upstream system and evaluate its validity against domain rules."
        ),
        new("The value was loaded from corrupted or outdated persisted data.",
            ErrorCauseType.Input,
            "Examine the persisted data source to determine whether stored values comply with current domain rules."
        ),
        new("The value was computed internally without using domain-safe methods.",
            ErrorCauseType.System,
            "Inspect the internal computation logic to confirm that domain invariants are preserved."
        ),
        new("The value originates from system configuration or defaults that are incorrect or outdated.",
            ErrorCauseType.Input,
            "Review the relevant configuration or default parameters to assess their compliance with domain rules."
        )
    ];

    #endregion

}
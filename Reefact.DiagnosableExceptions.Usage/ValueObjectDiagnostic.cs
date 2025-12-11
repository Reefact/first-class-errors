namespace Reefact.DiagnosableExceptions.Usage;

internal static class ValueObjectDiagnostic {

    #region Statics members declarations

    public static readonly ErrorDiagnostic[] Diagnostic = [
        new() {
            Cause = "The value entered manually by a user is invalid.",
            Fix   = "The user must correct the entered value so that it complies with the domain rules."
        },
        new() {
            Cause = "The value received from an external system (API, message, etc.) is invalid.",
            Fix   = "Ensure upstream systems validate or normalize their data before sending it."
        },
        new() {
            Cause = "The value was loaded from corrupted or outdated persisted data.",
            Fix   = "Correct or migrate persisted data to ensure compliance with current domain rules."
        },
        new() {
            Cause = "The value was computed internally without using domain-safe methods.",
            Fix   = "Review and fix the computation logic to ensure it preserves domain invariants."
        },
        new() {
            Cause = "The value originates from system configuration or defaults that are incorrect or outdated.",
            Fix   = "Review and correct system configuration or default parameters so they comply with domain rules."
        }
    ];

    #endregion

}
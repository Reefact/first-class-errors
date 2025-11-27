namespace Reefact.DiagnosableExceptions.Usage;

internal static class ValueObjectDiagnostic {

    #region Statics members declarations

    public static readonly ErrorDiagnostic[] Diagnostic = [
        new() {
            Cause = "The value entered manually by a user is invalid.",
            Fix   = "The user must enter a valid temperature above absolute zero."
        },
        new() {
            Cause = "The value received from an external system (API, message, etc.) is invalid.",
            Fix   = "Fix the upstream system to ensure it only sends valid temperatures."
        },
        new() {
            Cause = "The value was loaded from corrupted or outdated persisted data.",
            Fix   = "Correct the stored data to comply with the domain rules before attempting to use it."
        },
        new() {
            Cause = "The value was computed internally without using domain-safe methods.",
            Fix   = "Fix the internal computation logic to prevent invalid temperature values."
        }
    ];

    #endregion

}
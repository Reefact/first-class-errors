#region Usings declarations

using FirstClassErrors.Usage.Resources;

#endregion

namespace FirstClassErrors.Usage;

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
    ///     The diagnostics are rebuilt on each access so their text reflects the current UI culture (localized via the
    ///     <see cref="UsageErrorMessages" /> resources) rather than being frozen at type initialization.
    /// </remarks>
    public static ErrorDiagnostic[] Diagnostic => [
        new(UsageErrorMessages.Get("ValueObject_Cause1"),
            ErrorOrigin.External,
            UsageErrorMessages.Get("ValueObject_Hint1")
        ),
        new(UsageErrorMessages.Get("ValueObject_Cause2"),
            ErrorOrigin.External,
            UsageErrorMessages.Get("ValueObject_Hint2")
        ),
        new(UsageErrorMessages.Get("ValueObject_Cause3"),
            ErrorOrigin.External,
            UsageErrorMessages.Get("ValueObject_Hint3")
        ),
        new(UsageErrorMessages.Get("ValueObject_Cause4"),
            ErrorOrigin.Internal,
            UsageErrorMessages.Get("ValueObject_Hint4")
        ),
        new(UsageErrorMessages.Get("ValueObject_Cause5"),
            ErrorOrigin.External,
            UsageErrorMessages.Get("ValueObject_Hint5")
        )
    ];

    #endregion

}
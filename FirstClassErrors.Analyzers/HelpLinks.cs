namespace FirstClassErrors.Analyzers;

/// <summary>
///     Builds the documentation URL surfaced by each diagnostic (the "help link" in the IDE). Per-rule pages are added
///     under <c>doc/handwritten/for-users/analyzers/</c> in a later phase.
/// </summary>
internal static class HelpLinks {

    private const string Base = "https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-users/analyzers";

    public static string For(string diagnosticId) {
        return $"{Base}/{diagnosticId}.en.md";
    }

}

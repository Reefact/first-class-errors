namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary, non-blank internal diagnostic message for tests that need <i>some</i> diagnostic text but
///     do not assert on it. The value reads as arbitrary (for example <c>Any diagnostic message 7F3A9C.</c>) and is
///     drawn from Dummies' ambient random context, so a test wrapped in <c>Dummies.Any.Reproducibly(...)</c> replays it.
/// </summary>
public static class DiagnosticMessageFactory {

    /// <summary>Returns an arbitrary, non-blank diagnostic message.</summary>
    /// <returns>An arbitrary diagnostic message.</returns>
    public static string Any() {
        return "Any diagnostic message " + Dummies.Any.String().WithLength(6).Generate() + ".";
    }

}

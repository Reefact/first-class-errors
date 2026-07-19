namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary, non-blank public detailed message for tests that need <i>some</i> detailed text but do
///     not assert on it. The value reads as arbitrary (for example <c>Any detailed message 7F3A9C.</c>) and is drawn
///     from Dummies' ambient random context, so a test wrapped in <c>Dummies.Any.Reproducibly(...)</c> replays it.
/// </summary>
public static class DetailedMessageFactory {

    /// <summary>Returns an arbitrary, non-blank detailed message.</summary>
    /// <returns>An arbitrary detailed message.</returns>
    public static string Any() {
        return "Any detailed message " + Dummies.Any.String().WithLength(6).Generate() + ".";
    }

}

namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary, non-blank public short message for tests that need <i>some</i> short text but do not
///     assert on it. The value reads as arbitrary (for example <c>Any short message 7F3A9C.</c>) and is drawn from
///     Dummies' ambient random context, so a test wrapped in <c>Dummies.Any.Reproducibly(...)</c> replays it.
/// </summary>
public static class ShortMessageFactory {

    /// <summary>Returns an arbitrary, non-blank short message.</summary>
    /// <returns>An arbitrary short message.</returns>
    public static string Any() {
        return "Any short message " + Dummies.Any.String().WithLength(6).Generate() + ".";
    }

}

namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary <see cref="ErrorOrigin" />, uniformly across all of its members, for tests that need
///     <i>some</i> origin but do not assert on it. Drawn from Dummies' ambient random context.
/// </summary>
public static class ErrorOriginFactory {

    /// <summary>Returns an arbitrary <see cref="ErrorOrigin" />.</summary>
    /// <returns>An arbitrary error origin.</returns>
    public static ErrorOrigin Any() {
        return Dummies.Any.Enum<ErrorOrigin>().Generate();
    }

}

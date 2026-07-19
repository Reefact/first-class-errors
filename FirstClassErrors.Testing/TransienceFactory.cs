namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary <b>meaningful</b> <see cref="Transience" /> — <see cref="Transience.Transient" /> or
///     <see cref="Transience.NonTransient" />, never the <see cref="Transience.Unknown" /> sentinel — for tests that
///     need <i>some</i> classification but do not assert on it. Drawn from Dummies' ambient random context.
/// </summary>
public static class TransienceFactory {

    /// <summary>
    ///     Returns an arbitrary transience classification other than <see cref="Transience.Unknown" />.
    /// </summary>
    /// <returns>An arbitrary meaningful transience.</returns>
    public static Transience Any() {
        return Dummies.Any.Enum<Transience>().Except(Transience.Unknown).Generate();
    }

}

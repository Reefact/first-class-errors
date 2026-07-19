namespace FirstClassErrors.Testing;

/// <summary>
///     Supplies an arbitrary <b>meaningful</b> <see cref="InteractionDirection" /> —
///     <see cref="InteractionDirection.Incoming" /> or <see cref="InteractionDirection.Outgoing" />, never the
///     <see cref="InteractionDirection.Unknown" /> sentinel — for tests that need <i>some</i> direction but do not
///     assert on it. Drawn from Dummies' ambient random context.
/// </summary>
public static class InteractionDirectionFactory {

    /// <summary>
    ///     Returns an arbitrary interaction direction other than <see cref="InteractionDirection.Unknown" />.
    /// </summary>
    /// <returns>An arbitrary meaningful interaction direction.</returns>
    public static InteractionDirection Any() {
        return Dummies.Any.Enum<InteractionDirection>().Except(InteractionDirection.Unknown).Generate();
    }

}

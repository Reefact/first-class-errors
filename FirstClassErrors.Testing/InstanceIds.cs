namespace FirstClassErrors.Testing;

/// <summary>
///     Test-only entry point for controlling the identifier that FirstClassErrors assigns to each error occurrence
///     (<c>Error.InstanceId</c>). Overriding it makes the otherwise-random id deterministic, which is what snapshot and
///     equality assertions over a whole error need.
/// </summary>
/// <remarks>
///     <para>
///         Same contract as <see cref="Clock" />: always scope an override with <c>using</c>; the override flows with
///         the current execution context and does not leak across parallel tests. Outside a scope — in production — a
///         fresh <see cref="Guid" /> is generated as before.
///     </para>
///     <example>
///         <code>
///         using (InstanceIds.UseSequential()) {
///             MyError first  = MyError.Create(...); // InstanceId 00000001-0000-0000-0000-000000000000
///             MyError second = MyError.Create(...); // InstanceId 00000002-0000-0000-0000-000000000000
///         }
///         </code>
///     </example>
/// </remarks>
public static class InstanceIds {

    /// <summary>
    ///     Pins every error created within the scope to the same fixed identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>A scope that restores the default (random) identifier when disposed.</returns>
    public static IDisposable UseFixed(Guid id) {
        return AmbientInstanceId.Use(() => id);
    }

    /// <summary>
    ///     Assigns identifiers from the supplied source. Use this for a custom or mocked id strategy.
    /// </summary>
    /// <param name="next">A function returning the identifier for each error created within the scope.</param>
    /// <returns>A scope that restores the default (random) identifier when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="next" /> is <c>null</c>.</exception>
    public static IDisposable Use(Func<Guid> next) {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return AmbientInstanceId.Use(next);
    }

    /// <summary>
    ///     Assigns readable, monotonically increasing identifiers (<c>00000001-0000-0000-0000-000000000000</c>,
    ///     <c>00000002-...</c>, ...) so several errors created within the scope get distinct yet stable ids. A thin
    ///     convenience over <see cref="Use(Func{Guid})" /> for the common deterministic case.
    /// </summary>
    /// <returns>A scope that restores the default (random) identifier when disposed.</returns>
    public static IDisposable UseSequential() {
        int counter = 0;

        return Use(() => new Guid(++counter, 0, 0, new byte[8]));
    }

    /// <summary>
    ///     Assigns an <b>arbitrary</b> identifier to each error created within the scope. Use this when a test needs
    ///     stable-yet-distinct ids but does not assert on their exact values: it reads as "the ids are irrelevant here".
    /// </summary>
    /// <remarks>
    ///     Each error created within the scope gets its own fresh arbitrary id (as in production, several errors do not
    ///     collide), but the ids are drawn from <see cref="Any" />'s source rather than <see cref="System.Guid.NewGuid" />.
    ///     Wrap the call in <c>Any.UseSeed(...)</c>, or use the <see cref="UseAny(int)" /> overload, to make the sequence
    ///     reproducible. To pin a single fixed id instead, use <see cref="UseFixed" />.
    /// </remarks>
    /// <returns>A scope that restores the default (random) identifier when disposed.</returns>
    public static IDisposable UseAny() {
        return Use(() => ArbitrarySource.Guid());
    }

    /// <summary>
    ///     Assigns arbitrary but <b>reproducible</b> identifiers, drawn from a sequence seeded with
    ///     <paramref name="seed" />, to the errors created within the scope.
    /// </summary>
    /// <param name="seed">The seed that makes the sequence of assigned identifiers reproducible across runs.</param>
    /// <returns>A scope that restores the default (random) identifier when disposed.</returns>
    public static IDisposable UseAny(int seed) {
        Random random = new(seed);

        return Use(() => ArbitrarySource.NewGuid(random));
    }

}

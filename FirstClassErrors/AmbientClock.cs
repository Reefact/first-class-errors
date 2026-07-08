namespace FirstClassErrors;

/// <summary>
///     Ambient clock used by <see cref="Error" /> to stamp <see cref="Error.OccurredAt" />. It reads the real system
///     clock by default, so production behavior is unchanged: <see cref="UtcNow" /> is exactly
///     <see cref="DateTimeOffset.UtcNow" />.
/// </summary>
/// <remarks>
///     <para>
///         The clock is captured deep inside <see cref="Error" /> construction (a staged-builder pipeline), where
///         threading an explicit dependency through every error type would be invasive. An ambient hook keeps the
///         construction API untouched while still allowing tests to pin the occurrence time.
///     </para>
///     <para>
///         This type is <c>internal</c> on purpose: the public, mockable test seam lives in the companion
///         <c>FirstClassErrors.Testing</c> package (a friend assembly), so the core library exposes no ambient mutable
///         state and takes no runtime dependency. The override is stored in an <see cref="AsyncLocal{T}" /> so it flows
///         with the test's execution context and never leaks across tests running in parallel.
///     </para>
/// </remarks>
internal static class AmbientClock {

    #region Fields declarations

    private static readonly AsyncLocal<Func<DateTimeOffset>?> Overridden = new();

    #endregion

    /// <summary>
    ///     Gets the current UTC instant: the test override when one is in scope, otherwise the real system clock
    ///     (<see cref="DateTimeOffset.UtcNow" />).
    /// </summary>
    internal static DateTimeOffset UtcNow => Overridden.Value?.Invoke() ?? DateTimeOffset.UtcNow;

    /// <summary>
    ///     Overrides the ambient clock for the current execution context until the returned scope is disposed. Intended
    ///     for tests only; surfaced publicly through <c>FirstClassErrors.Testing.Clock</c>.
    /// </summary>
    /// <param name="now">A function returning the UTC instant to use while the scope is active.</param>
    /// <returns>A scope that restores the previous clock when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="now" /> is <c>null</c>.</exception>
    internal static IDisposable Use(Func<DateTimeOffset> now) {
        if (now is null) { throw new ArgumentNullException(nameof(now)); }

        Func<DateTimeOffset>? previous = Overridden.Value;
        Overridden.Value = now;

        return new OverrideScope(previous);
    }

    #region Nested types

    private sealed class OverrideScope : IDisposable {

        private readonly Func<DateTimeOffset>? _previous;
        private          bool                  _disposed;

        internal OverrideScope(Func<DateTimeOffset>? previous) {
            _previous = previous;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed        = true;
            Overridden.Value = _previous;
        }

    }

    #endregion

}

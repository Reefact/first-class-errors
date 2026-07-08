namespace FirstClassErrors;

/// <summary>
///     Abstraction over the wall-clock reading used when an <see cref="Error" /> captures its
///     <see cref="Error.OccurredAt" />. It exists so occurrence times can be made deterministic in tests; production
///     code never sees it — the ambient <see cref="Clock" /> resolves to the real system clock unless a test overrides it.
/// </summary>
/// <remarks>
///     This is intentionally a home-grown, <c>internal</c> abstraction rather than <c>System.TimeProvider</c> (absent on
///     netstandard2.0) so the shipped library keeps its zero runtime-dependency footprint. It is <c>internal</c> so it
///     adds nothing to the public API; tests substitute it via the assembly's <c>InternalsVisibleTo</c> grant.
/// </remarks>
internal interface IClock {

    /// <summary>
    ///     Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    DateTimeOffset UtcNow { get; }

}

/// <summary>
///     Ambient access point to the current <see cref="IClock" />. It reads the real system clock by default, so
///     production behavior is unchanged: <see cref="UtcNow" /> is exactly <see cref="DateTimeOffset.UtcNow" />.
/// </summary>
/// <remarks>
///     <para>
///         The clock is captured deep inside <see cref="Error" /> construction (a staged-builder pipeline), where
///         threading an explicit dependency through every error type would be invasive. An ambient hook keeps the
///         construction API untouched while still allowing tests to pin the occurrence time.
///     </para>
///     <para>
///         The test override is stored in an <see cref="AsyncLocal{T}" /> so it flows with the test's execution context
///         and does not leak across tests running in parallel. Always scope an override with <c>using</c> so it is
///         restored on exit.
///     </para>
/// </remarks>
internal static class Clock {

    #region Fields declarations

    private static readonly AsyncLocal<IClock?> Overridden = new();

    #endregion

    /// <summary>
    ///     Gets the current UTC instant from the ambient clock: the test override when one is in scope, otherwise the
    ///     real system clock (<see cref="DateTimeOffset.UtcNow" />).
    /// </summary>
    internal static DateTimeOffset UtcNow => (Overridden.Value ?? SystemClock.Instance).UtcNow;

    /// <summary>
    ///     Overrides the ambient clock for the current execution context until the returned scope is disposed. Intended
    ///     for tests only.
    /// </summary>
    /// <param name="clock">The clock to use while the scope is active.</param>
    /// <returns>A scope that restores the previous clock when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clock" /> is <c>null</c>.</exception>
    internal static IDisposable Use(IClock clock) {
        if (clock is null) { throw new ArgumentNullException(nameof(clock)); }

        IClock? previous = Overridden.Value;
        Overridden.Value = clock;

        return new OverrideScope(previous);
    }

    #region Nested types

    private sealed class SystemClock : IClock {

        internal static readonly IClock Instance = new SystemClock();

        private SystemClock() { }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    }

    private sealed class OverrideScope : IDisposable {

        private readonly IClock? _previous;
        private          bool    _disposed;

        internal OverrideScope(IClock? previous) {
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

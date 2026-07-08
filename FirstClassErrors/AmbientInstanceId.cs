namespace FirstClassErrors;

/// <summary>
///     Ambient source of the identifier assigned to each <see cref="Error" /> occurrence
///     (<see cref="Error.InstanceId" />). It produces a fresh <see cref="Guid" /> by default, so production behavior is
///     unchanged: <see cref="Next" /> is exactly <see cref="Guid.NewGuid" />.
/// </summary>
/// <remarks>
///     Counterpart of <see cref="AmbientClock" /> for the other non-deterministic field captured at construction. It is
///     <c>internal</c> on purpose; the public, test-only override lives in the companion <c>FirstClassErrors.Testing</c>
///     package. The override is stored in an <see cref="AsyncLocal{T}" /> so it flows with the test's execution context
///     and never leaks across tests running in parallel.
/// </remarks>
internal static class AmbientInstanceId {

    #region Fields declarations

    private static readonly AsyncLocal<Func<Guid>?> Overridden = new();

    #endregion

    /// <summary>
    ///     Gets the next instance identifier: the test override when one is in scope, otherwise a fresh
    ///     <see cref="Guid.NewGuid" />.
    /// </summary>
    internal static Guid Next() {
        return Overridden.Value?.Invoke() ?? Guid.NewGuid();
    }

    /// <summary>
    ///     Overrides the instance-id source for the current execution context until the returned scope is disposed.
    ///     Intended for tests only; surfaced publicly through <c>FirstClassErrors.Testing.InstanceIds</c>.
    /// </summary>
    /// <param name="next">A function returning the identifier to assign while the scope is active.</param>
    /// <returns>A scope that restores the previous source when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="next" /> is <c>null</c>.</exception>
    internal static IDisposable Use(Func<Guid> next) {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        Func<Guid>? previous = Overridden.Value;
        Overridden.Value = next;

        return new OverrideScope(previous);
    }

    #region Nested types

    private sealed class OverrideScope : IDisposable {

        private readonly Func<Guid>? _previous;
        private          bool        _disposed;

        internal OverrideScope(Func<Guid>? previous) {
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

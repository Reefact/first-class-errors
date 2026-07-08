namespace FirstClassErrors.Testing;

/// <summary>
///     Test-only entry point for controlling the clock that FirstClassErrors uses to stamp an error's
///     <c>OccurredAt</c>. Overriding it lets tests assert an exact occurrence time instead of a time window.
/// </summary>
/// <remarks>
///     <para>
///         Always scope an override with <c>using</c> so the real system clock is restored on exit. The override flows
///         with the current execution context (it is backed by an <see cref="AsyncLocal{T}" /> inside the library), so
///         it does not leak across tests running in parallel.
///     </para>
///     <para>
///         Outside an active scope — i.e. in production — the clock is always the real system clock; this type only
///         affects code that runs inside a <c>using</c> block created here.
///     </para>
///     <example>
///         <code>
///         using (Clock.UseFixed(new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero))) {
///             MyError error = MyError.Create(...);
///             Assert.Equal(new DateTimeOffset(2026, 7, 8, 10, 30, 0, TimeSpan.Zero), error.OccurredAt);
///         }
///         </code>
///     </example>
/// </remarks>
public static class Clock {

    /// <summary>
    ///     Overrides the clock with the supplied <see cref="IClock" /> until the returned scope is disposed. Use this
    ///     when you need a moving or mocked clock; otherwise prefer <see cref="UseFixed" />.
    /// </summary>
    /// <param name="clock">The clock to use while the scope is active.</param>
    /// <returns>A scope that restores the real system clock when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clock" /> is <c>null</c>.</exception>
    public static IDisposable Use(IClock clock) {
        if (clock is null) { throw new ArgumentNullException(nameof(clock)); }

        return AmbientClock.Use(() => clock.UtcNow);
    }

    /// <summary>
    ///     Pins the clock to a single fixed instant until the returned scope is disposed. This is the simplest way to
    ///     make occurrence times deterministic and requires no mocking library.
    /// </summary>
    /// <param name="instant">The UTC instant every error created within the scope will record as its occurrence time.</param>
    /// <returns>A scope that restores the real system clock when disposed.</returns>
    public static IDisposable UseFixed(DateTimeOffset instant) {
        return AmbientClock.Use(() => instant);
    }

}

namespace FirstClassErrors.Testing;

/// <summary>
///     Abstraction over a UTC wall-clock reading, used to make an error's <c>OccurredAt</c> deterministic in tests.
///     Implement it, or mock it with your mocking library of choice, and install it with
///     <see cref="Clock.Use(IClock)" />.
/// </summary>
/// <remarks>
///     For the common case of a single fixed instant, prefer <see cref="Clock.UseFixed" />, which needs no mocking
///     library at all.
/// </remarks>
public interface IClock {

    /// <summary>
    ///     Gets the current Coordinated Universal Time (UTC) that errors should record as their occurrence time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

}

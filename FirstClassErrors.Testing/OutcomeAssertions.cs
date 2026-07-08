namespace FirstClassErrors.Testing;

/// <summary>
///     Fluent, framework-agnostic assertions over <see cref="Outcome" /> and <see cref="Outcome{T}" />. They remove the
///     boilerplate of unwrapping an outcome by hand (and the null-forgiving <c>outcome.Error!</c>), and produce a clear
///     failure message instead of the domain's <see cref="DiagnosableException" /> when an expectation is not met.
/// </summary>
/// <remarks>
///     A failing assertion throws an <see cref="OutcomeAssertionException" />, which any test framework reports as a
///     failure; nothing here depends on a specific test or assertion library.
/// </remarks>
public static class OutcomeAssertions {

    /// <summary>
    ///     Asserts that the outcome is a success.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outcome" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the outcome is a failure.</exception>
    public static void ShouldSucceed(this Outcome outcome) {
        if (outcome is null) { throw new ArgumentNullException(nameof(outcome)); }

        if (outcome.IsFailure) {
            throw new OutcomeAssertionException(DescribeUnexpectedFailure(outcome.Error!));
        }
    }

    /// <summary>
    ///     Asserts that the outcome is a failure and returns a fluent handle for further checks on the error.
    /// </summary>
    /// <returns>An <see cref="ErrorAssertion" /> over the failing error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outcome" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the outcome is a success.</exception>
    public static ErrorAssertion ShouldFail(this Outcome outcome) {
        if (outcome is null) { throw new ArgumentNullException(nameof(outcome)); }

        if (outcome.IsSuccess) {
            throw new OutcomeAssertionException("Expected the outcome to be a failure, but it was a success.");
        }

        return new ErrorAssertion(outcome.Error!);
    }

    /// <summary>
    ///     Asserts that the outcome is a success and returns the carried value.
    /// </summary>
    /// <typeparam name="T">The type of the value carried by a successful outcome.</typeparam>
    /// <returns>The value of the successful outcome.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outcome" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the outcome is a failure.</exception>
    public static T ShouldSucceed<T>(this Outcome<T> outcome)
        where T : notnull {
        if (outcome is null) { throw new ArgumentNullException(nameof(outcome)); }

        if (outcome.IsFailure) {
            throw new OutcomeAssertionException(DescribeUnexpectedFailure(outcome.Error!));
        }

        return outcome.GetResultOrThrow();
    }

    /// <summary>
    ///     Asserts that the outcome is a failure and returns a fluent handle for further checks on the error.
    /// </summary>
    /// <typeparam name="T">The type of the value a successful outcome would carry.</typeparam>
    /// <returns>An <see cref="ErrorAssertion" /> over the failing error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="outcome" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the outcome is a success.</exception>
    public static ErrorAssertion ShouldFail<T>(this Outcome<T> outcome)
        where T : notnull {
        if (outcome is null) { throw new ArgumentNullException(nameof(outcome)); }

        if (outcome.IsSuccess) {
            throw new OutcomeAssertionException($"Expected the outcome to be a failure, but it succeeded with value {Describe(outcome.GetResultOrThrow())}.");
        }

        return new ErrorAssertion(outcome.Error!);
    }

    private static string DescribeUnexpectedFailure(Error error) {
        return $"Expected the outcome to succeed, but it failed with [{error.Code}]: {error.DiagnosticMessage}";
    }

    private static string Describe(object? value) {
        return value is null ? "null" : $"\"{value}\"";
    }

}

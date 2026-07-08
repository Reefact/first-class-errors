namespace FirstClassErrors.Testing;

/// <summary>
///     Thrown by the FirstClassErrors testing assertions (<see cref="OutcomeAssertions" /> and
///     <see cref="ErrorAssertion" />) when an expectation is not met. It carries a human-readable message describing the
///     mismatch; any test framework reports it as a failing test. The library intentionally throws its own exception
///     rather than depending on a specific assertion framework.
/// </summary>
public sealed class OutcomeAssertionException : Exception {

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutcomeAssertionException" /> class.
    /// </summary>
    /// <param name="message">A description of the failed expectation.</param>
    public OutcomeAssertionException(string message) : base(message) { }

}

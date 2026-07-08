namespace FirstClassErrors.Testing;

/// <summary>
///     Fluent assertions over an <see cref="Error" />, returned by <see cref="OutcomeAssertions.ShouldFail(Outcome)" />
///     and its overloads. Each method checks one facet and returns the same instance so expectations can be chained;
///     a failing check throws an <see cref="OutcomeAssertionException" /> with a descriptive message.
/// </summary>
/// <remarks>
///     <example>
///         <code>
///         outcome.ShouldFail()
///                .WithCode("PAYMENT.DECLINED")
///                .WithContextEntry("CardNetwork", "VISA");
///         </code>
///     </example>
/// </remarks>
public sealed class ErrorAssertion {

    #region Fields declarations

    private readonly Error _error;

    #endregion

    #region Constructors declarations

    internal ErrorAssertion(Error error) {
        _error = error;
    }

    #endregion

    /// <summary>
    ///     Gets the asserted <see cref="Error" />, as an escape hatch for checks not covered by this fluent surface.
    /// </summary>
    public Error Subject => _error;

    /// <summary>
    ///     Asserts that the error's <see cref="Error.Code" /> equals the given code.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedCode" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the code does not match.</exception>
    public ErrorAssertion WithCode(string expectedCode) {
        if (expectedCode is null) { throw new ArgumentNullException(nameof(expectedCode)); }

        string actual = _error.Code.ToString();
        if (!string.Equals(actual, expectedCode, StringComparison.Ordinal)) {
            throw new OutcomeAssertionException($"Expected the error to have code \"{expectedCode}\", but it was \"{actual}\".");
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the error's <see cref="Error.Code" /> equals the given code.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedCode" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the code does not match.</exception>
    public ErrorAssertion WithCode(ErrorCode expectedCode) {
        if (expectedCode is null) { throw new ArgumentNullException(nameof(expectedCode)); }

        return WithCode(expectedCode.ToString());
    }

    /// <summary>
    ///     Asserts that the error's <see cref="Error.DiagnosticMessage" /> equals the given message.
    /// </summary>
    /// <exception cref="OutcomeAssertionException">Thrown when the diagnostic message does not match.</exception>
    public ErrorAssertion WithDiagnosticMessage(string expected) {
        if (!string.Equals(_error.DiagnosticMessage, expected, StringComparison.Ordinal)) {
            throw new OutcomeAssertionException($"Expected the error's diagnostic message to be \"{expected}\", but it was \"{_error.DiagnosticMessage}\".");
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the error's <see cref="Error.ShortMessage" /> equals the given message.
    /// </summary>
    /// <exception cref="OutcomeAssertionException">Thrown when the short message does not match.</exception>
    public ErrorAssertion WithShortMessage(string expected) {
        if (!string.Equals(_error.ShortMessage, expected, StringComparison.Ordinal)) {
            throw new OutcomeAssertionException($"Expected the error's short message to be \"{expected}\", but it was \"{_error.ShortMessage}\".");
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the error context contains an entry under the given key, regardless of its value.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when no entry with that key is present.</exception>
    public ErrorAssertion WithContextEntry(string key) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        if (!_error.Context.ToNameDictionary().ContainsKey(key)) {
            throw new OutcomeAssertionException($"Expected the error context to contain an entry \"{key}\", but it did not. Present keys: {DescribeKeys()}.");
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the error context contains an entry under the given key whose value equals
    ///     <paramref name="expectedValue" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <c>null</c>.</exception>
    /// <exception cref="OutcomeAssertionException">Thrown when the entry is missing or its value differs.</exception>
    public ErrorAssertion WithContextEntry(string key, object? expectedValue) {
        WithContextEntry(key);

        object? actual = _error.Context.ToNameDictionary()[key];
        if (!Equals(actual, expectedValue)) {
            throw new OutcomeAssertionException($"Expected the error context entry \"{key}\" to equal {Format(expectedValue)}, but it was {Format(actual)}.");
        }

        return this;
    }

    private string DescribeKeys() {
        IReadOnlyDictionary<string, object?> entries = _error.Context.ToNameDictionary();

        return entries.Count == 0 ? "(none)" : string.Join(", ", entries.Keys);
    }

    private static string Format(object? value) {
        return value is null ? "null" : $"\"{value}\"";
    }

}

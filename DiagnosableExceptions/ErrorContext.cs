#region Usings declarations

using DiagnosableExceptions;

#endregion

/// <summary>
///     Represents a context that provides additional information about an error.
/// </summary>
/// <remarks>
///     The <see cref="ErrorContext" /> class encapsulates key-value pairs that describe supplementary details about an
///     error, which can be used for diagnostics and troubleshooting. It supports retrieving values by their associated
///     keys and converting the context into a dictionary with string keys for easier inspection.
/// </remarks>
public sealed class ErrorContext {

    #region Static members

    private static readonly Dictionary<ErrorContextKey, object?> EmptyValues = new(0);

    /// <summary>Represents an empty diagnostic context.</summary>
    public static ErrorContext Empty { get; } = new(EmptyValues);

    #endregion

    #region Fields

    private readonly Dictionary<ErrorContextKey, object?> _values;

    #endregion

    #region Constructors & Destructor

    internal ErrorContext(Dictionary<ErrorContextKey, object?> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        _values = new Dictionary<ErrorContextKey, object?>(values);
    }

    #endregion

    /// <summary>
    ///     Gets a read-only dictionary containing the key-value pairs that provide additional details about the error context.
    /// </summary>
    /// <value>
    ///     A dictionary where the keys are of type <see cref="ErrorContextKey" /> and the values are objects representing the
    ///     associated data.
    /// </value>
    /// <remarks>
    ///     The <see cref="Values" /> property allows access to the supplementary information stored in the error context. This
    ///     information can be used for diagnostics, logging, or troubleshooting purposes.
    /// </remarks>
    public IReadOnlyDictionary<ErrorContextKey, object?> Values => _values;

    /// <summary>
    ///     Gets a value indicating whether this context contains no entries.
    /// </summary>
    public bool IsEmpty => _values.Count == 0;

    /// <summary>
    ///     Attempts to retrieve a value from the <see cref="ErrorContext" /> associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value associated with the specified key.</typeparam>
    /// <param name="key">
    ///     The <see cref="ErrorContextKey{T}" /> representing the key for which the value is to be retrieved.
    /// </param>
    /// <param name="value">
    ///     When this method returns, contains the value associated with the specified key if the key exists and the value is
    ///     of the expected type; otherwise, the default value for the type <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the key exists in the <see cref="ErrorContext" /> and the value is of the expected type; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method is useful for safely retrieving strongly-typed values from the <see cref="ErrorContext" /> without the
    ///     need for explicit casting. If the key does not exist or the value is not of the expected type, the method returns
    ///     <c>false</c>, and the <paramref name="value" /> parameter is set to its default value.
    /// </remarks>
    public bool TryGet<T>(ErrorContextKey<T> key, out T? value) {
        if (_values.TryGetValue(key, out object? raw) && raw is T typed) {
            value = typed;

            return true;
        }

        value = default;

        return false;
    }

    /// <summary>
    ///     Converts the current <see cref="ErrorContext" /> into a dictionary with string keys representing the names of the
    ///     context keys.
    /// </summary>
    /// <returns>
    ///     A read-only dictionary where the keys are the names of the <see cref="ErrorContextKey" /> instances,
    ///     and the values are the corresponding values from the context.
    /// </returns>
    /// <remarks>
    ///     This method is useful for scenarios where the context needs to be represented in a more human-readable format,
    ///     such as logging or debugging. The keys in the resulting dictionary are derived from the
    ///     <see cref="ErrorContextKey.Name" /> property.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> ToNameDictionary() {
        return _values.ToDictionary(k => k.Key.Name, v => v.Value);
    }

}
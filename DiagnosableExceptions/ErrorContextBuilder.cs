namespace DiagnosableExceptions;

/// <summary>
///     Provides functionality to build an <see cref="ErrorContext" /> by adding key-value pairs.
/// </summary>
/// <remarks>
///     This builder is intended to be used only during exception construction. The resulting <see cref="ErrorContext" />
///     is immutable and represents a snapshot of the diagnostic information at the time of the error.
/// </remarks>
public sealed class ErrorContextBuilder {

    #region Fields

    private readonly Dictionary<ErrorContextKey, object?> _values = new();

    #endregion

    /// <summary>
    ///     Adds a key-value pair to the <see cref="ErrorContextBuilder" /> for building an <see cref="ErrorContext" />.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the specified key.</typeparam>
    /// <param name="key">The key representing the context information to be added. Must not be <c>null</c>.</param>
    /// <param name="value">The value associated with the specified key. Can be <c>null</c>.</param>
    /// <returns>The current instance of <see cref="ErrorContextBuilder" /> to allow method chaining.</returns>
    /// <remarks>
    ///     This method is used to include additional diagnostic information in the error context, which can later be utilized
    ///     for debugging or logging purposes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="key" /> is <c>null</c>.</exception>
    public ErrorContextBuilder Add<T>(ErrorContextKey<T> key, T? value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        _values[key] = value;

        return this;
    }

    internal ErrorContext Build() {
        return new ErrorContext(_values);
    }

}
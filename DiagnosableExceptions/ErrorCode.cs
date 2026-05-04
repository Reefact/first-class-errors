namespace DiagnosableExceptions;

/// <summary>
///     Represents a stable identifier for a specific error condition.
/// </summary>
public sealed class ErrorCode : IEquatable<ErrorCode> {

    #region Statics members declarations

    private static readonly HashSet<string> Registered = new(StringComparer.Ordinal);
    private static readonly object          Lock       = new();

    /// <summary>
    ///     Represents an unspecified error condition. This is used as a default value when no specific error code is provided.
    /// </summary>
    internal static readonly ErrorCode Unspecified = Create("#UNSPECIFIED");

    /// <summary>
    ///     Creates a new instance of the <see cref="ErrorCode" /> class with the specified code.
    /// </summary>
    /// <param name="code">The unique string identifier for the error condition.</param>
    /// <returns>A new <see cref="ErrorCode" /> instance representing the specified error condition.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the <paramref name="code" /> is null, empty, or consists only of
    ///     whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the <paramref name="code" /> has already been registered.</exception>
    public static ErrorCode Create(string code) {
        if (string.IsNullOrWhiteSpace(code)) { throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code)); }

        lock (Lock) {
            if (!Registered.Add(code)) { throw new InvalidOperationException($"Error code '{code}' has already been registered."); }
        }

        return new ErrorCode(code);
    }

    /// <summary>
    ///     Resets the internal state of registered <see cref="ErrorContextKey" /> instances.
    /// </summary>
    /// <remarks>
    ///     This method is intended for use in testing scenarios only. It clears all registered keys,
    ///     allowing a clean slate for subsequent tests that rely on <see cref="ErrorContextKey" /> registration.
    /// </remarks>
    internal static void ResetForTests() {
        lock (Lock) {
            Registered.Clear();
        }
    }

    #endregion

    /// <summary>
    ///     Determines whether two <see cref="ErrorCode" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ErrorCode" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ErrorCode" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ErrorCode" /> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(ErrorCode? left, ErrorCode? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="ErrorCode" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ErrorCode" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ErrorCode" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ErrorCode" /> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(ErrorCode? left, ErrorCode? right) {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Implicitly converts an <see cref="ErrorCode" /> instance to its string representation.
    /// </summary>
    /// <param name="errorCode">The <see cref="ErrorCode" /> instance to convert.</param>
    /// <returns>The string representation of the specified <see cref="ErrorCode" />.</returns>
    public static implicit operator string(ErrorCode errorCode) {
        return errorCode._code;
    }

    #region Fields declarations

    private readonly string _code;

    #endregion

    #region Constructors declarations

    private ErrorCode(string code) {
        _code = code;
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() {
        return _code;
    }

    /// <inheritdoc />
    public bool Equals(ErrorCode? other) {
        return other is not null && _code == other._code;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is ErrorCode other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(_code);
    }

}
namespace DiagnosableExceptions;

/// <summary>
///     Represents a stable identifier for a specific error condition.
/// </summary>
public sealed class ErrorCode : IEquatable<ErrorCode> {

    #region Static members

    private static readonly HashSet<string> Registered = new(StringComparer.Ordinal);
    private static readonly object          Lock       = new();

    public static ErrorCode Create(string code) {
        if (string.IsNullOrWhiteSpace(code)) { throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code)); }

        lock (Lock) {
            if (!Registered.Add(code)) { throw new InvalidOperationException($"An ErrorCode '{code}' has already been registered."); }
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

    public static bool operator ==(ErrorCode? left, ErrorCode? right) {
        return Equals(left, right);
    }

    public static bool operator !=(ErrorCode? left, ErrorCode? right) {
        return !Equals(left, right);
    }

    public static implicit operator string(ErrorCode errorCode) {
        return errorCode._code;
    }

    #region Fields

    private readonly string _code;

    #endregion

    #region Constructors & Destructor

    private ErrorCode(string code) {
        _code = code;
    }

    #endregion

    public override string ToString() {
        return _code;
    }

    public bool Equals(ErrorCode? other) {
        return other is not null && _code == other._code;
    }

    public override bool Equals(object? obj) {
        return obj is ErrorCode other && Equals(other);
    }

    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(_code);
    }

}
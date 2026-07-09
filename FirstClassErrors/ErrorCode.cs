#region Usings declarations

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace FirstClassErrors;

/// <summary>
///     Represents a stable identifier for a specific error condition.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class ErrorCode : IEquatable<ErrorCode> {

    #region Statics members declarations

    /// <summary>
    ///     Represents an unspecified error condition. This is used as a default value when no specific error code is provided.
    /// </summary>
    internal static readonly ErrorCode Unspecified = Create("#UNSPECIFIED");

    /// <summary>
    ///     Creates a new instance of the <see cref="ErrorCode" /> class with the specified code.
    /// </summary>
    /// <param name="code">The string identifier for the error condition.</param>
    /// <returns>A new <see cref="ErrorCode" /> instance representing the specified error condition.</returns>
    /// <remarks>
    ///     An error code is a value: two <see cref="ErrorCode" /> instances built from the same <paramref name="code" />
    ///     compare equal, so creating the same code more than once is allowed and never throws. A code is an identity, not a
    ///     runtime registry entry — a duplicated code silently merges two distinct errors into one, which the <c>FCE001</c>
    ///     analyzer flags at build time.
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     Thrown when the <paramref name="code" /> is null, empty, or consists only of
    ///     whitespace.
    /// </exception>
    public static ErrorCode Create(string code) {
        if (string.IsNullOrWhiteSpace(code)) { throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code)); }

        return new ErrorCode(code);
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorCode" /> is <c>null</c>.</exception>
    [SuppressMessage("Major Code Smell", "S3877:Exceptions should not be thrown from unexpected methods",
                     Justification =
                         "Deliberate technical guard, not domain logic: it turns the opaque NullReferenceException " +
                         "that the following 'errorCode._code' would raise into a clear ArgumentNullException(nameof(errorCode)). " +
                         "ErrorCode is a reference type still reachable as null via null-forgiving casts, default(ErrorCode), " +
                         "reflection, or consumers without nullable reference types enabled. The conversion stays implicit to " +
                         "avoid a breaking public API change, since null only arises on caller bug paths, never from legitimate use.")]
    public static implicit operator string(ErrorCode errorCode) {
        if (errorCode is null) { throw new ArgumentNullException(nameof(errorCode)); }

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
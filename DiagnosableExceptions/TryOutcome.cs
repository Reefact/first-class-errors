namespace Reefact.DiagnosableExceptions;

/// <summary>
///     Represents the outcome of a try operation, encapsulating either a successful result or an exception.
/// </summary>
/// <typeparam name="T">
///     The type of the value returned in case of a successful operation.
/// </typeparam>
/// <remarks>
///     This struct is designed to provide a unified way to handle operations that can either succeed or fail,
///     without relying on exceptions for control flow.
/// </remarks>
public readonly struct TryOutcome<T> {

    /// <summary>
    ///     Creates a successful <see cref="TryOutcome{T}" /> instance with the specified value.
    /// </summary>
    /// <param name="value">The value representing the successful outcome.</param>
    /// <returns>A <see cref="TryOutcome{T}" /> instance encapsulating the specified value.</returns>
    /// <remarks>
    ///     Use this method to represent a successful operation result within the <see cref="TryOutcome{T}" /> structure.
    /// </remarks>
    public static TryOutcome<T> Success(T value) {
        return new TryOutcome<T>(value, null);
    }

    /// <summary>
    ///     Creates a failed <see cref="TryOutcome{T}" /> instance with the specified exception.
    /// </summary>
    /// <param name="exception">The exception representing the failure.</param>
    /// <returns>A <see cref="TryOutcome{T}" /> instance encapsulating the specified exception.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="exception" /> parameter is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     Use this method to represent a failed operation result within the <see cref="TryOutcome{T}" /> structure.
    /// </remarks>
    public static TryOutcome<T> Failure(Exception exception) {
        if (exception is null) { throw new ArgumentNullException(nameof(exception)); }

        return new TryOutcome<T>(default, exception);
    }

    /// <summary>
    ///     Gets a value indicating whether the operation resulted in an error.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the operation failed and an exception is encapsulated; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///     This property is <c>true</c> when the <see cref="Exception" /> property is not <c>null</c>, indicating that the
    ///     operation did not succeed.
    /// </remarks>
    public bool HasError => Exception != null;

    /// <summary>
    ///     Gets the value representing the successful outcome of the operation.
    /// </summary>
    /// <value>
    ///     The value of type <typeparamref name="T" /> if the operation was successful; otherwise, <c>null</c>.
    /// </value>
    /// <remarks>
    ///     This property should only be accessed when the operation has succeeded. Check the <see cref="HasError" /> property
    ///     to determine whether the operation failed.
    /// </remarks>
    public T? Value { get; }
    /// <summary>
    ///     Gets the exception that represents the failure of the operation, if any.
    /// </summary>
    /// <value>
    ///     The exception encapsulated in this <see cref="TryOutcome{T}" /> instance if the operation failed; otherwise,
    ///     <c>null</c>.
    /// </value>
    /// <remarks>
    ///     This property is <c>null</c> when the operation was successful. Use the <see cref="HasError" /> property to
    ///     determine whether an exception is present.
    /// </remarks>
    public Exception? Exception { get; }

    private TryOutcome(T? value, Exception? exception) {
        Value     = value;
        Exception = exception;
    }

}
namespace DiagnosableExceptions;

/// <summary>
///     Represents the outcome of an attempted operation that may fail without throwing an exception.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TryOutcome{T}" /> allows exceptions to be used as <b>error information</b> rather than solely as
///         control-flow mechanisms. It provides a structured way to capture and propagate a failure, including its
///         associated exception, without interrupting execution.
///     </para>
///     <para>
///         This type is particularly useful in scenarios such as validation, parsing, or value object creation, where
///         failure is expected and should be handled explicitly rather than through thrown exceptions.
///     </para>
///     <para>
///         A <see cref="TryOutcome{T}" /> instance is always in one of two states:
///     </para>
///     <list type="bullet">
///         <item><b>Success</b>: contains a valid value of type <typeparamref name="T" />.</item>
///         <item><b>Failure</b>: contains an exception describing why the operation failed.</item>
///     </list>
///     <para>
///         This type does not represent runtime crashes or unexpected failures. Instead, it models anticipated error
///         conditions as data, while still leveraging the richness of the exception model for diagnostic purposes.
///     </para>
/// </remarks>
public readonly struct TryOutcome<T>
    where T : notnull {

    private readonly T? _value;

    /// <summary>
    ///     Creates a successful outcome containing the specified value.
    /// </summary>
    /// <param name="value">
    ///     The value produced by the successful operation.
    /// </param>
    /// <returns>
    ///     A <see cref="TryOutcome{T}" /> representing success.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="value" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     Use this method when the attempted operation completed successfully and produced a valid result.
    /// </remarks>
    public static TryOutcome<T> Success(T value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return new TryOutcome<T>(value, null);
    }

    /// <summary>
    ///     Creates a failed outcome containing the specified exception.
    /// </summary>
    /// <param name="exception">
    ///     The exception describing why the operation failed.
    /// </param>
    /// <returns>
    ///     A <see cref="TryOutcome{T}" /> representing failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="exception" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method is used when the operation could not produce a valid result. The exception serves as a structured
    ///     diagnostic description of the failure.
    /// </remarks>
    public static TryOutcome<T> Failure(Exception exception) {
        if (exception is null) { throw new ArgumentNullException(nameof(exception)); }

        return new TryOutcome<T>(default, exception);
    }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    /// <remarks>
    ///     When <c>true</c>, <see cref="Exception" /> contains the reason for the failure.
    /// </remarks>
    public bool IsFailure => Exception != null;

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <remarks>
    ///     When <c>true</c>, <see cref="Value" /> is guaranteed to contain a valid result.
    /// </remarks>
    public bool IsSuccess => Exception == null;

    /// <summary>
    ///     Gets the result value when the outcome represents success.
    /// </summary>
    /// <exception cref="Exception">
    ///     Thrown when the outcome represents failure. The exception originally captured in this instance is rethrown.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         When the outcome represents success, this property returns the value produced by the operation.
    ///     </para>
    ///     <para>
    ///         When the outcome represents failure, accessing this property rethrows the exception captured in
    ///         <see cref="Exception" />. This behavior allows a failed outcome to be escalated back into the exception-based
    ///         error handling model without losing any diagnostic information.
    ///     </para>
    ///     <para>
    ///         Use this property when you expect the operation to have succeeded and want a direct value, allowing failures to
    ///         propagate as exceptions.
    ///     </para>
    ///     <para>
    ///         Prefer using <see cref="GetOrThrow" /> when the intent is to explicitly escalate a failure into an exception.
    ///         The method form makes this decision clearer at the call site, while this property is better suited when
    ///         accessing the value is the primary intent.
    ///     </para>
    /// </remarks>
    public T Value => IsSuccess ? _value! : throw Exception!;

    /// <summary>
    ///     Gets the exception that represents the failure of the operation, if any.
    /// </summary>
    /// <value>
    ///     The exception encapsulated in this <see cref="TryOutcome{T}" /> instance if the operation failed; otherwise,
    ///     <c>null</c>.
    /// </value>
    /// <remarks>
    ///     This property is <c>null</c> when the operation was successful. Use the <see cref="IsFailure" /> property to
    ///     determine whether an exception is present.
    /// </remarks>
    public Exception? Exception { get; }

    private TryOutcome(T? value, Exception? exception) {
        _value    = value;
        Exception = exception;
    }

    /// <summary>
    ///     Returns the successful value if the outcome represents success; otherwise throws the associated exception.
    /// </summary>
    /// <returns>
    ///     The value produced by the successful operation.
    /// </returns>
    /// <exception cref="Exception">
    ///     Thrown when the outcome represents a failure.
    /// </exception>
    /// <remarks>
    ///     This method bridges non-throwing and throwing error handling models. It allows callers to defer exception throwing
    ///     until a boundary where exception-based flow becomes appropriate.
    /// </remarks>
    public T GetOrThrow() {
        return Value;
    }

}
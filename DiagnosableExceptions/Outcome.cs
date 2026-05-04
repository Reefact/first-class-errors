namespace DiagnosableExceptions;

/// <summary>
///     Represents the outcome of an attempted operation that may fail without throwing an error.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Outcome{T}" /> allows errors to be used as <b>error information</b> rather than solely as
///         control-flow mechanisms. It provides a structured way to capture and propagate a failure, including its
///         associated error, without interrupting execution.
///     </para>
///     <para>
///         This type is particularly useful in scenarios such as validation, parsing, or value object creation, where
///         failure is expected and should be handled explicitly rather than through thrown errors.
///     </para>
///     <para>
///         A <see cref="Outcome{T}" /> instance is always in one of two states:
///     </para>
///     <list type="bullet">
///         <item><b>Success</b>: contains a valid value of type <typeparamref name="T" />.</item>
///         <item><b>Failure</b>: contains an error describing why the operation failed.</item>
///     </list>
///     <para>
///         This type does not represent runtime crashes or unexpected failures. Instead, it models anticipated error
///         conditions as data, while still leveraging the richness of the error model for diagnostic purposes.
///     </para>
/// </remarks>
public readonly struct Outcome<T> {

    private readonly T? _result;

    /// <summary>
    ///     Creates a successful outcome containing the specified value.
    /// </summary>
    /// <param name="result">
    ///     The value produced by the successful operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Outcome{T}" /> representing success.
    /// </returns>
    /// <error cref="ArgumentNullException">
    ///     Thrown if <paramref name="result" /> is <c>null</c>.
    /// </error>
    /// <remarks>
    ///     Use this method when the attempted operation completed successfully and produced a valid result.
    /// </remarks>
    public static Outcome<T> Success(T result) {
        if (result is null) { throw new ArgumentNullException(nameof(result)); }

        return new Outcome<T>(result, null);
    }

    /// <summary>
    ///     Creates a failed outcome containing the specified error.
    /// </summary>
    /// <param name="error">
    ///     The error describing why the operation failed.
    /// </param>
    /// <returns>
    ///     A <see cref="Outcome{T}" /> representing failure.
    /// </returns>
    /// <error cref="ArgumentNullException">
    ///     Thrown if <paramref name="error" /> is <c>null</c>.
    /// </error>
    /// <remarks>
    ///     This method is used when the operation could not produce a valid result. The error serves as a structured
    ///     diagnostic description of the failure.
    /// </remarks>
    public static Outcome<T> Failure(Error error) {
        if (error is null) { throw new ArgumentNullException(nameof(error)); }

        return new Outcome<T>(default, error);
    }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    /// <remarks>
    ///     When <c>true</c>, <see cref="Error" /> contains the reason for the failure.
    /// </remarks>
    public bool IsFailure => Error != null;

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <remarks>
    ///     When <c>true</c>, <see cref="GetResultOrThrow" /> is guaranteed to return a valid result and not throw.
    /// </remarks>
    public bool IsSuccess => Error == null;

    /// <summary>
    ///     Gets the error that represents the failure of the operation, if any.
    /// </summary>
    /// <value>
    ///     The error encapsulated in this <see cref="Outcome{T}" /> instance if the operation failed; otherwise,
    ///     <c>null</c>.
    /// </value>
    /// <remarks>
    ///     This property is <c>null</c> when the operation was successful. Use the <see cref="IsFailure" /> property to
    ///     determine whether an error is present.
    /// </remarks>
    public Error? Error { get; }

    private Outcome(T? result, Error? error) {
        _result = result;
        Error   = error;
    }

    /// <summary>
    ///     Retrieves the result of the operation if it succeeded; otherwise, throws the associated exception.
    /// </summary>
    /// <returns>
    ///     The result of the operation if it was successful.
    /// </returns>
    /// <exception cref="Exception">
    ///     Thrown if the operation failed, using the exception associated with the failure.
    /// </exception>
    /// <remarks>
    ///     This method should only be called when <see cref="IsSuccess" /> is <c>true</c>. If <see cref="IsSuccess" /> is
    ///     <c>false</c>, the exception associated with the failure will be thrown.
    /// </remarks>
    public T GetResultOrThrow() {
        if (!IsSuccess) { throw Error!.ToException(); }

        return _result!;
    }

    /// <summary>
    ///     Continues the process with the next step if the current <see cref="Outcome{T}" /> is successful.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the subsequent operation.
    /// </typeparam>
    /// <param name="next">
    ///     A function that takes the successful result of the current <see cref="Outcome{T}" /> and returns the next
    ///     <see cref="Outcome{TResult}" />.
    /// </param>
    /// <returns>
    ///     If the current <see cref="Outcome{T}" /> is successful, the result of invoking the <paramref name="next" />
    ///     function
    ///     is returned. If the current <see cref="Outcome{T}" /> is a failure, the error is propagated unchanged as a failure
    ///     <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method enables chaining operations that may fail, allowing for a fluent and structured approach to handling
    ///     success and failure scenarios. If the current outcome is a failure, the error is propagated without modification.
    /// </remarks>
    public Outcome<TResult> Then<TResult>(Func<T, Outcome<TResult>> next)
        where TResult : notnull {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? next(_result!) : Outcome<TResult>.Failure(Error!);
    }

    /// <summary>
    ///     Applies a transformation to the successful value of the current <see cref="Outcome{T}" />.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the transformation function.
    /// </typeparam>
    /// <param name="transform">
    ///     A function that transforms the successful value of type <typeparamref name="T" /> into a new value of type
    ///     <typeparamref name="TResult" />.
    /// </param>
    /// <returns>
    ///     A new <see cref="Outcome{TResult}" /> containing the transformed value if the current instance represents a
    ///     success,
    ///     or the original error if the current instance represents a failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="transform" /> function is <c>null</c>.
    /// </exception>
    public Outcome<TResult> Apply<TResult>(Func<T, TResult> transform)
        where TResult : notnull {
        if (transform is null) { throw new ArgumentNullException(nameof(transform)); }

        return IsSuccess ? Outcome<TResult>.Success(transform(_result!)) : Outcome<TResult>.Failure(Error!);
    }

    /// <summary>
    ///     Produces a final value by handling both success and failure cases.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the provided functions.</typeparam>
    /// <param name="onSuccess">
    ///     A function to handle the success case. It is invoked with the successful result of the operation.
    /// </param>
    /// <param name="onFailure">
    ///     A function to handle the failure case. It is invoked with the <see cref="Error" /> describing the failure.
    /// </param>
    /// <returns>
    ///     The result of the <paramref name="onSuccess" /> function if the operation was successful,
    ///     or the result of the <paramref name="onFailure" /> function if the operation failed.
    /// </returns>
    /// <remarks>
    ///     This method typically marks the end of the processing pipeline, as it produces a final value
    ///     by resolving both success and failure scenarios.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public TResult Finally<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? onSuccess(_result!) : onFailure(Error!);
    }

    /// <summary>
    ///     Executes the specified actions based on whether the outcome is successful or failed.
    /// </summary>
    /// <param name="onSuccess">
    ///     The action to execute if the outcome is successful. The result of the operation is passed as a parameter.
    /// </param>
    /// <param name="onFailure">
    ///     The action to execute if the outcome is a failure. The associated error is passed as a parameter.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method is intended for side effects such as logging or monitoring. It allows you to handle both success and
    ///     failure outcomes
    ///     without altering the flow of the program.
    /// </remarks>
    public void Finally(Action<T> onSuccess, Action<Error> onFailure) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        if (IsSuccess) {
            onSuccess(_result!);
        } else {
            onFailure(Error!);
        }
    }

}
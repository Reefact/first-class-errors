namespace FirstClassErrors;

/// <summary>
///     Represents the outcome of an attempted operation that may fail without throwing an error,
///     where the operation produces no value on success.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Outcome" /> is the non-generic counterpart of <see cref="Outcome{T}" />. It is used when
///         an operation either succeeds (with no result to carry) or fails with a structured error.
///     </para>
///     <para>
///         Typical use cases include commands, side-effecting operations, or validation steps where only
///         success or failure matters, not a produced value.
///     </para>
/// </remarks>
public sealed class Outcome {

    #region Statics members declarations

    /// <summary>
    ///     A successful <see cref="Outcome" /> that carries no value.
    /// </summary>
    public static readonly Outcome Success = new(null);

    /// <summary>
    ///     Creates a failed outcome containing the specified error.
    /// </summary>
    /// <param name="error">
    ///     The error describing why the operation failed.
    /// </param>
    /// <returns>
    ///     A <see cref="Outcome" /> representing failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="error" /> is <c>null</c>.
    /// </exception>
    public static Outcome Failure(Error error) {
        if (error is null) { throw new ArgumentNullException(nameof(error)); }

        return new Outcome(error);
    }

    #endregion

    #region Constructors declarations

    private Outcome(Error? error) {
        Error = error;
    }

    #endregion

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => Error != null;

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Error == null;

    /// <summary>
    ///     Gets the error that represents the failure of the operation, if any.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    ///     Throws the associated exception if the outcome is a failure; otherwise does nothing.
    /// </summary>
    /// <exception cref="Exception">
    ///     Thrown if the operation failed, using the exception associated with the failure.
    /// </exception>
    public void ThrowIfFailure() {
        if (IsFailure) { throw Error!.ToException(); }
    }

    /// <summary>
    ///     Continues the process with the next step if the current <see cref="Outcome" /> is successful.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the subsequent operation.
    /// </typeparam>
    /// <param name="next">
    ///     A function that returns the next <see cref="Outcome{TResult}" />.
    /// </param>
    /// <returns>
    ///     If the current <see cref="Outcome" /> is successful, the result of invoking <paramref name="next" />
    ///     is returned. If the current <see cref="Outcome" /> is a failure, the error is propagated unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Outcome<TResult> Then<TResult>(Func<Outcome<TResult>> next)
        where TResult : notnull {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? next() : Outcome<TResult>.Failure(Error!);
    }

    /// <summary>
    ///     Continues the process with the next step if the current <see cref="Outcome" /> is successful.
    /// </summary>
    /// <param name="next">
    ///     A function that returns the next <see cref="Outcome" />.
    /// </param>
    /// <returns>
    ///     If the current <see cref="Outcome" /> is successful, the result of invoking <paramref name="next" />
    ///     is returned. If the current <see cref="Outcome" /> is a failure, the error is propagated unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Outcome Then(Func<Outcome> next) {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? next() : this;
    }

    /// <summary>
    ///     Continues the process with the next asynchronous step if the current <see cref="Outcome" /> is successful.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the subsequent operation.
    /// </typeparam>
    /// <param name="next">
    ///     An asynchronous function that returns a <see cref="Task{TResult}" /> of the next
    ///     <see cref="Outcome{TResult}" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Task<Outcome<TResult>> Then<TResult>(Func<CancellationToken, Task<Outcome<TResult>>> next,
                                                CancellationToken                               cancellationToken = default)
        where TResult : notnull {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(next(cancellationToken)) : Task.FromResult(Outcome<TResult>.Failure(Error!));
    }

    /// <summary>
    ///     Continues the process with the next asynchronous step if the current <see cref="Outcome" /> is successful.
    /// </summary>
    /// <param name="next">
    ///     An asynchronous function that returns a <see cref="Task{TResult}" /> of the next <see cref="Outcome" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Task<Outcome> Then(Func<CancellationToken, Task<Outcome>> next,
                              CancellationToken                      cancellationToken = default) {
        if (next is null) {
            throw new ArgumentNullException(nameof(next));
        }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(next(cancellationToken)) : Task.FromResult(this);
    }

    /// <summary>
    ///     Attempts to recover from a failure by invoking the specified fallback operation.
    /// </summary>
    /// <param name="fallback">
    ///     A function that receives the current <see cref="Error" /> and returns a new <see cref="Outcome" />,
    ///     which may itself succeed or fail.
    /// </param>
    /// <returns>
    ///     The current <see cref="Outcome" /> unchanged if the operation was successful; otherwise, the result
    ///     of invoking <paramref name="fallback" /> with the current error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     Use this method to implement compensation logic, alternative strategies, or conditional retries
    ///     when an operation fails. If the fallback itself fails, the new error replaces the original one.
    /// </remarks>
    public Outcome Recover(Func<Error, Outcome> fallback) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        return IsSuccess ? this : fallback(Error!);
    }

    /// <summary>
    ///     Attempts to recover from a failure by invoking the specified asynchronous fallback operation.
    /// </summary>
    /// <param name="fallback">
    ///     An asynchronous function that receives the current <see cref="Error" /> and returns a
    ///     <see cref="Task{TResult}" /> of a new <see cref="Outcome" />, which may itself succeed or fail.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. Resolves to the current
    ///     <see cref="Outcome" /> unchanged if successful; otherwise, the result of invoking
    ///     <paramref name="fallback" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>.
    /// </exception>
    public Task<Outcome> Recover(Func<Error, CancellationToken, Task<Outcome>> fallback,
                                 CancellationToken                             cancellationToken = default) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        return IsSuccess ? Task.FromResult(this) : AsyncCallbackGuard.EnsureTask(fallback(Error!, cancellationToken));
    }

    /// <summary>
    ///     Produces a final value by handling both success and failure cases.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the provided functions.</typeparam>
    /// <param name="onSuccess">
    ///     A function to handle the success case.
    /// </param>
    /// <param name="onFailure">
    ///     A function to handle the failure case. It is invoked with the <see cref="Error" /> describing the failure.
    /// </param>
    /// <returns>
    ///     The result of <paramref name="onSuccess" /> if the operation was successful,
    ///     or the result of <paramref name="onFailure" /> if the operation failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public TResult Finally<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? onSuccess() : onFailure(Error!);
    }

    /// <summary>
    ///     Executes the specified actions based on whether the outcome is successful or failed.
    /// </summary>
    /// <param name="onSuccess">
    ///     The action to execute if the outcome is successful.
    /// </param>
    /// <param name="onFailure">
    ///     The action to execute if the outcome is a failure. The associated error is passed as a parameter.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public void Finally(Action onSuccess, Action<Error> onFailure) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        if (IsSuccess) {
            onSuccess();
        } else {
            onFailure(Error!);
        }
    }

    /// <summary>
    ///     Asynchronously produces a final value by handling both success and failure cases.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the provided functions.</typeparam>
    /// <param name="onSuccess">
    ///     An asynchronous function to handle the success case.
    /// </param>
    /// <param name="onFailure">
    ///     An asynchronous function to handle the failure case.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> resolving to the result of <paramref name="onSuccess" /> if successful,
    ///     or the result of <paramref name="onFailure" /> if failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public Task<TResult> Finally<TResult>(Func<CancellationToken, Task<TResult>>        onSuccess,
                                          Func<Error, CancellationToken, Task<TResult>> onFailure,
                                          CancellationToken                             cancellationToken = default) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(onSuccess(cancellationToken)) : AsyncCallbackGuard.EnsureTask(onFailure(Error!, cancellationToken));
    }

    /// <summary>
    ///     Asynchronously executes the specified actions based on whether the outcome is successful or failed.
    /// </summary>
    /// <param name="onSuccess">
    ///     An asynchronous action to execute if the outcome is successful.
    /// </param>
    /// <param name="onFailure">
    ///     An asynchronous action to execute if the outcome is a failure.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public Task Finally(Func<CancellationToken, Task>        onSuccess,
                        Func<Error, CancellationToken, Task> onFailure,
                        CancellationToken                    cancellationToken = default) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(onSuccess(cancellationToken)) : AsyncCallbackGuard.EnsureTask(onFailure(Error!, cancellationToken));
    }

}

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
public sealed class Outcome<T>
    where T : notnull {

    #region Statics members declarations

    /// <summary>
    ///     Creates a successful outcome containing the specified value.
    /// </summary>
    /// <param name="result">
    ///     The value produced by the successful operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Outcome{T}" /> representing success.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="result" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     Although <typeparamref name="T" /> is constrained to <c>notnull</c>, the null guard is kept as a
    ///     runtime safety net against nullable reference types passed through unchecked contexts.
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
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="error" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method is used when the operation could not produce a valid result. The error serves as a structured
    ///     diagnostic description of the failure.
    /// </remarks>
    public static Outcome<T> Failure(Error error) {
        if (error is null) { throw new ArgumentNullException(nameof(error)); }

        return new Outcome<T>(default, error);
    }

    #endregion

    #region Fields declarations

    private readonly T? _result;

    #endregion

    #region Constructors declarations

    private Outcome(T? result, Error? error) {
        _result = result;
        Error   = error;
    }

    #endregion

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
        if (IsFailure) { throw Error!.ToException(); }

        return _result!;
    }

    /// <summary>
    ///     Throws the associated exception if the outcome is a failure; otherwise does nothing.
    /// </summary>
    /// <exception cref="Exception">
    ///     Thrown if the operation failed, using the exception associated with the failure.
    /// </exception>
    /// <remarks>
    ///     Use this method when the operation's value is not needed but failure must still be surfaced,
    ///     for example when asserting preconditions or verifying a side-effecting step succeeded.
    /// </remarks>
    public void ThrowIfFailure() {
        if (IsFailure) { throw Error!.ToException(); }
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
    ///     function is returned. If the current <see cref="Outcome{T}" /> is a failure, the error is propagated
    ///     unchanged as a failure <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Outcome<TResult> Then<TResult>(Func<T, Outcome<TResult>> next)
        where TResult : notnull {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? next(_result!) : Outcome<TResult>.Failure(Error!);
    }

    /// <summary>
    ///     Continues the process with the next step if the current <see cref="Outcome{T}" /> is successful,
    ///     discarding the current value.
    /// </summary>
    /// <param name="next">
    ///     A function that returns the next <see cref="Outcome" />.
    /// </param>
    /// <returns>
    ///     If the current <see cref="Outcome{T}" /> is successful, the result of invoking <paramref name="next" />
    ///     is returned. If the current <see cref="Outcome{T}" /> is a failure, the error is propagated unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Outcome Then(Func<T, Outcome> next) {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? next(_result!) : Outcome.Failure(Error!);
    }

    /// <summary>
    ///     Continues the process with the next asynchronous step if the current <see cref="Outcome{T}" /> is successful.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the subsequent operation.
    /// </typeparam>
    /// <param name="next">
    ///     An asynchronous function that takes the successful result of the current <see cref="Outcome{T}" /> and
    ///     returns a <see cref="Task{TResult}" /> of the next <see cref="Outcome{TResult}" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Task<Outcome<TResult>> Then<TResult>(Func<T, CancellationToken, Task<Outcome<TResult>>> next,
                                                CancellationToken                                  cancellationToken = default)
        where TResult : notnull {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(next(_result!, cancellationToken)) : Task.FromResult(Outcome<TResult>.Failure(Error!));
    }

    /// <summary>
    ///     Continues the process with the next asynchronous step if the current <see cref="Outcome{T}" /> is successful,
    ///     discarding the current value.
    /// </summary>
    /// <param name="next">
    ///     An asynchronous function that returns a <see cref="Task{TResult}" /> of the next <see cref="Outcome" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="next" /> function is <c>null</c>.
    /// </exception>
    public Task<Outcome> Then(Func<T, CancellationToken, Task<Outcome>> next,
                              CancellationToken                         cancellationToken = default) {
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(next(_result!, cancellationToken)) : Task.FromResult(Outcome.Failure(Error!));
    }

    /// <summary>
    ///     Converts the successful value of the current <see cref="Outcome{T}" /> into a new value of type
    ///     <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the conversion function.
    /// </typeparam>
    /// <param name="convert">
    ///     A function that converts the successful value of type <typeparamref name="T" /> into a new value of type
    ///     <typeparamref name="TResult" />. Unlike <see cref="Then{TResult}(Func{T, Outcome{TResult}})" />, this
    ///     function cannot itself produce a failure.
    /// </param>
    /// <returns>
    ///     A new <see cref="Outcome{TResult}" /> containing the converted value if the current instance represents
    ///     a success, or the original error if the current instance represents a failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="convert" /> function is <c>null</c>, or if <paramref name="convert" /> returns
    ///     <c>null</c> (the returned value flows through <see cref="Success" />, which rejects <c>null</c>).
    /// </exception>
    public Outcome<TResult> To<TResult>(Func<T, TResult> convert)
        where TResult : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        return IsSuccess ? Outcome<TResult>.Success(convert(_result!)) : Outcome<TResult>.Failure(Error!);
    }

    /// <summary>
    ///     Converts the successful value of the current <see cref="Outcome{T}" /> into a new value of type
    ///     <typeparamref name="TResult" /> asynchronously.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result produced by the conversion function.
    /// </typeparam>
    /// <param name="convert">
    ///     An asynchronous function that converts the successful value of type <typeparamref name="T" /> into a
    ///     new value of type <typeparamref name="TResult" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="convert" /> function is <c>null</c>, or if the awaited <paramref name="convert" />
    ///     result is <c>null</c> (it flows through <see cref="Success" />, which rejects <c>null</c>).
    /// </exception>
    public async Task<Outcome<TResult>> To<TResult>(Func<T, CancellationToken, Task<TResult>> convert, CancellationToken cancellationToken = default)
        where TResult : notnull {
        if (convert is null) { throw new ArgumentNullException(nameof(convert)); }

        if (IsFailure) { return Outcome<TResult>.Failure(Error!); }

        TResult? value = await convert(_result!, cancellationToken).ConfigureAwait(false);

        return Outcome<TResult>.Success(value);
    }

    /// <summary>
    ///     Attempts to recover from a failure by invoking the specified fallback operation.
    /// </summary>
    /// <param name="fallback">
    ///     A function that receives the current <see cref="Error" /> and returns a new <see cref="Outcome{T}" />,
    ///     which may itself succeed or fail.
    /// </param>
    /// <returns>
    ///     The current <see cref="Outcome{T}" /> unchanged if the operation was successful; otherwise, the result
    ///     of invoking <paramref name="fallback" /> with the current error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     Use this method to implement compensation logic, alternative strategies, or conditional retries.
    ///     If the fallback itself fails, the new error replaces the original one.
    /// </remarks>
    public Outcome<T> Recover(Func<Error, Outcome<T>> fallback) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        return IsSuccess ? this : fallback(Error!);
    }

    /// <summary>
    ///     Attempts to recover from a failure by providing a guaranteed fallback value.
    /// </summary>
    /// <param name="fallback">
    ///     A function that receives the current <see cref="Error" /> and returns a non-null value of type
    ///     <typeparamref name="T" /> used to recover from the failure.
    /// </param>
    /// <returns>
    ///     The current <see cref="Outcome{T}" /> unchanged if the operation was successful; otherwise, a successful
    ///     <see cref="Outcome{T}" /> containing the value returned by <paramref name="fallback" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>, or if <paramref name="fallback" /> returns <c>null</c>
    ///     (the returned value flows through <see cref="Success" />, which rejects <c>null</c>).
    /// </exception>
    /// <remarks>
    ///     Unlike <see cref="Recover(Func{Error, Outcome{T}})" />, this overload produces a success whenever the
    ///     fallback returns a non-null value. Use it when a default or cached value can always be substituted for the
    ///     failed result.
    /// </remarks>
    public Outcome<T> Recover(Func<Error, T> fallback) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        return IsSuccess ? this : Success(fallback(Error!));
    }

    /// <summary>
    ///     Attempts to recover from a failure by invoking the specified asynchronous fallback operation.
    /// </summary>
    /// <param name="fallback">
    ///     An asynchronous function that receives the current <see cref="Error" /> and returns a
    ///     <see cref="Task{TResult}" /> of a new <see cref="Outcome{T}" />, which may itself succeed or fail.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>.
    /// </exception>
    public Task<Outcome<T>> Recover(Func<Error, CancellationToken, Task<Outcome<T>>> fallback,
                                    CancellationToken                                cancellationToken = default) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        return IsSuccess ? Task.FromResult(this) : AsyncCallbackGuard.EnsureTask(fallback(Error!, cancellationToken));
    }

    /// <summary>
    ///     Attempts to recover from a failure by invoking the specified asynchronous fallback that always produces
    ///     a value.
    /// </summary>
    /// <param name="fallback">
    ///     An asynchronous function that receives the current <see cref="Error" /> and returns a
    ///     <see cref="Task{T}" /> containing a non-null fallback value.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. Resolves to the current
    ///     <see cref="Outcome{T}" /> unchanged if successful; otherwise, a successful <see cref="Outcome{T}" />
    ///     containing the fallback value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="fallback" /> is <c>null</c>, or if the awaited <paramref name="fallback" /> result
    ///     is <c>null</c> (it flows through <see cref="Success" />, which rejects <c>null</c>).
    /// </exception>
    public async Task<Outcome<T>> Recover(Func<Error, CancellationToken, Task<T>> fallback,
                                          CancellationToken                       cancellationToken = default) {
        if (fallback is null) { throw new ArgumentNullException(nameof(fallback)); }

        if (IsSuccess) { return this; }

        T? value = await fallback(Error!, cancellationToken).ConfigureAwait(false);

        return Success(value);
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
    ///     This method is intended for side effects such as logging or monitoring. It allows you to handle both
    ///     success and failure outcomes without altering the flow of the program.
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

    /// <summary>
    ///     Asynchronously produces a final value by handling both success and failure cases.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the provided functions.</typeparam>
    /// <param name="onSuccess">
    ///     An asynchronous function to handle the success case.
    /// </param>
    /// <param name="onFailure">
    ///     An asynchronous function to handle the failure case.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> resolving to the result of <paramref name="onSuccess" /> if successful,
    ///     or the result of <paramref name="onFailure" /> if failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public Task<TResult> Finally<TResult>(Func<T, CancellationToken, Task<TResult>>     onSuccess,
                                          Func<Error, CancellationToken, Task<TResult>> onFailure,
                                          CancellationToken                             cancellationToken = default) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(onSuccess(_result!, cancellationToken)) : AsyncCallbackGuard.EnsureTask(onFailure(Error!, cancellationToken));
    }

    /// <summary>
    ///     Asynchronously executes the specified actions based on whether the outcome is successful or failed.
    /// </summary>
    /// <param name="onSuccess">
    ///     An asynchronous action to execute if the outcome is successful.
    /// </param>
    /// <param name="onFailure">
    ///     An asynchronous action to execute if the outcome is a failure.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <c>null</c>.
    /// </exception>
    public Task Finally(Func<T, CancellationToken, Task>     onSuccess,
                        Func<Error, CancellationToken, Task> onFailure,
                        CancellationToken                    cancellationToken = default) {
        if (onSuccess is null) { throw new ArgumentNullException(nameof(onSuccess)); }
        if (onFailure is null) { throw new ArgumentNullException(nameof(onFailure)); }

        return IsSuccess ? AsyncCallbackGuard.EnsureTask(onSuccess(_result!, cancellationToken)) : AsyncCallbackGuard.EnsureTask(onFailure(Error!, cancellationToken));
    }

}

/// <summary>
///     Guards the tasks returned by the asynchronous callbacks passed to <see cref="Outcome" /> and
///     <see cref="Outcome{T}" />.
/// </summary>
internal static class AsyncCallbackGuard {

    #region Statics members declarations

    /// <summary>
    ///     Ensures an asynchronous callback did not return a <c>null</c> task. The success/failure overloads forward the
    ///     callback's task directly (rather than awaiting it), so a <c>null</c> would otherwise escape and surface as an
    ///     opaque <see cref="NullReferenceException" /> when the caller awaits it. This turns that contract violation into
    ///     an explicit, diagnosable failure at the point the callback is invoked.
    /// </summary>
    /// <typeparam name="TTask">The concrete <see cref="Task" /> type the callback returns.</typeparam>
    /// <param name="task">The task returned by the callback.</param>
    /// <returns>The <paramref name="task" />, guaranteed to be non-<c>null</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    internal static TTask EnsureTask<TTask>(TTask? task)
        where TTask : Task {
        if (task is null) {
            throw new InvalidOperationException("An asynchronous Outcome callback returned a null task. Callbacks must return a non-null task.");
        }

        return task;
    }

    #endregion

}
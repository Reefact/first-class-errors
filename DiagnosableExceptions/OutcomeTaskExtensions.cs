namespace DiagnosableExceptions;

/// <summary>
///     Provides a set of extension methods for working with <see cref="Task{TResult}" /> instances that return
///     <see cref="Outcome" /> or <see cref="Outcome{T}" /> results. These methods enable chaining, error recovery,
///     and finalization of asynchronous operations in a fluent and expressive manner.
/// </summary>
public static class OutcomeTaskExtensions {

    #region Statics members declarations

    // -------------------------------------------------------------------------
    // Task<Outcome> → Then
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Asynchronously executes the specified continuation function if the preceding <see cref="Outcome" /> is successful.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the continuation function.</typeparam>
    /// <param name="task">The task representing the preceding <see cref="Outcome" />.</param>
    /// <param name="next">
    ///     A function that produces the next <see cref="Outcome{TResult}" /> to be executed if the preceding
    ///     <see cref="Outcome" /> is successful.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the <see cref="Outcome{TResult}" />
    ///     produced by the continuation function if the preceding <see cref="Outcome" /> is successful; otherwise, it contains
    ///     the original <see cref="Outcome" />.
    /// </returns>
    /// <remarks>
    ///     This method ensures that the continuation function is executed only if the preceding <see cref="Outcome" /> is
    ///     successful.
    ///     If the preceding <see cref="Outcome" /> is not successful, the continuation function is not invoked, and the
    ///     original <see cref="Outcome" /> is returned.
    /// </remarks>
    public static async Task<Outcome<TResult>> Then<TResult>(this Task<Outcome> task, Func<Outcome<TResult>> next)
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return outcome.Then(next);
    }

    /// <summary>
    ///     Asynchronously executes the specified <paramref name="next" /> function after the completion of the current
    ///     <see cref="Task{Outcome}" />.
    /// </summary>
    /// <param name="task">The task representing the current <see cref="Outcome" />.</param>
    /// <param name="next">
    ///     A function that produces the next <see cref="Outcome" /> to execute after the current task completes successfully.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation, which resolves to the <see cref="Outcome" /> returned by the
    ///     <paramref name="next" /> function.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<Outcome> Then(this Task<Outcome> task, Func<Outcome> next) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return outcome.Then(next);
    }

    /// <summary>
    ///     Asynchronously executes a continuation function after the completion of a task that produces an
    ///     <see cref="Outcome" />.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the continuation function.</typeparam>
    /// <param name="task">The task that produces an <see cref="Outcome" />.</param>
    /// <param name="next">
    ///     A function that takes a <see cref="CancellationToken" /> and returns a task producing an
    ///     <see cref="Outcome{TResult}" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is an <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<Outcome<TResult>> Then<TResult>(this Task<Outcome>                              task,
                                                             Func<CancellationToken, Task<Outcome<TResult>>> next,
                                                             CancellationToken                               cancellationToken = default)
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously executes the specified continuation function after the completion of the current
    ///     <see cref="Outcome" /> task.
    /// </summary>
    /// <param name="task">The task representing the current <see cref="Outcome" />.</param>
    /// <param name="next">
    ///     A function that takes a <see cref="CancellationToken" /> and returns a <see cref="Task{Outcome}" />
    ///     representing the next operation to be executed.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete. Defaults to
    ///     <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result of the task is the <see cref="Outcome" />
    ///     returned by the <paramref name="next" /> function.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="task" /> or <paramref name="next" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome> Then(this Task<Outcome>                     task,
                                           Func<CancellationToken, Task<Outcome>> next,
                                           CancellationToken                      cancellationToken = default) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> → Recover
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous <see cref="Outcome" /> operation
    ///     by applying the specified fallback function.
    /// </summary>
    /// <param name="task">The task representing the asynchronous <see cref="Outcome" /> operation.</param>
    /// <param name="fallback">
    ///     A function that takes an <see cref="Error" /> as input and returns an alternative <see cref="Outcome" />
    ///     to recover from the error.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation, which resolves to the original <see cref="Outcome" />
    ///     if successful, or the result of the <paramref name="fallback" /> function if an error occurs.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome> Recover(this Task<Outcome> task, Func<Error, Outcome> fallback) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return outcome.Recover(fallback);
    }

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous operation represented by the current
    ///     <see cref="Task{Outcome}" />.
    /// </summary>
    /// <param name="task">
    ///     The task representing the asynchronous operation that may have resulted in an error.
    /// </param>
    /// <param name="fallback">
    ///     A function that provides a fallback <see cref="Outcome" /> when an error occurs. The function receives the error
    ///     and a <see cref="CancellationToken" /> as parameters.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete or for the fallback function
    ///     to execute.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result of the task is the original <see cref="Outcome" /> if
    ///     no error occurred, or the fallback <see cref="Outcome" /> if an error was handled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome> Recover(this Task<Outcome>                            task,
                                              Func<Error, CancellationToken, Task<Outcome>> fallback,
                                              CancellationToken                             cancellationToken = default) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> → Finally
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Executes a final operation on a completed <see cref="Task{TResult}" /> of type <see cref="Outcome" />.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the final operation.</typeparam>
    /// <param name="task">The <see cref="Task{TResult}" /> of type <see cref="Outcome" /> to process.</param>
    /// <param name="onSuccess">
    ///     A function to execute if the <see cref="Outcome" /> represents a successful result.
    ///     This function returns a value of type <typeparamref name="TResult" />.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute if the <see cref="Outcome" /> represents a failure.
    ///     This function takes an <see cref="Error" /> as input and returns a value of type <typeparamref name="TResult" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> that represents the result of the final operation.
    ///     The result is determined by invoking either <paramref name="onSuccess" /> or <paramref name="onFailure" />
    ///     based on the state of the <see cref="Outcome" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<TResult> Finally<TResult>(this Task<Outcome>   task,
                                                       Func<TResult>        onSuccess,
                                                       Func<Error, TResult> onFailure) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return outcome.Finally(onSuccess, onFailure);
    }

    /// <summary>
    ///     Executes the specified actions based on the outcome of the task.
    /// </summary>
    /// <param name="task">The task representing an asynchronous operation that produces an <see cref="Outcome" />.</param>
    /// <param name="onSuccess">The action to execute if the task completes successfully.</param>
    /// <param name="onFailure">The action to execute if the task fails with an <see cref="Error" />.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    /// <remarks>
    ///     This method awaits the completion of the <paramref name="task" /> and invokes the appropriate action
    ///     depending on whether the outcome is successful or contains an error.
    /// </remarks>
    public static async Task Finally(this Task<Outcome> task,
                                     Action             onSuccess,
                                     Action<Error>      onFailure) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        outcome.Finally(onSuccess, onFailure);
    }

    /// <summary>
    ///     Executes a final operation based on the outcome of the preceding <see cref="Task{Outcome}" />.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the final operation.</typeparam>
    /// <param name="task">The task representing the outcome to evaluate.</param>
    /// <param name="onSuccess">
    ///     A function to execute if the outcome is successful. The function receives a <see cref="CancellationToken" />
    ///     and returns a task that produces the result of type <typeparamref name="TResult" />.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute if the outcome is a failure. The function receives an <see cref="Error" /> and a
    ///     <see cref="CancellationToken" />, and returns a task that produces the result of type
    ///     <typeparamref name="TResult" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<TResult> Finally<TResult>(this Task<Outcome>                            task,
                                                       Func<CancellationToken, Task<TResult>>        onSuccess,
                                                       Func<Error, CancellationToken, Task<TResult>> onFailure,
                                                       CancellationToken                             cancellationToken = default) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        return await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Executes the specified asynchronous actions based on the outcome of the task.
    /// </summary>
    /// <param name="task">The task representing the operation whose outcome is to be handled.</param>
    /// <param name="onSuccess">
    ///     A function to execute asynchronously if the task completes successfully.
    ///     The function receives a <see cref="CancellationToken" /> as a parameter.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute asynchronously if the task fails.
    ///     The function receives an <see cref="Error" /> and a <see cref="CancellationToken" /> as parameters.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests.
    /// </param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task Finally(this Task<Outcome>                   task,
                                     Func<CancellationToken, Task>        onSuccess,
                                     Func<Error, CancellationToken, Task> onFailure,
                                     CancellationToken                    cancellationToken = default) {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome? outcome = await task.ConfigureAwait(false);

        await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Then
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Asynchronously executes the specified function <paramref name="next" /> after the completion of the current task,
    ///     passing the result of the current task as input to the function.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the current <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">The type of the result contained in the resulting <see cref="Outcome{TResult}" />.</typeparam>
    /// <param name="task">The task representing the current <see cref="Outcome{T}" />.</param>
    /// <param name="next">
    ///     A function to execute after the completion of the current task. The function takes the result of the current
    ///     <see cref="Outcome{T}" /> as input and returns a new <see cref="Outcome{TResult}" />.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation, which contains the resulting <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<TResult>> Then<T, TResult>(this Task<Outcome<T>>     task,
                                                                Func<T, Outcome<TResult>> next)
        where T : notnull
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.Then(next);
    }

    /// <summary>
    ///     Asynchronously executes the specified function if the preceding <see cref="Outcome{T}" /> is successful.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the preceding <see cref="Outcome{T}" />.</typeparam>
    /// <param name="task">A task that represents the asynchronous operation returning an <see cref="Outcome{T}" />.</param>
    /// <param name="next">
    ///     A function to execute if the preceding <see cref="Outcome{T}" /> is successful.
    ///     The function receives the result of the preceding <see cref="Outcome{T}" /> as its parameter.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is an <see cref="Outcome" />
    ///     that represents the result of the operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<Outcome> Then<T>(this Task<Outcome<T>> task,
                                              Func<T, Outcome>      next)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.Then(next);
    }

    /// <summary>
    ///     Chains the execution of a task that produces an <see cref="Outcome{T}" /> with a subsequent asynchronous operation.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the initial <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">The type of the result contained in the resulting <see cref="Outcome{TResult}" />.</typeparam>
    /// <param name="task">The task producing the initial <see cref="Outcome{T}" />.</param>
    /// <param name="next">
    ///     A function that takes the result of the initial <see cref="Outcome{T}" /> and a <see cref="CancellationToken" />,
    ///     and returns a task producing the next <see cref="Outcome{TResult}" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe while waiting for the task to complete. Defaults to <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation, producing an <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<Outcome<TResult>> Then<T, TResult>(this Task<Outcome<T>>                              task,
                                                                Func<T, CancellationToken, Task<Outcome<TResult>>> next,
                                                                CancellationToken                                  cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Chains the execution of a task that produces an <see cref="Outcome{T}" /> with another asynchronous operation.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained within the <see cref="Outcome{T}" />. Must be non-nullable.
    /// </typeparam>
    /// <param name="task">
    ///     The task producing an <see cref="Outcome{T}" /> to chain from. Cannot be <c>null</c>.
    /// </param>
    /// <param name="next">
    ///     A function that takes the result of the <see cref="Outcome{T}" /> and a <see cref="CancellationToken" />,
    ///     and returns a task producing the next <see cref="Outcome" />. Cannot be <c>null</c>.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe while waiting for the task to complete. Defaults to <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation, producing the resulting <see cref="Outcome" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome> Then<T>(this Task<Outcome<T>>                     task,
                                              Func<T, CancellationToken, Task<Outcome>> next,
                                              CancellationToken                         cancellationToken = default)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → To
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Converts the result of a completed <see cref="Task{TResult}" /> of type <see cref="Outcome{T}" />
    ///     to a new <see cref="Outcome{TResult}" /> using the specified conversion function.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the original <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">The type of the value contained in the resulting <see cref="Outcome{TResult}" />.</typeparam>
    /// <param name="task">The task producing the <see cref="Outcome{T}" /> to be converted.</param>
    /// <param name="convert">
    ///     A function that converts the value of type <typeparamref name="T" />
    ///     to a value of type <typeparamref name="TResult" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> that represents the asynchronous operation,
    ///     containing the converted <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<Outcome<TResult>> To<T, TResult>(this Task<Outcome<T>> task,
                                                              Func<T, TResult>      convert)
        where T : notnull
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.To(convert);
    }

    /// <summary>
    ///     Converts the result of a completed <see cref="Task{TResult}" /> of <see cref="Outcome{T}" />
    ///     to a new <see cref="Outcome{TResult}" /> using the specified asynchronous conversion function.
    /// </summary>
    /// <typeparam name="T">The type of the input value contained in the <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">The type of the result value to be produced by the conversion function.</typeparam>
    /// <param name="task">The task representing the <see cref="Outcome{T}" /> to be converted.</param>
    /// <param name="convert">
    ///     A function that asynchronously converts the input value of type <typeparamref name="T" />
    ///     to a result of type <typeparamref name="TResult" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the converted
    ///     <see cref="Outcome{TResult}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<TResult>> To<T, TResult>(this Task<Outcome<T>>                     task,
                                                              Func<T, CancellationToken, Task<TResult>> convert,
                                                              CancellationToken                         cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.To(convert, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Recover
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous operation represented by the <see cref="Task{TResult}" />
    ///     of type <see cref="Outcome{T}" /> by applying the specified fallback function.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained within the <see cref="Outcome{T}" />. Must be non-nullable.
    /// </typeparam>
    /// <param name="task">
    ///     The task representing the asynchronous operation that produces an <see cref="Outcome{T}" />.
    /// </param>
    /// <param name="fallback">
    ///     A function to invoke when the operation results in an error. The function receives the <see cref="Error" />
    ///     and returns a new <see cref="Outcome{T}" /> to recover from the error.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is the recovered <see cref="Outcome{T}" />
    ///     if an error occurred, or the original <see cref="Outcome{T}" /> if no error occurred.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<T>> Recover<T>(this Task<Outcome<T>>   task,
                                                    Func<Error, Outcome<T>> fallback)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.Recover(fallback);
    }

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous operation represented by the <see cref="Task{TResult}" />
    ///     of <see cref="Outcome{T}" /> by applying the specified fallback function.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained in the <see cref="Outcome{T}" />.
    /// </typeparam>
    /// <param name="task">
    ///     The task representing the asynchronous operation that produces an <see cref="Outcome{T}" />.
    /// </param>
    /// <param name="fallback">
    ///     A function that provides a fallback value of type <typeparamref name="T" /> in case of an error.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result of the task is an <see cref="Outcome{T}" />
    ///     containing either the original result or the fallback value provided by the <paramref name="fallback" /> function.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<T>> Recover<T>(this Task<Outcome<T>> task,
                                                    Func<Error, T>        fallback)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.Recover(fallback);
    }

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous operation represented by the <see cref="Task{TResult}" />.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained within the <see cref="Outcome{T}" />.
    /// </typeparam>
    /// <param name="task">
    ///     The task representing the asynchronous operation that may result in an <see cref="Outcome{T}" />.
    /// </param>
    /// <param name="fallback">
    ///     A function that provides a fallback <see cref="Outcome{T}" /> in case of an error. The function receives
    ///     the <see cref="Error" /> and a <see cref="CancellationToken" /> as parameters.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result of the task is an <see cref="Outcome{T}" />.
    ///     If the original task completes successfully, its result is returned. If it fails, the fallback function is invoked.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<T>> Recover<T>(this Task<Outcome<T>>                            task,
                                                    Func<Error, CancellationToken, Task<Outcome<T>>> fallback,
                                                    CancellationToken                                cancellationToken = default)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempts to recover from an error in the asynchronous operation represented by the <see cref="Task{TResult}" />.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained in the <see cref="Outcome{T}" />. Must be non-nullable.
    /// </typeparam>
    /// <param name="task">
    ///     The task representing the asynchronous operation that may result in an <see cref="Outcome{T}" />.
    /// </param>
    /// <param name="fallback">
    ///     A function that provides a fallback value in case of an error. The function takes an <see cref="Error" />
    ///     and a <see cref="CancellationToken" /> as parameters and returns a task that produces the fallback value.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None" /> if not specified.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation. If the original task completes successfully, the result is
    ///     returned.
    ///     If the original task results in an error, the fallback function is invoked to provide a recovery value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<Outcome<T>> Recover<T>(this Task<Outcome<T>>                   task,
                                                    Func<Error, CancellationToken, Task<T>> fallback,
                                                    CancellationToken                       cancellationToken = default)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Finally
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Executes the specified actions based on the outcome of the <see cref="Task{TResult}" /> representing an
    ///     <see cref="Outcome{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the successful result contained in the <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the result returned by the <paramref name="onSuccess" /> or
    ///     <paramref name="onFailure" /> function.
    /// </typeparam>
    /// <param name="task">The task representing the <see cref="Outcome{T}" /> to process.</param>
    /// <param name="onSuccess">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a successful result.
    ///     The function receives the successful result of type <typeparamref name="T" /> and returns a value of type
    ///     <typeparamref name="TResult" />.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a failure.
    ///     The function receives an <see cref="Error" /> and returns a value of type <typeparamref name="TResult" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is the value returned by either
    ///     <paramref name="onSuccess" /> or <paramref name="onFailure" />, depending on the outcome of the
    ///     <see cref="Outcome{T}" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task<TResult> Finally<T, TResult>(this Task<Outcome<T>> task,
                                                          Func<T, TResult>      onSuccess,
                                                          Func<Error, TResult>  onFailure)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return outcome.Finally(onSuccess, onFailure);
    }

    /// <summary>
    ///     Executes the specified actions based on the outcome of the task.
    /// </summary>
    /// <typeparam name="T">The type of the successful result.</typeparam>
    /// <param name="task">The task representing the outcome to process.</param>
    /// <param name="onSuccess">
    ///     The action to execute if the task completes successfully with a result.
    ///     The result of the task is passed as a parameter to this action.
    /// </param>
    /// <param name="onFailure">
    ///     The action to execute if the task completes with an error.
    ///     The error is passed as a parameter to this action.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="task" /> is <c>null</c>.</exception>
    public static async Task Finally<T>(this Task<Outcome<T>> task,
                                        Action<T>             onSuccess,
                                        Action<Error>         onFailure)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        outcome.Finally(onSuccess, onFailure);
    }

    /// <summary>
    ///     Executes the specified asynchronous actions based on the outcome of the <see cref="Outcome{T}" /> task.
    /// </summary>
    /// <typeparam name="T">The type of the successful result contained in the <see cref="Outcome{T}" />.</typeparam>
    /// <typeparam name="TResult">The type of the result returned by the specified actions.</typeparam>
    /// <param name="task">
    ///     The <see cref="Task{TResult}" /> representing the asynchronous operation that produces an
    ///     <see cref="Outcome{T}" />.
    /// </param>
    /// <param name="onSuccess">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a successful result.
    ///     The function receives the successful result and a <see cref="CancellationToken" /> as parameters.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a failure.
    ///     The function receives the <see cref="Error" /> and a <see cref="CancellationToken" /> as parameters.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task's result is the value returned by either
    ///     <paramref name="onSuccess" /> or <paramref name="onFailure" />, depending on the outcome.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task<TResult> Finally<T, TResult>(this Task<Outcome<T>>                         task,
                                                          Func<T, CancellationToken, Task<TResult>>     onSuccess,
                                                          Func<Error, CancellationToken, Task<TResult>> onFailure,
                                                          CancellationToken                             cancellationToken = default)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        return await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Executes the specified asynchronous actions based on the outcome of the <see cref="Outcome{T}" /> task.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the result contained in the <see cref="Outcome{T}" />.
    /// </typeparam>
    /// <param name="task">
    ///     The <see cref="Task{TResult}" /> representing the asynchronous operation that produces an <see cref="Outcome{T}" />
    ///     .
    /// </param>
    /// <param name="onSuccess">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a successful result.
    ///     The function receives the result of type <typeparamref name="T" /> and a <see cref="CancellationToken" />.
    /// </param>
    /// <param name="onFailure">
    ///     A function to execute if the <see cref="Outcome{T}" /> represents a failure.
    ///     The function receives an <see cref="Error" /> and a <see cref="CancellationToken" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that represents the asynchronous execution of the specified actions.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="task" /> is <c>null</c>.
    /// </exception>
    public static async Task Finally<T>(this Task<Outcome<T>>                task,
                                        Func<T, CancellationToken, Task>     onSuccess,
                                        Func<Error, CancellationToken, Task> onFailure,
                                        CancellationToken                    cancellationToken = default)
        where T : notnull {
        if (task is null) { throw new ArgumentNullException(nameof(task)); }

        Outcome<T>? outcome = await task.ConfigureAwait(false);

        await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    #endregion

}
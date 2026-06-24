namespace DiagnosableExceptions;


public static class OutcomeTaskExtensions {

    // -------------------------------------------------------------------------
    // Task<Outcome> → Then
    // -------------------------------------------------------------------------

    public static async Task<Outcome<TResult>> Then<TResult>(
        this Task<Outcome> task,
        Func<Outcome<TResult>> next)
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    public static async Task<Outcome> Then(
        this Task<Outcome> task,
        Func<Outcome> next) {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    public static async Task<Outcome<TResult>> Then<TResult>(
        this Task<Outcome> task,
        Func<CancellationToken, Task<Outcome<TResult>>> next,
        CancellationToken cancellationToken = default)
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Outcome> Then(
        this Task<Outcome> task,
        Func<CancellationToken, Task<Outcome>> next,
        CancellationToken cancellationToken = default) {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> → Recover
    // -------------------------------------------------------------------------

    public static async Task<Outcome> Recover(
        this Task<Outcome> task,
        Func<Error, Outcome> fallback) {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Recover(fallback);
    }

    public static async Task<Outcome> Recover(
        this Task<Outcome> task,
        Func<Error, CancellationToken, Task<Outcome>> fallback,
        CancellationToken cancellationToken = default) {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> → Finally
    // -------------------------------------------------------------------------

    public static async Task<TResult> Finally<TResult>(
        this Task<Outcome> task,
        Func<TResult> onSuccess,
        Func<Error, TResult> onFailure) {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Finally(onSuccess, onFailure);
    }

    public static async Task Finally(
        this Task<Outcome> task,
        Action onSuccess,
        Action<Error> onFailure) {
        var outcome = await task.ConfigureAwait(false);
        outcome.Finally(onSuccess, onFailure);
    }

    public static async Task<TResult> Finally<TResult>(
        this Task<Outcome> task,
        Func<CancellationToken, Task<TResult>> onSuccess,
        Func<Error, CancellationToken, Task<TResult>> onFailure,
        CancellationToken cancellationToken = default) {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static async Task Finally(
        this Task<Outcome> task,
        Func<CancellationToken, Task> onSuccess,
        Func<Error, CancellationToken, Task> onFailure,
        CancellationToken cancellationToken = default) {
        var outcome = await task.ConfigureAwait(false);
        await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Then
    // -------------------------------------------------------------------------

    public static async Task<Outcome<TResult>> Then<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, Outcome<TResult>> next)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    public static async Task<Outcome> Then<T>(
        this Task<Outcome<T>> task,
        Func<T, Outcome> next)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    public static async Task<Outcome<TResult>> Then<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<Outcome<TResult>>> next,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Outcome> Then<T>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<Outcome>> next,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → To
    // -------------------------------------------------------------------------

    public static async Task<Outcome<TResult>> To<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, TResult> convert)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.To(convert);
    }

    public static async Task<Outcome<TResult>> To<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<TResult>> convert,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.To(convert, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Recover
    // -------------------------------------------------------------------------

    public static async Task<Outcome<T>> Recover<T>(
        this Task<Outcome<T>> task,
        Func<Error, Outcome<T>> fallback)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Recover(fallback);
    }

    public static async Task<Outcome<T>> Recover<T>(
        this Task<Outcome<T>> task,
        Func<Error, T> fallback)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Recover(fallback);
    }

    public static async Task<Outcome<T>> Recover<T>(
        this Task<Outcome<T>> task,
        Func<Error, CancellationToken, Task<Outcome<T>>> fallback,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Outcome<T>> Recover<T>(
        this Task<Outcome<T>> task,
        Func<Error, CancellationToken, Task<T>> fallback,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Recover(fallback, cancellationToken).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> → Finally
    // -------------------------------------------------------------------------

    public static async Task<TResult> Finally<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Finally(onSuccess, onFailure);
    }

    public static async Task Finally<T>(
        this Task<Outcome<T>> task,
        Action<T> onSuccess,
        Action<Error> onFailure)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        outcome.Finally(onSuccess, onFailure);
    }

    public static async Task<TResult> Finally<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<TResult>> onSuccess,
        Func<Error, CancellationToken, Task<TResult>> onFailure,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static async Task Finally<T>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task> onSuccess,
        Func<Error, CancellationToken, Task> onFailure,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        await outcome.Finally(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }
}

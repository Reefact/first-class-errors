namespace DiagnosableExceptions;

public static class OutcomeTaskExtensions {

    // Task<Outcome> → Outcome<TResult>
    public static async Task<Outcome<TResult>> Then<TResult>(
        this Task<Outcome> task,
        Func<Outcome<TResult>> next)
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    // Task<Outcome> → Outcome
    public static async Task<Outcome> Then(
        this Task<Outcome> task,
        Func<Outcome> next) {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    // Task<Outcome> → Task<Outcome<TResult>>  (async next)
    public static async Task<Outcome<TResult>> Then<TResult>(
        this Task<Outcome> task,
        Func<CancellationToken, Task<Outcome<TResult>>> next,
        CancellationToken cancellationToken = default)
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // Task<Outcome> → Task<Outcome>  (async next)
    public static async Task<Outcome> Then(
        this Task<Outcome> task,
        Func<CancellationToken, Task<Outcome>> next,
        CancellationToken cancellationToken = default) {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // Task<Outcome<T>> → Outcome<TResult>
    public static async Task<Outcome<TResult>> Then<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, Outcome<TResult>> next)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    // Task<Outcome<T>> → Outcome  (discards value)
    public static async Task<Outcome> Then<T>(
        this Task<Outcome<T>> task,
        Func<T, Outcome> next)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Then(next);
    }

    // Task<Outcome<T>> → Task<Outcome<TResult>>  (async next)
    public static async Task<Outcome<TResult>> Then<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<Outcome<TResult>>> next,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // Task<Outcome<T>> → Task<Outcome>  (async next, discards value)
    public static async Task<Outcome> Then<T>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<Outcome>> next,
        CancellationToken cancellationToken = default)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.Then(next, cancellationToken).ConfigureAwait(false);
    }

    // Task<Outcome<T>> → Outcome<TResult>  (To, sync)
    public static async Task<Outcome<TResult>> To<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, TResult> convert)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.To(convert);
    }

    // Task<Outcome<T>> → Task<Outcome<TResult>>  (To, async)
    public static async Task<Outcome<TResult>> To<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, CancellationToken, Task<TResult>> convert,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TResult : notnull {
        var outcome = await task.ConfigureAwait(false);
        return await outcome.To(convert, cancellationToken).ConfigureAwait(false);
    }

    // Task<Outcome<T>> → TResult  (Finally, avec valeur)
    public static async Task<TResult> Finally<T, TResult>(
        this Task<Outcome<T>> task,
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Finally(onSuccess, onFailure);
    }

    // Task<Outcome<T>> → void  (Finally, side effects)
    public static async Task Finally<T>(
        this Task<Outcome<T>> task,
        Action<T> onSuccess,
        Action<Error> onFailure)
        where T : notnull {
        var outcome = await task.ConfigureAwait(false);
        outcome.Finally(onSuccess, onFailure);
    }

    // Task<Outcome> → TResult  (Finally, sans valeur)
    public static async Task<TResult> Finally<TResult>(
        this Task<Outcome> task,
        Func<TResult> onSuccess,
        Func<Error, TResult> onFailure) {
        var outcome = await task.ConfigureAwait(false);
        return outcome.Finally(onSuccess, onFailure);
    }

    // Task<Outcome> → void  (Finally, side effects)
    public static async Task Finally(
        this Task<Outcome> task,
        Action onSuccess,
        Action<Error> onFailure) {
        var outcome = await task.ConfigureAwait(false);
        outcome.Finally(onSuccess, onFailure);
    }
}

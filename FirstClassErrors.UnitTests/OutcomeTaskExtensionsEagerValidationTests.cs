#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Verifies that <see cref="OutcomeTaskExtensions" /> validates its arguments <b>eagerly</b>: the guard clauses run
///     synchronously at the call site instead of being captured into the returned <see cref="Task" /> and only observed
///     on <c>await</c>. Each test drives the extension through a void-returning delegate that discards the result without
///     awaiting it (<c>() =&gt; { _ = call; }</c>), so the assertion can only succeed if the exception is thrown while the
///     method runs synchronously. The same call wrapped in an <c>async</c> method that defers validation would return a
///     faulted task and throw nothing here.
/// </summary>
[TestSubject(typeof(OutcomeTaskExtensions))]
public sealed class OutcomeTaskExtensionsEagerValidationTests {

    // -------------------------------------------------------------------------
    // A null task is rejected synchronously, before any await.
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then over a null Task<Outcome> throws synchronously, before the task is awaited.")]
    public void ThenOverANullTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome>)null!).Then(() => Outcome.Success); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then over a null Task<Outcome> throws synchronously, before the task is awaited.")]
    public void TheAsyncThenOverANullTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome>)null!).Then((_) => Task.FromResult(Outcome.Success)); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover over a null Task<Outcome> throws synchronously, before the task is awaited.")]
    public void RecoverOverANullTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome>)null!).Recover(_ => Outcome.Success); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally over a null Task<Outcome> throws synchronously, before the task is awaited.")]
    public void FinallyOverANullTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome>)null!).Finally(() => "ok", _ => "ko"); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Then over a null Task<Outcome<T>> throws synchronously, before the task is awaited.")]
    public void ThenOverANullGenericTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome<int>>)null!).Then(_ => Outcome.Success); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "To over a null Task<Outcome<T>> throws synchronously, before the task is awaited.")]
    public void ToOverANullGenericTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome<int>>)null!).To(value => value.ToString()); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover over a null Task<Outcome<T>> throws synchronously, before the task is awaited.")]
    public void RecoverOverANullGenericTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome<int>>)null!).Recover(_ => Outcome<int>.Success(1)); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally over a null Task<Outcome<T>> throws synchronously, before the task is awaited.")]
    public void FinallyOverANullGenericTaskOutcomeThrowsSynchronously() {
        // Exercise & verify
        Check.ThatCode(() => { _ = ((Task<Outcome<int>>)null!).Finally(value => $"ok:{value}", _ => "ko"); })
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // A null callback is rejected synchronously too, not deferred to the instance
    // method after the await.
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then over a Task<Outcome> with a null continuation throws synchronously.")]
    public void ThenWithANullContinuationThrowsSynchronously() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Then((Func<Outcome>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then over a Task<Outcome> with a null continuation throws synchronously.")]
    public void TheAsyncThenWithANullContinuationThrowsSynchronously() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Then((Func<CancellationToken, Task<Outcome>>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover over a Task<Outcome> with a null fallback throws synchronously.")]
    public void RecoverWithANullFallbackThrowsSynchronously() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Recover((Func<Error, Outcome>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally over a Task<Outcome> with a null success branch throws synchronously.")]
    public void FinallyWithANullSuccessBranchThrowsSynchronously() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Finally((Func<string>)null!, _ => "ko"); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally over a Task<Outcome> with a null failure branch throws synchronously.")]
    public void FinallyWithANullFailureBranchThrowsSynchronously() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Finally(() => "ok", (Func<Error, string>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "To over a Task<Outcome<T>> with a null conversion throws synchronously.")]
    public void ToWithANullConversionThrowsSynchronously() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(1));

        // Exercise & verify
        Check.ThatCode(() => { _ = task.To((Func<int, string>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover over a Task<Outcome<T>> with a null fallback throws synchronously.")]
    public void GenericRecoverWithANullFallbackThrowsSynchronously() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(1));

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Recover((Func<Error, Outcome<int>>)null!); })
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally over a Task<Outcome<T>> with a null failure branch throws synchronously.")]
    public void GenericFinallyWithANullFailureBranchThrowsSynchronously() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(1));

        // Exercise & verify
        Check.ThatCode(() => { _ = task.Finally(value => $"ok:{value}", (Func<Error, string>)null!); })
             .Throws<ArgumentNullException>();
    }

}

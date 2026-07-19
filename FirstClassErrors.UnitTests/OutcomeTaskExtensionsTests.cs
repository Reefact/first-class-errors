#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(OutcomeTaskExtensions))]
public sealed class OutcomeTaskExtensionsTests {

    [Fact(DisplayName = "Awaiting Then over a Task<Outcome<T>> chains the next step on success.")]
    public async Task AwaitingThenOverATaskOutcomeChainsTheNextStepOnSuccess() {
        // Setup
        System.Threading.Tasks.Task<Outcome<int>> task = System.Threading.Tasks.Task.FromResult(Outcome<int>.Success(4));

        // Exercise
        Outcome<int> result = await task.Then(value => Outcome<int>.Success(value + 1));

        // Verify
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.GetResultOrThrow()).IsEqualTo(5);
    }

    [Fact(DisplayName = "Awaiting Then over a Task<Outcome<T>> propagates the error on failure.")]
    public async Task AwaitingThenOverATaskOutcomePropagatesTheErrorOnFailure() {
        // Setup
        DomainError                               error  = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        System.Threading.Tasks.Task<Outcome<int>> task   = System.Threading.Tasks.Task.FromResult(Outcome<int>.Failure(error));
        bool                                      called = false;

        // Exercise
        Outcome<int> result = await task.Then(value => {
            called = true;

            return Outcome<int>.Success(value);
        });

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting Then (value mapping) over a Task<Outcome<T>> maps the value on success.")]
    public async Task AwaitingToOverATaskOutcomeMapsTheValueOnSuccess() {
        // Setup
        System.Threading.Tasks.Task<Outcome<int>> task = System.Threading.Tasks.Task.FromResult(Outcome<int>.Success(6));

        // Exercise
        Outcome<string> result = await task.Then(value => $"n={value}");

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo("n=6");
    }

    [Fact(DisplayName = "Awaiting Recover over a Task<Outcome<T>> recovers a failure into a success.")]
    public async Task AwaitingRecoverOverATaskOutcomeRecoversAFailureIntoASuccess() {
        // Setup
        DomainError                               error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        System.Threading.Tasks.Task<Outcome<int>> task  = System.Threading.Tasks.Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        Outcome<int> result = await task.Recover(_ => 42);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Awaiting Finally over a Task<Outcome<T>> resolves the success branch.")]
    public async Task AwaitingFinallyOverATaskOutcomeResolvesTheSuccessBranch() {
        // Setup
        System.Threading.Tasks.Task<Outcome<int>> task = System.Threading.Tasks.Task.FromResult(Outcome<int>.Success(5));

        // Exercise
        string result = await task.Finally(value => $"ok:{value}", _ => "ko");

        // Verify
        Check.That(result).IsEqualTo("ok:5");
    }

    [Fact(DisplayName = "Awaiting Finally over a Task<Outcome<T>> resolves the failure branch.")]
    public async Task AwaitingFinallyOverATaskOutcomeResolvesTheFailureBranch() {
        // Setup
        DomainError                               error = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        System.Threading.Tasks.Task<Outcome<int>> task  = System.Threading.Tasks.Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        string result = await task.Finally(value => $"ok:{value}", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "Awaiting Then over a Task<Outcome> chains the non-generic case.")]
    public async Task AwaitingThenOverATaskOutcomeChainsTheNonGenericCase() {
        // Setup
        System.Threading.Tasks.Task<Outcome> task = System.Threading.Tasks.Task.FromResult(Outcome.Success);

        // Exercise
        Outcome result = await task.Then(() => Outcome.Success);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Then over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void ThenOverANullTaskOutcomeThrowsAnArgumentNullException() {
        // Exercise & verify
        Check.ThatCode(() => ((System.Threading.Tasks.Task<Outcome<int>>)null!)
                             .Then(value => Outcome<int>.Success(value))
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then overload threads the ambient cancellation token to the callback.")]
    public async Task TheAsyncThenOverloadThreadsTheAmbientCancellationTokenToTheCallback() {
        // Setup
        CancellationToken                         token    = TestContext.Current.CancellationToken;
        CancellationToken                         received = default;
        System.Threading.Tasks.Task<Outcome<int>> task     = System.Threading.Tasks.Task.FromResult(Outcome<int>.Success(4));

        // Exercise
        Outcome<int> result = await task.Then(async (value, ct) => {
            received = ct;

            await System.Threading.Tasks.Task.CompletedTask;

            return Outcome<int>.Success(value + 1);
        }, token);

        // Verify
        Check.That(received).IsEqualTo(token);
        Check.That(result.GetResultOrThrow()).IsEqualTo(5);
    }

}

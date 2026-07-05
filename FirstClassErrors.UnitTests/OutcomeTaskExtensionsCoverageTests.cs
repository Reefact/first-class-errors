#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Exhaustive coverage of the <see cref="OutcomeTaskExtensions" /> overload matrix
///     (Then / To / Recover / Finally over Task&lt;Outcome&gt; and Task&lt;Outcome&lt;T&gt;&gt;,
///     synchronous and asynchronous callbacks, success and failure branches, and the null-task guard).
/// </summary>
[TestSubject(typeof(OutcomeTaskExtensions))]
public sealed class OutcomeTaskExtensionsCoverageTests {

    // -------------------------------------------------------------------------
    // Task<Outcome> -> Then
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then over a successful Task<Outcome> to a typed outcome runs the continuation.")]
    public async Task ThenOverASuccessfulTaskOutcomeToATypedOutcomeRunsTheContinuation() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise
        Outcome<int> result = await task.Then(() => Outcome<int>.Success(7));

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(7);
    }

    [Fact(DisplayName = "Then over a failed Task<Outcome> to a typed outcome propagates the error.")]
    public async Task ThenOverAFailedTaskOutcomeToATypedOutcomePropagatesTheError() {
        // Setup
        DomainError   error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome> task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        Outcome<int> result = await task.Then(() => Outcome<int>.Success(7));

        // Verify
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Then over a null Task<Outcome> to a typed outcome throws an ArgumentNullException.")]
    public void ThenOverANullTaskOutcomeToATypedOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Then(() => Outcome<int>.Success(1)).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Then over a failed Task<Outcome> propagates the error unchanged.")]
    public async Task ThenOverAFailedTaskOutcomePropagatesTheErrorUnchanged() {
        // Setup
        DomainError   error  = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome> task   = Task.FromResult(Outcome.Failure(error));
        bool          called = false;

        // Exercise
        Outcome result = await task.Then(() => {
            called = true;

            return Outcome.Success;
        });

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then over a Task<Outcome> to a typed outcome runs the continuation on success.")]
    public async Task TheAsyncThenOverATaskOutcomeToATypedOutcomeRunsTheContinuationOnSuccess() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        Task<Outcome>     task  = Task.FromResult(Outcome.Success);

        // Exercise
        Outcome<int> result = await task.Then((_) => Task.FromResult(Outcome<int>.Success(3)), token);

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(3);
    }

    [Fact(DisplayName = "The async Then over a failed Task<Outcome> to a typed outcome propagates the error.")]
    public async Task TheAsyncThenOverAFailedTaskOutcomeToATypedOutcomePropagatesTheError() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome>     task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        Outcome<int> result = await task.Then((_) => Task.FromResult(Outcome<int>.Success(3)), token);

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then over a null Task<Outcome> to a typed outcome throws an ArgumentNullException.")]
    public void TheAsyncThenOverANullTaskOutcomeToATypedOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Then((_) => Task.FromResult(Outcome<int>.Success(1)), TestContext.Current.CancellationToken)
                                                   .GetAwaiter()
                                                   .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then over a successful Task<Outcome> runs the non-generic continuation.")]
    public async Task TheAsyncThenOverASuccessfulTaskOutcomeRunsTheNonGenericContinuation() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        Task<Outcome>     task  = Task.FromResult(Outcome.Success);

        // Exercise
        Outcome result = await task.Then((_) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "The async Then over a failed Task<Outcome> propagates the error unchanged.")]
    public async Task TheAsyncThenOverAFailedTaskOutcomePropagatesTheErrorUnchanged() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome>     task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        Outcome result = await task.Then((_) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then over a null non-generic Task<Outcome> throws an ArgumentNullException.")]
    public void TheAsyncThenOverANullNonGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Then((_) => Task.FromResult(Outcome.Success), TestContext.Current.CancellationToken)
                                                   .GetAwaiter()
                                                   .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> -> Recover
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Recover over a failed Task<Outcome> replaces the failure with the fallback.")]
    public async Task RecoverOverAFailedTaskOutcomeReplacesTheFailureWithTheFallback() {
        // Setup
        DomainError   error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome> task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        Outcome result = await task.Recover(_ => Outcome.Success);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Recover over a successful Task<Outcome> leaves the outcome unchanged.")]
    public async Task RecoverOverASuccessfulTaskOutcomeLeavesTheOutcomeUnchanged() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise
        Outcome result = await task.Recover(_ => Outcome.Failure(ErrorFactory.Domain(ErrorCode.Unspecified, "fallback")));

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Recover over a null Task<Outcome> throws an ArgumentNullException.")]
    public void RecoverOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Recover(_ => Outcome.Success).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover over a failed Task<Outcome> replaces the failure with the fallback.")]
    public async Task TheAsyncRecoverOverAFailedTaskOutcomeReplacesTheFailureWithTheFallback() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome>     task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        Outcome result = await task.Recover((_, _) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "The async Recover over a null Task<Outcome> throws an ArgumentNullException.")]
    public void TheAsyncRecoverOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Recover((_, _) => Task.FromResult(Outcome.Success), TestContext.Current.CancellationToken)
                                                   .GetAwaiter()
                                                   .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome> -> Finally
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Finally over a Task<Outcome> resolves the success branch to a value.")]
    public async Task FinallyOverATaskOutcomeResolvesTheSuccessBranchToAValue() {
        // Setup
        Task<Outcome> task = Task.FromResult(Outcome.Success);

        // Exercise
        string result = await task.Finally(() => "ok", _ => "ko");

        // Verify
        Check.That(result).IsEqualTo("ok");
    }

    [Fact(DisplayName = "Finally over a Task<Outcome> resolves the failure branch to a value.")]
    public async Task FinallyOverATaskOutcomeResolvesTheFailureBranchToAValue() {
        // Setup
        DomainError   error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome> task  = Task.FromResult(Outcome.Failure(error));

        // Exercise
        string result = await task.Finally(() => "ok", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "Finally with a value over a null Task<Outcome> throws an ArgumentNullException.")]
    public void FinallyWithAValueOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Finally(() => "ok", _ => "ko").GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with actions over a Task<Outcome> runs the branch matching the outcome.")]
    public async Task FinallyWithActionsOverATaskOutcomeRunsTheBranchMatchingTheOutcome() {
        // Setup
        DomainError   error          = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        bool          successOnOk    = false;
        Error?        capturedOnFail = null;

        // Exercise
        await Task.FromResult(Outcome.Success).Finally(() => successOnOk       = true, _ => { });
        await Task.FromResult(Outcome.Failure(error)).Finally(() => { }, failure => capturedOnFail = failure);

        // Verify
        Check.That(successOnOk).IsTrue();
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Finally with actions over a null Task<Outcome> throws an ArgumentNullException.")]
    public void FinallyWithActionsOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Finally(() => { }, _ => { }).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally over a Task<Outcome> resolves both branches to a value.")]
    public async Task TheAsyncFinallyOverATaskOutcomeResolvesBothBranchesToAValue() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        string onSuccess = await Task.FromResult(Outcome.Success)
                                     .Finally((_) => Task.FromResult("ok"), (_, _) => Task.FromResult("ko"), token);
        string onFailure = await Task.FromResult(Outcome.Failure(error))
                                     .Finally((_) => Task.FromResult("ok"), (_, _) => Task.FromResult("ko"), token);

        // Verify
        Check.That(onSuccess).IsEqualTo("ok");
        Check.That(onFailure).IsEqualTo("ko");
    }

    [Fact(DisplayName = "The async Finally with a value over a null Task<Outcome> throws an ArgumentNullException.")]
    public void TheAsyncFinallyWithAValueOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!)
                             .Finally((_) => Task.FromResult("ok"), (_, _) => Task.FromResult("ko"), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with actions over a Task<Outcome> runs the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithActionsOverATaskOutcomeRunsTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token          = TestContext.Current.CancellationToken;
        DomainError       error          = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        bool              successOnOk    = false;
        Error?            capturedOnFail = null;

        // Exercise
        await Task.FromResult(Outcome.Success)
                  .Finally((_) => { successOnOk = true; return Task.CompletedTask; }, (_, _) => Task.CompletedTask, token);
        await Task.FromResult(Outcome.Failure(error))
                  .Finally((_) => Task.CompletedTask, (failure, _) => { capturedOnFail = failure; return Task.CompletedTask; }, token);

        // Verify
        Check.That(successOnOk).IsTrue();
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Finally with actions over a null Task<Outcome> throws an ArgumentNullException.")]
    public void TheAsyncFinallyWithActionsOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!)
                             .Finally((_) => Task.CompletedTask, (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> -> Then
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then discarding the value over a Task<Outcome<T>> chains to a non-generic outcome on success.")]
    public async Task ThenDiscardingTheValueOverATaskOutcomeChainsToANonGenericOutcomeOnSuccess() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(3));

        // Exercise
        Outcome result = await task.Then(_ => Outcome.Success);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Then discarding the value over a failed Task<Outcome<T>> propagates the error.")]
    public async Task ThenDiscardingTheValueOverAFailedTaskOutcomePropagatesTheError() {
        // Setup
        DomainError        error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome<int>> task  = Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        Outcome result = await task.Then(_ => Outcome.Success);

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Then discarding the value over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void ThenDiscardingTheValueOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).Then(_ => Outcome.Success).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then over a failed Task<Outcome<T>> to a typed outcome propagates the error.")]
    public async Task TheAsyncThenOverAFailedTaskOutcomeToATypedOutcomePropagatesTheErrorGeneric() {
        // Setup
        CancellationToken  token = TestContext.Current.CancellationToken;
        DomainError        error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome<int>> task  = Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        Outcome<int> result = await task.Then((value, _) => Task.FromResult(Outcome<int>.Success(value + 1)), token);

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then discarding the value over a Task<Outcome<T>> chains on success and propagates on failure.")]
    public async Task TheAsyncThenDiscardingTheValueOverATaskOutcomeChainsOnSuccessAndPropagatesOnFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome onSuccess = await Task.FromResult(Outcome<int>.Success(3))
                                      .Then((_, _) => Task.FromResult(Outcome.Success), token);
        Outcome onFailure = await Task.FromResult(Outcome<int>.Failure(error))
                                      .Then((_, _) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(onSuccess.IsSuccess).IsTrue();
        Check.That(onFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then discarding the value over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncThenDiscardingTheValueOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Then((_, _) => Task.FromResult(Outcome.Success), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> -> To
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "To over a failed Task<Outcome<T>> propagates the error.")]
    public async Task ToOverAFailedTaskOutcomePropagatesTheError() {
        // Setup
        DomainError        error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome<int>> task  = Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        Outcome<string> result = await task.To(value => $"n={value}");

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async To over a Task<Outcome<T>> maps on success and propagates on failure.")]
    public async Task TheAsyncToOverATaskOutcomeMapsOnSuccessAndPropagatesOnFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome<string> onSuccess = await Task.FromResult(Outcome<int>.Success(6))
                                              .To((value, _) => Task.FromResult($"n={value}"), token);
        Outcome<string> onFailure = await Task.FromResult(Outcome<int>.Failure(error))
                                              .To((value, _) => Task.FromResult($"n={value}"), token);

        // Verify
        Check.That(onSuccess.GetResultOrThrow()).IsEqualTo("n=6");
        Check.That(onFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async To over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncToOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .To((value, _) => Task.FromResult(value.ToString()), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> -> Recover
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Recover with an outcome fallback over a failed Task<Outcome<T>> replaces the failure.")]
    public async Task RecoverWithAnOutcomeFallbackOverAFailedTaskOutcomeReplacesTheFailure() {
        // Setup
        DomainError        error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome<int>> task  = Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        Outcome<int> result = await task.Recover(_ => Outcome<int>.Success(42));

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Recover with an outcome fallback over a successful Task<Outcome<T>> leaves it unchanged.")]
    public async Task RecoverWithAnOutcomeFallbackOverASuccessfulTaskOutcomeLeavesItUnchanged() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(1));

        // Exercise
        Outcome<int> result = await task.Recover(_ => Outcome<int>.Success(42));

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "Recover with an outcome fallback over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void RecoverWithAnOutcomeFallbackOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).Recover(_ => Outcome<int>.Success(1)).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover with a value fallback over a successful Task<Outcome<T>> leaves it unchanged.")]
    public async Task RecoverWithAValueFallbackOverASuccessfulTaskOutcomeLeavesItUnchanged() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(1));

        // Exercise
        Outcome<int> result = await task.Recover(_ => 42);

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "Recover with a value fallback over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void RecoverWithAValueFallbackOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).Recover(_ => 42).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover with an outcome fallback over a Task<Outcome<T>> recovers a failure and passes a success through.")]
    public async Task TheAsyncRecoverWithAnOutcomeFallbackOverATaskOutcomeRecoversAndPassesThrough() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome<int> recovered = await Task.FromResult(Outcome<int>.Failure(error))
                                           .Recover((_, _) => Task.FromResult(Outcome<int>.Success(42)), token);
        Outcome<int> passed = await Task.FromResult(Outcome<int>.Success(1))
                                        .Recover((_, _) => Task.FromResult(Outcome<int>.Success(42)), token);

        // Verify
        Check.That(recovered.GetResultOrThrow()).IsEqualTo(42);
        Check.That(passed.GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "The async Recover with an outcome fallback over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncRecoverWithAnOutcomeFallbackOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Recover((_, _) => Task.FromResult(Outcome<int>.Success(1)), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover with a value fallback over a Task<Outcome<T>> recovers a failure and passes a success through.")]
    public async Task TheAsyncRecoverWithAValueFallbackOverATaskOutcomeRecoversAndPassesThrough() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome<int> recovered = await Task.FromResult(Outcome<int>.Failure(error))
                                           .Recover((_, _) => Task.FromResult(42), token);
        Outcome<int> passed = await Task.FromResult(Outcome<int>.Success(1))
                                        .Recover((_, _) => Task.FromResult(42), token);

        // Verify
        Check.That(recovered.GetResultOrThrow()).IsEqualTo(42);
        Check.That(passed.GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "The async Recover with a value fallback over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncRecoverWithAValueFallbackOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Recover((_, _) => Task.FromResult(1), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Task<Outcome<T>> -> Finally
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Finally over a failed Task<Outcome<T>> resolves the failure branch to a value.")]
    public async Task FinallyOverAFailedTaskOutcomeResolvesTheFailureBranchToAValue() {
        // Setup
        DomainError        error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        Task<Outcome<int>> task  = Task.FromResult(Outcome<int>.Failure(error));

        // Exercise
        string result = await task.Finally(value => $"ok:{value}", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "Finally with a value over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void FinallyWithAValueOverANullGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).Finally(value => $"ok:{value}", _ => "ko").GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with actions over a Task<Outcome<T>> runs the branch matching the outcome.")]
    public async Task FinallyWithActionsOverATaskGenericOutcomeRunsTheBranchMatchingTheOutcome() {
        // Setup
        DomainError error          = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        int         capturedValue  = 0;
        Error?      capturedOnFail = null;

        // Exercise
        await Task.FromResult(Outcome<int>.Success(5)).Finally(value => capturedValue = value, _ => { });
        await Task.FromResult(Outcome<int>.Failure(error)).Finally(_ => { }, failure => capturedOnFail = failure);

        // Verify
        Check.That(capturedValue).IsEqualTo(5);
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Finally with actions over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void FinallyWithActionsOverANullGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).Finally(_ => { }, _ => { }).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally over a Task<Outcome<T>> resolves both branches to a value.")]
    public async Task TheAsyncFinallyOverATaskGenericOutcomeResolvesBothBranchesToAValue() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");

        // Exercise
        string onSuccess = await Task.FromResult(Outcome<int>.Success(5))
                                     .Finally((value, _) => Task.FromResult($"ok:{value}"), (_, _) => Task.FromResult("ko"), token);
        string onFailure = await Task.FromResult(Outcome<int>.Failure(error))
                                     .Finally((value, _) => Task.FromResult($"ok:{value}"), (_, _) => Task.FromResult("ko"), token);

        // Verify
        Check.That(onSuccess).IsEqualTo("ok:5");
        Check.That(onFailure).IsEqualTo("ko");
    }

    [Fact(DisplayName = "The async Finally with a value over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncFinallyWithAValueOverANullGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Finally((value, _) => Task.FromResult($"ok:{value}"), (_, _) => Task.FromResult("ko"), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with actions over a Task<Outcome<T>> runs the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithActionsOverATaskGenericOutcomeRunsTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token          = TestContext.Current.CancellationToken;
        DomainError       error          = ErrorFactory.Domain(ErrorCode.Unspecified, "boom");
        int               capturedValue  = 0;
        Error?            capturedOnFail = null;

        // Exercise
        await Task.FromResult(Outcome<int>.Success(5))
                  .Finally((value, _) => { capturedValue = value; return Task.CompletedTask; }, (_, _) => Task.CompletedTask, token);
        await Task.FromResult(Outcome<int>.Failure(error))
                  .Finally((_, _) => Task.CompletedTask, (failure, _) => { capturedOnFail = failure; return Task.CompletedTask; }, token);

        // Verify
        Check.That(capturedValue).IsEqualTo(5);
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Finally with actions over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void TheAsyncFinallyWithActionsOverANullGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Finally((_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask, TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Remaining null-task guards
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then over a null non-generic Task<Outcome> throws an ArgumentNullException.")]
    public void ThenOverANullNonGenericTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome>)null!).Then(() => Outcome.Success).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then over a null Task<Outcome<T>> to a typed outcome throws an ArgumentNullException.")]
    public void TheAsyncThenOverANullGenericTaskOutcomeToATypedOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!)
                             .Then((value, _) => Task.FromResult(Outcome<int>.Success(value + 1)), TestContext.Current.CancellationToken)
                             .GetAwaiter()
                             .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "To over a null Task<Outcome<T>> throws an ArgumentNullException.")]
    public void ToOverANullTaskOutcomeThrows() {
        // Exercise & verify
        Check.ThatCode(() => ((Task<Outcome<int>>)null!).To(value => value.ToString()).GetAwaiter().GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Null-outcome guard (task that resolves to a null Outcome)
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Then over a Task<Outcome> that resolves to null throws an InvalidOperationException.")]
    public void ThenOverATaskResolvingToANullOutcomeThrows() {
        // Setup
        Task<Outcome> task = Task.FromResult<Outcome>(null!);

        // Exercise & verify
        Check.ThatCode(() => task.Then(() => Outcome<int>.Success(1)).GetAwaiter().GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "Then over a Task<Outcome<T>> that resolves to null throws an InvalidOperationException.")]
    public void ThenOverAGenericTaskResolvingToANullOutcomeThrows() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult<Outcome<int>>(null!);

        // Exercise & verify
        Check.ThatCode(() => task.Then(value => Outcome<int>.Success(value + 1)).GetAwaiter().GetResult())
             .Throws<InvalidOperationException>();
    }

}

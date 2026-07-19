#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Covers the asynchronous instance methods and the action-based <c>Finally</c> overloads of the non-generic
///     <see cref="Outcome" />, together with the branches and guards left uncovered by the primary suite.
/// </summary>
[TestSubject(typeof(Outcome))]
public sealed class OutcomeAsyncInstanceCoverageTests {

    [Fact(DisplayName = "Then to a typed outcome runs the next step when the outcome is a success.")]
    public void ThenToATypedOutcomeRunsTheNextStepOnSuccess() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise
        Outcome<int> result = outcome.Then(() => Outcome<int>.Success(1));

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(1);
    }

    [Fact(DisplayName = "Then to a typed outcome guards against a null next function.")]
    public void ThenToATypedOutcomeGuardsAgainstANullNextFunction() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<Outcome<int>>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then to a typed outcome chains on success and propagates on failure.")]
    public async Task TheAsyncThenToATypedOutcomeChainsOnSuccessAndPropagatesOnFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> onSuccess = await Outcome.Success.Then((_) => Task.FromResult(Outcome<int>.Success(9)), token);
        Outcome<int> onFailure = await Outcome.Failure(error).Then((_) => Task.FromResult(Outcome<int>.Success(9)), token);

        // Verify
        Check.That(onSuccess.GetResultOrThrow()).IsEqualTo(9);
        Check.That(onFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then to a typed outcome guards against a null next function.")]
    public void TheAsyncThenToATypedOutcomeGuardsAgainstANullNextFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<CancellationToken, Task<Outcome<int>>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then guards against a null next function.")]
    public void TheAsyncThenGuardsAgainstANullNextFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<CancellationToken, Task<Outcome>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover passes a success through and recovers a failure.")]
    public async Task TheAsyncRecoverPassesASuccessThroughAndRecoversAFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome passed    = await Outcome.Success.Recover((_, _) => Task.FromResult(Outcome.Failure(error)), token);
        Outcome recovered = await Outcome.Failure(error).Recover((_, _) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(passed.IsSuccess).IsTrue();
        Check.That(recovered.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "The async Recover guards against a null fallback function.")]
    public void TheAsyncRecoverGuardsAgainstANullFallbackFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, CancellationToken, Task<Outcome>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with a value resolves the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithAValueResolvesTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        string onSuccess = await Outcome.Success.Finally((_) => Task.FromResult("ok"), (_, _) => Task.FromResult("ko"), token);
        string onFailure = await Outcome.Failure(error).Finally((_) => Task.FromResult("ok"), (_, _) => Task.FromResult("ko"), token);

        // Verify
        Check.That(onSuccess).IsEqualTo("ok");
        Check.That(onFailure).IsEqualTo("ko");
    }

    [Fact(DisplayName = "The async Finally with a value guards against null handlers.")]
    public void TheAsyncFinallyWithAValueGuardsAgainstNullHandlers() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<CancellationToken, Task<string>>)null!, (_, _) => Task.FromResult("ko"), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally((_) => Task.FromResult("ok"), (Func<Error, CancellationToken, Task<string>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with actions runs the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithActionsRunsTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token          = TestContext.Current.CancellationToken;
        DomainError       error          = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        bool              successOnOk    = false;
        Error?            capturedOnFail = null;

        // Exercise
        await Outcome.Success.Finally((_) => { successOnOk = true; return Task.CompletedTask; }, (_, _) => Task.CompletedTask, token);
        await Outcome.Failure(error).Finally((_) => Task.CompletedTask, (failure, _) => { capturedOnFail = failure; return Task.CompletedTask; }, token);

        // Verify
        Check.That(successOnOk).IsTrue();
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Finally with actions guards against null handlers.")]
    public void TheAsyncFinallyWithActionsGuardsAgainstNullHandlers() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<CancellationToken, Task>)null!, (_, _) => Task.CompletedTask, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally((_) => Task.CompletedTask, (Func<Error, CancellationToken, Task>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with a value guards against a null onFailure function.")]
    public void FinallyWithAValueGuardsAgainstANullOnFailureFunction() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally(() => "ok", (Func<Error, string>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with actions guards against null handlers.")]
    public void FinallyWithActionsGuardsAgainstNullHandlers() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Action)null!, _ => { }))
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally(() => { }, (Action<Error>)null!))
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Null-task guard: the async forward overloads return the callback's task
    // directly, so a callback that hands back a null task fails with an explicit
    // InvalidOperationException instead of letting the null escape and surface as
    // a NullReferenceException when the caller awaits it.
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "The async Then to a typed outcome throws when the next callback returns a null task.")]
    public void TheAsyncThenToATypedOutcomeThrowsWhenNextReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<CancellationToken, Task<Outcome<int>>>)(_ => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Then throws when the next callback returns a null task.")]
    public void TheAsyncThenThrowsWhenNextReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<CancellationToken, Task<Outcome>>)(_ => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Recover throws when the fallback callback returns a null task.")]
    public void TheAsyncRecoverThrowsWhenFallbackReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome           outcome = Outcome.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, CancellationToken, Task<Outcome>>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Finally with a value throws when the onSuccess callback returns a null task.")]
    public void TheAsyncFinallyWithAValueThrowsWhenOnSuccessReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome           outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<CancellationToken, Task<string>>)(_ => null!), (_, _) => Task.FromResult("ko"), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Finally with actions throws when the onFailure callback returns a null task.")]
    public void TheAsyncFinallyWithActionsThrowsWhenOnFailureReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome           outcome = Outcome.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally(_ => Task.CompletedTask, (Func<Error, CancellationToken, Task>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

}

/// <summary>
///     Covers the asynchronous instance methods, the action-based <c>Finally</c> overloads, and the recovery
///     branches of <see cref="Outcome{T}" /> left uncovered by the primary suite.
/// </summary>
[TestSubject(typeof(Outcome<>))]
public sealed class OutcomeGenericAsyncInstanceCoverageTests {

    [Fact(DisplayName = "Then to a typed outcome guards against a null next function.")]
    public void ThenToATypedOutcomeGuardsAgainstANullNextFunction() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, Outcome<string>>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then to a typed outcome chains on success and propagates on failure.")]
    public async Task TheAsyncThenToATypedOutcomeChainsOnSuccessAndPropagatesOnFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> onSuccess = await Outcome<int>.Success(4).Then((value, _) => Task.FromResult(Outcome<int>.Success(value + 1)), token);
        Outcome<int> onFailure = await Outcome<int>.Failure(error).Then((value, _) => Task.FromResult(Outcome<int>.Success(value + 1)), token);

        // Verify
        Check.That(onSuccess.GetResultOrThrow()).IsEqualTo(5);
        Check.That(onFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then to a typed outcome guards against a null next function.")]
    public void TheAsyncThenToATypedOutcomeGuardsAgainstANullNextFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, CancellationToken, Task<Outcome<string>>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then discarding the value chains on success and propagates on failure.")]
    public async Task TheAsyncThenDiscardingTheValueChainsOnSuccessAndPropagatesOnFailure() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome onSuccess = await Outcome<int>.Success(4).Then((_, _) => Task.FromResult(Outcome.Success), token);
        Outcome onFailure = await Outcome<int>.Failure(error).Then((_, _) => Task.FromResult(Outcome.Success), token);

        // Verify
        Check.That(onSuccess.IsSuccess).IsTrue();
        Check.That(onFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then discarding the value guards against a null next function.")]
    public void TheAsyncThenDiscardingTheValueGuardsAgainstANullNextFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, CancellationToken, Task<Outcome>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Then (value mapping) propagates the error on failure.")]
    public async Task TheAsyncToPropagatesTheErrorOnFailure() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int>      outcome = Outcome<int>.Failure(error);

        // Exercise
        Outcome<string> result = await outcome.Then(async (value, _) => {
            await Task.CompletedTask;

            return value.ToString();
        }, token);

        // Verify
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Then (value mapping) guards against a null converter.")]
    public void TheAsyncToGuardsAgainstANullConverter() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, CancellationToken, Task<string>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover with an outcome fallback passes a success through and recovers a failure.")]
    public void RecoverWithAnOutcomeFallbackPassesASuccessThroughAndRecoversAFailure() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int> success = Outcome<int>.Success(1);
        Outcome<int> failure = Outcome<int>.Failure(error);

        // Exercise
        Outcome<int> passed    = success.Recover(_ => Outcome<int>.Success(42));
        Outcome<int> recovered = failure.Recover(_ => Outcome<int>.Success(42));

        // Verify
        Check.That(passed).IsSameReferenceAs(success);
        Check.That(recovered.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Recover with a value fallback passes a success through and recovers a failure.")]
    public void RecoverWithAValueFallbackPassesASuccessThroughAndRecoversAFailure() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int> success = Outcome<int>.Success(1);
        Outcome<int> failure = Outcome<int>.Failure(error);

        // Exercise
        Outcome<int> passed    = success.Recover(_ => 42);
        Outcome<int> recovered = failure.Recover(_ => 42);

        // Verify
        Check.That(passed).IsSameReferenceAs(success);
        Check.That(recovered.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Recover with a value fallback guards against a null fallback function.")]
    public void RecoverWithAValueFallbackGuardsAgainstANullFallbackFunction() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, int>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover with an outcome fallback passes a success through and recovers a failure.")]
    public async Task TheAsyncRecoverWithAnOutcomeFallbackPassesASuccessThroughAndRecoversAFailure() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int>      success = Outcome<int>.Success(1);

        // Exercise
        Outcome<int> passed    = await success.Recover((_, _) => Task.FromResult(Outcome<int>.Success(42)), token);
        Outcome<int> recovered = await Outcome<int>.Failure(error).Recover((_, _) => Task.FromResult(Outcome<int>.Success(42)), token);

        // Verify
        Check.That(passed).IsSameReferenceAs(success);
        Check.That(recovered.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "The async Recover with an outcome fallback guards against a null fallback function.")]
    public void TheAsyncRecoverWithAnOutcomeFallbackGuardsAgainstANullFallbackFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, CancellationToken, Task<Outcome<int>>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Recover with a value fallback passes a success through.")]
    public async Task TheAsyncRecoverWithAValueFallbackPassesASuccessThrough() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      success = Outcome<int>.Success(1);

        // Exercise
        Outcome<int> passed = await success.Recover((_, _) => Task.FromResult(42), token);

        // Verify
        Check.That(passed).IsSameReferenceAs(success);
    }

    [Fact(DisplayName = "The async Recover with a value fallback guards against a null fallback function.")]
    public void TheAsyncRecoverWithAValueFallbackGuardsAgainstANullFallbackFunction() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, CancellationToken, Task<int>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with actions runs the branch matching the outcome.")]
    public void FinallyWithActionsRunsTheBranchMatchingTheOutcome() {
        // Setup
        DomainError error          = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        int         capturedValue  = 0;
        Error?      capturedOnFail = null;

        // Exercise
        Outcome<int>.Success(5).Finally(value => capturedValue = value, _ => { });
        Outcome<int>.Failure(error).Finally(_ => { }, failure => capturedOnFail = failure);

        // Verify
        Check.That(capturedValue).IsEqualTo(5);
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Finally with actions guards against null handlers.")]
    public void FinallyWithActionsGuardsAgainstNullHandlers() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Action<int>)null!, _ => { }))
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally(_ => { }, (Action<Error>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally with a value resolves the failure branch.")]
    public void FinallyWithAValueResolvesTheFailureBranch() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Exercise
        string result = outcome.Finally(value => $"ok:{value}", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "Finally with a value guards against a null onFailure function.")]
    public void FinallyWithAValueGuardsAgainstANullOnFailureFunction() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally(value => value.ToString(), (Func<Error, string>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with a value resolves the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithAValueResolvesTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token = TestContext.Current.CancellationToken;
        DomainError       error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        string onSuccess = await Outcome<int>.Success(5).Finally((value, _) => Task.FromResult($"ok:{value}"), (_, _) => Task.FromResult("ko"), token);
        string onFailure = await Outcome<int>.Failure(error).Finally((value, _) => Task.FromResult($"ok:{value}"), (_, _) => Task.FromResult("ko"), token);

        // Verify
        Check.That(onSuccess).IsEqualTo("ok:5");
        Check.That(onFailure).IsEqualTo("ko");
    }

    [Fact(DisplayName = "The async Finally with a value guards against null handlers.")]
    public void TheAsyncFinallyWithAValueGuardsAgainstNullHandlers() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<int, CancellationToken, Task<string>>)null!, (_, _) => Task.FromResult("ko"), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally((value, _) => Task.FromResult(value.ToString()), (Func<Error, CancellationToken, Task<string>>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "The async Finally with actions runs the branch matching the outcome.")]
    public async Task TheAsyncFinallyWithActionsRunsTheBranchMatchingTheOutcome() {
        // Setup
        CancellationToken token          = TestContext.Current.CancellationToken;
        DomainError       error          = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        int               capturedValue  = 0;
        Error?            capturedOnFail = null;

        // Exercise
        await Outcome<int>.Success(5).Finally((value, _) => { capturedValue = value; return Task.CompletedTask; }, (_, _) => Task.CompletedTask, token);
        await Outcome<int>.Failure(error).Finally((_, _) => Task.CompletedTask, (failure, _) => { capturedOnFail = failure; return Task.CompletedTask; }, token);

        // Verify
        Check.That(capturedValue).IsEqualTo(5);
        Check.That(capturedOnFail).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The async Finally with actions guards against null handlers.")]
    public void TheAsyncFinallyWithActionsGuardsAgainstNullHandlers() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<int, CancellationToken, Task>)null!, (_, _) => Task.CompletedTask, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
        Check.ThatCode(() => outcome.Finally((_, _) => Task.CompletedTask, (Func<Error, CancellationToken, Task>)null!, token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // Null-task guard (see the non-generic suite): a callback returning a null
    // task fails with InvalidOperationException instead of forwarding the null.
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "The async Then to a typed outcome throws when the next callback returns a null task.")]
    public void TheAsyncThenToATypedOutcomeThrowsWhenNextReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, CancellationToken, Task<Outcome<string>>>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Then discarding the value throws when the next callback returns a null task.")]
    public void TheAsyncThenDiscardingTheValueThrowsWhenNextReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, CancellationToken, Task<Outcome>>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Recover with an outcome fallback throws when the fallback returns a null task.")]
    public void TheAsyncRecoverWithAnOutcomeFallbackThrowsWhenFallbackReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int>      outcome = Outcome<int>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, CancellationToken, Task<Outcome<int>>>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Finally with a value throws when the onSuccess callback returns a null task.")]
    public void TheAsyncFinallyWithAValueThrowsWhenOnSuccessReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        Outcome<int>      outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<int, CancellationToken, Task<string>>)((_, _) => null!), (_, _) => Task.FromResult("ko"), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "The async Finally with actions throws when the onFailure callback returns a null task.")]
    public void TheAsyncFinallyWithActionsThrowsWhenOnFailureReturnsANullTask() {
        // Setup
        CancellationToken token   = TestContext.Current.CancellationToken;
        DomainError       error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int>      outcome = Outcome<int>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((_, _) => Task.CompletedTask, (Func<Error, CancellationToken, Task>)((_, _) => null!), token)
                                    .GetAwaiter()
                                    .GetResult())
             .Throws<InvalidOperationException>();
    }

}

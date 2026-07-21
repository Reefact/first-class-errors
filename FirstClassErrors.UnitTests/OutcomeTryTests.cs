#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Outcome))]
public sealed class OutcomeTryTests {

    #region Synchronous value-producing Try

    [Fact(DisplayName = "Try returns a success carrying the result when the operation does not throw.")]
    public void TryReturnsASuccessCarryingTheResultWhenTheOperationDoesNotThrow() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> outcome = Outcome.Try<int, InvalidOperationException>(() => 42, _ => error);

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Try maps the caught exception to a failure carrying the mapper's error.")]
    public void TryMapsTheCaughtExceptionToAFailureCarryingTheMappersError() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> outcome = Outcome.Try<int, InvalidOperationException>((Func<int>)(() => throw new InvalidOperationException("boom")), _ => error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Try passes the thrown exception instance to the mapper.")]
    public void TryPassesTheThrownExceptionInstanceToTheMapper() {
        // Setup
        DomainError               error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        InvalidOperationException thrown  = new("boom");
        Exception?                received = null;

        // Exercise
        Outcome.Try<int, InvalidOperationException>((Func<int>)(() => throw thrown), exception => {
            received = exception;

            return error;
        });

        // Verify
        Check.That(received).IsSameReferenceAs(thrown);
    }

    [Fact(DisplayName = "Try does not catch an exception of a different type.")]
    public void TryDoesNotCatchAnExceptionOfADifferentType() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<int, FormatException>((Func<int>)(() => throw new InvalidOperationException("boom")), _ => error))
             .Throws<InvalidOperationException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Try lets an OperationCanceledException propagate even when TException is Exception.")]
    public void TryLetsAnOperationCanceledExceptionPropagateEvenWhenTExceptionIsException() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<int, Exception>((Func<int>)(() => throw new OperationCanceledException()), _ => error))
             .Throws<OperationCanceledException>();
    }

    [Fact(DisplayName = "Try guards against a null operation.")]
    public void TryGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<int, InvalidOperationException>((Func<int>)null!, _ => error))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Try guards against a null onError mapper.")]
    public void TryGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<int, InvalidOperationException>(() => 42, (Func<InvalidOperationException, Error>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Try throws an ArgumentNullException when the mapper maps a caught exception to a null error.")]
    public void TryThrowsAnArgumentNullExceptionWhenTheMapperReturnsANullError() {
        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<int, InvalidOperationException>((Func<int>)(() => throw new InvalidOperationException()), _ => (Error)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Try surfaces a null operation result as a contract violation rather than mapping it, even when TException is broad.")]
    public void TrySurfacesANullOperationResultRatherThanMappingIt() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify: Success rejects a null result, and because it runs after the catch that
        // ArgumentNullException surfaces as the contract violation it is rather than being mapped through onError.
        Check.ThatCode(() => Outcome.Try<string, Exception>((Func<string>)(() => null!), _ => error))
             .Throws<ArgumentNullException>();
    }

    #endregion

    #region Synchronous side-effecting Try

    [Fact(DisplayName = "Try (void) returns a success and runs the side effect when the operation does not throw.")]
    public void TryVoidReturnsASuccessAndRunsTheSideEffectWhenTheOperationDoesNotThrow() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        bool        ran   = false;

        // Exercise
        Outcome outcome = Outcome.Try<InvalidOperationException>(() => { ran = true; }, _ => error);

        // Verify
        Check.That(ran).IsTrue();
        Check.That(outcome.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Try (void) maps the caught exception to a failure.")]
    public void TryVoidMapsTheCaughtExceptionToAFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome outcome = Outcome.Try<InvalidOperationException>((Action)(() => throw new InvalidOperationException()), _ => error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Try (void) does not catch an exception of a different type.")]
    public void TryVoidDoesNotCatchAnExceptionOfADifferentType() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<FormatException>((Action)(() => throw new InvalidOperationException("boom")), _ => error))
             .Throws<InvalidOperationException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Try (void) lets an OperationCanceledException propagate.")]
    public void TryVoidLetsAnOperationCanceledExceptionPropagate() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<Exception>((Action)(() => throw new OperationCanceledException()), _ => error))
             .Throws<OperationCanceledException>();
    }

    [Fact(DisplayName = "Try (void) guards against a null operation.")]
    public void TryVoidGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<InvalidOperationException>((Action)null!, _ => error))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Try (void) guards against a null onError mapper.")]
    public void TryVoidGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        Check.ThatCode(() => Outcome.Try<InvalidOperationException>(() => { }, (Func<InvalidOperationException, Error>)null!))
             .Throws<ArgumentNullException>();
    }

    #endregion

    #region Asynchronous value-producing Try

    [Fact(DisplayName = "Awaiting the async Try returns a success carrying the result when the operation does not throw.")]
    public async Task AwaitingTheAsyncTryReturnsASuccessCarryingTheResultWhenTheOperationDoesNotThrow() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> outcome = await Outcome.Try<int, InvalidOperationException>(
            _ => Task.FromResult(42), _ => error, TestContext.Current.CancellationToken);

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Awaiting the async Try maps a caught exception to a failure.")]
    public async Task AwaitingTheAsyncTryMapsACaughtExceptionToAFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> outcome = await Outcome.Try<int, InvalidOperationException>(
            async _ => {
                await Task.Yield();

                throw new InvalidOperationException("boom");
            },
            _ => error,
            TestContext.Current.CancellationToken);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the async Try does not catch an exception of a different type.")]
    public async Task AwaitingTheAsyncTryDoesNotCatchAnExceptionOfADifferentType() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Outcome.Try<int, FormatException>(
                async _ => {
                    await Task.Yield();

                    throw new InvalidOperationException("boom");
                },
                _ => error,
                TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try lets a cancellation propagate rather than mapping it to a failure.")]
    public async Task AwaitingTheAsyncTryLetsACancellationPropagateRatherThanMappingItToAFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Exercise & verify
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Outcome.Try<int, Exception>(
                ct => throw new OperationCanceledException(ct),
                _ => error,
                cts.Token));
    }

    [Fact(DisplayName = "Awaiting the async Try passes the cancellation token to the operation.")]
    public async Task AwaitingTheAsyncTryPassesTheCancellationTokenToTheOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        using CancellationTokenSource cts      = new();
        CancellationToken             received = default;

        // Exercise
        await Outcome.Try<int, Exception>(
            ct => {
                received = ct;

                return Task.FromResult(0);
            },
            _ => error,
            cts.Token);

        // Verify
        Check.That(received).IsEqualTo(cts.Token);
    }

    [Fact(DisplayName = "Awaiting the async Try surfaces a null task returned by the operation.")]
    public async Task AwaitingTheAsyncTrySurfacesANullTaskReturnedByTheOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Outcome.Try<int, FormatException>((Func<CancellationToken, Task<int>>)(_ => null!), _ => error, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try surfaces a null task even when TException is broad, rather than mapping it.")]
    public async Task AwaitingTheAsyncTrySurfacesANullTaskEvenWhenTExceptionIsBroad() {
        // A null task is a contract violation, not an anticipated failure: the guard sits outside the mapping region,
        // so it surfaces as an InvalidOperationException even under a broad Exception catch, mirroring the null-result
        // contract. A specific TException surfaces it too (see the test above).
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Outcome.Try<int, Exception>(
                (Func<CancellationToken, Task<int>>)(_ => null!), _ => error, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try surfaces a null operation result as a contract violation rather than mapping it, even when TException is broad.")]
    public async Task AwaitingTheAsyncTrySurfacesANullOperationResultRatherThanMappingIt() {
        // Distinct from the null-task case above: here the operation returns a non-null task that resolves to a null
        // value. Success rejects it after the catch, so the ArgumentNullException surfaces even though TException is
        // broad enough to have mapped it.
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<string, Exception>(_ => Task.FromResult<string>(null!), _ => error, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try guards against a null operation.")]
    public async Task AwaitingTheAsyncTryGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<int, InvalidOperationException>((Func<CancellationToken, Task<int>>)null!, _ => error, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try guards against a null onError mapper.")]
    public async Task AwaitingTheAsyncTryGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<int, InvalidOperationException>(_ => Task.FromResult(0), (Func<InvalidOperationException, Error>)null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Asynchronous side-effecting Try

    [Fact(DisplayName = "Awaiting the async Try (void) returns a success and runs the side effect when the operation does not throw.")]
    public async Task AwaitingTheAsyncTryVoidReturnsASuccessAndRunsTheSideEffectWhenTheOperationDoesNotThrow() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        bool        ran   = false;

        // Exercise
        Outcome outcome = await Outcome.Try<InvalidOperationException>(
            async _ => {
                await Task.Yield();

                ran = true;
            },
            _ => error,
            TestContext.Current.CancellationToken);

        // Verify
        Check.That(ran).IsTrue();
        Check.That(outcome.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Awaiting the async Try (void) maps a caught exception to a failure.")]
    public async Task AwaitingTheAsyncTryVoidMapsACaughtExceptionToAFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome outcome = await Outcome.Try<InvalidOperationException>(
            async _ => {
                await Task.Yield();

                throw new InvalidOperationException();
            },
            _ => error,
            TestContext.Current.CancellationToken);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the async Try (void) lets a cancellation propagate.")]
    public async Task AwaitingTheAsyncTryVoidLetsACancellationPropagate() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Exercise & verify
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Outcome.Try<Exception>(
                ct => throw new OperationCanceledException(ct),
                _ => error,
                cts.Token));
    }

    [Fact(DisplayName = "Awaiting the async Try (void) guards against a null operation.")]
    public async Task AwaitingTheAsyncTryVoidGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<InvalidOperationException>((Func<CancellationToken, Task>)null!, _ => error, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try (void) does not catch an exception of a different type.")]
    public async Task AwaitingTheAsyncTryVoidDoesNotCatchAnExceptionOfADifferentType() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Outcome.Try<FormatException>(
                async _ => {
                    await Task.Yield();

                    throw new InvalidOperationException("boom");
                },
                _ => error,
                TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Awaiting the async Try (void) guards against a null onError mapper.")]
    public async Task AwaitingTheAsyncTryVoidGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<InvalidOperationException>(
                (CancellationToken _) => Task.CompletedTask, (Func<InvalidOperationException, Error>)null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Asynchronous value-producing Try (no cancellation token)

    [Fact(DisplayName = "Awaiting the token-less async Try returns a success carrying the result when the operation does not throw.")]
    public async Task AwaitingTheTokenlessAsyncTryReturnsASuccessCarryingTheResult() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise: an async lambda with no token binds to the Func<Task<T>> overload.
        Outcome<int> outcome = await Outcome.Try<int, InvalidOperationException>(
            async () => {
                await Task.Yield();

                return 42;
            },
            _ => error);

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Awaiting the token-less async Try maps a throwing async lambda to a failure.")]
    public async Task AwaitingTheTokenlessAsyncTryMapsAThrowingAsyncLambda() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<int> outcome = await Outcome.Try<int, InvalidOperationException>(
            async () => {
                await Task.Yield();

                throw new InvalidOperationException("boom");
            },
            _ => error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the token-less async Try surfaces a null operation result rather than mapping it.")]
    public async Task AwaitingTheTokenlessAsyncTrySurfacesANullOperationResult() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<string, Exception>(() => Task.FromResult<string>(null!), _ => error));
    }

    [Fact(DisplayName = "Awaiting the token-less async Try guards against a null operation.")]
    public async Task AwaitingTheTokenlessAsyncTryGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<int, InvalidOperationException>((Func<Task<int>>)null!, _ => error));
    }

    [Fact(DisplayName = "Awaiting the token-less async Try guards against a null onError mapper.")]
    public async Task AwaitingTheTokenlessAsyncTryGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<int, InvalidOperationException>(() => Task.FromResult(0), (Func<InvalidOperationException, Error>)null!));
    }

    #endregion

    #region Asynchronous side-effecting Try (no cancellation token)

    [Fact(DisplayName = "Awaiting the token-less async Try (void) returns a success and runs the side effect when the operation does not throw.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidReturnsASuccessAndRunsTheSideEffect() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        bool        ran   = false;

        // Exercise
        Outcome outcome = await Outcome.Try<InvalidOperationException>(
            async () => {
                await Task.Yield();

                ran = true;
            },
            _ => error);

        // Verify
        Check.That(ran).IsTrue();
        Check.That(outcome.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Awaiting the token-less async Try (void) maps a throwing async lambda instead of letting it escape as async void.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidMapsAThrowingAsyncLambda() {
        // Regression guard for the async-void footgun: a parameterless async lambda now binds to the Func<Task>
        // overload, so its post-await exception is awaited and mapped rather than raised out-of-band.
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome outcome = await Outcome.Try<InvalidOperationException>(
            async () => {
                await Task.Yield();

                throw new InvalidOperationException("boom");
            },
            _ => error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the token-less async Try (void) maps a fire-and-forget task that faults instead of dropping it.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidMapsAFireAndForgetTask() {
        // Regression guard for the non-async fire-and-forget case: () => ReturnsTask() binds to the Func<Task> overload
        // and is awaited, so a faulting task is mapped rather than silently dropped as it would be on the Action overload.
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome outcome = await Outcome.Try<InvalidOperationException>(() => FaultingTaskAsync(), _ => error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the token-less async Try (void) does not catch an exception of a different type.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidDoesNotCatchADifferentType() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Outcome.Try<FormatException>(
                async () => {
                    await Task.Yield();

                    throw new InvalidOperationException("boom");
                },
                _ => error));
    }

    [Fact(DisplayName = "Awaiting the token-less async Try (void) guards against a null operation.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidGuardsAgainstANullOperation() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<InvalidOperationException>((Func<Task>)null!, _ => error));
    }

    [Fact(DisplayName = "Awaiting the token-less async Try (void) guards against a null onError mapper.")]
    public async Task AwaitingTheTokenlessAsyncTryVoidGuardsAgainstANullOnErrorMapper() {
        // Exercise & verify
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Outcome.Try<InvalidOperationException>(() => Task.CompletedTask, (Func<InvalidOperationException, Error>)null!));
    }

    private static Task FaultingTaskAsync() {
        return Task.FromException(new InvalidOperationException("boom"));
    }

    #endregion

}

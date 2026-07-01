#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Outcome<>))]
public sealed class OutcomeGenericAdditionalTests {

    [Fact(DisplayName = "Then discarding the value chains to a non-generic Outcome on success.")]
    public void ThenDiscardingTheValueChainsToANonGenericOutcomeOnSuccess() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(3);

        // Exercise
        Outcome result = outcome.Then(_ => Outcome.Success);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Then discarding the value propagates the error on failure.")]
    public void ThenDiscardingTheValuePropagatesTheErrorOnFailure() {
        // Setup
        DomainError  error   = new(ErrorCode.Unspecified, "boom");
        Outcome<int> outcome = Outcome<int>.Failure(error);
        bool         called  = false;

        // Exercise
        Outcome result = outcome.Then(_ => {
            called = true;

            return Outcome.Success;
        });

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting the async instance To maps the value on success.")]
    public async Task AwaitingTheAsyncInstanceToMapsTheValueOnSuccess() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(7);

        // Exercise
        Outcome<string> result = await outcome.To(async (value, ct) => {
            await System.Threading.Tasks.Task.CompletedTask;

            return value.ToString();
        }, TestContext.Current.CancellationToken);

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo("7");
    }

    [Fact(DisplayName = "Awaiting the async instance Recover value overload recovers a failure.")]
    public async Task AwaitingTheAsyncInstanceRecoverValueOverloadRecoversAFailure() {
        // Setup
        DomainError  error   = new(ErrorCode.Unspecified, "boom");
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Exercise
        Outcome<int> result = await outcome.Recover(async (failure, ct) => {
            await System.Threading.Tasks.Task.CompletedTask;

            return 42;
        }, TestContext.Current.CancellationToken);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Then throws an ArgumentNullException when the continuation is null.")]
    public void ThenThrowsAnArgumentNullExceptionWhenTheContinuationIsNull() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<int, Outcome>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "To throws an ArgumentNullException when the converter is null.")]
    public void ToThrowsAnArgumentNullExceptionWhenTheConverterIsNull() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.To((Func<int, string>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover throws an ArgumentNullException when the fallback is null.")]
    public void RecoverThrowsAnArgumentNullExceptionWhenTheFallbackIsNull() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, Outcome<int>>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally throws an ArgumentNullException when the success handler is null.")]
    public void FinallyThrowsAnArgumentNullExceptionWhenTheSuccessHandlerIsNull() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(1);

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<int, string>)null!, _ => "ko"))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover with a value-returning fallback that returns null throws an ArgumentNullException.")]
    public void RecoverWithAValueReturningFallbackThatReturnsNullThrowsAnArgumentNullException() {
        // Setup
        DomainError     error   = new(ErrorCode.Unspecified, "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover(_ => (string)null!))
             .Throws<ArgumentNullException>();
    }

}

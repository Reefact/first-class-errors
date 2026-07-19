#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Outcome<>))]
public sealed class OutcomeTests {

    [Fact(DisplayName = "A successful outcome is marked as success.")]
    public void SuccessfulOutcomeIsMarkedAsSuccess() {
        // Exercise
        Outcome<string> outcome = Outcome<string>.Success(Dummies.Any.String().NonEmpty().Generate());

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.IsFailure).IsFalse();
        Check.That(outcome.Error).IsNull();
    }

    [Fact(DisplayName = "A successful outcome exposes its value.")]
    public void SuccessfulOutcomeExposesItsValue() {
        // Exercise
        Outcome<string> outcome = Outcome<string>.Success("ok");

        // Verify
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("ok");
    }

    [Fact(DisplayName = "A successful outcome can be escalated to a value.")]
    public void ASuccessfulOutcomeCanBeEscalatedToAValue() {
        // Exercise
        Outcome<string> outcome = Outcome<string>.Success("ok");

        // Verify
        Check.That(outcome.GetResultOrThrow()).IsEqualTo("ok");
    }

    [Fact(DisplayName = "A successful outcome cannot be created from a null value.")]
    public void SuccessfulOutcomeCannotBeCreatedFromANullValue() {
        // Exercise & verify
        Check.ThatCode(() => Outcome<string>.Success(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A failed outcome is marked as failure.")]
    public void FailedOutcomeIsMarkedAsFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Verify
        Check.That(outcome.IsSuccess).IsFalse();
        Check.That(outcome.IsFailure).IsTrue();
    }

    [Fact(DisplayName = "A failed outcome exposes its error.")]
    public void AFailedOutcomeExposesItsError() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Verify
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "A failed outcome cannot be created from a null error.")]
    public void AFailedOutcomeCannotBeCreatedFromANullError() {
        // Exercise & verify
        Check.ThatCode(() => Outcome<string>.Failure(null!))
             .ThrowsAny();
    }

    [Fact(DisplayName = "Accessing the value of a failed outcome throws the associated exception.")]
    public void AccessingTheValueOfAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        DomainError     error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => _ = outcome.GetResultOrThrow())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Escalating a failed outcome throws the associated exception.")]
    public void EscalatingAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        DomainError     error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.GetResultOrThrow())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "A failed outcome preserves the original error instance.")]
    public void FailedOutcomePreservesTheOriginalErrorInstance() {
        // Setup
        DomainError     error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        DomainException thrownException = Check.ThatCode(() => outcome.GetResultOrThrow()).Throws<DomainException>().Value;
        Check.That(thrownException.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "ThrowIfFailure throws the associated exception when the outcome is a failure.")]
    public void ThrowIfFailureThrowsTheAssociatedExceptionWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError     error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.ThrowIfFailure())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "ThrowIfFailure does nothing when the outcome is a success.")]
    public void ThrowIfFailureDoesNothingWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome<string> outcome = Outcome<string>.Success(Dummies.Any.String().NonEmpty().Generate());

        // Exercise & verify
        Check.ThatCode(() => outcome.ThrowIfFailure()).DoesNotThrow();
    }

    [Fact(DisplayName = "Then chains the next step when the outcome is a success.")]
    public void ThenChainsTheNextStepWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(2);

        // Exercise
        Outcome<int> result = outcome.Then(value => Outcome<int>.Success(value * 10));

        // Verify
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.GetResultOrThrow()).IsEqualTo(20);
    }

    [Fact(DisplayName = "Then short-circuits and propagates the error when the outcome is a failure.")]
    public void ThenShortCircuitsAndPropagatesTheErrorWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int> outcome = Outcome<int>.Failure(error);
        bool         called  = false;

        // Exercise
        Outcome<int> result = outcome.Then(value => {
            called = true;

            return Outcome<int>.Success(value);
        });

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Then maps the value when the outcome is a success.")]
    public void ToMapsTheValueWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(3);

        // Exercise
        Outcome<string> result = outcome.Then(value => $"v={value}");

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo("v=3");
    }

    [Fact(DisplayName = "Then (value mapping) propagates the error without invoking the converter on a failure.")]
    public void ToPropagatesTheErrorWithoutInvokingTheConverterOnAFailure() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Exercise
        Outcome<string> result = outcome.Then(value => value.ToString());

        // Verify
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Recover replaces a failure with a guaranteed fallback value.")]
    public void RecoverReplacesAFailureWithAGuaranteedFallbackValue() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Exercise
        Outcome<int> result = outcome.Recover(_ => 42);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.GetResultOrThrow()).IsEqualTo(42);
    }

    [Fact(DisplayName = "Recover leaves a successful outcome unchanged.")]
    public void RecoverLeavesASuccessfulOutcomeUnchanged() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(7);

        // Exercise
        Outcome<int> result = outcome.Recover(_ => 42);

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(7);
    }

    [Fact(DisplayName = "Finally resolves the success branch when the outcome is a success.")]
    public void FinallyResolvesTheSuccessBranchWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome<int> outcome = Outcome<int>.Success(5);

        // Exercise
        string result = outcome.Finally(value => $"ok:{value}", _ => "ko");

        // Verify
        Check.That(result).IsEqualTo("ok:5");
    }

    [Fact(DisplayName = "Finally resolves the failure branch when the outcome is a failure.")]
    public void FinallyResolvesTheFailureBranchWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError  error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome<int> outcome = Outcome<int>.Failure(error);

        // Exercise
        string result = outcome.Finally(value => $"ok:{value}", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "The non-generic Outcome.Then chains when successful and propagates on failure.")]
    public void NonGenericOutcomeThenChainsWhenSuccessfulAndPropagatesOnFailure() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome chainedFromSuccess = Outcome.Success.Then(() => Outcome.Success);
        Outcome chainedFromFailure = Outcome.Failure(error).Then(() => Outcome.Success);

        // Verify
        Check.That(chainedFromSuccess.IsSuccess).IsTrue();
        Check.That(chainedFromFailure.IsFailure).IsTrue();
        Check.That(chainedFromFailure.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Awaiting Then over a Task<Outcome<T>> chains the next step.")]
    public async Task AwaitingThenOverATaskOutcomeChainsTheNextStep() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(4));

        // Exercise
        Outcome<int> result = await task.Then(value => Outcome<int>.Success(value + 1));

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo(5);
    }

    [Fact(DisplayName = "Awaiting Then (value mapping) over a Task<Outcome<T>> maps the value.")]
    public async Task AwaitingToOverATaskOutcomeMapsTheValue() {
        // Setup
        Task<Outcome<int>> task = Task.FromResult(Outcome<int>.Success(6));

        // Exercise
        Outcome<string> result = await task.Then(value => $"n={value}");

        // Verify
        Check.That(result.GetResultOrThrow()).IsEqualTo("n=6");
    }

}

#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Outcome))]
public sealed class OutcomeNonGenericTests {

    [Fact(DisplayName = "Outcome.Success is a successful outcome.")]
    public void OutcomeSuccessIsASuccessfulOutcome() {
        // Exercise
        Outcome outcome = Outcome.Success;

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.IsFailure).IsFalse();
        Check.That(outcome.Error).IsNull();
    }

    [Fact(DisplayName = "Outcome.Failure is a failed outcome exposing its error.")]
    public void OutcomeFailureIsAFailedOutcomeExposingItsError() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        Outcome outcome = Outcome.Failure(error);

        // Verify
        Check.That(outcome.IsFailure).IsTrue();
        Check.That(outcome.IsSuccess).IsFalse();
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Outcome.Failure cannot be created from a null error.")]
    public void OutcomeFailureCannotBeCreatedFromANullError() {
        // Exercise & verify
        Check.ThatCode(() => Outcome.Failure(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ThrowIfFailure does nothing when the outcome is a success.")]
    public void ThrowIfFailureDoesNothingWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.ThrowIfFailure()).DoesNotThrow();
    }

    [Fact(DisplayName = "ThrowIfFailure throws the associated exception when the outcome is a failure.")]
    public void ThrowIfFailureThrowsTheAssociatedExceptionWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome     outcome = Outcome.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.ThrowIfFailure())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Then runs the next step when the outcome is a success.")]
    public void ThenRunsTheNextStepWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise
        Outcome result = outcome.Then(() => Outcome.Success);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Then short-circuits and propagates the error when the outcome is a failure.")]
    public void ThenShortCircuitsAndPropagatesTheErrorWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome     outcome = Outcome.Failure(error);
        bool        called  = false;

        // Exercise
        Outcome result = outcome.Then(() => {
            called = true;

            return Outcome.Success;
        });

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Then to a typed outcome propagates the error on a failure.")]
    public void ThenToATypedOutcomePropagatesTheErrorOnAFailure() {
        // Setup
        DomainError error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome     outcome = Outcome.Failure(error);

        // Exercise
        Outcome<int> result = outcome.Then(() => Outcome<int>.Success(1));

        // Verify
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Recover leaves a successful outcome unchanged.")]
    public void RecoverLeavesASuccessfulOutcomeUnchanged() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise
        Outcome result = outcome.Recover(_ => Outcome.Failure(ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any())));

        // Verify
        Check.That(result).IsSameReferenceAs(outcome);
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Recover replaces a failure with the fallback result.")]
    public void RecoverReplacesAFailureWithTheFallbackResult() {
        // Setup
        DomainError error    = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome     outcome  = Outcome.Failure(error);
        Outcome     fallback = Outcome.Success;

        // Exercise
        Outcome result = outcome.Recover(_ => fallback);

        // Verify
        Check.That(result).IsSameReferenceAs(fallback);
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Finally resolves the success branch when the outcome is a success.")]
    public void FinallyResolvesTheSuccessBranchWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise
        string result = outcome.Finally(() => "ok", _ => "ko");

        // Verify
        Check.That(result).IsEqualTo("ok");
    }

    [Fact(DisplayName = "Finally resolves the failure branch when the outcome is a failure.")]
    public void FinallyResolvesTheFailureBranchWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), "boom");
        Outcome     outcome = Outcome.Failure(error);

        // Exercise
        string result = outcome.Finally(() => "ok", failure => $"ko:{failure.DiagnosticMessage}");

        // Verify
        Check.That(result).IsEqualTo("ko:boom");
    }

    [Fact(DisplayName = "Finally with actions executes the success branch when the outcome is a success.")]
    public void FinallyWithActionsExecutesTheSuccessBranchWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome outcome         = Outcome.Success;
        bool    successExecuted = false;
        bool    failureExecuted = false;

        // Exercise
        outcome.Finally(() => successExecuted = true, _ => failureExecuted = true);

        // Verify
        Check.That(successExecuted).IsTrue();
        Check.That(failureExecuted).IsFalse();
    }

    [Fact(DisplayName = "Finally with actions executes the failure branch when the outcome is a failure.")]
    public void FinallyWithActionsExecutesTheFailureBranchWhenTheOutcomeIsAFailure() {
        // Setup
        DomainError error           = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome     outcome         = Outcome.Failure(error);
        bool        successExecuted = false;
        Error?      capturedError   = null;

        // Exercise
        outcome.Finally(() => successExecuted = true, failure => capturedError = failure);

        // Verify
        Check.That(successExecuted).IsFalse();
        Check.That(capturedError).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "Then guards against a null next function.")]
    public void ThenGuardsAgainstANullNextFunction() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Then((Func<Outcome>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Recover guards against a null fallback function.")]
    public void RecoverGuardsAgainstANullFallbackFunction() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Recover((Func<Error, Outcome>)null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Finally guards against a null onSuccess function.")]
    public void FinallyGuardsAgainstANullOnSuccessFunction() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise & verify
        Check.ThatCode(() => outcome.Finally((Func<string>)null!, _ => "ko"))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Awaiting the asynchronous Then chains the next step when the outcome is a success.")]
    public async Task AwaitingTheAsynchronousThenChainsTheNextStepWhenTheOutcomeIsASuccess() {
        // Setup
        Outcome outcome = Outcome.Success;

        // Exercise
        Outcome result = await outcome.Then((_) => Task.FromResult(Outcome.Success), TestContext.Current.CancellationToken);

        // Verify
        Check.That(result.IsSuccess).IsTrue();
    }

    [Fact(DisplayName = "Awaiting the asynchronous Then short-circuits and propagates the error on a failure.")]
    public async Task AwaitingTheAsynchronousThenShortCircuitsAndPropagatesTheErrorOnAFailure() {
        // Setup
        DomainError error   = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());
        Outcome     outcome = Outcome.Failure(error);
        bool        called  = false;

        // Exercise
        Outcome result = await outcome.Then((_) => {
            called = true;

            return Task.FromResult(Outcome.Success);
        }, TestContext.Current.CancellationToken);

        // Verify
        Check.That(called).IsFalse();
        Check.That(result.IsFailure).IsTrue();
        Check.That(result.Error).IsSameReferenceAs(error);
    }

}

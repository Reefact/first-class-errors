#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(TryOutcome<>))]
public sealed class TryOutcomeTests {

    [Fact(DisplayName = "A successful outcome is marked as success.")]
    public void SuccessfulOutcomeIsMarkedAsSuccess() {
        // Exercise
        TryOutcome<string> outcome = TryOutcome<string>.Success("ok");

        // Verify
        Check.That(outcome.IsSuccess).IsTrue();
        Check.That(outcome.IsFailure).IsFalse();
        Check.That(outcome.Exception).IsNull();
    }

    [Fact(DisplayName = "A successful outcome exposes its value.")]
    public void SuccessfulOutcomeExposesItsValue() {
        // Exercise
        TryOutcome<string> outcome = TryOutcome<string>.Success("ok");

        // Verify
        Check.That(outcome.Value).IsEqualTo("ok");
    }

    [Fact(DisplayName = "A successful outcome can be escalated to a value.")]
    public void ASuccessfulOutcomeCanBeEscalatedToAValue() {
        // Exercise
        TryOutcome<string> outcome = TryOutcome<string>.Success("ok");

        // Verify
        Check.That(outcome.GetOrThrow()).IsEqualTo("ok");
    }

    [Fact(DisplayName = "A successful outcome cannot be created from a null value.")]
    public void SuccessfulOutcomeCannotBeCreatedFromANullValue() {
        // Exercise & verify
        Check.ThatCode(() => TryOutcome<string>.Success(null!))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A failed outcome is marked as failure.")]
    public void FailedOutcomeIsMarkedAsFailure() {
        // Setup
        Exception exception = new InvalidOperationException("boom");

        // Exercise
        TryOutcome<string> outcome = TryOutcome<string>.Failure(exception);

        // Verify
        Check.That(outcome.IsSuccess).IsFalse();
        Check.That(outcome.IsFailure).IsTrue();
    }

    [Fact(DisplayName = "A failed outcome exposes its exception.")]
    public void AFailedOutcomeExposesItsException() {
        // Setup
        Exception exception = new InvalidOperationException("boom");

        // Exercise
        TryOutcome<string> outcome = TryOutcome<string>.Failure(exception);

        // Verify
        Check.That(outcome.Exception).IsSameReferenceAs(exception);
    }

    [Fact(DisplayName = "A failed outcome cannot be created from a null exception.")]
    public void AFailedOutcomeCannotBeCreatedFromANullException() {
        // Exercise & verify
        Check.ThatCode(() => TryOutcome<string>.Failure(null!))
             .ThrowsAny();
    }

    [Fact(DisplayName = "Accessing the value of a failed outcome throws the associated exception.")]
    public void AccessingTheValueOfAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        Exception          exception = new InvalidOperationException("boom");
        TryOutcome<string> outcome   = TryOutcome<string>.Failure(exception);

        // Exercise & verify
        Check.ThatCode(() => _ = outcome.Value)
             .Throws<InvalidOperationException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Escalating a failed outcome throws the associated exception.")]
    public void EscalatingAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        Exception          exception = new InvalidOperationException("boom");
        TryOutcome<string> outcome   = TryOutcome<string>.Failure(exception);

        // Exercise & verify
        Check.ThatCode(() => outcome.GetOrThrow())
             .Throws<InvalidOperationException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "A failed outcome preserves the original exception instance.")]
    public void FailedOutcomePreservesTheOriginalExceptionInstance() {
        // Setup
        Exception          exception = new InvalidOperationException("boom");
        TryOutcome<string> outcome   = TryOutcome<string>.Failure(exception);

        // Exercise & verify
        Exception thrownException = Check.ThatCode(() => outcome.Value).Throws<Exception>().Value;
        Check.That(thrownException).IsSameReferenceAs(exception);
    }

}
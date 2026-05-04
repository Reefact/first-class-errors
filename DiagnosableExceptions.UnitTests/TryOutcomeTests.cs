#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace DiagnosableExceptions.UnitTests;

[TestSubject(typeof(Outcome<>))]
public sealed class TryOutcomeTests {

    [Fact(DisplayName = "A successful outcome is marked as success.")]
    public void SuccessfulOutcomeIsMarkedAsSuccess() {
        // Exercise
        Outcome<string> outcome = Outcome<string>.Success("ok");

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
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Verify
        Check.That(outcome.IsSuccess).IsFalse();
        Check.That(outcome.IsFailure).IsTrue();
    }

    [Fact(DisplayName = "A failed outcome exposes its exception.")]
    public void AFailedOutcomeExposesItsException() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Verify
        Check.That(outcome.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "A failed outcome cannot be created from a null exception.")]
    public void AFailedOutcomeCannotBeCreatedFromANullException() {
        // Exercise & verify
        Check.ThatCode(() => Outcome<string>.Failure(null!))
             .ThrowsAny();
    }

    [Fact(DisplayName = "Accessing the value of a failed outcome throws the associated exception.")]
    public void AccessingTheValueOfAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        DomainError     error   = new(ErrorCode.Unspecified, "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => _ = outcome.GetResultOrThrow())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "Escalating a failed outcome throws the associated exception.")]
    public void EscalatingAFailedOutcomeThrowsTheAssociatedException() {
        // Setup
        DomainError     error   = new(ErrorCode.Unspecified, "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        Check.ThatCode(() => outcome.GetResultOrThrow())
             .Throws<DomainException>()
             .WithMessage("boom");
    }

    [Fact(DisplayName = "A failed outcome preserves the original exception instance.")]
    public void FailedOutcomePreservesTheOriginalExceptionInstance() {
        // Setup
        DomainError     error   = new(ErrorCode.Unspecified, "boom");
        Outcome<string> outcome = Outcome<string>.Failure(error);

        // Exercise & verify
        DomainException thrownException = Check.ThatCode(() => outcome.GetResultOrThrow()).Throws<DomainException>().Value;
        Check.That(thrownException.Error).IsSameReferenceAs(error);
    }

}
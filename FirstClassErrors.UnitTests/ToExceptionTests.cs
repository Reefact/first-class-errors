#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(DiagnosableException))]
public sealed class ToExceptionTests {

    [Fact(DisplayName = "Converting a domain error produces a domain exception.")]
    public void ConvertingADomainErrorProducesADomainException() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<DomainException>();
    }

    [Fact(DisplayName = "Converting an infrastructure error produces an infrastructure exception.")]
    public void ConvertingAnInfrastructureErrorProducesAnInfrastructureException() {
        // Setup
        InfrastructureError error = new(ErrorCode.Unspecified, "boom", InteractionDirection.Incoming, Transience.NonTransient);

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<InfrastructureException>();
    }

    [Fact(DisplayName = "Converting a primary port error produces a primary port exception.")]
    public void ConvertingAPrimaryPortErrorProducesAPrimaryPortException() {
        // Setup
        PrimaryPortError error = new(ErrorCode.Unspecified, "boom", Transience.NonTransient);

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<PrimaryPortException>();
    }

    [Fact(DisplayName = "Converting a secondary port error produces a secondary port exception.")]
    public void ConvertingASecondaryPortErrorProducesASecondaryPortException() {
        // Setup
        SecondaryPortError error = new(ErrorCode.Unspecified, "boom", Transience.NonTransient);

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<SecondaryPortException>();
    }

    [Fact(DisplayName = "The converted exception carries the same error reference.")]
    public void TheConvertedExceptionCarriesTheSameErrorReference() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The converted exception message equals the error detailed message.")]
    public void TheConvertedExceptionMessageEqualsTheErrorDetailedMessage() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.Message).IsEqualTo(error.DetailedMessage);
    }

    [Fact(DisplayName = "The converted exception has no inner exception even when the error has inner errors.")]
    public void TheConvertedExceptionHasNoInnerExceptionEvenWhenTheErrorHasInnerErrors() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "root", new DomainError(ErrorCode.Unspecified, "inner"));

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.InnerException).IsNull();
        Check.That(exception.Error.InnerErrors).CountIs(1);
    }

    [Fact(DisplayName = "Throwing and catching a converted error preserves the error reference.")]
    public void ThrowingAndCatchingAConvertedErrorPreservesTheErrorReference() {
        // Setup
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Exercise
        Error? caughtError = null;
        try {
            throw error.ToException();
        } catch (DiagnosableException ex) {
            caughtError = ex.Error;
        }

        // Verify
        Check.That(caughtError).IsSameReferenceAs(error);
    }

}

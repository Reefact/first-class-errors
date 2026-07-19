#region Usings declarations

using FirstClassErrors.Testing;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(DiagnosableException))]
public sealed class ToExceptionTests {

    [Fact(DisplayName = "Converting a domain error produces a domain exception.")]
    public void ConvertingADomainErrorProducesADomainException() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<DomainException>();
    }

    [Fact(DisplayName = "Converting an infrastructure error produces an infrastructure exception.")]
    public void ConvertingAnInfrastructureErrorProducesAnInfrastructureException() {
        // Setup
        InfrastructureError error = ErrorFactory.Infrastructure(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), InteractionDirectionFactory.Any(), TransienceFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<InfrastructureException>();
    }

    [Fact(DisplayName = "Converting a primary port error produces a primary port exception.")]
    public void ConvertingAPrimaryPortErrorProducesAPrimaryPortException() {
        // Setup
        PrimaryPortError error = ErrorFactory.Primary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<PrimaryPortException>();
    }

    [Fact(DisplayName = "Converting a secondary port error produces a secondary port exception.")]
    public void ConvertingASecondaryPortErrorProducesASecondaryPortException() {
        // Setup
        SecondaryPortError error = ErrorFactory.Secondary(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), TransienceFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception).IsInstanceOf<SecondaryPortException>();
    }

    [Fact(DisplayName = "The converted exception carries the same error reference.")]
    public void TheConvertedExceptionCarriesTheSameErrorReference() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.Error).IsSameReferenceAs(error);
    }

    [Fact(DisplayName = "The converted exception message equals the error diagnostic message.")]
    public void TheConvertedExceptionMessageEqualsTheErrorDiagnosticMessage() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.Message).IsEqualTo(error.DiagnosticMessage);
    }

    [Fact(DisplayName = "The converted exception has no inner exception even when the error has inner errors.")]
    public void TheConvertedExceptionHasNoInnerExceptionEvenWhenTheErrorHasInnerErrors() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any(), ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any()));

        // Exercise
        DiagnosableException exception = error.ToException();

        // Verify
        Check.That(exception.InnerException).IsNull();
        Check.That(exception.Error.InnerErrors).CountIs(1);
    }

    [Fact(DisplayName = "Throwing and catching a converted error preserves the error reference.")]
    public void ThrowingAndCatchingAConvertedErrorPreservesTheErrorReference() {
        // Setup
        DomainError error = ErrorFactory.Domain(ErrorCodeFactory.Any(), DiagnosticMessageFactory.Any());

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

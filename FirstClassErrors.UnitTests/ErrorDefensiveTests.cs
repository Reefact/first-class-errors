#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Error))]
public sealed class ErrorDefensiveTests {

    private const string UnknownDetailedMessageFragment = "Exception created without an error message";

    [Fact(DisplayName = "A null error code is replaced by the unspecified error code.")]
    public void ANullErrorCodeIsReplacedByTheUnspecifiedErrorCode() {
        // Exercise
        DomainError error = new(null!, "m");

        // Verify
        Check.That(error.Code).IsSameReferenceAs(ErrorCode.Unspecified);
    }

    [Fact(DisplayName = "A null detailed message falls back to the default message.")]
    public void ANullDetailedMessageFallsBackToTheDefaultMessage() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, null!);

        // Verify
        Check.That(error.DetailedMessage).IsNotNull();
        Check.That(error.DetailedMessage).Contains(UnknownDetailedMessageFragment);
    }

    [Fact(DisplayName = "A whitespace-only detailed message falls back to the default message.")]
    public void AWhitespaceOnlyDetailedMessageFallsBackToTheDefaultMessage() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, "   ");

        // Verify
        Check.That(error.DetailedMessage).IsNotNull();
        Check.That(error.DetailedMessage).Contains(UnknownDetailedMessageFragment);
    }

    [Fact(DisplayName = "A detailed message is trimmed.")]
    public void ADetailedMessageIsTrimmed() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, "  hello  ");

        // Verify
        Check.That(error.DetailedMessage).IsEqualTo("hello");
    }

    [Fact(DisplayName = "A short message is stored verbatim and is not trimmed.")]
    public void AShortMessageIsStoredVerbatimAndIsNotTrimmed() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, "m", "  s  ");

        // Verify
        Check.That(error.ShortMessage).IsEqualTo("  s  ");
    }

    [Fact(DisplayName = "A configure-context delegate that throws is captured into the context.")]
    public void AConfigureContextDelegateThatThrowsIsCapturedIntoTheContext() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, "m", configureContext: _ => throw new InvalidOperationException("boom"));

        // Verify
        Check.That(error.Context.IsEmpty).IsFalse();
        Check.That(error.Context.Values.ContainsKey(ErrorContextKey.CannotBuildErrorContext)).IsTrue();

        object? captured = error.Context.Values[ErrorContextKey.CannotBuildErrorContext];
        Check.That(captured).IsInstanceOf<InvalidOperationException>();
        Check.That(((Exception)captured!).Message).IsEqualTo("boom");
    }

    [Fact(DisplayName = "Inner errors are stored as a defensive copy of the provided collection.")]
    public void InnerErrorsAreStoredAsADefensiveCopyOfTheProvidedCollection() {
        // Setup
        List<DomainError> innerErrors = new() {
            new DomainError(ErrorCode.Unspecified, "first")
        };
        DomainError error = new(ErrorCode.Unspecified, "m", innerErrors);

        // Exercise
        innerErrors.Add(new DomainError(ErrorCode.Unspecified, "second"));

        // Verify
        Check.That(error.InnerErrors).CountIs(1);
    }

    [Fact(DisplayName = "The string representation combines the detailed message and the code.")]
    public void TheStringRepresentationCombinesTheDetailedMessageAndTheCode() {
        // Exercise
        DomainError error = new(ErrorCode.Unspecified, "boom");

        // Verify
        Check.That(error.ToString()).IsEqualTo("boom (#UNSPECIFIED)");
    }

}

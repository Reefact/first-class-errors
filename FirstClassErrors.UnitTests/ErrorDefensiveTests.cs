#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(Error))]
public sealed class ErrorDefensiveTests {

    [Fact(DisplayName = "A null error code is replaced by the unspecified error code.")]
    public void ANullErrorCodeIsReplacedByTheUnspecifiedErrorCode() {
        // Exercise
        DomainError error = DomainError.Create(null!, "diagnostic").WithPublicMessage("short");

        // Verify
        Check.That(error.Code).IsSameReferenceAs(ErrorCode.Unspecified);
    }

    [Fact(DisplayName = "A null diagnostic message is rejected.")]
    public void ANullDiagnosticMessageIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => DomainError.Create(ErrorCode.Unspecified, null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An empty or whitespace diagnostic message is rejected.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnEmptyOrWhitespaceDiagnosticMessageIsRejected(string value) {
        // Exercise & verify
        Check.ThatCode(() => DomainError.Create(ErrorCode.Unspecified, value))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The diagnostic message is trimmed.")]
    public void TheDiagnosticMessageIsTrimmed() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "  hello  ").WithPublicMessage("short");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo("hello");
    }

    [Fact(DisplayName = "A null short message is rejected.")]
    public void ANullShortMessageIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage(null!))
             .Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "An empty or whitespace short message is rejected.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnEmptyOrWhitespaceShortMessageIsRejected(string value) {
        // Exercise & verify
        Check.ThatCode(() => DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage(value))
             .Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The short message is trimmed.")]
    public void TheShortMessageIsTrimmed() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage("  short  ");

        // Verify
        Check.That(error.ShortMessage).IsEqualTo("short");
    }

    [Fact(DisplayName = "A null detailed message leaves the detailed message null.")]
    public void ANullDetailedMessageLeavesTheDetailedMessageNull() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage("short", null);

        // Verify
        Check.That(error.DetailedMessage).IsNull();
    }

    [Theory(DisplayName = "An empty or whitespace detailed message leaves the detailed message null.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnEmptyOrWhitespaceDetailedMessageLeavesTheDetailedMessageNull(string value) {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage("short", value);

        // Verify
        Check.That(error.DetailedMessage).IsNull();
    }

    [Fact(DisplayName = "A detailed message is trimmed when provided.")]
    public void ADetailedMessageIsTrimmedWhenProvided() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage("short", "  detailed  ");

        // Verify
        Check.That(error.DetailedMessage).IsEqualTo("detailed");
    }

    [Fact(DisplayName = "A configure-context delegate that throws is captured into the context.")]
    public void AConfigureContextDelegateThatThrowsIsCapturedIntoTheContext() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic", _ => throw new InvalidOperationException("boom"))
                                       .WithPublicMessage("short");

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
            ErrorFactory.Domain(ErrorCode.Unspecified, "first")
        };
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic", innerErrors).WithPublicMessage("short");

        // Exercise
        innerErrors.Add(ErrorFactory.Domain(ErrorCode.Unspecified, "second"));

        // Verify
        Check.That(error.InnerErrors).CountIs(1);
    }

    [Fact(DisplayName = "Null entries in the provided inner errors collection are filtered out.")]
    public void NullEntriesInTheProvidedInnerErrorsCollectionAreFilteredOut() {
        // Setup
        List<DomainError> innerErrors = new() {
            ErrorFactory.Domain(ErrorCode.Unspecified, "first"),
            null!,
            ErrorFactory.Domain(ErrorCode.Unspecified, "second")
        };

        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic", innerErrors).WithPublicMessage("short");

        // Verify
        Check.That(error.InnerErrors).CountIs(2);
        Check.That(error.InnerErrors.Select(innerError => innerError.DiagnosticMessage)).ContainsExactly("first", "second");
    }

    [Fact(DisplayName = "A collection made only of null inner errors yields an empty inner errors list.")]
    public void ACollectionMadeOnlyOfNullInnerErrorsYieldsAnEmptyInnerErrorsList() {
        // Setup
        List<DomainError> innerErrors = new() { null!, null! };

        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic", innerErrors).WithPublicMessage("short");

        // Verify
        Check.That(error.InnerErrors).IsEmpty();
    }

    [Fact(DisplayName = "The string representation combines the diagnostic message and the code.")]
    public void TheStringRepresentationCombinesTheDiagnosticMessageAndTheCode() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "boom").WithPublicMessage("short");

        // Verify
        Check.That(error.ToString()).IsEqualTo("boom (#UNSPECIFIED)");
    }

}

#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[Collection("SmartEnumSideEffects")]
[TestSubject(typeof(Error))]
public sealed class ErrorDefensiveTests : IDisposable {

    #region Constructors & Destructor

    public ErrorDefensiveTests() {
        ErrorContextKey.ResetForTests();
    }

    #endregion

    [SuppressMessage("Usage", "CA1816", Justification = "IDisposable is used as an xUnit teardown hook. The class has no finalizer and does not own unmanaged resources.")]
    public void Dispose() {
        ErrorContextKey.ResetForTests();
    }

    [Fact(DisplayName = "A null error code is replaced by the unspecified error code.")]
    public void ANullErrorCodeIsReplacedByTheUnspecifiedErrorCode() {
        // Exercise
        DomainError error = DomainError.Create(null!, "diagnostic").WithPublicMessage("short");

        // Verify
        Check.That(error.Code).IsSameReferenceAs(ErrorCode.Unspecified);
    }

    [Fact(DisplayName = "A null diagnostic message is replaced by a fallback sentinel.")]
    public void ANullDiagnosticMessageIsReplacedByAFallbackSentinel() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, null!).WithPublicMessage("short");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo(Error.MissingDiagnosticMessage);
    }

    [Theory(DisplayName = "An empty or whitespace diagnostic message is replaced by a fallback sentinel.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnEmptyOrWhitespaceDiagnosticMessageIsReplacedByAFallbackSentinel(string value) {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, value).WithPublicMessage("short");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo(Error.MissingDiagnosticMessage);
    }

    [Fact(DisplayName = "The diagnostic message is trimmed.")]
    public void TheDiagnosticMessageIsTrimmed() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "  hello  ").WithPublicMessage("short");

        // Verify
        Check.That(error.DiagnosticMessage).IsEqualTo("hello");
    }

    [Fact(DisplayName = "A null short message is replaced by a fallback sentinel.")]
    public void ANullShortMessageIsReplacedByAFallbackSentinel() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage(null!);

        // Verify
        Check.That(error.ShortMessage).IsEqualTo(Error.MissingShortMessage);
    }

    [Theory(DisplayName = "An empty or whitespace short message is replaced by a fallback sentinel.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    public void AnEmptyOrWhitespaceShortMessageIsReplacedByAFallbackSentinel(string value) {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage(value);

        // Verify
        Check.That(error.ShortMessage).IsEqualTo(Error.MissingShortMessage);
    }

    [Fact(DisplayName = "A missing diagnostic message is recorded in the context under #MISSING_REQUIRED_MESSAGE.")]
    public void AMissingDiagnosticMessageIsRecordedInTheContext() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, null!).WithPublicMessage("short");

        // Verify
        Check.That(error.Context.Values.ContainsKey(ErrorContextKey.MissingRequiredMessages)).IsTrue();

        IReadOnlyList<string> missing = (IReadOnlyList<string>)error.Context.Values[ErrorContextKey.MissingRequiredMessages]!;
        Check.That(missing).ContainsExactly("diagnosticMessage");
    }

    [Fact(DisplayName = "Missing diagnostic and short messages are both recorded in the context.")]
    public void MissingDiagnosticAndShortMessagesAreBothRecordedInTheContext() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, null!).WithPublicMessage(null!);

        // Verify
        IReadOnlyList<string> missing = (IReadOnlyList<string>)error.Context.Values[ErrorContextKey.MissingRequiredMessages]!;
        Check.That(missing).ContainsExactly("diagnosticMessage", "shortMessage");
    }

    [Fact(DisplayName = "Present mandatory messages leave no #MISSING_REQUIRED_MESSAGE entry in the context.")]
    public void PresentMandatoryMessagesLeaveNoMissingEntryInTheContext() {
        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic").WithPublicMessage("short");

        // Verify
        Check.That(error.Context.Values.ContainsKey(ErrorContextKey.MissingRequiredMessages)).IsFalse();
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

    [Fact(DisplayName = "A configure-context delegate that throws preserves the entries added before the failure.")]
    public void AConfigureContextDelegateThatThrowsPreservesTheEntriesAddedBeforeTheFailure() {
        // Setup
        ErrorContextKey<string> beforeKey = ErrorContextKey.Create<string>("BeforeFailure");

        // Exercise
        DomainError error = DomainError.Create(ErrorCode.Unspecified, "diagnostic", builder => {
                                           builder.Add(beforeKey, "kept");

                                           throw new InvalidOperationException("boom");
                                       })
                                       .WithPublicMessage("short");

        // Verify — the entry added before the failure survives alongside the captured exception.
        bool found = error.Context.TryGet(beforeKey, out string? kept);
        Check.That(found).IsTrue();
        Check.That(kept).IsEqualTo("kept");
        Check.That(error.Context.Values.ContainsKey(ErrorContextKey.CannotBuildErrorContext)).IsTrue();
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

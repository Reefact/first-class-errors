#region Usings declarations

using FirstClassErrors.GenDoc;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

[TestSubject(typeof(ErrorDocumentationExtractionFailure))]
public sealed class ErrorDocumentationExtractionFailureTests {

    [Fact(DisplayName = "A failure preserves the values it is built from.")]
    public void AFailurePreservesTheValuesItIsBuiltFrom() {
        // Exercise
        ErrorDocumentationExtractionFailure failure = new("Some.Type", "Factory", "It broke.", "detail");

        // Verify
        Check.That(failure.TypeName).IsEqualTo("Some.Type");
        Check.That(failure.MemberName).IsEqualTo("Factory");
        Check.That(failure.Message).IsEqualTo("It broke.");
        Check.That(failure.ExceptionDetail).IsEqualTo("detail");
    }

    [Fact(DisplayName = "A failure cannot be created without a type name.")]
    public void AFailureCannotBeCreatedWithoutATypeName() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDocumentationExtractionFailure(null!, "Factory", "It broke.", null))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A failure cannot be created without a message.")]
    public void AFailureCannotBeCreatedWithoutAMessage() {
        // Exercise & verify
        Check.ThatCode(() => new ErrorDocumentationExtractionFailure("Some.Type", "Factory", null!, null))
             .Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A type-scoped failure without exception detail reads as 'type: message'.")]
    public void ATypeScopedFailureReadsAsTypeColonMessage() {
        // Setup
        ErrorDocumentationExtractionFailure failure = new("Some.Type", null, "It broke.", null);

        // Exercise & verify
        Check.That(failure.ToString()).IsEqualTo("Some.Type: It broke.");
    }

    [Fact(DisplayName = "A member-scoped failure without exception detail reads as 'type.member: message'.")]
    public void AMemberScopedFailureReadsAsTypeDotMemberColonMessage() {
        // Setup
        ErrorDocumentationExtractionFailure failure = new("Some.Type", "Factory", "It broke.", null);

        // Exercise & verify
        Check.That(failure.ToString()).IsEqualTo("Some.Type.Factory: It broke.");
    }

    [Fact(DisplayName = "A failure carrying exception detail appends it in parentheses.")]
    public void AFailureCarryingExceptionDetailAppendsItInParentheses() {
        // Setup
        ErrorDocumentationExtractionFailure failure = new("Some.Type", "Factory", "It broke.", "boom");

        // Exercise & verify
        Check.That(failure.ToString()).IsEqualTo("Some.Type.Factory: It broke. (boom)");
    }

}

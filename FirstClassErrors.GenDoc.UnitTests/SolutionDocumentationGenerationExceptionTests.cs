#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(SolutionDocumentationGenerationException))]
public sealed class SolutionDocumentationGenerationExceptionTests {

    [Fact(DisplayName = "The exception preserves its message.")]
    public void TheExceptionPreservesItsMessage() {
        // Exercise
        SolutionDocumentationGenerationException exception = new("Generation failed.");

        // Verify
        Check.That(exception.Message).IsEqualTo("Generation failed.");
        Check.That(exception.InnerException).IsNull();
    }

    [Fact(DisplayName = "The exception preserves its message and inner exception.")]
    public void TheExceptionPreservesItsMessageAndInnerException() {
        // Setup
        InvalidOperationException inner = new("root cause");

        // Exercise
        SolutionDocumentationGenerationException exception = new("Generation failed.", inner);

        // Verify
        Check.That(exception.Message).IsEqualTo("Generation failed.");
        Check.That(exception.InnerException).IsSameReferenceAs(inner);
    }

}

#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

[TestSubject(typeof(SolutionDocumentationGenerationException))]
public sealed class SolutionDocumentationGenerationExceptionTests {

    [Fact(DisplayName = "The exception carries the error and surfaces its diagnostic message.")]
    public void TheExceptionCarriesTheErrorAndSurfacesItsDiagnosticMessage() {
        // Setup
        Error error = DocumentationRequestError.SolutionNotFound("/src/app/Application.sln");

        // Exercise
        SolutionDocumentationGenerationException exception = new(error);

        // Verify: the exception is diagnosable — the full structured error is reachable, and the log-facing
        // message is the error's diagnostic message.
        Check.That(exception.Error).IsSameReferenceAs(error);
        Check.That(exception.Message).IsEqualTo(error.DiagnosticMessage);
        Check.That(exception.InnerException).IsNull();
    }

    [Fact(DisplayName = "The exception preserves the runtime cause as its inner exception.")]
    public void TheExceptionPreservesTheRuntimeCauseAsItsInnerException() {
        // Setup
        Error                     error = DocumentationToolchainError.WorkerRunFailed("/src/app/bin/Application.dll");
        InvalidOperationException inner = new("root cause");

        // Exercise
        SolutionDocumentationGenerationException exception = new(error, inner);

        // Verify
        Check.That(exception.Error).IsSameReferenceAs(error);
        Check.That(exception.Message).IsEqualTo(error.DiagnosticMessage);
        Check.That(exception.InnerException).IsSameReferenceAs(inner);
    }

}

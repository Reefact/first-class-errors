#region Usings declarations

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.UnitTests;

/// <summary>
///     Proves, from an assembly that has no access to the library's <c>internal</c> members, that the built-in error
///     types can be specialized by a consumer (ADR-0072) and that GenDoc documents such specialized errors like any other.
/// </summary>
/// <remarks>
///     This project is deliberately the vantage point: the library only exposes its internals to
///     <c>FirstClassErrors.UnitTests</c> and <c>FirstClassErrors.Testing</c>, so a fixture that compiles here is a fixture a
///     real consumer can write.
/// </remarks>
public sealed class CustomErrorTypesTests {

    #region Statics members declarations

    private static ErrorDocumentationExtractionResult Extract() {
        return AssemblyErrorDocumentationReader.GetErrorDocumentationFrom(typeof(CustomErrorTypesTests).Assembly);
    }

    #endregion

    [Fact(DisplayName = "A consumer assembly can derive a typed error from a built-in error and build it through its own staged factory.")]
    public void AConsumerAssemblyCanDeriveATypedError() {
        // Exercise
        CustomPersistenceError error = CustomRepositoryErrors.Unavailable();

        // Verify
        Check.That(error.Code).IsEqualTo(ErrorCode.Create("CUSTOM_REPOSITORY_UNAVAILABLE"));
        Check.That(error.ShortMessage).IsEqualTo("Service temporarily unavailable.");
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Outgoing);
        Check.That(error.Transience).IsEqualTo(Transience.Transient);
        Check.That(error.ToException()).IsInstanceOf<SecondaryPortException>();
    }

    [Fact(DisplayName = "GenDoc extracts a documented factory that returns a custom error type, running its examples.")]
    public void GenDocExtractsADocumentedFactoryReturningACustomErrorType() {
        // Exercise
        ErrorDocumentationExtractionResult extraction = Extract();

        // Verify
        ErrorDocumentation? documentation = extraction.Documentation.FirstOrDefault(doc => doc.Code == "CUSTOM_REPOSITORY_UNAVAILABLE");

        Check.That(documentation).IsNotNull();
        Check.That(documentation!.Title).IsEqualTo("Custom repository unavailable");
        Check.That(documentation.Source).IsEqualTo("CustomRepository");
        Check.That(documentation.Examples).CountIs(1);
    }

    [Fact(DisplayName = "Extracting a custom error type records no failure.")]
    public void ExtractingACustomErrorTypeRecordsNoFailure() {
        // Exercise
        ErrorDocumentationExtractionResult extraction = Extract();

        // Verify
        Check.That(extraction.Failures).IsEmpty();
    }

    private sealed class CustomPersistenceError : SecondaryPortError {

        private CustomPersistenceError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, Transience transience)
            : base(code, diagnosticMessage, shortMessage, detailedMessage, transience) { }

        public static PublicMessageStage<CustomPersistenceError> Create(ErrorCode code, string diagnosticMessage, Transience transience) {
            return new PublicMessageStage<CustomPersistenceError>((shortMessage, detailedMessage) =>
                                                                      new CustomPersistenceError(code, diagnosticMessage, shortMessage, detailedMessage, transience));
        }

    }

    [ProvidesErrorsFor("CustomRepository", Description = "Errors raised by the custom repository fixture.")]
    private static class CustomRepositoryErrors {

        [DocumentedBy(nameof(UnavailableDocumentation))]
        public static CustomPersistenceError Unavailable() {
            return CustomPersistenceError.Create(ErrorCode.Create("CUSTOM_REPOSITORY_UNAVAILABLE"),
                                                 "The custom repository is unavailable.",
                                                 Transience.Transient)
                                         .WithPublicMessage("Service temporarily unavailable.");
        }

        private static ErrorDocumentation UnavailableDocumentation() {
            return DescribeError.WithTitle("Custom repository unavailable")
                                .WithDescription("This error occurs when the custom repository cannot be reached.")
                                .WithoutRule()
                                .WithoutDiagnostic()
                                .WithExamples(Unavailable);
        }

    }

}

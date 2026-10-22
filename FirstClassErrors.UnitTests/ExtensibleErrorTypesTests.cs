#region Usings declarations

using FirstClassErrors.Testing;

using NFluent;

#endregion

namespace FirstClassErrors.UnitTests;

/// <summary>
///     Exercises custom error types as they would be defined in an application referencing FirstClassErrors.
/// </summary>
public sealed class ExtensibleErrorTypesTests {

    [Fact(DisplayName = "Derived domain errors retain their type and diagnostic fields.")]
    public void DerivedDomainErrorRetainsItsTypeAndDiagnosticFields() {
        // Exercise
        ErrorCode         code  = ErrorCodeFactory.Any();
        CustomDomainError error = CustomDomainError.Create(code, "Internal diagnostic")
                                                   .WithPublicMessage("Public summary", "Public details");

        // Verify
        Check.That(error.Code).IsEqualTo(code);
        Check.That(error.DiagnosticMessage).IsEqualTo("Internal diagnostic");
        Check.That(error.ShortMessage).IsEqualTo("Public summary");
        Check.That(error.DetailedMessage).IsEqualTo("Public details");
        Check.That(error.ToException() is DomainException).IsTrue();
    }

    [Fact(DisplayName = "Derived infrastructure errors preserve direction and transience.")]
    public void DerivedInfrastructureErrorPreservesMetadata() {
        // Exercise
        CustomInfrastructureError error = CustomInfrastructureError.Create(ErrorCodeFactory.Any(), "Diagnostic")
                                                                     .WithPublicMessage("Unavailable");

        // Verify
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Outgoing);
        Check.That(error.Transience).IsEqualTo(Transience.Transient);
        Check.That(error.ToException() is InfrastructureException).IsTrue();
    }

    [Fact(DisplayName = "Derived primary port errors aggregate nested errors without losing direction.")]
    public void DerivedPrimaryPortErrorAggregatesInnerErrors() {
        // Setup
        DomainError inner = ErrorFactory.Domain(ErrorCodeFactory.Any(), "Invalid input");
        PrimaryPortInnerErrors innerErrors = new PrimaryPortInnerErrors().Add(inner);

        // Exercise
        CustomPrimaryPortError error = CustomPrimaryPortError.Create(ErrorCodeFactory.Any(), "Diagnostic", innerErrors)
                                                               .WithPublicMessage("Invalid request");

        // Verify
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Incoming);
        Check.That(error.Transience).IsEqualTo(Transience.Unknown);
        Check.That(error.InnerErrors).CountIs(1);
        Check.That(error.InnerErrors[0]).IsSameReferenceAs(inner);
        Check.That(error.ToException() is PrimaryPortException).IsTrue();
    }

    [Fact(DisplayName = "A repository factory can produce a derived secondary port error.")]
    public void RepositoryFactoryProducesTypedPersistenceError() {
        // Exercise
        PersistenceError error = CommandRepositoryErrors.Unavailable();

        // Verify
        Check.That(error.Code).IsEqualTo(ErrorCode.Create("COMMAND_REPOSITORY_UNAVAILABLE"));
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Outgoing);
        Check.That(error.Transience).IsEqualTo(Transience.Transient);
        Check.That(error.ToException() is SecondaryPortException).IsTrue();
    }

    [Fact(DisplayName = "Derived secondary port errors preserve nested outgoing errors and computed transience.")]
    public void DerivedSecondaryPortErrorAggregatesInnerErrors() {
        // Setup
        SecondaryPortError inner = ErrorFactory.Secondary(ErrorCodeFactory.Any(), "Dependency failed", Transience.NonTransient);
        SecondaryPortInnerErrors innerErrors = new SecondaryPortInnerErrors().Add(inner);

        // Exercise
        PersistenceError error = PersistenceError.Create(ErrorCodeFactory.Any(), "Repository failed", innerErrors)
                                                 .WithPublicMessage("Repository unavailable");

        // Verify
        Check.That(error.Direction).IsEqualTo(InteractionDirection.Outgoing);
        Check.That(error.Transience).IsEqualTo(Transience.NonTransient);
        Check.That(error.InnerErrors).CountIs(1);
        Check.That(error.InnerErrors[0]).IsSameReferenceAs(inner);
    }

    private sealed class CustomDomainError : DomainError {

        private CustomDomainError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage)
            : base(code, diagnosticMessage, shortMessage, detailedMessage) { }

        public new static PublicMessageStage<CustomDomainError> Create(ErrorCode code, string diagnosticMessage) {
            return new PublicMessageStage<CustomDomainError>((shortMessage, detailedMessage) =>
                                                                new CustomDomainError(code, diagnosticMessage, shortMessage, detailedMessage));
        }

    }

    private sealed class CustomInfrastructureError : InfrastructureError {

        private CustomInfrastructureError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage)
            : base(code, diagnosticMessage, shortMessage, detailedMessage, InteractionDirection.Outgoing, Transience.Transient) { }

        public new static PublicMessageStage<CustomInfrastructureError> Create(ErrorCode code, string diagnosticMessage) {
            return new PublicMessageStage<CustomInfrastructureError>((shortMessage, detailedMessage) =>
                                                                        new CustomInfrastructureError(code, diagnosticMessage, shortMessage, detailedMessage));
        }

    }

    private sealed class CustomPrimaryPortError : PrimaryPortError {

        private CustomPrimaryPortError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, PrimaryPortInnerErrors innerErrors)
            : base(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors) { }

        public new static PublicMessageStage<CustomPrimaryPortError> Create(ErrorCode code, string diagnosticMessage, PrimaryPortInnerErrors innerErrors) {
            return new PublicMessageStage<CustomPrimaryPortError>((shortMessage, detailedMessage) =>
                                                                     new CustomPrimaryPortError(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors));
        }

    }

    private sealed class PersistenceError : SecondaryPortError {

        private PersistenceError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, Transience transience)
            : base(code, diagnosticMessage, shortMessage, detailedMessage, transience) { }

        private PersistenceError(ErrorCode code, string diagnosticMessage, string shortMessage, string? detailedMessage, SecondaryPortInnerErrors innerErrors)
            : base(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors) { }

        public new static PublicMessageStage<PersistenceError> Create(ErrorCode code, string diagnosticMessage, Transience transience) {
            return new PublicMessageStage<PersistenceError>((shortMessage, detailedMessage) =>
                                                               new PersistenceError(code, diagnosticMessage, shortMessage, detailedMessage, transience));
        }

        public new static PublicMessageStage<PersistenceError> Create(ErrorCode code, string diagnosticMessage, SecondaryPortInnerErrors innerErrors) {
            return new PublicMessageStage<PersistenceError>((shortMessage, detailedMessage) =>
                                                               new PersistenceError(code, diagnosticMessage, shortMessage, detailedMessage, innerErrors));
        }

    }

    private static class CommandRepositoryErrors {

        public static PersistenceError Unavailable() {
            return PersistenceError.Create(ErrorCode.Create("COMMAND_REPOSITORY_UNAVAILABLE"),
                                           "The command repository is unavailable.",
                                           Transience.Transient)
                                   .WithPublicMessage("Service temporarily unavailable.");
        }

    }

}
